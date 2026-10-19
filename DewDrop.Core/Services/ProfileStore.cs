using System.Text.Json;
using DewDrop.Core.Models;

namespace DewDrop.Core.Services
{
    /// <summary>Profiles live one per file in Stardrop's Profiles folder, named after the profile.</summary>
    public class ProfileStore
    {
        public const string DefaultProfileName = "Default";

        private readonly DewDropPaths _paths;
        private readonly ILog _log;
        private static readonly JsonSerializerOptions _readOptions = new() { AllowTrailingCommas = true, ReadCommentHandling = JsonCommentHandling.Skip };
        private static readonly JsonSerializerOptions _writeOptions = new() { WriteIndented = true };

        public List<Profile> Profiles { get; } = new();

        /// <summary>Profile files that exist but couldn't be read on the last load. DewDrop never writes over these.</summary>
        private readonly HashSet<string> _unreadable = new(StringComparer.OrdinalIgnoreCase);

        public ProfileStore(DewDropPaths paths, ILog log)
        {
            _paths = paths;
            _log = log;
        }

        /// <param name="newDefaultMods">Mods to enable when the Default profile has to be created, e.g. the ones loaded right now.</param>
        public void Load(IEnumerable<ModReference>? newDefaultMods = null)
        {
            Profiles.Clear();
            _unreadable.Clear();
            Directory.CreateDirectory(_paths.ProfilesFolder);
            foreach (var file in new DirectoryInfo(_paths.ProfilesFolder).GetFiles("*.json", SearchOption.AllDirectories))
            {
                var profile = TryRead(file.FullName, out var corrupt, out var error);
                if (profile is null)
                {
                    // Stardrop may be rewriting it, or a scanner or sync tool may hold it; try once more before giving up
                    Thread.Sleep(150);
                    profile = TryRead(file.FullName, out corrupt, out error);
                }

                if (profile is null)
                {
                    // A Default.json that isn't valid JSON gets replaced, as Stardrop does, but only after a copy is kept
                    if (corrupt && file.FullName.Equals(PathFor(DefaultProfileName), StringComparison.OrdinalIgnoreCase) && TryMoveAside(file.FullName, out var backup))
                    {
                        _log.Warn($"Default.json isn't a valid profile ({error}); moved it to {backup} and started a new Default");
                        continue;
                    }

                    _unreadable.Add(file.FullName);
                    _log.Warn($"Skipped unreadable profile {file.Name}; DewDrop won't write over it: {error}");
                    continue;
                }

                Sanitize(profile, file);
                Profiles.Add(profile);
            }

            if (Get(DefaultProfileName) is null)
            {
                var defaultProfile = new Profile(DefaultProfileName, isProtected: true);
                defaultProfile.EnabledModIds.AddRange((newDefaultMods ?? Enumerable.Empty<ModReference>()).Where(r => r is not null).Distinct());
                Profiles.Insert(0, defaultProfile);

                // Only a missing file gets written; an unreadable one keeps the player's list on disk, and this
                // stand-in stays in memory (see IsUnreadable)
                if (!File.Exists(PathFor(DefaultProfileName)))
                {
                    Save(defaultProfile);
                }
            }

            // Default first, collection profiles last, the rest by name
            Profiles.Sort((a, b) =>
            {
                int Rank(Profile p) => p.Name == DefaultProfileName ? 0 : p.IsFromCollection ? 2 : 1;
                var rank = Rank(a).CompareTo(Rank(b));
                return rank != 0 ? rank : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            });
        }

