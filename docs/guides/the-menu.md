---
description: "Open and close the DewDrop menu, find your way around its tabs and bottom bar, and use it with a mouse, keyboard or controller."
icon: window-maximize
---

# The DewDrop menu

Everything DewDrop does happens in one menu that you open from Stardew Valley's title screen. This page covers the parts every tab shares: the tabs, the bottom bar, messages, dialogs and controls.

## Opening the menu

On the title screen, click **Mods**. It sits between **Co-op** and **Exit**, in the same wooden style as the other buttons. The title art slides away, the same way it does for **Load**, and the menu opens on the **Installed** tab.

If your game window is too narrow for five big buttons, DewDrop puts a small dew-drop button above the small buttons in the bottom-right corner instead. It opens the same menu.

If a **Load your profile?** box is showing over the title screen, answer it first. Nothing on the title screen can be clicked while it's open. See [Start in your profile](start-in-your-profile.md).

<figure><img src="../.gitbook/assets/shot-title.png" alt="The Stardew Valley title screen with DewDrop's wooden Mods button, marked with a dew drop on a leaf, between the Co-op and Exit buttons"><figcaption><p>The Mods button on the title screen.</p></figcaption></figure>

{% hint style="info" %}
The menu only opens from the title screen. There's no hotkey for it once a save is loaded, so to change your mods in the middle of a game, exit to the title screen first.
{% endhint %}

## Closing the menu

Either of these takes you back to the title screen:

* Click the title screen's back button in the bottom-right corner, the same one the game's other title menus use.
* On a controller, press **B**.

The keyboard's menu keys, **Esc** and **E**, don't close the menu itself. They close a dialog, and **Esc** also stops typing in a text box.

While a dialog is showing or you're typing in a box, the back button is hidden, and controller **B** closes the dialog or leaves the box instead of closing the menu. Close the dialog or leave the box first, then close the menu.

To get from a mod's page back to your search results, click **Back** or press **Backspace**. See [A mod's page](mod-page.md).

Each time you open the menu, it starts fresh on the **Installed** tab, so searches and filters are reset. Browse remembers its results for 10 minutes, so going back to a search you just ran is quick.

## The tabs

The five tabs along the top are always in this order. Click one to switch, or press **LB** and **RB** on a controller.

| Tab | What it's for | Guide |
| --- | --- | --- |
| **Installed** | Turn mods on and off for your profile, find mods that need something, and check for updates. When updates are found, the tab reads **Installed (2 new)**, for example. | [Managing your mods](installed.md) |
| **Browse** | Search Nexus Mods and open a mod's page to install it. | [Browsing Nexus Mods](browse.md) |
| **Profiles** | Create, switch, copy, rename and delete profiles. | [Profiles](profiles.md) |
| **Downloads** | Watch downloads and installs. While some are running, the tab shows how many, such as **Downloads (1)**. | [Downloads](downloads.md) |
| **Account** | Log in to Nexus Mods, set up mod manager links (Windows), and change settings. | [Connecting your Nexus account](../nexus-mods/connecting.md), [Settings](settings.md) |

## The bottom bar

The strip along the bottom of the menu tells you which profile is active and whether a restart is needed.

The first line shows the active profile, for example **Profile: Default**. This is the profile **Apply & Restart** uses. You change it on the [Profiles](profiles.md) tab.

The line under it is the status. It's always one of these:

| Status | What it means |
| --- | --- |
| A red message saying Stardrop's `Settings.json` or a profile file "couldn't be read" | DewDrop couldn't read one of its files, so it won't change or apply your profile. **Apply & Restart** stays off. Close the game and start it again, or for a profile file, pick another profile. If it keeps happening, see [Troubleshooting](../help/troubleshooting.md). |
| "3 mods that are off in this profile loaded anyway. Restart to load this profile." (orange) | A normal launch: you started the game from a shortcut, from SMAPI, or from Steam without **Start in my profile from Steam**, so SMAPI loaded everything in your `Mods` folder, including mods that are off in your profile. That's normal. Click **Apply & Restart** when you want to switch into your profile. |
| "2 mods in this profile didn't load. Restart to load this profile." (orange) | A normal launch that didn't load some mods that are on in your profile, for example ones you added after the game started. **Apply & Restart** loads them. |
| "1 mod loaded from a different copy than this profile uses. Restart to load this profile." (orange) | A normal launch loaded a different copy of a mod than the one your profile uses, such as your own copy instead of a Stardrop collection's. **Apply & Restart** switches to the right one. |
| "This session didn't start from a DewDrop profile. Restart to load this profile." (orange) | SMAPI was started with its `--mods-path` launch option on a folder DewDrop doesn't manage. Click **Apply & Restart** when you want to switch into your profile. |
| "Changes are ready. Restart the game to load them." (orange) | Since this session started, you turned mods on or off, installed or updated a mod, or switched profiles. |
| "Changes load the next time you start the game from Steam, or restart now." (orange) | The same, with **Start in my profile from Steam** on. DewDrop links your changes when you quit the game, so they load the next time you press **Play** in Steam. **Apply & Restart** loads them now. |
| "The mods loaded right now match this profile." (gray) | Nothing to do. What's loaded is exactly your profile. |

