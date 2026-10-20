---
description: "Keep different sets of mods turned on, and switch between them."
icon: users
---

# Profiles

A profile is a list of which of your installed mods are turned on. You can keep several, for example one for your main farm, one for co-op with friends and one with almost nothing turned on, and switch between them on the **Profiles** tab.

Profiles never move or delete mods. Every mod stays installed in your `Mods` folder. A profile only decides which of them load when the game starts in it, such as after **Apply & Restart**, or from Steam's **Play** button with [**Start in my profile from Steam**](start-in-your-profile.md) on.

<figure><img src="../.gitbook/assets/shot-profiles.png" alt="The Profiles tab of the DewDrop menu: a name box, a Create button and the list of profiles on the left, and the selected profile's buttons and mods on the right"><figcaption><p>The Profiles tab.</p></figcaption></figure>

## The Profiles tab

The left side has a **New profile name** box, a **Create** button and your profiles, with **Default** at the top and collection profiles at the bottom. A gold star marks the active profile, the one the bottom bar names after **Profile:**. Under each name you'll see how many mods it turns on, plus tags such as **active**, **protected** or **collection, read-only**.

Click a profile, or move through the list with **Up** and **Down**, to see it on the right: whether it's active, the buttons **Use this profile**, **Duplicate**, **Rename** and **Delete**, and the mods it turns on. That mod list is only for looking; you change a profile's mods on the [Installed tab](installed.md). If a button is grayed out, hover over it to see why.

## Make a new profile

1. Type a name in the **New profile name** box.
2. Click **Create**, or press **Enter**.
3. With the new profile selected, click **Use this profile**.
4. Go to the **Installed** tab and tick the mods you want.
5. Click [**Apply & Restart**](apply-and-restart.md) to load them.

A new profile starts with only DewDrop turned on, so the **Mods** button is still there after the restart.

{% hint style="info" %}
You can only change the mods of the active profile. To change a different profile, select it and click **Use this profile** first.
{% endhint %}

## Switch profiles

Select a profile and click **Use this profile**. DewDrop remembers your choice straight away, but the mods you're playing with don't change until you click **Apply & Restart**. A message tells you whether a restart is needed.

## Duplicate a profile

**Duplicate** makes a copy you can change without touching the original. It's also the way to get an editable version of a collection profile.

1. Select the profile you want to copy.
2. Type a name for the copy in the **New profile name** box, or leave the box empty to get a name like "Default copy".
3. Click **Duplicate**.

The copy is selected but not active. Click **Use this profile** to switch to it.

## Rename a profile

Select the profile, type the new name in the **New profile name** box, then click **Rename**. If you rename the active profile, it stays active.

You can't rename **Default**, collection profiles, or a profile whose file couldn't be read. Profile names become file names, so names that differ only in capital letters, or only in characters a file name can't use (such as `?` or `/`), count as the same name. A message tells you if a name is taken.

## Delete a profile

Select the profile, click **Delete**, then click **Delete** again in the dialog. You can't delete the active profile (switch to another one first), **Default**, or collection profiles.

{% hint style="danger" %}
Deleting a profile is permanent. It doesn't go to the Recycle Bin, and there's no undo. Only the profile's file is deleted: the mods themselves stay installed. Stardrop shares your profiles, so the profile is gone there too.
{% endhint %}

## The Default profile

If you don't have a **Default** profile yet, DewDrop creates one the first time it runs. It starts with the mods that loaded in that first session, so your first **Apply & Restart** doesn't turn anything off. See [Your first launch](../getting-started/first-launch.md).

**Default** is protected: you can't rename or delete it. You can still turn its mods on and off like any other profile.

## Collection profiles

When you install a Nexus Mods collection with Stardrop, Stardrop makes a profile for it. DewDrop lists these as **collection, read-only**. You can use them and **Apply & Restart** with them, but you can't change, rename or delete them in DewDrop. Use **Duplicate** to get a copy you can edit.

DewDrop can't install collections itself. See [Using DewDrop with Stardrop](../stardrop.md).

## DewDrop stays on in every profile

DewDrop keeps itself turned on in every profile, so the **Mods** button is always there after a restart. Its checkbox on the **Installed** tab can't be cleared. When the game starts, DewDrop adds itself to any editable profile that's missing it, including profiles you made in Stardrop. It also loads with collection profiles, which don't list it.

## Profiles are shared with Stardrop

Each profile is one `.json` file in Stardrop's data folder. On Windows that's `%AppData%\Stardrop\Data\Profiles`. DewDrop and Stardrop read the same files, so a profile you make or change in one shows up in the other. See [Where DewDrop keeps files](../reference/file-locations.md).

Notes you added to mods in Stardrop show in the tooltips of a profile's mod list. DewDrop can't edit them.

<details>

<summary>A profile says some mods are "not installed". What does that mean?</summary>

The profile turns on mods that aren't in your `Mods` folder, maybe because you deleted them or the profile came from someone else. They show in red in the profile's mod list. DewDrop skips them when it restarts the game, and the others load as normal. Install them again if you want them back.

</details>
