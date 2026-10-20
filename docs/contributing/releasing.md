---
description: "How a DewDrop release happens: bump the version on development, merge development into master, and CI publishes it."
icon: tag
---

# Releasing

DewDrop has two long-lived branches:

* **`development`** is where work happens. Every change reaches it through a pull request.
* **`master`** holds what's been released. It only ever gets `development`, through a pull request, and each merge into it publishes a release.

GitHub enforces this: neither branch takes direct pushes, and the checks below have to pass before a pull request can merge.

## Make a release

{% stepper %}
{% step %}
### Bump the version on development

On a branch from `development`, set the new version:

```bash
node tools/bump-version.mjs minor
```

Use `patch` for fixes (0.2.0 to 0.2.1), `minor` for new features (0.2.1 to 0.3.0) and `major` for big changes (0.3.0 to 1.0.0). You can also give an exact version, such as `1.0.0-beta.1`. The script changes `DewDrop.Mod/manifest.json` and `DewDrop.Mod/DewDrop.Mod.csproj` together, and refuses a version that isn't higher than the current one.

Open a pull request into `development` and merge it like any other.
{% endstep %}

{% step %}
### Open a pull request from development into master

Its title becomes the release's merge commit, so something like "Release 0.3.0" works well. Two checks run on it besides the usual CI:

* **`version`** (`.github/workflows/release-check.yml`): the pull request has to come from this repository's `development`, and the version in both files has to match and be higher than every release so far.
* **`build-mod`**: the mod builds and packages exactly as the release will.
{% endstep %}

{% step %}
### Merge it with a merge commit

`master` only takes merge commits, so it keeps `development`'s exact history and the two never drift apart.

The **Release** workflow (`.github/workflows/release.yml`) then builds the mod, packages both zips with `tools/package-release.mjs`, tags the merge with the version (for example `0.3.0`), and publishes a GitHub release with both zips and notes listing the pull requests since the last release. You can edit the notes on GitHub afterwards.
{% endstep %}
{% endstepper %}

A version with a tag such as `-beta.1` is published as a **pre-release**. GitHub's "latest release" skips pre-releases, so the installer's download and the update check keep offering the last normal release.

{% hint style="info" %}
The release is tagged with the bare version, not `v0.3.0`. DewDrop's manifest has the update key `GitHub:friessssss/DewDrop`, and SMAPI's update check (which DewDrop uses too) compares that tag with each player's installed version. See [Updating DewDrop](../getting-started/updating-dewdrop.md).
{% endhint %}

## How CI builds the mod without the game

`DewDrop.Mod` builds against the game's and SMAPI's DLLs, and CI runners have no game install. So CI checks out [StardewModders/mod-reference-assemblies](https://github.com/StardewModders/mod-reference-assemblies): metadata-only copies of those DLLs, with the types and methods but none of the code, made for building mods in CI. It passes the folder to the packaging script:

```bash
node tools/package-release.mjs --game-path=.game-refs
```

The reference assemblies are pinned to one commit in both `build.yml` and `release.yml`. A DewDrop built against them is the same as one built against the game, except that it records the SMAPI version of the reference assemblies (4.4.0 at the time of writing) instead of yours. SMAPI loads mods built against older versions, so that doesn't matter to players.

When a game or SMAPI update adds something DewDrop uses, move the pin forward: find the newest commit of that repository and change the `ref:` in both workflows to it.

`build-mod` runs on every pull request and every push to `development`, and attaches the two zips to the run as **DewDrop-zips**. Download them from the run's summary page to try a pull request's build without building it yourself.

## If something goes wrong

* **The `version` check fails.** Its message says why: the two files disagree, the version is already released, or it isn't higher than the latest release. Fix it with `node tools/bump-version.mjs` in a pull request into `development`; the pull request into `master` picks the change up.
* **The Release workflow fails after the merge.** Nothing is published until its last step. If the failure was a hiccup (a download timed out, say), open the failed run in the **Actions** tab and click **Re-run jobs**. Otherwise fix the problem on `development` and release again with a new pull request. If a tag or a half-made release was left behind, delete both on GitHub first, or the next run stops because the version is already released.
* **You need to package by hand,** for example to test the installer: see [Packaging a release](building.md#packaging-a-release).
