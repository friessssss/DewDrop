using System.Text.Json;
using System.Text.Json.Serialization;

namespace DewDrop.Core.Services
{
    /// <summary>A restart DewDrop scheduled, kept until the game it started reaches the title screen.</summary>
    public class RestartRecord
    {
        /// <summary>Matches the DEWDROP_LAUNCH variable the relauncher hands the new game, so it knows it's that restart.</summary>
        public string Id { get; set; } = "";

        /// <summary><see cref="StageRestarting"/> until the new game starts, then <see cref="StageArrived"/> until its title screen.</summary>
        public string Stage { get; set; } = StageRestarting;

        public string Profile { get; set; } = "";

        public DateTime AtUtc { get; set; }

        public const string StageRestarting = "Restarting";
        public const string StageArrived = "Arrived";
    }

    public class StartupStateData
    {
        public RestartRecord? Restart { get; set; }

        /// <summary>
        /// Unique IDs of mods DewDrop already knows about: everything installed when this list was made, plus every mod
        /// switched on or off, installed through DewDrop, or named in a "Load your profile?" answer since. An installed
        /// mod that isn't here was copied into the Mods folder by hand.
        /// </summary>
        public List<string> SeenMods { get; set; } = new();
    }

    /// <summary>
    /// Reads and writes Cache/DewDrop/startup.json, which DewDrop uses to tell when a restart didn't come back and to
    /// notice mods copied in by hand. A damaged file is set aside as .bak and treated as missing.
    /// </summary>
    public class StartupState
    {
        private static readonly JsonSerializerOptions _json = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly string _path;
        private readonly ILog _log;
        private HashSet<string> _seen = new(StringComparer.OrdinalIgnoreCase);

        public StartupState(string path, ILog log)
        {
            _path = path;
            _log = log;
        }

        public StartupStateData Data { get; private set; } = new();

        /// <summary>False when there was no file to read (first run, or it was damaged): the seen-mods list starts empty.</summary>
        public bool Existed { get; private set; }

        public void Load()
        {
            Data = new StartupStateData();
            Existed = false;
            if (!File.Exists(_path))
            {
                _seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                return;
            }

            try
            {
                Data = JsonSerializer.Deserialize<StartupStateData>(File.ReadAllText(_path), _json) ?? new StartupStateData();
                Data.SeenMods ??= new List<string>();
                Existed = true;
            }
            catch (Exception ex)
            {
                _log.Warn($"Couldn't read {_path}, so DewDrop starts it again: {ex.Message}");
                try
                {
                    File.Move(_path, _path + ".bak", overwrite: true);
                }
                catch
                {
                    // Left in place; the next save writes over it
                }
                Data = new StartupStateData();
            }

            _seen = new HashSet<string>(Data.SeenMods.Where(id => !string.IsNullOrEmpty(id)), StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Writes the file through a temporary copy so a crash mid-write can't leave half a file. Returns whether it worked.</summary>
        public bool Save()
        {
            try
            {
                Data.SeenMods = _seen.OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToList();
                Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
                var temp = _path + ".tmp";
                File.WriteAllText(temp, JsonSerializer.Serialize(Data, _json));
                File.Move(temp, _path, overwrite: true);
                Existed = true;
                return true;
            }
            catch (Exception ex)
            {
                _log.Warn($"Couldn't save {_path}: {ex.Message}");
                return false;
            }
        }

        public bool HasSeen(string uniqueId) => _seen.Contains(uniqueId);

        /// <summary>Takes mods off the seen list, so they count as new again (the caller saves).</summary>
        public void Forget(IEnumerable<string> uniqueIds)
        {
            foreach (var id in uniqueIds)
            {
                _seen.Remove(id);
            }
        }

        /// <summary>Adds mods to the seen list; returns whether any were new (the caller saves).</summary>
        public bool MarkSeen(IEnumerable<string> uniqueIds)
        {
            var added = false;
            foreach (var id in uniqueIds)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    added |= _seen.Add(id);
                }
            }
            return added;
        }
    }
}
