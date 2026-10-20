using System.Reflection;
using DewDrop.Core.Models;
using DewDrop.Core.Services;
using StardewModdingAPI;
using StardewValley;

namespace DewDrop.Framework
{
    /// <summary>Owns the installed mod list, the active profile and what it takes to apply them.</summary>
    internal class ModManager
    {
        private readonly IModHelper _helper;
        private readonly IManifest _self;
        private readonly ILog _log;

        public DewDropPaths Paths { get; }
        public SettingsStore Settings { get; }
        public ProfileStore Profiles { get; }
        public ModScanner Scanner { get; }
        public ModLinker Linker { get; }
        public ModInstaller Installer { get; }
        public PendingUpdates Pending { get; }
        public UpdateChecker Updates { get; }

        public List<InstalledMod> Installed { get; private set; } = new();
        public Profile ActiveProfile { get; private set; } = null!;

        /// <summary>Unique IDs SMAPI actually loaded this session.</summary>
        public HashSet<string> LoadedIds { get; }

        /// <summary>The folder SMAPI loaded mods from this session.</summary>
        public string RunningModsPath { get; }

        /// <summary>Whether SMAPI was started on the linked profile folder (rather than e.g. the raw Mods folder).</summary>
        public bool RunningFromProfileFolder { get; }

        /// <summary>
        /// Profile: SMAPI loaded the linked profile folder. Plain: it loaded the game's Mods folder (or Stardrop's mods
        /// folder), as a Steam or shortcut launch does. Custom: some other folder, or SMAPI didn't say.
        /// </summary>
        public SessionKind Kind { get; }

        /// <summary>What DewDrop keeps between sessions about restarts and mods it has seen.</summary>
        public StartupState Startup { get; }

        /// <summary>The restart that started this session, until its title screen is reached (then it's cleared).</summary>
        public RestartRecord? ArrivedRestart { get; private set; }

        /// <summary>A restart that never came back in its profile: this plain session was started some other way after it.</summary>
        public RestartRecord? FailedRestart { get; }

        /// <summary>What SMAPI loaded, with the folder each came from when SMAPI says.</summary>
        private readonly List<LoadedMod> _loadedMods;

        public bool UpdateCheckRunning { get; private set; }
        public DateTime? LastUpdateCheck { get; private set; }

        public string SelfId => _self.UniqueID;

