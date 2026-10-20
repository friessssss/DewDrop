---
description: "Search Nexus Mods for Stardew Valley mods, sort the results, see which ones you have, and open a mod's page, all from the title screen."
icon: magnifying-glass
---

# Browsing Nexus Mods

The **Browse** tab shows Stardew Valley mods from [Nexus Mods](https://www.nexusmods.com/stardewvalley), the site most Stardew mods are shared on. It opens on the most endorsed of the mods added in the last 30 days.

You don't need to log in to browse. To download and install mods, [connect your Nexus account](../nexus-mods/connecting.md) first.

<figure><img src="../.gitbook/assets/shot-browse.png" alt="DewDrop's Browse tab: a Search Nexus Mods box and Search button, sort buttons from Trending to New, and a grid of mod cards with pictures, names, authors and download counts"><figcaption><p>The Browse tab.</p></figcaption></figure>

## Search for a mod

1. Click the **Search Nexus Mods** box and type part of a mod's name.
2. Press **Enter** or click **Search**.

Browse searches when you ask it to, not while you type. It matches the mod's **name** only, not its author or description. A search for a single letter or character matches whole words.

To go back to the full list, clear the box and press **Enter** again.

## Sort the results

The buttons next to the search box change the order. The one in use is green. In a narrow window, some labels get shorter (**Top**, **Downloads**).

| Button | Shows first |
| --- | --- |
| **Trending** | The most endorsed of the mods added in the last 30 days. This is where Browse starts. |
| **Top rated** | The most endorsed mods of all time. |
| **Most downloaded** | The most downloaded mods of all time. |
| **Updated** | Recently updated mods. |
| **New** | The newest mods. |

Clicking a sort button also runs whatever is in the search box. With search text, **Trending** shows the most endorsed matches from any time, not only from the last 30 days, so the well-known mod you're looking for is near the top.

## Read a card

Each mod is a card with:

* its picture (or "No picture" if it has none),
* its name and author,
* a heart with its number of endorsements, then its download count,
* a tag if you already have it: a green **Installed**, or an orange **Update** when a newer version of your copy was found.

Point at a card to read the mod's short summary. If you have the mod, it also says which version, such as "Installed v1.2." or "Installed v1.2. Update available: v1.3". Click a card to open [the mod's page](mod-page.md), where you can read more and install it. Its **Back** button brings you back to the cards, on the same page and scroll position.

### How DewDrop knows you have a mod

A card gets its tag when any of these links one of your mods to that mod on Nexus Mods:

* **Its update key:** the Nexus update key in your copy's `manifest.json`, if its author added one.
* **DewDrop's install record:** DewDrop remembers which Nexus file it installed each mod from, in `mod-sources.json`. See [Where DewDrop keeps files](../reference/file-locations.md).
* **smapi.io's mod list:** during the [update check](updating-mods.md), DewDrop also asks smapi.io which Nexus mod each of your mods is. Its list covers code mods, such as the code part of Stardew Valley Expanded or Generic Mod Config Menu, but not content packs.

So every mod you install through DewDrop gets the tag. A content pack you installed yourself, without a Nexus update key, may not.

A mod you uninstall loses its tag straight away, before the game closes. See [Uninstall a mod](installed.md#uninstall-a-mod).

## Move between pages

Browse shows 24 mods per page. The strip under the cards shows how many results there are and which page you're on, for example **312 results for "farm"** and **Page 2 of 13**.

* Click the arrows on either side of the page number, or press **Left** and **Right** (the D-pad on a controller).
* Scroll with the mouse wheel, **Up** and **Down**, or the scroll bar when the cards don't all fit.

Browse keeps each page of results for 10 minutes, even if you close and reopen the menu, so going back to a page is instant.

## Loading and errors

While results load, the grid says **Loading...**. If Nexus Mods doesn't answer, a red message such as "Nexus Mods took too long to answer." appears, usually with a **Try again** button. Check your internet connection and click it.

If nothing matches, the grid says so, for example **No mods match "farm".** Try a shorter part of the name.

## Adult content

Mods that Nexus Mods marks as adult content are hidden. To include them, tick **Show adult content in Browse** in the **Account** tab > **Settings** panel. Browse reloads its results the next time you open the tab. See [Settings](settings.md).

## What Browse can't do yet

* Filter by category, or search by author or description.
* Install Nexus collections. Collections install through [Stardrop](../stardrop.md).
