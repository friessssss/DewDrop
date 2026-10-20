---
description: "Search Nexus Mods for Stardew Valley mods, sort the results and open a mod's page, all from the title screen."
icon: magnifying-glass
---

# Browsing Nexus Mods

The **Browse** tab shows Stardew Valley mods from [Nexus Mods](https://www.nexusmods.com/stardewvalley), the site most Stardew mods are shared on. When you open it, it lists the most endorsed of the mods added in the last 30 days.

You don't need to log in to browse. To download and install mods, [connect your Nexus account](../nexus-mods/connecting.md) first.

<figure><img src="../.gitbook/assets/shot-browse.png" alt="DewDrop's Browse tab: a Search Nexus Mods box and Search button, sort buttons from Trending to New, and a grid of mod cards with pictures, names, authors and download counts"><figcaption><p>The Browse tab.</p></figcaption></figure>

## Search for a mod

1. Click the **Search Nexus Mods** box and type part of a mod's name.
2. Press **Enter** or click **Search**.

Browse searches when you ask it to, not while you type. It matches the mod's **name** only, not its author or description. A search of a single letter or character matches whole words.

To go back to the full list, clear the box and press **Enter** again.

## Sort the results

The buttons next to the search box change the order. The one in use is green. On a narrow window, some labels get shorter (**Top**, **Downloads**).

| Button | Shows first |
| --- | --- |
| **Trending** | The most endorsed of the mods added in the last 30 days. This is where Browse starts. |
| **Top rated** | The most endorsed mods of all time. |
| **Most downloaded** | The most downloaded mods of all time. |
| **Updated** | Recently updated mods. |
| **New** | The newest mods. |

Clicking a sort button also runs whatever is in the search box.

With search text, **Trending** shows the most endorsed matches from any time, not only mods added in the last 30 days. That way the well-known mod you're looking for is near the top.

## Read a card

Each mod is a card with:

* its picture (or "No picture" if it has none),
* its name and author,
* a heart with its number of endorsements, then its download count,
* a green **Installed** tag if you already have it.

Hover over a card to read the mod's short summary. Click a card to open [the mod's page](mod-page.md), where you can read more and install it.

The **Installed** tag relies on the update keys in each of your mods' `manifest.json` files. A mod you have might not show the tag if its author didn't add a Nexus update key.

## Move between pages

Browse shows 24 mods per page. The strip under the cards shows how many results there are and which page you're on, for example **312 results for "farm"** and **Page 2 of 13**.

* Click the arrows on either side of the page number.
* Or press **Left** and **Right** on your keyboard, or on the controller's D-pad.
* Scroll with the mouse wheel, **Up** and **Down**, or the scroll bar when cards don't all fit.

Browse remembers each page of results for 10 minutes, even if you close and reopen the menu, so going back to a page is instant.

## Loading and errors

While results load, the grid says **Loading...**. If Nexus Mods doesn't answer, you'll see a red message such as "Nexus Mods took too long to answer.", usually with a **Try again** button. Check your internet connection and click it.

If nothing matches, the grid says so, for example **No mods match "farm".** Try a shorter part of the name.

## Adult content

Mods that Nexus Mods marks as adult content are hidden. To include them, turn on **Show adult content in Browse** in the **Account** tab > **Settings** panel. Browse reloads when you change it. See [Settings](settings.md).

## What Browse can't do yet

* Filter by category, or search by author or description.
* Install Nexus collections. Collections install through [Stardrop](../stardrop.md).

{% hint style="info" %}
If you switch tabs while a mod's page is open, it's still there when you come back to **Browse**. Click **Back** to return to the cards, which keep your page and scroll position.
{% endhint %}