        public ModManager(IModHelper helper, IManifest self, ILog log)
        {
            _helper = helper;
            _self = self;
            _log = log;

            Paths = new DewDropPaths();
            Paths.EnsureFolders();
            Settings = new SettingsStore(Paths, log);
            Settings.Load(Constants.GamePath);
            Profiles = new ProfileStore(Paths, log);
            Scanner = new ModScanner(log);
            Linker = new ModLinker(log);
            Pending = new PendingUpdates(Paths, log);
            Installer = new ModInstaller(log, Pending);
            Updates = new UpdateChecker(self.Version.ToString(), log);

            _loadedNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _loadedMods = new List<LoadedMod>();
            foreach (var loaded in helper.ModRegistry.GetAll())
            {
                var name = string.IsNullOrWhiteSpace(loaded.Manifest.Name) ? loaded.Manifest.UniqueID : loaded.Manifest.Name;
                _loadedNames[loaded.Manifest.UniqueID] = name;
                _loadedMods.Add(new LoadedMod(loaded.Manifest.UniqueID, name, LoadedFolder(loaded)));
            }
            LoadedIds = new HashSet<string>(_loadedNames.Keys, StringComparer.OrdinalIgnoreCase);
            var smapiModsPath = GetSmapiModsPath();
            var gameModsPath = Path.Combine(Constants.GamePath, "Mods");
            RunningModsPath = smapiModsPath ?? gameModsPath;
            RunningFromProfileFolder = DewDropPaths.IsSameOrUnder(RunningModsPath, Paths.SelectedModsFolder);

            // The game's Mods folder and Stardrop's mods folder can be one folder under two names (moved to another drive
            // with a junction, or Steam's two library paths on Linux), so those compare with links resolved
            var sameAsModsFolder = smapiModsPath is not null
                && (DewDropPaths.IsSame(smapiModsPath, ModsFolder) || DewDropPaths.IsSame(DewDropPaths.Canonical(smapiModsPath), DewDropPaths.Canonical(ModsFolder)));
            Kind = RunningFromProfileFolder
                ? SessionKind.Profile
                : smapiModsPath is not null && (DewDropPaths.IsSame(smapiModsPath, gameModsPath) || sameAsModsFolder)
                    ? SessionKind.Plain
                    : SessionKind.Custom;

            // Then what SMAPI loaded is renamed into the scan's names, or every mod would look like another copy
            _diffRunningPath = RunningModsPath;
            if (Kind == SessionKind.Plain && sameAsModsFolder && !DewDropPaths.IsSame(RunningModsPath, ModsFolder))
            {
                for (var i = 0; i < _loadedMods.Count; i++)
                {
                    if (_loadedMods[i].Folder is { } folder && DewDropPaths.IsSameOrUnder(folder, RunningModsPath))
                    {
                        _loadedMods[i] = _loadedMods[i] with { Folder = Path.Combine(ModsFolder, Path.GetRelativePath(RunningModsPath, folder)) };
                    }
                }
                _diffRunningPath = ModsFolder;
            }

            Startup = new StartupState(Paths.StartupStatePath, log);
            Startup.Load();

            // Updates left over from last session can go in now for any mod this session didn't load
            Pending.ApplyAll();
            Reload();

            FailedRestart = TakeRestartRecord();
            SeedSeenMods();
            NewMods = Installed.Where(m => m.SourceId is null && !Startup.HasSeen(m.UniqueId)).ToList();

            // Nothing is installing yet, so staging copies no pending update needs are leftovers (a crash mid-install,
            // an old copy that was still open when its update went in)
            Pending.CleanUpLeftovers(Installed
                .Where(m => m.SourceId is null)
                .Select(m => m.Folder.Parent?.FullName ?? "")
                .Append(InstallFolder));

            // Mods the profile enabled that SMAPI still skipped (broken, wrong game version...) shouldn't keep
            // nagging for a restart that would not change anything
            if (RunningFromProfileFolder)
            {
                _failedAtLaunch = new HashSet<string>(Installed.Where(IsEnabled).Select(m => m.UniqueId).Where(id => !LoadedIds.Contains(id)), StringComparer.OrdinalIgnoreCase);
            }
            else if (Kind == SessionKind.Plain)
            {
                // The same for a plain launch: an enabled mod whose only copy SMAPI saw in its folder and still skipped.
                // Taken once, so a mod copied in or installed later counts as not loaded rather than rejected. Mods new in
                // the folder count as on, since they're about to be turned on.
                var turningOn = new HashSet<InstalledMod>(NewMods);
                _failedAtLaunch = new HashSet<string>(
                    SessionDiff.Compute(Installed, m => IsEnabled(m) || turningOn.Contains(m), ActiveProfile.SourceId, _loadedMods, _diffRunningPath, new HashSet<string>(new[] { SelfId }, StringComparer.OrdinalIgnoreCase)).Rejected,
                    StringComparer.OrdinalIgnoreCase);
                _rejectedAtLaunch = _failedAtLaunch;
                _difference = null;
            }

            _changesAtLaunch = _changes;
        }

        /// <summary>Mods in the Mods folder DewDrop had never seen when the game started: copied in by hand since.</summary>
        public IReadOnlyList<InstalledMod> NewMods { get; private set; }

        /// <summary>
        /// Turns on the mods copied in by hand since DewDrop last ran ("Turn on mods when installed" covers them too), so
        /// they still load when the game starts in the profile. Returns the ones turned on.
        /// </summary>
        public List<InstalledMod> TurnOnNewMods()
        {
            var turnedOn = TurnOnUnseen(NewMods);
            NewMods = Array.Empty<InstalledMod>();
            return turnedOn;
        }

