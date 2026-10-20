using System.Reflection;
using Microsoft.Xna.Framework;
using StardewValley;

namespace DewDrop.Framework
{
    /// <summary>
    /// Steam's overlay browser and floating keyboard, through the Steamworks.NET the game ships (and has started, for
    /// the Steam version). Game Mode on a Steam Deck has no desktop browser or keyboard, so these stand in for them.
    /// Found by reflection: the reference assemblies DewDrop builds against don't include Steamworks.NET.
    /// </summary>
    internal static class SteamOverlay
    {
        private static Assembly? _steamworks;
        private static bool _looked;

        /// <summary>Opens a web page in Steam's overlay. Returns false when it can't (no Steam, or the overlay is off).</summary>
        public static bool OpenWebPage(string url)
        {
            try
            {
                if (Call("SteamUtils", "IsOverlayEnabled") is not true)
                {
                    return false;
                }

                // The second parameter, where there is one, is a mode whose default (0) is what's wanted
                var method = Find("SteamFriends", "ActivateGameOverlayToWebPage");
                if (method is null)
                {
                    return false;
                }

                var parameters = method.GetParameters();
                method.Invoke(null, parameters.Length == 2 ? new[] { url, Enum.ToObject(parameters[1].ParameterType, 0) } : new object[] { url });
                return true;
            }
            catch (Exception ex)
            {
                ModEntry.Log.Trace($"Steam's overlay couldn't open {url}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Shows Steam's floating keyboard, which types into the game like a real one, clear of a box on screen (in UI
        /// coordinates). Returns false when it can't.
        /// </summary>
        public static bool ShowKeyboard(Rectangle avoid)
        {
            try
            {
                var method = Find("SteamUtils", "ShowFloatingGamepadTextInput");
                if (method is null)
                {
                    return false;
                }

                // Steam wants screen pixels; the menu is drawn at the UI scale. Mode 0 is a single line.
                var scale = Game1.options.uiScale;
                var parameters = method.GetParameters();
                var result = method.Invoke(null, new[]
                {
                    Enum.ToObject(parameters[0].ParameterType, 0),
                    (int)(avoid.X * scale),
                    (int)(avoid.Y * scale),
                    (int)(avoid.Width * scale),
                    (int)(avoid.Height * scale)
                });
                return result is true;
            }
            catch (Exception ex)
            {
                ModEntry.Log.Trace($"Steam's keyboard couldn't open: {ex.Message}");
                return false;
            }
        }

        private static object? Call(string type, string name)
        {
            return Find(type, name)?.Invoke(null, null);
        }

        private static MethodInfo? Find(string type, string name)
        {
            if (!_looked)
            {
                _looked = true;
                _steamworks = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "Steamworks.NET");
            }

            return _steamworks?.GetType($"Steamworks.{type}")?.GetMethods(BindingFlags.Public | BindingFlags.Static).FirstOrDefault(m => m.Name == name);
        }
    }
}
