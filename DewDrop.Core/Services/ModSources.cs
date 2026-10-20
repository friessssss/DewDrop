using System.Text.Json;
using DewDrop.Core.Models;

namespace DewDrop.Core.Services
{
    /// <summary>The Nexus file DewDrop last installed a mod from.</summary>
    public class InstallSource
    {
        public int ModId { get; set; }
        public int FileId { get; set; }

        /// <summary>The manifest version that file put in place; a different version installed now means the mod changed since.</summary>
        public string Version { get; set; } = "";

        public DateTime InstalledUtc { get; set; }

        /// <summary>
        /// Every Nexus mod whose download DewDrop installed this mod from, the last one included. A framework that comes
        /// bundled in other mods' downloads belongs to each of them as well as to its own page.
        /// </summary>
        public List<int> FromMods { get; set; } = new();
    }

    public class ModSourcesData
    {
        /// <summary>By unique ID: the Nexus file each mod was last installed from through DewDrop.</summary>
        public Dictionary<string, InstallSource> Installs { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>By unique ID: the Nexus mod smapi.io knows each mod by, for mods without a usable Nexus update key.</summary>
        public Dictionary<string, int> NexusIds { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Reads and writes Cache/DewDrop/mod-sources.json: which Nexus mod each installed mod belongs to, beyond what its
    /// manifest's update keys say, so Browse can tell what's installed and a mod's page which of its files. Kept in
    /// DewDrop's own file, since Stardrop rewrites its own cache without unknown fields and replaces mod folders on update.
    /// Entries for mods that are gone are harmless: they're only looked up for mods that are installed.
    /// </summary>
    public class ModSources
    {
        private static readonly JsonSerializerOptions _json = new() { WriteIndented = true };

        private readonly string _path;
        private readonly ILog _log;

        public ModSources(string path, ILog log)
        {
            _path = path;
            _log = log;
        }

        public ModSourcesData Data { get; private set; } = new();

        public void Load()
        {
            Data = new ModSourcesData();
            if (!File.Exists(_path))
            {
                return;
            }

            try
            {
                var data = JsonSerializer.Deserialize<ModSourcesData>(File.ReadAllText(_path), _json) ?? new ModSourcesData();

                // Read back with the default comparer, and a hand-edited file can hold nulls
                Data = new ModSourcesData
                {
                    Installs = new Dictionary<string, InstallSource>(
                        (data.Installs ?? new()).Where(p => !string.IsNullOrEmpty(p.Key) && p.Value is not null),
                        StringComparer.OrdinalIgnoreCase),
                    NexusIds = new Dictionary<string, int>(
                        (data.NexusIds ?? new()).Where(p => !string.IsNullOrEmpty(p.Key) && p.Value > 0),
                        StringComparer.OrdinalIgnoreCase)
                };
                foreach (var source in Data.Installs.Values)
                {
                    source.FromMods ??= new List<int>();
                }
            }
            catch (Exception ex)
            {
                // Only the "Installed" marks on Browse are lost; installs and update checks fill it in again
                _log.Warn($"Couldn't read {_path}, so DewDrop starts it again: {ex.Message}");
                try
                {
                    File.Move(_path, _path + ".bak", overwrite: true);
                }
                catch
                {
                    // Left in place; the next save writes over it
                }
            }
        }

        /// <summary>Writes the file through a temporary copy so a crash mid-write can't leave half a file. Returns whether it worked.</summary>
        public bool Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
                var temp = _path + ".tmp";
                File.WriteAllText(temp, JsonSerializer.Serialize(Data, _json));
                File.Move(temp, _path, overwrite: true);
                return true;
            }
            catch (Exception ex)
            {
                _log.Warn($"Couldn't save {_path}: {ex.Message}");
                return false;
            }
        }

        public InstallSource? InstallOf(string uniqueId) => Data.Installs.TryGetValue(uniqueId, out var source) ? source : null;

        /// <summary>
        /// Remembers that a Nexus file installed a mod; returns whether anything changed (the caller saves). The file is
        /// the one installed now, and the Nexus mod joins the ones it came from. Archives often bundle another mod, such
        /// as a framework: one whose own manifest names a different Nexus mod is left to that one's page.
        /// </summary>
        /// <param name="manifestNexusId">The Nexus mod ID in the installed manifest's update keys, if any.</param>
        public bool RecordInstall(string uniqueId, int modId, int fileId, string version, int? manifestNexusId, DateTime nowUtc)
        {
            if (string.IsNullOrEmpty(uniqueId) || modId <= 0 || fileId <= 0)
            {
                return false;
            }

            if (manifestNexusId is > 0 && manifestNexusId != modId)
            {
                return false;
            }

            var existing = InstallOf(uniqueId);
            if (existing is not null && existing.ModId == modId && existing.FileId == fileId && existing.Version == version && existing.FromMods.Contains(modId))
            {
                return false;
            }

            var from = existing?.FromMods.ToList() ?? new List<int>();
            if (existing is not null && !from.Contains(existing.ModId))
            {
                from.Add(existing.ModId);
            }
            if (!from.Contains(modId))
            {
                from.Add(modId);
            }

            Data.Installs[uniqueId] = new InstallSource { ModId = modId, FileId = fileId, Version = version, InstalledUtc = nowUtc, FromMods = from };
            return true;
        }

        /// <summary>
        /// The unique IDs a Nexus file installed last time, as far as the records go: a file only counts as installed while
        /// all of them still are, so one part trashed on its own can be put back.
        /// </summary>
        public IEnumerable<string> IdsInstalledBy(int modId, int fileId)
        {
            return Data.Installs.Where(p => p.Value.ModId == modId && p.Value.FileId == fileId).Select(p => p.Key);
        }

        /// <summary>
        /// Takes in what smapi.io said each mod's Nexus ID is (null for none); returns whether anything changed (the caller
        /// saves). A mod smapi.io wasn't asked about keeps what it had, so a check that skipped it doesn't lose its ID.
        /// </summary>
        public bool SetNexusIds(IEnumerable<KeyValuePair<string, int?>> answers)
        {
            var changed = false;
            foreach (var (uniqueId, nexusId) in answers)
            {
                if (string.IsNullOrEmpty(uniqueId))
                {
                    continue;
                }

                if (nexusId is int id && id > 0)
                {
                    if (!Data.NexusIds.TryGetValue(uniqueId, out var old) || old != id)
                    {
                        Data.NexusIds[uniqueId] = id;
                        changed = true;
                    }
                }
                else
                {
                    changed |= Data.NexusIds.Remove(uniqueId);
                }
            }

            return changed;
        }

        /// <summary>
        /// Every Nexus mod an installed mod belongs to: the one in its update keys, the one DewDrop installed it from, and
        /// the one smapi.io knows it by. Usually one, but a framework bundled in another mod's download can have two.
        /// </summary>
        public IEnumerable<int> NexusIdsOf(InstalledMod mod)
        {
            var ids = new HashSet<int>();

            // "UpdateKeys": null reads as an empty list, but a hand-built manifest can still hold null
            if (mod.Manifest.UpdateKeys is not null && mod.NexusId is { } fromKeys)
            {
                ids.Add(fromKeys);
            }

            // Collection copies are pinned by their collection; only loose installs come from DewDrop
            if (mod.SourceId is null && InstallOf(mod.UniqueId) is { } source)
            {
                ids.Add(source.ModId);
                ids.UnionWith(source.FromMods.Where(id => id > 0));
            }

            if (Data.NexusIds.TryGetValue(mod.UniqueId, out var known))
            {
                ids.Add(known);
            }

            return ids;
        }
    }
}