        /// <summary>
        /// Turns on the loose mods among these that DewDrop has never seen (one moved or re-extracted is still the same
        /// mod, and keeps its choice), then marks them all seen. Returns the ones turned on.
        /// </summary>
        public List<InstalledMod> TurnOnUnseen(IEnumerable<InstalledMod> candidates)
        {
            var list = candidates.ToList();
            var turnedOn = new List<InstalledMod>();
            if (CanEditActiveProfile)
            {
                foreach (var mod in list.Where(m => m.SourceId is null && !Startup.HasSeen(m.UniqueId) && !IsEnabled(m)))
                {
                    SetEnabled(mod, true);
                    turnedOn.Add(mod);
                }
            }

            MarkSeen(list.Select(m => m.UniqueId));
            return turnedOn;
        }

        private int _changesAtLaunch;

        /// <summary>Whether the links in Selected Mods may not be what the active profile wants now.</summary>
        // In a profile launch, an enabled mod that didn't load may simply have no link (made by Stardrop, or a link failed)
        public bool LinksMayBeStale => Kind != SessionKind.Profile || _changes != _changesAtLaunch || NeedsRestart() || _failedAtLaunch.Count > 0;

        /// <summary>Set once a restart is on its way, so nothing else is scheduled for when this game closes.</summary>
        public bool RestartScheduled { get; private set; }

        /// <summary>
        /// Links the active profile's mods into Selected Mods. Returns false, with the reason for the player in
        /// <paramref name="error"/>, when some couldn't be linked.
        /// </summary>
        public bool RebuildLinks(out string? error)
        {
            // DewDrop's own folder is always linked, so the Mods button comes back whatever the profile lists (a
            // collection profile never lists it) and wherever DewDrop is installed
            var links = Linker.Rebuild(Paths.SelectedModsFolder, ActiveProfile, Installed, Settings.CollectionsSkipSmapiUpdateCheck, _helper.DirectoryPath);
            if (links.Succeeded)
            {
                error = null;
                _changesAtLaunch = _changes;
                return true;
            }

            var names = string.Join(", ", links.Failed.Take(3)) + (links.Failed.Count > 3 ? $" and {links.Failed.Count - 3} more" : "");
            error = $"DewDrop couldn't link {names}, so {(links.Failed.Count == 1 ? "it" : "they")} wouldn't load. The SMAPI log says why.";
            return false;
        }

        /// <summary>
        /// Reads the restart left in startup.json. The game that restart started marks it as arrived (cleared at its title
        /// screen). Any other launch means the restart never came back in its profile; a plain or custom launch gets the
        /// record back, so the "Load your profile?" box can say why it's asking.
        /// </summary>
        private RestartRecord? TakeRestartRecord()
        {
            var record = Startup.Data.Restart;
            if (record is null)
            {
                return null;
            }

            if (Kind == SessionKind.Profile)
            {
                if (record.Stage == RestartRecord.StageRestarting && record.Id == Environment.GetEnvironmentVariable(Relauncher.LaunchIdVariable))
                {
                    record.Stage = RestartRecord.StageArrived;
                    ArrivedRestart = record;
                }
                else
                {
                    _log.Warn($"Last time DewDrop restarted the game into the '{record.Profile}' profile, it didn't come back from that restart.");
                    Startup.Data.Restart = null;
                }
                Startup.Save();
                return null;
            }

            _log.Warn(record.Stage == RestartRecord.StageArrived
                ? $"The game closed while the '{record.Profile}' profile was loading last time."
                : $"Last time DewDrop restarted the game into the '{record.Profile}' profile, it didn't come back in that profile.");
            Startup.Data.Restart = null;
            Startup.Save();
            return record;
        }

        /// <summary>
        /// Called on the restarted game's first title screen: the restart worked, so the record goes. Returns the record
        /// (null if this session wasn't a restart).
        /// </summary>
        public RestartRecord? ConfirmArrived()
        {
            var record = ArrivedRestart;
            if (record is null)
            {
                return null;
            }

            ArrivedRestart = null;
            Startup.Data.Restart = null;
            Startup.Save();
            return record;
        }

