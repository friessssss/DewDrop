---
description: "Open and close the DewDrop menu, read its tabs and bottom bar, and use it with a mouse, keyboard, controller or Steam Deck."
icon: window-maximize
---

# The DewDrop menu

Everything DewDrop does happens in one menu on Stardew Valley's title screen. This page covers what every tab shares: the tabs, the bottom bar, messages, dialogs and controls.

## Open the menu

On the title screen, click **Mods**. It sits between **Co-op** and **Exit**, in the same wooden style. The menu opens on the **Installed** tab.

If the game window is too narrow for five big buttons, a small dew-drop button above the small buttons in the bottom-right corner opens the same menu.

<figure><img src="../.gitbook/assets/shot-title.png" alt="The Stardew Valley title screen with DewDrop's wooden Mods button, marked with a dew drop on a leaf, between the Co-op and Exit buttons"><figcaption><p>The Mods button on the title screen.</p></figcaption></figure>

The menu only opens from the title screen. To change your mods in the middle of a game, exit to the title screen first.

If a **Load your profile?** box is showing, answer it first: nothing else on the title screen can be clicked while it's open. See [Start in your profile](start-in-your-profile.md).

## Close the menu

* Click the back button in the bottom-right corner, the same one the game's other title menus use.
* On a controller, press **B**.

While a dialog is open or you're typing in a box, the back button is hidden, and **B** closes the dialog or leaves the box instead. The keyboard's **Esc** and **E** never close the menu itself: they close a dialog, and **Esc** also stops typing.

Each time you open the menu, it starts fresh on the **Installed** tab, with searches and filters cleared. Browse keeps its results for 10 minutes, so going back to a recent search is quick.

## The tabs

The five tabs along the top are always in this order. Click one to switch, or press **LB** and **RB** on a controller (**L1** and **R1** on a Steam Deck).

| Tab | What it's for | Guide |
| --- | --- | --- |
| **Installed** | Turn mods on and off for your profile, find problems, update and uninstall mods. When updates are found, it reads **Installed (2 new)**, for example. | [Managing your mods](installed.md) |
| **Browse** | Search Nexus Mods and open a mod's page to install it. | [Browsing Nexus Mods](browse.md) |
| **Profiles** | Create, switch, copy, rename and delete profiles. | [Profiles](profiles.md) |
| **Downloads** | Follow downloads and installs. While some are running, it reads **Downloads (1)**, for example. | [Downloads](downloads.md) |
| **Account** | Log in to Nexus Mods, set up mod manager links (Windows only), and change settings. | [Connecting your Nexus account](../nexus-mods/connecting.md), [Settings](settings.md) |

## The bottom bar

The strip along the bottom says which profile is active and whether a restart would change anything.

The first line names the active profile, for example **Profile: Default**. That's the profile **Apply & Restart** loads. You change it on the [Profiles](profiles.md) tab.

The second line is the status. It's always one of these. When more than one is true, the bar shows the one highest in the table.

