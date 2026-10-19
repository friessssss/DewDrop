using System.Text.Json;
using System.Text.Json.Nodes;

namespace DewDrop.Core.Services
{
    /// <summary>
    /// Reads and writes Stardrop's Settings.json. The file is held as a JSON tree and only the fields DewDrop
    /// cares about are touched, so settings Stardrop adds in later versions survive a save from DewDrop. A save
    /// re-reads the file and changes only the fields DewDrop set since the last save, so it doesn't undo what
    /// Stardrop wrote in the meantime.
    /// </summary>
    public class SettingsStore
    {
        private readonly DewDropPaths _paths;
        private readonly ILog _log;
        private JsonObject _root = new();

        /// <summary>Top-level fields DewDrop changed since the last save.</summary>
        private readonly HashSet<string> _changed = new();

        /// <summary>Set when Settings.json exists but couldn't be read, so writing would replace the player's settings.</summary>
        private bool _readOnly;

        public SettingsStore(DewDropPaths paths, ILog log)
        {
            _paths = paths;
            _log = log;
        }

        public void Load(string gamePath)
        {
            _readOnly = false;
            _root = new JsonObject();
            if (File.Exists(_paths.SettingsPath))
            {
                var root = ReadWithRetries(out var corrupt, out var error);
                if (root is not null)
                {
                    _root = root;
                }
                else if (corrupt && TryBackUp(out var backup))
                {
                    // Stardrop also starts over from a Settings.json that isn't valid JSON; a copy is kept first
                    _log.Warn($"{_paths.SettingsPath} isn't valid JSON ({error}); kept a copy at {backup} and started from defaults");
                }
                else
                {
                    // Locked or unreadable: leave it alone for this session rather than replace it with a few fields
                    _readOnly = true;
                    _log.Warn($"Couldn't read {_paths.SettingsPath}, so DewDrop won't change it this session: {error}");
                }
            }

            // First run without Stardrop: fill in the paths it would have asked for
            var changed = false;
            if (string.IsNullOrEmpty(SmapiFolderPath))
            {
                SmapiFolderPath = gamePath;
                changed = true;
            }
            if (string.IsNullOrEmpty(ModFolderPath))
            {
                ModFolderPath = Path.Combine(gamePath, "Mods");
                changed = true;
            }
            if (string.IsNullOrEmpty(ModInstallPath))
            {
                ModInstallPath = ModFolderPath;
                changed = true;
            }

            if (changed && !_readOnly)
            {
                Save();
            }
        }