        /// <summary>
        /// The seen-mods list starts with everything installed (and loaded) the first time; after that every mod switched
        /// on in any profile counts too, so only mods copied in by hand are ever new.
        /// </summary>
        private void SeedSeenMods()
        {
            var changed = !Startup.Existed;
            if (!Startup.Existed)
            {
                Startup.MarkSeen(Installed.Select(m => m.UniqueId));
                Startup.MarkSeen(LoadedIds);
            }

            changed |= Startup.MarkSeen(Profiles.Profiles.SelectMany(p => p.EnabledModIds ?? new()).Where(r => r is not null).Select(r => r.UniqueId));
            if (changed)
            {
                Startup.Save();
            }
        }

        /// <summary>Remembers mods the player has made a choice about (or installed through DewDrop), so they're never "new".</summary>
        public void MarkSeen(IEnumerable<string> uniqueIds)
        {
            if (Startup.MarkSeen(uniqueIds))
            {
                Startup.Save();
            }
        }

        /// <summary>The folder SMAPI loaded a mod from, read from its internal mod metadata; null if it won't say.</summary>
        private static string? LoadedFolder(IModInfo mod)
        {
            try
            {
                return mod.GetType().GetProperty("DirectoryPath", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(mod) as string;
            }
            catch
            {
                return null;
            }
        }

        private readonly HashSet<string> _failedAtLaunch = new(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> _pendingFolders = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Names of the mods SMAPI loaded this session, by unique ID.</summary>
        private readonly Dictionary<string, string> _loadedNames;

        /// <summary>
        /// The last update check's answers by unique ID. A rescan makes new mod objects, so they're applied again
        /// to each new list (only to mods still at the version that was checked).
        /// </summary>
        private IReadOnlyDictionary<string, UpdateCheckResult> _updateResults = new Dictionary<string, UpdateCheckResult>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Mods installed or updated this session; an update swapped in live still runs its old code.</summary>
        private readonly HashSet<string> _changedOnDisk = new(StringComparer.OrdinalIgnoreCase);

        private Dictionary<string, List<InstalledMod>> _byId = new(StringComparer.OrdinalIgnoreCase);

        // IsEnabled and NeedsRestart run for every installed mod several times a frame, so the active profile's
        // references are indexed once and indexed again only when the profile, its list or anything here changes
        private int _changes;
        private (Profile? Profile, List<ModReference>? List, int Count, int Changes) _indexedFor;
        private HashSet<ModReference> _enabledIndex = new();
        private bool? _needsRestart;

        /// <summary>Updates downloaded this session that finish installing on restart.</summary>
        public int PendingUpdateCount => _pendingFolders.Count;

        public bool HasPendingUpdate(InstalledMod mod) => _pendingFolders.Contains(mod.Folder.FullName.TrimEnd(Path.DirectorySeparatorChar));

        /// <summary>An update was found and isn't already downloaded and waiting for a restart.</summary>
        public bool HasNewUpdate(InstalledMod mod) => mod.Update is not null && !HasPendingUpdate(mod);

        public string ModsFolder => Settings.ModFolderPath ?? Path.Combine(Constants.GamePath, "Mods");
        public string InstallFolder => Settings.ModInstallPath ?? ModsFolder;

        /// <summary>
        /// Whether the active profile's file exists but couldn't be read. What's in memory is then the copy from
        /// before (or an empty stand-in), which ProfileStore won't save and which mustn't be applied either. Kept
        /// up to date when the profile changes, since list rows ask every frame.
        /// </summary>
        public bool ActiveProfileUnreadable { get; private set; }

        /// <summary>Whether mods can be switched on and off in the active profile.</summary>
        public bool CanEditActiveProfile => !ActiveProfile.IsReadOnly && !ActiveProfileUnreadable;

        /// <summary>Whether Apply &amp; Restart may run; <see cref="ApplyBlockedReason"/> says why not.</summary>
        public bool CanApply => !Settings.IsReadOnly && !ActiveProfileUnreadable;

        /// <summary>A problem with Stardrop's settings or the active profile the menu should warn about, or null.</summary>
        public string? Problem
        {
            get
            {
                if (CanApply)
                {
                    return null;
                }

                return Settings.IsReadOnly
                    ? "Stardrop's Settings.json couldn't be read, so your profile and Nexus login didn't load and changes to them aren't saved. Close the game and start it again."
                    : $"The '{ActiveProfile.Name}' profile file couldn't be read, so DewDrop won't change or apply it. Pick another profile, or close the game and start it again.";
            }
        }

        /// <summary>Why Apply &amp; Restart can't run now, for the player, or null when it can.</summary>
        public string? ApplyBlockedReason
        {
            get
            {
                if (CanApply)
                {
                    return null;
                }

                // The profile name comes from Settings.json, so this session would link Default instead of the player's profile
                return Settings.IsReadOnly
                    ? "Stardrop's Settings.json couldn't be read, so DewDrop doesn't know which profile is yours. Close the game and start it again."
                    : $"The '{ActiveProfile.Name}' profile file couldn't be read, so it can't be applied. Pick another profile, or close the game and start it again.";
            }
        }

        public void Reload()
        {
            Installed = Scanner.Scan(ModsFolder, Settings.CollectionInstallPath(Paths), Settings.IgnoreHiddenFolders)
                .OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            // The same mod can be installed twice (loose and in a collection), so group before keying
            _byId = Installed
                .GroupBy(m => m.UniqueId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            // Keep update results for mods that weren't touched since the last check
            UpdateChecker.Apply(_updateResults, Installed);

            _pendingFolders = new HashSet<string>(Pending.Load().Select(s => Path.GetFullPath(s.Target).TrimEnd(Path.DirectorySeparatorChar)), StringComparer.OrdinalIgnoreCase);

            // Only used when Default has to be created (a first run without Stardrop): it starts with what SMAPI loaded,
            // so the first Apply & Restart doesn't turn every mod off
            Profiles.Load(Installed
                .Where(m => m.SourceId is null && LoadedIds.Contains(m.UniqueId))
                .Select(m => new ModReference(m.UniqueId)));

            // Stardrop never writes empty entries, but a hand-edited profile can hold "" or null, which would break
            // every lookup that follows (and turn DewDrop off for the session)
            foreach (var profile in Profiles.Profiles)
            {
                profile.EnabledModIds ??= new();
                profile.EnabledModIds.RemoveAll(r => r is null);
            }

            Profile? previous = ActiveProfile;
            var selected = Settings.LastSelectedProfileName;
            ActiveProfile = Profiles.Get(selected) ?? UnreadableStandIn(selected, previous) ?? Profiles.Get(ProfileStore.DefaultProfileName)!;
            ActiveProfileUnreadable = Profiles.IsUnreadable(ActiveProfile.Name);
            EnsureSelfEnabled();
            _changes++;
        }

        /// <summary>
        /// Scans the Mods folder again, for mods added or removed by hand while the game runs, and says which mod
        /// folders appeared and which names are gone.
        /// </summary>
        public (List<InstalledMod> Added, List<string> Removed) Rescan()
        {
            var before = Installed.ToDictionary(m => m.Folder.FullName, m => m, StringComparer.OrdinalIgnoreCase);
            Reload();
            var after = Installed.Select(m => m.Folder.FullName).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var added = Installed.Where(m => !before.ContainsKey(m.Folder.FullName)).ToList();
            var removed = before.Where(p => !after.Contains(p.Key)).Select(p => p.Value.Name).ToList();

            // New folders, and a different version put in place of an old one, aren't what SMAPI loaded or skipped at
            // launch: they take a restart, and one SMAPI turned down before may load now
            var replaced = Installed.Where(m => before.TryGetValue(m.Folder.FullName, out var old)
                && (!string.Equals(old.Version, m.Version, StringComparison.OrdinalIgnoreCase) || !old.UniqueId.Equals(m.UniqueId, StringComparison.OrdinalIgnoreCase)));
            var changed = added.Concat(replaced).Select(m => m.UniqueId).ToList();
            if (changed.Count > 0)
            {
                MarkChanged(changed);
            }

            return (added, removed);
        }

        /// <summary>
        /// For a selected profile whose file couldn't be read: the copy from before this reload, else an empty stand-in
        /// with its name. Falling back to Default instead would quietly apply Default on the next restart.
        /// </summary>
        private Profile? UnreadableStandIn(string? name, Profile? previous)
        {
            if (name is null || !Profiles.IsUnreadable(name))
            {
                return null;
            }

            return previous is not null && previous.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ? previous : new Profile(name);
        }

        /// <summary>DewDrop has to stay enabled in every profile, or it would vanish from the title screen after a restart.</summary>
        private void EnsureSelfEnabled()
        {
            foreach (var profile in Profiles.Profiles.Where(p => !p.IsReadOnly && !Profiles.IsUnreadable(p.Name)))
            {
                if (!profile.EnabledModIds.Any(r => r.UniqueId.Equals(SelfId, StringComparison.OrdinalIgnoreCase)))
                {
                    profile.EnabledModIds.Add(new ModReference(SelfId));
                    Profiles.Save(profile);
                }
            }
        }

        public bool IsLocked(InstalledMod mod) => mod.UniqueId.Equals(SelfId, StringComparison.OrdinalIgnoreCase);

        public bool IsEnabled(InstalledMod mod) => IsLocked(mod) || EnabledIndex().Contains(new ModReference(mod.UniqueId, mod.SourceId));

        /// <summary>The active profile's references as a set (ModReference compares case-insensitively), built again when stale.</summary>
        private HashSet<ModReference> EnabledIndex()
        {
            var list = ActiveProfile.EnabledModIds;
            var stamp = (ActiveProfile, list, list?.Count ?? 0, _changes);
            if (stamp != _indexedFor)
            {
                _indexedFor = stamp;
                _enabledIndex = new HashSet<ModReference>(list?.Where(r => r is not null) ?? Enumerable.Empty<ModReference>());
                _needsRestart = null;
                _difference = null;
            }

            return _enabledIndex;
        }

        private SessionDifference? _difference;

        /// <summary>The folder SMAPI loaded from, under the name DewDrop's scan uses for it.</summary>
        private readonly string _diffRunningPath;

        /// <summary>For a plain launch: the mods SMAPI saw and skipped as the game started (null until that's known).</summary>
        private ISet<string>? _rejectedAtLaunch;

        /// <summary>
        /// How what SMAPI loaded differs from what a restart into the active profile would load. Meaningful for plain
        /// launches; a profile launch loaded the profile's links, and a custom one can't be compared.
        /// </summary>
        public SessionDifference Difference()
        {
            EnabledIndex();
            return _difference ??= SessionDiff.Compute(
                Installed,
                IsEnabled,
                ActiveProfile.SourceId,
                _loadedMods,
                _diffRunningPath,
                new HashSet<string>(new[] { SelfId }, StringComparer.OrdinalIgnoreCase),
                _rejectedAtLaunch);
        }

        public bool IsLoaded(InstalledMod mod) => LoadedIds.Contains(mod.UniqueId);

        /// <summary>Whether a mod loaded right now would be gone after a restart: no installed copy of it is on in the active profile.</summary>
        public bool WillTurnOff(InstalledMod mod) => IsLoaded(mod) && !IsIdEnabled(mod.UniqueId);

        /// <summary>Names of the mods loaded right now that a restart with the active profile leaves out.</summary>
        public List<string> LoadedModsTurnedOff()
        {
            // Mods SMAPI loaded from a folder the scan doesn't cover (say the game's Mods folder while Stardrop points
            // elsewhere) count too: nothing links them, so they're gone after the restart as well
            return _loadedNames
                .Where(p => !p.Key.Equals(SelfId, StringComparison.OrdinalIgnoreCase) && !IsIdEnabled(p.Key))
                .Select(p => p.Value)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private bool IsIdEnabled(string uniqueId) => _byId.TryGetValue(uniqueId, out var copies) && copies.Any(IsEnabled);

        /// <summary>Toggles a mod. Turning one on also turns on the installed mods it requires; returns their names.</summary>
        public List<string> SetEnabled(InstalledMod mod, bool enabled)
        {
            var alsoEnabled = new List<string>();
            if (IsLocked(mod) || !CanEditActiveProfile)
            {
                return alsoEnabled;
            }

            ActiveProfile.SetEnabled(mod, enabled);
            if (enabled)
            {
                EnableDependencies(mod, alsoEnabled, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            }

            // A mod the player has made a choice about is never "new in the Mods folder" (mods turned on with
            // it are in the profile now, which counts at the next start)
            MarkSeen(new[] { mod.UniqueId });

            _changes++;
            Profiles.Save(ActiveProfile);
            return alsoEnabled;
        }

        private void EnableDependencies(InstalledMod mod, List<string> enabledNames, HashSet<string> visited)
        {
            if (!visited.Add(mod.UniqueId))
            {
                return;
            }

            foreach (var id in mod.RequiredIds())
            {
                var dependency = FindPreferred(id, mod.SourceId);
                if (dependency is null)
                {
                    continue;
                }

                if (!ActiveProfile.IsEnabled(dependency))
                {
                    ActiveProfile.SetEnabled(dependency, true);
                    enabledNames.Add(dependency.Name);
                }
                EnableDependencies(dependency, enabledNames, visited);
            }
        }

        /// <summary>Required mods that are missing or switched off for an enabled mod.</summary>
        public List<string> MissingDependencies(InstalledMod mod)
        {
            var missing = new List<string>();
            foreach (var id in mod.RequiredIds())
            {
                // SMAPI's bundled framework, always present
                if (id.Equals("SMAPI", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var dependency = FindPreferred(id, mod.SourceId);
                if (dependency is null)
                {
                    missing.Add(id);
                }
                else if (!IsEnabled(dependency))
                {
                    missing.Add(dependency.Name);
                }
            }

            return missing;
        }

        private InstalledMod? FindPreferred(string uniqueId, string? sourceId)
        {
            if (!_byId.TryGetValue(uniqueId, out var matches))
            {
                return null;
            }

            return matches.FirstOrDefault(m => string.Equals(m.SourceId, sourceId, StringComparison.OrdinalIgnoreCase))
                ?? matches.FirstOrDefault(m => m.SourceId is null)
                ?? matches.FirstOrDefault();
        }

        public void SwitchProfile(Profile profile)
        {
            ActiveProfile = profile;
            ActiveProfileUnreadable = Profiles.IsUnreadable(profile.Name);
            Settings.LastSelectedProfileName = profile.Name;
            Settings.Save();
            _changes++;
        }

        /// <summary>Records mods whose files changed this session: they take a restart even though the same IDs stay loaded.</summary>
        public void MarkChanged(IEnumerable<string> ids)
        {
            foreach (var id in ids)
            {
                _changedOnDisk.Add(id);

                // A new version of a mod that failed to load is worth a restart again
                _failedAtLaunch.Remove(id);
            }

            _changes++;
        }

        /// <summary>Whether the active profile differs from what's loaded right now, i.e. a restart would change something.</summary>
        public bool NeedsRestart()
        {
            // A folder DewDrop doesn't know can't be compared, and waiting updates only go in on a restart
            if (Kind == SessionKind.Custom || PendingUpdateCount > 0)
            {
                return true;
            }

            // Asked several times a frame; refreshing the index forgets the answer whenever something changed
            EnabledIndex();
            return _needsRestart ??= Kind == SessionKind.Plain ? ComputePlainNeedsRestart() : ComputeNeedsRestart();
        }

        /// <summary>A plain launch needs a restart only if it loaded different mods (or copies) than the profile would.</summary>
        private bool ComputePlainNeedsRestart()
        {
            var wanted = Installed.Where(IsEnabled).Select(m => m.UniqueId);
            return !Difference().Matches || wanted.Any(_changedOnDisk.Contains);
        }

        private bool ComputeNeedsRestart()
        {
            var installedIds = new HashSet<string>(Installed.Select(m => m.UniqueId), StringComparer.OrdinalIgnoreCase);
            var wanted = new HashSet<string>(Installed.Where(IsEnabled).Select(m => m.UniqueId).Where(id => !_failedAtLaunch.Contains(id)), StringComparer.OrdinalIgnoreCase);

            // An update swapped in while the game runs (content packs hold no open files) keeps the same ID loaded
            return wanted.Overlaps(_changedOnDisk) || !wanted.SetEquals(LoadedIds.Where(installedIds.Contains));
        }

        /// <summary>
        /// Links the active profile's mods and restarts the game through SMAPI. Returns false, with the reason for the
        /// player in <paramref name="error"/>, when it didn't: the game then keeps running as it is.
        /// </summary>
        public bool ApplyAndRestart(out string? error)
        {
            error = ApplyBlockedReason;
            if (error is not null)
            {
                return false;
            }

            // Checked before any link is cleared: without SMAPI the game would close and never come back
            if (!File.Exists(ProfileStart.RestartLauncherPath))
            {
                error = "Didn't restart: SMAPI isn't in your game folder. Run the DewDrop installer (or SMAPI's) to put it back.";
                return false;
            }

            // Quitting mid-install could leave a mod half extracted, or moved into place but never turned on
            if (ModEntry.Downloads.Jobs.FirstOrDefault(j => j.State == DownloadState.Installing) is { } installing)
            {
                error = $"{installing.Title} is still installing. Restart when it's done.";
                return false;
            }

            if (!RebuildLinks(out var linkError))
            {
                error = $"Didn't restart: {linkError}";
                return false;
            }

            // Remembered until the new game reaches its title screen, so a restart that never comes back isn't repeated
            var record = new RestartRecord
            {
                Id = Guid.NewGuid().ToString("N"),
                Profile = ActiveProfile.Name,
                AtUtc = DateTime.UtcNow
            };
            Startup.Data.Restart = record;
            Startup.Save();

            try
            {
                if (ProfileStart.UsesSteamLauncher)
                {
                    Relauncher.ScheduleThroughLauncher(Paths.SteamLauncherPath, Constants.GamePath, Paths.ScriptFolder, record.Id, _log);
                }
                else
                {
                    Relauncher.Schedule(Constants.GamePath, Paths.SelectedModsFolder, Pending.FilePath, Paths.ScriptFolder, _log, record.Id);
                }
            }
            catch (Exception ex)
            {
                Startup.Data.Restart = null;
                Startup.Save();
                _log.Error($"Couldn't schedule the relaunch: {ex}");
                // Not everyone has Stardrop, so only mention it as one way to start in the (already linked) profile
                error = $"Didn't restart: DewDrop couldn't set up the restart ({ex.Message}). The SMAPI log says why. If you use Stardrop, starting the game from Stardrop loads this profile.";
                return false;
            }

            // Nothing should still be writing archives when the game quits
            RestartScheduled = true;
            ModEntry.Downloads.CancelAll();
            Game1.playSound("wand");
            Game1.game1.Exit();
            return true;
        }

        /// <param name="onDone">Gets how many mods have an update, once the answers are on the mods.</param>
        /// <param name="onError">Gets why the check failed (smapi.io unreachable, or no usable answer).</param>
        public void CheckForUpdates(Action<int>? onDone = null, Action<Exception>? onError = null)
        {
            if (UpdateCheckRunning)
            {
                return;
            }

            UpdateCheckRunning = true;
            var mods = Installed.ToList();
            var platform = Constants.TargetPlatform.ToString();
            var gameVersion = Game1.version;
            MainThread.Run(
                () => Updates.FetchAsync(mods, Constants.ApiVersion.ToString(), gameVersion, platform),
                results =>
                {
                    UpdateCheckRunning = false;
                    LastUpdateCheck = DateTime.Now;
                    _updateResults = results;

                    // Applied to the list as it is now, which a download may have rescanned meanwhile. The new answers
                    // replace the old ones, so an update that was installed since the last check stops showing.
                    foreach (var mod in Installed.Where(m => m.SourceId is null))
                    {
                        mod.Update = null;
                    }
                    UpdateChecker.Apply(results, Installed);
                    onDone?.Invoke(results.Values.Count(r => r.Update is not null));
                },
                ex =>
                {
                    UpdateCheckRunning = false;
                    _log.Warn($"Update check failed: {ex.Message}");
                    onError?.Invoke(ex);
                });
        }

        /// <summary>SMAPI keeps the active mods path internal; read it so a relaunch can be compared against it.</summary>
        private static string? GetSmapiModsPath()
        {
            try
            {
                return typeof(Constants).GetProperty("ModsPath", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)?.GetValue(null) as string;
            }
            catch
            {
                return null;
            }
        }
    }

    internal enum SessionKind
    {
        Profile,
        Plain,
        Custom
    }
}
