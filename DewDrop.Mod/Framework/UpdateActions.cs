using DewDrop.Core.Models;
using DewDrop.UI.Pages;

namespace DewDrop.Framework
{
    /// <summary>Turns "this mod has an update" into the right download for the player's account.</summary>
    internal static class UpdateActions
    {
        /// <summary>
        /// Premium: downloads and installs the new main file in place. Free: opens the file on Nexus so the
        /// website's "Mod manager download" button can hand it back. No Nexus ID, or an update that isn't on
        /// Nexus: opens the update page.
        /// </summary>
        public static void Update(InstalledMod mod)
        {
            var nexusId = mod.NexusId;
            if (nexusId is null || !ModEntry.Nexus.IsLoggedIn)
            {
                var url = mod.Update?.Url ?? (nexusId is null ? null : $"https://www.nexusmods.com/stardewvalley/mods/{nexusId}?tab=files");
                if (url is null)
                {
                    ModEntry.Toast($"{mod.Name} doesn't say where its updates come from.", error: true);
                    return;
                }

                if (nexusId is not null)
                {
                    ModEntry.Toast("Log in to Nexus Mods on the Account tab to update in-game.");
                }
                DownloadManager.OpenUrl(url);
                return;
            }

            // smapi.io can suggest a GitHub or ModDrop release, or an unofficial update, for a mod that also has a
            // Nexus page. No Nexus file is that release, so send the player to where it is
            var updateUrl = mod.Update?.Url;
            if (updateUrl is not null && !IsNexusPage(updateUrl, nexusId.Value))
            {
                ModEntry.Toast($"The {mod.Name} update isn't on Nexus Mods; opening where it is.");
                DownloadManager.OpenUrl(updateUrl);
                return;
            }

            var wanted = mod.Update?.Version;
            ModEntry.Toast($"Finding the {mod.Name} update...");
            MainThread.Run(
                () => ModEntry.Nexus.Api.GetFilesAsync(nexusId.Value),
                files =>
                {
                    // Falling back to the newest main file would often reinstall the version already there, and the
                    // update would never go away; only the file for the wanted version will do
                    var file = PickMainFile(files, wanted);
                    if (file is null || VersionsEqual(file.Version, mod.Version))
                    {
                        ModEntry.Toast($"Couldn't find {mod.Name}{(wanted is null ? "" : $" {wanted}")} among its Nexus files; opening them instead.", error: true);
                        DownloadManager.OpenOnNexus(nexusId.Value);
                        return;
                    }

                    if (ModEntry.Nexus.IsPremium)
                    {
                        ModEntry.Downloads.Start(nexusId.Value, file.FileId, $"{mod.Name} {file.Version}");
                    }
                    else
                    {
                        OpenForFreeAccount(nexusId.Value, file.FileId);
                    }
                });
        }

        /// <summary>
        /// Free accounts download on the website: opens the file there and says what happens next on this PC. Only
        /// Windows has the nxm:// handler that sends the website's download back to the game.
        /// </summary>
        public static void OpenForFreeAccount(int modId, int? fileId)
        {
            DownloadManager.OpenOnNexus(modId, fileId);
            if (!OperatingSystem.IsWindows())
            {
                ModEntry.Toast("Opened the file on Nexus Mods. Mod manager links only work on Windows for now, so add the mod to your Mods folder yourself.");
            }
            else if (!ModEntry.Nxm.IsRegistered(ModEntry.NxmHandlerScriptPath))
            {
                // Windows' Default apps choice is something only the player can change, so say that rather than "set it up"
                ModEntry.Toast(AccountPage.LinksProblem() ?? "Turn on mod manager links on the Account tab first, then click 'Slow download'.", error: true);
            }
            else
            {
                ModEntry.Toast("Click 'Slow download' on the website; DewDrop installs it when it arrives.");
            }
        }

        /// <summary>What a free account's download button does on this PC, for tooltips (no registry reads, as they're drawn every frame).</summary>
        public static string FreeAccountHint => OperatingSystem.IsWindows()
            ? "Free accounts download from the website. With mod manager links on (Account tab), DewDrop installs the file when it arrives."
            : "Free accounts download from the website. Mod manager links only work on Windows for now, so add the mod to your Mods folder yourself.";

        /// <summary>The file marked with a version, preferring a main file; null if Nexus has none.</summary>
        public static NexusModFile? PickMainFile(List<NexusModFile> files, string? version)
        {
            if (version is null)
            {
                return null;
            }

            return files.FirstOrDefault(f => f.Category == "MAIN" && VersionsEqual(f.Version, version))
                ?? files.FirstOrDefault(f => f.IsCurrent && VersionsEqual(f.Version, version));
        }

        /// <summary>Whether a URL is this mod's Nexus page, the form smapi.io suggests (nexusmods.com/stardewvalley/mods/541).</summary>
        private static bool IsNexusPage(string url, int nexusId)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
                || !(uri.Host.Equals("nexusmods.com", StringComparison.OrdinalIgnoreCase) || uri.Host.EndsWith(".nexusmods.com", StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            return segments.Length >= 3
                && segments[0].Equals("stardewvalley", StringComparison.OrdinalIgnoreCase)
                && segments[1].Equals("mods", StringComparison.OrdinalIgnoreCase)
                && segments[2] == nexusId.ToString();
        }

        private static bool VersionsEqual(string? a, string? b)
        {
            // "v1.2" and "1.2.0" are the same release
            static string Normalize(string v)
            {
                v = v.Trim().TrimStart('v', 'V');
                var end = v.IndexOfAny(new[] { '-', '+', ' ' });
                var parts = (end < 0 ? v : v[..end]).Split('.').ToList();
                while (parts.Count > 1 && parts[^1] == "0")
                {
                    parts.RemoveAt(parts.Count - 1);
                }
                return string.Join(".", parts) + (end < 0 ? "" : v[end..]);
            }

            return a is not null && b is not null && Normalize(a).Equals(Normalize(b), StringComparison.OrdinalIgnoreCase);
        }
    }
}
