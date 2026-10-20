using DewDrop.Core.Models;
using StardewValley;

namespace DewDrop.UI
{
    /// <summary>The "Uninstall?" dialog shared by the Installed tab and a mod's page, and what happens when it's answered.</summary>
    internal static class UninstallPrompt
    {
        /// <summary>
        /// Asks before uninstalling, saying what goes and what it breaks, then uninstalls. Each mod's folder is deleted
        /// when the game closes; until then the Installed tab can undo it.
        /// </summary>
        /// <param name="fromMod">The Nexus mod's name when several of its copies go at once from its page.</param>
        /// <param name="onDone">Runs after anything was uninstalled.</param>
        public static void Show(DewDropMenu menu, IReadOnlyList<InstalledMod> targets, string? fromMod = null, Action? onDone = null)
        {
            var mods = ModEntry.Mods;
            if (targets.Count == 0)
            {
                return;
            }

            // Checked first so the player isn't asked about something that then can't happen
            var blocked = targets.Select(mods.UninstallBlockedReason).FirstOrDefault(r => r is not null);
            if (blocked is not null)
            {
                ModEntry.Toast(blocked, error: true);
                Game1.playSound("cancel");
                return;
            }

            menu.Confirm(Message(targets, fromMod), "Uninstall", () =>
            {
                var done = new List<InstalledMod>();
                foreach (var mod in targets)
                {
                    // Downloads and installs can start while the dialog is open, so this checks again
                    if (mods.Uninstall(mod, out var error))
                    {
                        done.Add(mod);
                    }
                    else
                    {
                        ModEntry.Toast(error ?? $"Couldn't uninstall {mod.Name}.", error: true);
                    }
                }

                if (done.Count == 0)
                {
                    Game1.playSound("cancel");
                    return;
                }

                Game1.playSound("trashcan");
                ModEntry.Toast(done.Count == 1
                    ? $"Uninstalled {done[0].Name}. It's deleted when the game closes."
                    : $"Uninstalled {done.Count} mods. They're deleted when the game closes.");
                onDone?.Invoke();
            });
        }

        private static string Message(IReadOnlyList<InstalledMod> targets, string? fromMod)
        {
            var mods = ModEntry.Mods;
            var one = targets.Count == 1;
            var it = one ? "it" : "them";

            var message = one
                ? $"Uninstall {targets[0].Name}?"
                : $"Uninstall {fromMod ?? "this mod"}? That's {targets.Count} mods: {Names(targets.Select(m => m.Name).ToList())}.";

            // Only what really happens: a mod with another copy left stays on, and a read-only profile isn't changed
            var turnedOff = mods.TurnedOffByUninstalling(targets);
            var folders = one ? "its folder" : "their folders";
            message += turnedOff.Count == targets.Count
                ? $"\n\nDewDrop turns {it} off now and deletes {folders}, settings included, when the game closes."
                : turnedOff.Count == 0
                    ? $"\n\nDewDrop deletes {folders}, settings included, when the game closes."
                    : $"\n\nDewDrop turns off {Names(turnedOff.Select(m => m.Name).ToList())} now and deletes {folders}, settings included, when the game closes.";
            message += " Until then you can undo it on the Installed tab.";

            var dependents = mods.DependentsLeftWithout(targets);
            if (dependents.Count > 0)
            {
                var need = dependents.Count == 1 ? "needs" : "need";
                message += $"\n\n{Names(dependents.Select(m => m.Name).ToList())} {need} {it} and won't work without {it}.";
            }

            if (targets.Any(mods.HasPendingUpdate))
            {
                message += one
                    ? "\n\nThe update downloaded for it is thrown away too."
                    : "\n\nUpdates downloaded for them are thrown away too.";
            }

            // SMAPI's own mods (Console Commands, Save Backup) come back with SMAPI's installer
            if (targets.FirstOrDefault(m => m.UniqueId.StartsWith("SMAPI.", StringComparison.OrdinalIgnoreCase)) is { } bundled)
            {
                message += $"\n\n{bundled.Name} came with SMAPI, so installing SMAPI again puts it back.";
            }

            if (one && mods.OtherLooseCopies(targets[0]).FirstOrDefault() is { } other)
            {
                message += $"\n\nYour other copy, in the '{other.Folder.Name}' folder, stays installed.";
            }

            return message;
        }

        /// <summary>Up to three names, then how many more.</summary>
        internal static string Names(IReadOnlyList<string> names)
        {
            var shown = names.Take(3).ToList();
            if (names.Count > 3)
            {
                return $"{string.Join(", ", shown)} and {names.Count - 3} more";
            }

            return shown.Count switch
            {
                0 => "",
                1 => shown[0],
                2 => $"{shown[0]} and {shown[1]}",
                _ => $"{shown[0]}, {shown[1]} and {shown[2]}"
            };
        }
    }
}