The numbers are examples. If more than one of the three normal-launch lines is true, the bar shows the one highest in the table.

While downloads are running, the status also says how many, like **Downloading 2...**. If a line is too long to fit, it ends with "...". Hover over it to read the whole thing.

**Apply & Restart** sits on the right. It lights up when restarting would change something. Click it, confirm, and the game closes and starts again with your profile's mods. When it's grayed out, hover over it to see why, for example "Nothing to apply: the loaded mods already match this profile." [Apply & Restart](apply-and-restart.md) explains what happens in detail.

{% hint style="info" %}
Your profile applies when the game starts in it: after **Apply & Restart**, or, on Windows with the Steam version, from Steam's **Play** button once **Start in my profile from Steam** is on. Any other launch loads every mod in your `Mods` folder, including ones you turned off in DewDrop. [How launching works](../getting-started/launching.md) explains why. When the mods that load differ from your profile, DewDrop offers on the title screen to restart into it. See [Start in your profile](start-in-your-profile.md).
{% endhint %}

## Messages

Short messages pop up at the bottom of the page, just above the bottom bar, for example "Everything's up to date." Up to four show at a time, with the newest at the bottom.

* Normal messages disappear after about 5 seconds.
* Errors are red and stay for about 8 seconds. DewDrop also writes them to the SMAPI log, which helps when you [report a problem](../help/reporting-issues.md).
* Click a message to dismiss it early.

Messages only show while the menu is open. A few come from the game starting, such as "Steam now starts the game in your profile." or "Turned on *mods*, new in your Mods folder." Those wait until you next open the menu.

## Dialogs

Some actions ask before they go ahead, such as **Apply & Restart**, deleting a profile, or logging out of Nexus Mods. The screen darkens and a box explains what will happen.

* To go ahead, click the action button (like **Restart** or **Delete**) or press **Enter**.
* To back out, click **Cancel**, click anywhere outside the box, or press **Esc**, **E** or controller **B**.

## Tooltips

Hover over almost anything to learn more. Buttons say what they do, mod rows show a mod's description, unique ID and folder, and grayed-out buttons say why they're off. If a name or line is cut short, its tooltip shows all of it.

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
| **E** | Closes a dialog. While you're typing in a box, it types the letter e as usual. |
| **Enter** | Confirms a dialog. In a text box, it runs the box: searches on **Browse**, creates a profile on **Profiles**, and logs in on **Account**. |
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
| **LB** / **RB** | Switch to the previous or next tab. They wrap around from the last tab to the first. |
| **D-pad** | Works like the arrow keys: scroll lists, change pages on **Browse**. |
| **B** | Closes a dialog. Otherwise leaves the text box. Otherwise closes the menu. |

Selecting a text box with the controller opens the game's on-screen keyboard.

To go back from a mod's page, move the cursor to **Back** and press **A**. **B** closes the whole menu.
{% endtab %}
{% endtabs %}

## Good to know

* The menu is in English only for now.
* On small screens, such as 720p, the menu uses tighter spacing and slimmer rows so more fits.
* Downloads keep going if you load a save. Their progress is on the **Downloads** tab when you're back at the title screen.
* If the menu ever runs into an error, DewDrop closes it rather than the whole title screen, and writes the error to the SMAPI log. Open **Mods** again to carry on, and see [Reporting issues](../help/reporting-issues.md) if it keeps happening.
