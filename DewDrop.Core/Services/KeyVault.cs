using System.Security.Cryptography;
using System.Text.Json;

namespace DewDrop.Core.Services
{
    /// <summary>
    /// Stores the Nexus API key the way Stardrop does: AES ciphertext in Settings.json, AES key and IV in
    /// Cache/Notion.json. This is obfuscation, not protection, but it means a login in either tool works in both.
    /// </summary>
    public class KeyVault
    {
        private class PairedKeys
        {
            public byte[]? Lock { get; set; }
            public byte[]? Vector { get; set; }
        }

        private readonly DewDropPaths _paths;
        private readonly SettingsStore _settings;
        private readonly ILog _log;

        public KeyVault(DewDropPaths paths, SettingsStore settings, ILog log)
        {
            _paths = paths;
            _settings = settings;
            _log = log;
        }

        public string? GetKey()
        {
            var cipher = _settings.NexusEncryptedKey;
            if (cipher is null || !File.Exists(_paths.NotionCachePath))
            {
                return null;
            }

            try
            {
                var keys = JsonSerializer.Deserialize<PairedKeys>(File.ReadAllText(_paths.NotionCachePath));
                if (keys?.Lock is null || keys.Vector is null)
                {
                    return null;
                }

                using var aes = Aes.Create();
                using var decryptor = aes.CreateDecryptor(keys.Lock, keys.Vector);
                using var stream = new CryptoStream(new MemoryStream(cipher), decryptor, CryptoStreamMode.Read);
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                _log.Warn($"Couldn't read the saved Nexus key: {ex.Message}");
                return null;
            }
        }

        /// <summary>Saves the key for both tools; returns false (and logs) if it couldn't be saved.</summary>
        public bool SaveKey(string apiKey, string username, bool isPremium)
        {
            // Reuse the existing key pair when there is one: Stardrop, still open behind the game, writes its own copy
            // of the encrypted key back when it closes, and that copy must still decrypt with what's in Notion.json
            var keys = ReadKeys(out var unreadable);
            if (unreadable)
            {
                // A new pair would orphan the key both tools saved, so change nothing
                return false;
            }

            var isNewPair = keys is null;
            using var aes = Aes.Create();
            if (keys is not null)
            {
                aes.Key = keys.Lock!;
                aes.IV = keys.Vector!;
            }

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var buffer = new MemoryStream();
            using (var stream = new CryptoStream(buffer, encryptor, CryptoStreamMode.Write))
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(apiKey);
            }

            if (isNewPair)
            {
                // No usable pair means no saved key decrypts today, so writing a new one can't break a login
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(_paths.NotionCachePath)!);
                    File.WriteAllText(_paths.NotionCachePath, JsonSerializer.Serialize(new PairedKeys { Lock = aes.Key, Vector = aes.IV }));
                }
                catch (Exception ex)
                {
                    _log.Error($"Couldn't save the Nexus key: {ex.Message}");
                    return false;
                }
            }

            _settings.SetNexusDetails(username, isPremium, buffer.ToArray());
            return _settings.Save();
        }

        /// <summary>The saved AES key and IV, if Notion.json holds a usable pair.</summary>
        /// <param name="unreadable">Set when Notion.json exists but couldn't be opened (as opposed to holding no usable pair).</param>
        private PairedKeys? ReadKeys(out bool unreadable)
        {
            unreadable = false;
            if (!File.Exists(_paths.NotionCachePath))
            {
                return null;
            }

            string text;
            try
            {
                text = File.ReadAllText(_paths.NotionCachePath);
            }
            catch (Exception ex)
            {
                unreadable = true;
                _log.Error($"Couldn't save the Nexus key, {_paths.NotionCachePath} can't be read: {ex.Message}");
                return null;
            }

            try
            {
                var keys = JsonSerializer.Deserialize<PairedKeys>(text);
                return keys?.Lock is { Length: 16 or 24 or 32 } && keys.Vector is { Length: 16 } ? keys : null;
            }
            catch (Exception ex)
            {
                _log.Warn($"The saved Nexus key pair isn't usable, so a new one is made: {ex.Message}");
                return null;
            }
        }

        public void Clear()
        {
            _settings.SetNexusDetails(null, false, null);
            _settings.Save();
        }
    }
}