| Status | What it means |
| --- | --- |
| A red message that Stardrop's `Settings.json` or your profile file "couldn't be read" | DewDrop won't change or apply your profile, and **Apply & Restart** stays off. Close the game and start it again, or pick another profile. See [Troubleshooting](../help/troubleshooting.md). |
| "3 mods that are off in this profile loaded anyway. Restart to load this profile." | The game started on your whole `Mods` folder, for example from a shortcut, or from Steam without **Start in my profile from Steam**. That's normal. Click **Apply & Restart** when you want your profile. |
| "2 mods in this profile didn't load. Restart to load this profile." | The same kind of launch, and some mods that are on in your profile didn't load, such as ones you added after the game started. |
| "1 mod loaded from a different copy than this profile uses. Restart to load this profile." | The same kind of launch loaded another copy of a mod than your profile uses, such as your own copy instead of a Stardrop collection's. |
| "This session didn't start from a DewDrop profile. Restart to load this profile." | SMAPI was started with its `--mods-path` option on a folder DewDrop doesn't manage. |
| "*name*, which you uninstalled, is still loaded. Restart to finish uninstalling." | The game is still running a mod you uninstalled. With more than one, it says "2 mods you uninstalled are still loaded." See [Uninstall a mod](installed.md#uninstall-a-mod). |
| "Changes are ready. Restart the game to load them." | Since the game started, you turned mods on or off, installed or updated a mod, or switched profiles. |
| "Changes load the next time you start the game from Steam, or restart now." | The same, with **Start in my profile from Steam** on. Your changes load the next time you press **Play** in Steam. |
| "The mods loaded right now match this profile. Uninstalled mods are deleted when the game closes." | Nothing needs a restart. The mods you uninstalled aren't loaded, and their folders go when you quit. |
| "The mods loaded right now match this profile." | Nothing to do. |

The numbers are examples. The last two lines are gray, and all the others but the first are orange. While downloads are running, the line ends with how many, like "Downloading 2...". A line too long to fit ends in "...": point at it to read the rest.

**Apply & Restart** sits on the right. It lights up when restarting would change something. When it's grayed out, point at it to see why, for example "Nothing to apply: the loaded mods already match this profile." See [Apply & Restart](apply-and-restart.md).

{% hint style="info" %}
Your profile loads only when the game starts in it: after **Apply & Restart**, or from Steam's **Play** button with [**Start in my profile from Steam**](start-in-your-profile.md) on. Any other launch loads every mod in your `Mods` folder, including the ones you turned off. [How launching works](../getting-started/launching.md) explains why.
{% endhint %}

## Messages

Short messages pop up above the bottom bar, such as "Everything's up to date." Up to four show at a time, with the newest at the bottom.

* Normal messages go after about 5 seconds.
* Errors are red and stay for about 8 seconds. DewDrop also writes them to the SMAPI log, which helps when you [report a problem](../help/reporting-issues.md).
* Click a message to dismiss it early.

Messages only show while the menu is open, so the ones from the game's start wait until you first open it, and then stay a little longer (about 8 seconds, or 12 for errors). Examples are "Turned on *mods*, new in your Mods folder." and, on Windows, "Steam now starts the game in your profile."

## Dialogs and tooltips

Some actions ask first, such as **Apply & Restart**, uninstalling a mod, deleting a profile or logging out of Nexus Mods. The screen darkens and a box says what will happen.

* To go ahead, click the action button (such as **Restart** or **Delete**) or press **Enter**.
* To back out, click **Cancel**, click outside the box, or press **Esc**, **E** or controller **B**.

Point at almost anything to learn more. Buttons say what they do, and grayed-out buttons say why they're off. If a name or line is cut short, its tooltip shows all of it.

## Controls

The menu works with a mouse, a keyboard, a controller, or a mix.

{% tabs %}
{% tab title="Mouse" icon="computer-mouse" %}
* **Left-click** tabs, buttons, mod rows and cards.
* The **mouse wheel** scrolls the list under the pointer.
* Lists that don't fit have a scroll bar. Click its arrows, or drag the handle.
* Click a text box to type in it. Click anywhere else to stop typing.
* Right-click does nothing in the menu.
{% endtab %}

{% tab title="Keyboard" icon="keyboard" %}

| Key | What it does |
| --- | --- |
| **Esc** | Closes a dialog, or stops typing in a text box. |
| **E** | Closes a dialog. In a text box, it types an e as usual. |
| **Enter** | Confirms a dialog. In a text box, it searches on **Browse**, creates a profile on **Profiles**, and logs in on **Account**. |
| **Up** / **Down** | Scroll the list. On **Profiles**, they move the selection. On a mod's page, they scroll the description. |
| **Left** / **Right** | Go to the previous or next page of results on **Browse**. |
| **Backspace** | On a mod's page, goes back to your search results. |
| **Ctrl+V** | Pastes into a text box, such as your Nexus Mods API key. |

The arrow keys don't scroll while you're typing in a box. Press **Esc** to leave the box first.
{% endtab %}

{% tab title="Controller" icon="gamepad" %}

| Button | What it does |
| --- | --- |
| **Left stick** | Moves the cursor. It starts in the middle of the page. |
| **A** | Clicks whatever is under the cursor. |
| **LB** / **RB** | Switch to the previous or next tab, wrapping around at the ends. |
| **D-pad** | Works like the arrow keys: scrolls lists and changes pages on **Browse**. |
| **B** | Closes a dialog. Otherwise leaves the text box. Otherwise closes the menu. |

Selecting a text box opens the game's on-screen keyboard.

To go back from a mod's page, move the cursor to **Back** and press **A**. **B** closes the whole menu.
{% endtab %}

{% tab title="Steam Deck" icon="steam" %}
In Game Mode, the Deck's controls work like a controller (see the **Controller** tab), with **L1** and **R1** to switch tabs. Because Game Mode has no desktop, a few things change:

* **Typing.** Selecting a text box, by tapping, clicking or pressing **A**, opens Steam's on-screen keyboard, or the game's own if Steam's can't open.
* **Web pages.** Buttons such as **Open on Nexus** and **Get my API key** open the page in Steam's overlay browser over the game, or in Steam's browser when the overlay is off.
* **Open mods folder** on the **Downloads** tab is grayed out. It works in Desktop Mode.

Steam Deck support is new and being tested. See [DewDrop on the Steam Deck](../getting-started/steam-deck.md).
{% endtab %}
{% endtabs %}

## Good to know

* The menu is in English only for now.
* On small screens, such as 720p and the Steam Deck, the menu uses tighter spacing and slimmer rows so more fits.
* Downloads keep going if you load a save. Their progress is on the **Downloads** tab when you're back at the title screen.
* If the menu runs into an error, DewDrop closes the menu rather than the whole title screen, and writes the error to the SMAPI log. Open **Mods** again to carry on, and see [Reporting issues](../help/reporting-issues.md) if it keeps happening.