        /// <summary>Writes DewDrop's changes into Settings.json; returns false (and logs) if they couldn't be written.</summary>
        public bool Save()
        {
            if (_readOnly)
            {
                _log.Warn($"Didn't save {_paths.SettingsPath}: it couldn't be read at startup");
                return false;
            }

            var temp = _paths.SettingsPath + ".dewdrop-tmp";
            try
            {
                // Start from what's on disk now, so fields Stardrop changed since the game started are kept
                var root = _root;
                if (File.Exists(_paths.SettingsPath))
                {
                    var current = ReadWithRetries(out _, out _);
                    if (current is not null)
                    {
                        foreach (var name in _changed)
                        {
                            var value = _root[name];
                            if (value is null)
                            {
                                current.Remove(name);
                            }
                            else
                            {
                                current[name] = JsonNode.Parse(value.ToJsonString());
                            }
                        }
                        root = current;
                    }
                }

                // Written beside the file and moved over it, so a failed write never leaves it cut short
                Directory.CreateDirectory(Path.GetDirectoryName(_paths.SettingsPath)!);
                File.WriteAllText(temp, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
                File.Move(temp, _paths.SettingsPath, overwrite: true);

                _root = root;
                _changed.Clear();
                return true;
            }
            catch (Exception ex)
            {
                _log.Error($"Couldn't save {_paths.SettingsPath}: {ex.Message}");
                try
                {
                    File.Delete(temp);
                }
                catch
                {
                    // Only a leftover temp file
                }
                return false;
            }
        }

        /// <summary>
        /// Whether Settings.json existed but couldn't be read at startup. Saves are refused for the session, and the
        /// values here (selected profile, Nexus login) are defaults rather than the player's.
        /// </summary>
        public bool IsReadOnly => _readOnly;

        public string? SmapiFolderPath { get => GetString("SMAPIFolderPath"); set => Set("SMAPIFolderPath", value); }
        public string? ModFolderPath { get => GetString("ModFolderPath"); set => Set("ModFolderPath", value); }
        public string? ModInstallPath { get => GetString("ModInstallPath"); set => Set("ModInstallPath", value); }
        public string? LastSelectedProfileName { get => GetString("LastSelectedProfileName"); set => Set("LastSelectedProfileName", value); }
        public bool IgnoreHiddenFolders => GetBool("IgnoreHiddenFolders", true);
        public bool CollectionsSkipSmapiUpdateCheck => GetBool("CollectionsSkipSMAPIUpdateCheck", true);

        public string CollectionInstallPath(DewDropPaths paths)
        {
            var value = GetString("CollectionInstallPath");
            return string.IsNullOrWhiteSpace(value) ? paths.DefaultCollectionsFolder : value;
        }

        // Nexus details: { Username, IsPremium, Key (base64 AES ciphertext) }
        public string? NexusUsername => _root["NexusDetails"]?["Username"]?.GetValue<string>();
        public bool NexusIsPremium => _root["NexusDetails"]?["IsPremium"]?.GetValue<bool>() ?? false;

        public byte[]? NexusEncryptedKey
        {
            get
            {
                var value = _root["NexusDetails"]?["Key"]?.GetValue<string>();
                return string.IsNullOrEmpty(value) ? null : Convert.FromBase64String(value);
            }
        }

        public void SetNexusDetails(string? username, bool isPremium, byte[]? encryptedKey)
        {
            _changed.Add("NexusDetails");
            if (encryptedKey is null)
            {
                _root.Remove("NexusDetails");
                return;
            }

            _root["NexusDetails"] = new JsonObject
            {
                ["Username"] = username,
                ["IsPremium"] = isPremium,
                ["Key"] = Convert.ToBase64String(encryptedKey)
            };
        }

        private void Set(string name, string? value)
        {
            _root[name] = value;
            _changed.Add(name);
        }

        /// <param name="corrupt">Set when the file was read but isn't a JSON object, as opposed to being locked or unreadable.</param>
        private JsonObject? ReadWithRetries(out bool corrupt, out string? error)
        {
            corrupt = false;
            error = null;
            for (var attempt = 0; attempt < 3; attempt++)
            {
                if (attempt > 0)
                {
                    // Stardrop may be halfway through rewriting it, or a scanner or sync tool may hold it
                    Thread.Sleep(150);
                }

                string text;
                try
                {
                    text = File.ReadAllText(_paths.SettingsPath);
                }
                catch (Exception ex)
                {
                    corrupt = false;
                    error = ex.Message;
                    continue;
                }

                try
                {
                    if (JsonNode.Parse(text, documentOptions: new JsonDocumentOptions { AllowTrailingCommas = true }) is JsonObject root)
                    {
                        return root;
                    }

                    error = "it doesn't hold a JSON object";
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                }
                corrupt = true;
            }

            return null;
        }

        private bool TryBackUp(out string backup)
        {
            backup = $"{_paths.SettingsPath}.bak-{DateTime.Now:yyyyMMdd-HHmmss}";
            try
            {
                File.Copy(_paths.SettingsPath, backup, overwrite: true);
                return true;
            }
            catch (Exception ex)
            {
                _log.Warn($"Couldn't back up {_paths.SettingsPath}: {ex.Message}");
                return false;
            }
        }

        private string? GetString(string name)
        {
            return _root[name] is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;
        }

        private bool GetBool(string name, bool fallback)
        {
            return _root[name] is JsonValue value && value.TryGetValue<bool>(out var flag) ? flag : fallback;
        }
    }
}
