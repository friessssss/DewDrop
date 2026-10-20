using DewDrop.Framework;
using DewDrop.UI;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace DewDrop.Patches
{
    /// <summary>
    /// Adds a "Mods" button between Co-op and Exit on the title screen. It behaves like the vanilla buttons: the
    /// title art slides away and the DewDrop menu opens as the title screen's sub-menu, so the vanilla back button,
    /// clouds and music all keep working.
    /// </summary>
    internal static class TitleMenuPatches
    {
        public const string ButtonName = "DewDrop";
        private const int ButtonId = 81190;
        private const int ButtonWidth = 74;
        private const int ButtonHeight = 58;
        private const int Gap = 8;

        // Vanilla button IDs from TitleMenu.setUpIcons
        private const int CoopId = 81119;
        private const int ExitId = 81117;

        /// <summary>Set while the slide-away transition is running for our button.</summary>
        private static bool _opening;

        /// <summary>Whether the DewDrop menu was open at the last update, to notice it closing.</summary>
        private static bool _menuOpen;

        /// <summary>The title buttons texture ButtonTexture was built from.</summary>
        private static Texture2D? _builtFrom;

        /// <summary>Set when a mod edited the title buttons texture in place, so the Mods button gets built again.</summary>
        private static bool _sourceChanged;

        public static Texture2D? ButtonTexture { get; private set; }

        public static void Apply(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(TitleMenu), nameof(TitleMenu.setUpIcons)), postfix: new HarmonyMethod(typeof(TitleMenuPatches), nameof(SetUpIcons_Postfix)));
            harmony.Patch(AccessTools.Method(typeof(TitleMenu), nameof(TitleMenu.performButtonAction)), prefix: new HarmonyMethod(typeof(TitleMenuPatches), nameof(PerformButtonAction_Prefix)));
            harmony.Patch(AccessTools.Method(typeof(TitleMenu), nameof(TitleMenu.update)), postfix: new HarmonyMethod(typeof(TitleMenuPatches), nameof(Update_Postfix)));
            harmony.Patch(AccessTools.Method(typeof(TitleMenu), nameof(TitleMenu.overrideSnappyMenuCursorMovementBan)), postfix: new HarmonyMethod(typeof(TitleMenuPatches), nameof(OverrideSnappyMenuCursorMovementBan_Postfix)));
            harmony.Patch(AccessTools.Method(typeof(TitleMenu), nameof(TitleMenu.receiveKeyPress), new[] { typeof(Keys) }), prefix: new HarmonyMethod(typeof(TitleMenuPatches), nameof(ReceiveKeyPress_Prefix)));
        }

        /// <summary>
        /// Game1.updateActiveMenu asks the title screen, not its sub-menu, whether a controller's stick may move a free
        /// cursor; the DewDrop menu has no snap points, so without this the stick can't reach anything in it.
        /// </summary>
        private static void OverrideSnappyMenuCursorMovementBan_Postfix(ref bool __result)
        {
            if (TitleMenu.subMenu is DewDropMenu or TitlePrompt)
            {
                __result |= TitleMenu.subMenu.overrideSnappyMenuCursorMovementBan();
            }
        }

        /// <summary>
        /// The title screen never passes its menu keys (Escape, E) to a sub-menu. The DewDrop menu gets them first,
        /// and only uses them to close a confirm dialog or leave a text box; anything else goes on as usual.
        /// </summary>
        private static bool ReceiveKeyPress_Prefix(Keys key)
        {
            try
            {
                // The "Load your profile?" box answers Esc itself (as Not now)
                if (TitleMenu.subMenu is TitlePrompt prompt && Game1.options.doesInputListContain(Game1.options.menuButton, key))
                {
                    prompt.ReceiveMenuKey();
                    return false;
                }

                return !(TitleMenu.subMenu is DewDropMenu menu && Game1.options.doesInputListContain(Game1.options.menuButton, key) && menu.ReceiveMenuKey(key));
            }
            catch (Exception ex)
            {
                ModEntry.Log.Error($"Couldn't pass a menu key to the DewDrop menu: {ex}");
                return true;
            }
        }

        private static void SetUpIcons_Postfix(TitleMenu __instance)
        {
            try
            {
                var buttons = __instance.buttons;
                var exitIndex = buttons.FindIndex(b => b.name == "Exit");
                if (exitIndex < 0 || buttons.Any(b => b.name == ButtonName))
                {
                    return;
                }

                // A new title screen or language loads a fresh texture, possibly with different art
                var texture = ButtonTexture is not null && _builtFrom == __instance.titleButtonsTexture ? ButtonTexture : BuildTexture(__instance);
                var zoom = TitleMenu.pixelZoom;
                var template = buttons[exitIndex];

                if (FitRow(__instance, buttons.Count + 1) is { } row)
                {
                    var mods = new ClickableTextureComponent(ButtonName, new Rectangle(0, template.bounds.Y, ButtonWidth * zoom, ButtonHeight * zoom), null, "", texture, new Rectangle(0, TitleButtonArt.NormalY, ButtonWidth, ButtonHeight), zoom)
                    {
                        myID = ButtonId,
                        leftNeighborID = CoopId,
                        rightNeighborID = ExitId,
                        upNeighborID = 81111
                    };
                    buttons.Insert(exitIndex, mods);

                    foreach (var button in buttons)
                    {
                        if (button.myID == CoopId)
                        {
                            button.rightNeighborID = ButtonId;
                        }
                        else if (button.myID == ExitId)
                        {
                            button.leftNeighborID = ButtonId;
                        }
                    }

                    // setUpIcons already offset each button by globalXOffset, so the new positions carry it too
                    var x = row.Left + __instance.globalXOffset;
                    foreach (var button in buttons)
                    {
                        button.bounds.X = x;
                        x += (ButtonWidth + row.Gap) * zoom;
                    }
                }
                else
                {
                    AddCompactButton(__instance, texture, exitIndex);
                }

                if (Game1.options.gamepadControls && Game1.options.snappyMenus)
                {
                    __instance.populateClickableComponentList();

                    // Vanilla snapped the cursor before the row moved, which leaves it over the next button along
                    if (TitleMenu.subMenu is null)
                    {
                        __instance.snapToDefaultClickableComponent();
                    }
                }
            }
            catch (Exception ex)
            {
                ModEntry.Log.Error($"Couldn't add the Mods button: {ex}");
            }
        }

        /// <summary>
        /// Where a row of big buttons fits between the left edge and the About/Language buttons in the bottom-right
        /// corner, in unshifted screen coordinates: centred with vanilla's gap when there's room, otherwise with
        /// tighter gaps and moved left just enough to clear the corner. Null when even the tightest row won't fit.
        /// </summary>
        private static (int Left, int Gap)? FitRow(TitleMenu menu, int count)
        {
            var zoom = TitleMenu.pixelZoom;
            var leftLimit = 4 * zoom;
            var rightLimit = CornerLeft(menu) - 4 * zoom;

            foreach (var gap in new[] { Gap, 6, 4, 2 })
            {
                var rowWidth = (count * ButtonWidth + (count - 1) * gap) * zoom;
                var left = Math.Min(menu.width / 2 - rowWidth / 2, rightLimit - rowWidth);
                if (left >= leftLimit)
                {
                    return (left, gap);
                }
            }

            return null;
        }

        /// <summary>The left edge of the small buttons in the bottom-right corner (About, and Language when shown).</summary>
        private static int CornerLeft(TitleMenu menu)
        {
            var left = menu.aboutButton?.bounds.X ?? menu.width;
            if (menu.languageButton is { visible: true } language)
            {
                left = Math.Min(left, language.bounds.X);
            }
            return left;
        }

        /// <summary>
        /// For windows too narrow for five big buttons: a small Mods button stacked above the About/Language buttons,
        /// in their style, leaving vanilla's four buttons where vanilla put them.
        /// </summary>
        private static void AddCompactButton(TitleMenu menu, Texture2D texture, int exitIndex)
        {
            var zoom = TitleMenu.pixelZoom;
            var corner = menu.languageButton is { visible: true } language ? language : menu.aboutButton;
            var bounds = new Rectangle(
                CornerLeft(menu) + menu.globalXOffset,
                corner.bounds.Y - (TitleButtonArt.CompactHeight + 4) * zoom,
                TitleButtonArt.CompactWidth * zoom,
                TitleButtonArt.CompactHeight * zoom);

            var mods = new ClickableTextureComponent(ButtonName, bounds, null, "", texture, new Rectangle(TitleButtonArt.CompactX, TitleButtonArt.NormalY, TitleButtonArt.CompactWidth, TitleButtonArt.CompactHeight), zoom)
            {
                myID = ButtonId,
                leftNeighborID = ExitId,
                downNeighborID = corner.myID,
                upNeighborID = 81111
            };

            // Last in the list so vanilla's four keep their order (the draw and hover loops just walk the list)
            menu.buttons.Add(mods);
            menu.buttons[exitIndex].rightNeighborID = ButtonId;
        }

        /// <summary>
        /// Content packs edit the title buttons once SMAPI has started, after the first title screen was built, and
        /// SMAPI updates the vanilla texture in place; the Mods button is built again from it on the next update.
        /// </summary>
        public static void OnAssetsInvalidated(object? sender, AssetsInvalidatedEventArgs e)
        {
            if (e.NamesWithoutLocale.Any(name => name.IsEquivalentTo("Minigames/TitleButtons")))
            {
                _sourceChanged = true;
            }
        }

        /// <summary>Builds the Mods button art from the menu's title buttons, swapping it into the button if there is one.</summary>
        private static Texture2D BuildTexture(TitleMenu menu)
        {
            var old = ButtonTexture;
            var texture = TitleButtonArt.Build(menu.titleButtonsTexture);
            ButtonTexture = texture;
            _builtFrom = menu.titleButtonsTexture;
            _sourceChanged = false;

            foreach (var button in menu.buttons)
            {
                if (button.name == ButtonName)
                {
                    button.texture = texture;
                }
            }

            old?.Dispose();
            return texture;
        }

        private static bool PerformButtonAction_Prefix(TitleMenu __instance, string which)
        {
            if (which != ButtonName)
            {
                return true;
            }

            // Mirror what vanilla does for Load: slide the title away, then open the menu when it's off screen
            _opening = true;
            __instance.buttonsDX = 1;
            __instance.isTransitioningButtons = true;
            Game1.playSound("select");
            return false;
        }

        private static void Update_Postfix(TitleMenu __instance)
        {
            if (_sourceChanged && ButtonTexture is not null)
            {
                try
                {
                    BuildTexture(__instance);
                }
                catch (Exception ex)
                {
                    _sourceChanged = false;
                    ModEntry.Log.Error($"Couldn't redraw the Mods button: {ex}");
                }
            }

            // The Browse pictures aren't needed once the menu closes; they load again from the disk cache
            var menuOpen = TitleMenu.subMenu is DewDropMenu;
            if (_menuOpen && !menuOpen)
            {
                ModEntry.Thumbnails.Clear();
            }
            _menuOpen = menuOpen;

            // The draw loop only shows the first buttonsToShow buttons and vanilla stops counting at 4
            if (__instance.buttonsToShow == 4 && __instance.buttons.Count > 4)
            {
                __instance.buttonsToShow = __instance.buttons.Count;
            }

            if (_opening && __instance.isTransitioningButtons && __instance.buttonsDX > 0 && __instance.globalXOffset >= __instance.width && TitleMenu.subMenu is null)
            {
                _opening = false;
                __instance.buttonsDX = 0;
                __instance.isTransitioningButtons = false;
                TitleMenu.subMenu = new DewDropMenu();
                Game1.changeMusicTrack("title_night");
            }
            else if (_opening && !__instance.isTransitioningButtons)
            {
                _opening = false;
            }
        }
    }
}