        public Profile? Get(string? name)
        {
            return name is null ? null : Profiles.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Whether a profile name is in use, counting files on disk that couldn't be loaded.</summary>
        public bool IsNameTaken(string name)
        {
            var fileName = FileNameFor(name);
            return Profiles.Any(p => FileNameFor(p.Name).Equals(fileName, StringComparison.OrdinalIgnoreCase)) || File.Exists(PathFor(name));
        }

        /// <summary>
        /// Whether the profile with this name has a file that couldn't be read on the last load. The copy in memory,
        /// if any, is only a stand-in (the Default always exists), so it isn't saved and shouldn't be applied.
        /// </summary>
        public bool IsUnreadable(string? name)
        {
            return name is not null && _unreadable.Contains(PathFor(name));
        }

        /// <summary>Writes the profile's file; returns false (and logs) if it couldn't be written.</summary>
        public bool Save(Profile profile)
        {
            if (IsUnreadable(profile.Name))
            {
                _log.Warn($"Didn't save profile {profile.Name}: its file couldn't be read, and saving would replace it");
                return false;
            }

            try
            {
                File.WriteAllText(PathFor(profile.Name), JsonSerializer.Serialize(profile, _writeOptions));
                return true;
            }
            catch (Exception ex)
            {
                _log.Error($"Couldn't save profile {profile.Name}: {ex.Message}");
                return false;
            }
        }

        public Profile Create(string name, Profile? copyFrom = null)
        {
            var profile = copyFrom?.Clone(name) ?? new Profile(name);
            Profiles.Add(profile);
            Save(profile);
            return profile;
        }

        public bool Rename(Profile profile, string newName)
        {
            if (profile.IsProtected || profile.IsFromCollection || IsNameTaken(newName))
            {
                return false;
            }

            var oldName = profile.Name;
            var oldPath = PathFor(oldName);
            profile.Name = newName;
            if (!Save(profile))
            {
                // Keep the old file (and name) rather than lose the profile
                profile.Name = oldName;
                return false;
            }

            if (File.Exists(oldPath) && !oldPath.Equals(PathFor(newName), StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(oldPath);
            }

            return true;
        }

        public bool Delete(Profile profile)
        {
            if (profile.IsProtected || profile.IsFromCollection)
            {
                return false;
            }

            Profiles.Remove(profile);
            var path = PathFor(profile.Name);
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            return true;
        }

        /// <param name="corrupt">Set when the file was read but isn't a valid profile, as opposed to being locked or unreadable.</param>
        private static Profile? TryRead(string path, out bool corrupt, out string? error)
        {
            corrupt = false;
            error = null;
            string text;
            try
            {
                text = File.ReadAllText(path);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return null;
            }

            try
            {
                var profile = JsonSerializer.Deserialize<Profile>(text, _readOptions);
                if (profile is null)
                {
                    corrupt = true;
                    error = "the file holds no profile";
                }
                return profile;
            }
            catch (Exception ex)
            {
                corrupt = true;
                error = ex.Message;
                return null;
            }
        }

        private bool TryMoveAside(string path, out string backup)
        {
            backup = $"{path}.bak-{DateTime.Now:yyyyMMdd-HHmmss}";
            try
            {
                File.Move(path, backup);
                return true;
            }
            catch (Exception ex)
            {
                _log.Warn($"Couldn't move {path} aside: {ex.Message}");
                return false;
            }
        }

        /// <summary>Hand-edited files can hold nulls the rest of DewDrop doesn't expect; the next save writes them back clean.</summary>
        private static void Sanitize(Profile profile, FileInfo file)
        {
            if (string.IsNullOrWhiteSpace(profile.Name))
            {
                profile.Name = Path.GetFileNameWithoutExtension(file.Name);
            }

            profile.EnabledModIds = (profile.EnabledModIds ?? new()).Where(r => r is not null).ToList();
            profile.Notes = (profile.Notes ?? new()).Where(n => n?.UniqueId is not null).ToList();
            profile.PreservedModConfigs ??= new();
        }

        private string PathFor(string name) => Path.GetFullPath(Path.Combine(_paths.ProfilesFolder, FileNameFor(name)));

        private static string FileNameFor(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return new string(name.Select(c => Array.IndexOf(invalid, c) >= 0 ? '_' : c).ToArray()) + ".json";
        }
    }
}
