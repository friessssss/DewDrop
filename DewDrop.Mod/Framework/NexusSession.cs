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
            if (TakeHandedOffKey(paths.NexusKeyHandoffPath) is { } handedOff)
            {
                Login(handedOff, save: true, onDone: ok =>
                {
                    var message = ok
                        ? $"Logged in to Nexus Mods as {Account?.Name} with the key from the installer."
                        : $"The Nexus key from the installer didn't work: {LastError} Log in on the Account tab.";

                    // The login takes a moment, so the menu may already be open
                    if (StardewValley.Menus.TitleMenu.subMenu is UI.DewDropMenu)
                    {
                        ModEntry.Toast(message, error: !ok);
                    }
                    else
                    {
                        ModEntry.ToastWhenMenuOpens(message, error: !ok);
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

        /// <summary>The key in the installer's file, which is removed either way: it's a secret in plain text.</summary>
        private static string? TakeHandedOffKey(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                var key = File.ReadAllText(path).Trim();
                File.Delete(path);
                return key.Length > 0 ? key : null;
            }
            catch (Exception ex)
            {
                ModEntry.Log.Warn($"Couldn't read the Nexus key the installer left in {path}: {ex.Message}");
                try
                {
                    File.Delete(path);
                }
                catch
                {
                    // Tried the next time
                }
                return null;
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
                    ModEntry.Log.Info($"Logged in to Nexus Mods as {account.Name}{(account.IsPremium ? " (Premium)" : "")}");
                    onDone?.Invoke(true);
                },
                ex =>
                {
                    LoggingIn = false;
                    LastError = $"Couldn't reach Nexus Mods: {ex.Message}";
                    onDone?.Invoke(false);
                });
        }

        public void Logout()
        {
            Api.Logout();
            _vault.Clear();
            LastError = null;
        }
    }
}
