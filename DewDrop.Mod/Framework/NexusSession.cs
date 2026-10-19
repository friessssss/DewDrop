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

        /// <summary>Signs in with the key saved by DewDrop or Stardrop, if there is one.</summary>
        public void RestoreSavedLogin()
        {
            var key = _vault.GetKey();
            if (key is not null)
            {
                Login(key, save: false);
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
