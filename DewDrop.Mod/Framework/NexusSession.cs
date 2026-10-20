using DewDrop.Core.Models;
using DewDrop.Core.Services;

namespace DewDrop.Framework
{
    /// <summary>Login state for Nexus Mods, sharing the stored key with Stardrop.</summary>
    internal class NexusSession
    {
        private readonly KeyVault _vault;

        public NexusApi Api { get; }
        public bool LoggingIn { get; private set; }
        public string? LastError { get; private set; }

        public NexusAccount? Account => Api.Account;
        public bool IsLoggedIn => Api.IsLoggedIn;
        public bool IsPremium => Api.Account?.IsPremium ?? false;

        public NexusSession(NexusApi api, KeyVault vault)
        {
            Api = api;
            _vault = vault;
        }

        /// <summary>
        /// Signs in with a key the installer was given (see <see cref="DewDropPaths.NexusKeyHandoffPath"/>), which is then
        /// saved like one typed in, else with the key saved by DewDrop or Stardrop, if there is one.
        /// </summary>
        public void RestoreSavedLogin(DewDropPaths paths)
        {
            _handoffPath = paths.NexusKeyHandoffPath;
            if (ReadHandedOffKey() is { } handedOff)
            {
                Login(handedOff, save: true, onDone: ok =>
                {
                    // The file goes once the key is saved, or Nexus turned it down. While Nexus can't be reached, or the
                    // key couldn't be saved, it stays for the next start: typing it again is what it's there to save.
                    if (ok ? LastError is null : !LastLoginUnreachable)
                    {
                        DiscardHandedOffKey();
                    }

                    var message = ok
                        ? LastError ?? $"Logged in to Nexus Mods as {Account?.Name} with the key from the installer."
                        : LastLoginUnreachable
                            ? $"{LastError} DewDrop tries the key from the installer again the next time the game starts."
                            : "Nexus Mods didn't accept the key from the installer. Log in on the Account tab.";
                    var error = !ok || LastError is not null;

                    // The login takes a moment, so the menu may already be open
                    if (StardewValley.Menus.TitleMenu.subMenu is UI.DewDropMenu)
                    {
                        ModEntry.Toast(message, error);
                    }
                    else
                    {
                        ModEntry.ToastWhenMenuOpens(message, error);
                    }

                    // A key that didn't work changes nothing: the login from before still stands
                    if (!ok && _vault.GetKey() is { } saved)
                    {
                        Login(saved, save: false);
                    }
                });
                return;
            }

            var key = _vault.GetKey();
            if (key is not null)
            {
                Login(key, save: false);
            }
        }

        private string? _handoffPath;

        /// <summary>Whether the last login failed because Nexus couldn't be reached or was busy, which says nothing about the key.</summary>
        public bool LastLoginUnreachable { get; private set; }

        /// <summary>The key in the installer's file, if there is one. A file that's empty or can't be read is removed.</summary>
        private string? ReadHandedOffKey()
        {
            if (_handoffPath is null || !File.Exists(_handoffPath))
            {
                return null;
            }

            try
            {
                var key = File.ReadAllText(_handoffPath).Trim();
                if (key.Length > 0)
                {
                    return key;
                }
            }
            catch (Exception ex)
            {
                ModEntry.Log.Warn($"Couldn't read the Nexus key the installer left in {_handoffPath}: {ex.Message}");
            }

            DiscardHandedOffKey();
            return null;
        }

        /// <summary>Removes the installer's key: it's a secret in plain text, and once it's done with it mustn't override a later login.</summary>
        private void DiscardHandedOffKey()
        {
            try
            {
                if (_handoffPath is not null && File.Exists(_handoffPath))
                {
                    File.Delete(_handoffPath);
                }
            }
            catch (Exception ex)
            {
                ModEntry.Log.Warn($"Couldn't remove the Nexus key the installer left in {_handoffPath}: {ex.Message}");
            }
        }

        public void Login(string apiKey, bool save = true, Action<bool>? onDone = null)
        {
            if (LoggingIn || string.IsNullOrWhiteSpace(apiKey))
            {
                return;
            }

            LoggingIn = true;
            LastError = null;
            LastLoginUnreachable = false;
            MainThread.Run(
                () => Api.LoginAsync(apiKey),
                account =>
                {
                    LoggingIn = false;
                    if (account is null)
                    {
                        LastError = "Nexus didn't accept that API key.";
                        onDone?.Invoke(false);
                        return;
                    }

                    // The login works for this session either way; only the next start (and Stardrop) depend on the save
                    if (save && !_vault.SaveKey(apiKey.Trim(), account.Name, account.IsPremium))
                    {
                        LastError = "Logged in, but DewDrop couldn't save the key for next time. The SMAPI log says why.";
                    }
                    else if (save && ReadHandedOffKey() is { } handedOff && handedOff != apiKey.Trim())
                    {
                        // A key typed in since: the installer's mustn't replace it at the next start
                        DiscardHandedOffKey();
                    }
                    ModEntry.Log.Info($"Logged in to Nexus Mods as {account.Name}{(account.IsPremium ? " (Premium)" : "")}");
                    onDone?.Invoke(true);
                },
                ex =>
                {
                    LoggingIn = false;
                    LastError = $"Couldn't reach Nexus Mods: {ex.Message}";
                    LastLoginUnreachable = true;
                    onDone?.Invoke(false);
                });
        }

        public void Logout()
        {
            Api.Logout();
            _vault.Clear();
            DiscardHandedOffKey();
            LastError = null;
        }
    }
}
