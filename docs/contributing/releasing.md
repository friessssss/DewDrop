---
description: "How a DewDrop release goes from a version bump on development to a published release, and the plan for 0.2.0."
icon: tag
---

# Releasing

DewDrop has two long-lived branches:

* **`development`** is where work happens. Every change reaches it through a pull request, which is squashed into one commit when it merges.
* **`master`** holds what's been released. It only ever gets `development`, through a pull request, and each merge into it publishes a release.

GitHub enforces this: neither branch takes direct pushes, and a pull request can only merge once its checks pass and its review conversations are resolved. Pull requests into `development` also need an approving review.

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

Give it a title like "Release 0.3.0". The title shows in the merge commit and in the release notes. Besides the usual CI, two checks have to pass:

* **`version`** (`.github/workflows/release-check.yml`): the pull request has to come from this repository's `development`, and the version in both files has to match and be higher than every release so far.
* **`build-mod`**: the mod builds and packages exactly as the release will.
{% endstep %}

{% step %}
### Merge it with a merge commit

`master` only takes merge commits, so it keeps `development`'s exact history and the two never drift apart.

The **Release** workflow (`.github/workflows/release.yml`) then builds the mod, packages both zips with `tools/package-release.mjs`, tags the merge with the version (for example `0.3.0`), and publishes a GitHub release named "DewDrop 0.3.0" with both zips.
{% endstep %}
{% endstepper %}

The release notes list the titles of the pull requests merged since the last release. There's no changelog file, so open the release on GitHub afterwards and add a few lines at the top that tell players what's new.

A version with a tag such as `-beta.1` is published as a **pre-release**. GitHub's "latest release" skips pre-releases, so the installer's download and the update check keep offering the last normal release.

{% hint style="info" %}
The release is tagged with the bare version, not `v0.3.0`. DewDrop's manifest has the update key `GitHub:friessssss/DewDrop`, and SMAPI's update check (which DewDrop uses too) compares that tag with each player's installed version. See [Updating DewDrop](../getting-started/updating-dewdrop.md).
{% endhint %}

### What a release changes for players

* **The one-line installers.** `install.ps1` and `install.sh` in the repository root are read from `master`, and they download the installer scripts from `master` and DewDrop from the latest release. So the merge updates them for everyone straight away.
* **The guide does not wait.** GitBook publishes the `docs/` folder from `development` (see [Docs and art style](docs-and-art.md#where-the-docs-live)), so the guide describes features as soon as they merge, before they're released. Release soon after a feature merges, or players read about something they can't get yet.

## The next release

DewDrop 0.1.0 is the latest release. Everything merged since is waiting for 0.2.0: the one-line installers, Rosetta on Apple Silicon Macs and uninstalling mods. Steam Deck support is still an open pull request. [What's in progress](README.md#whats-in-progress) lists each pull request and where it stands.

{% hint style="warning" %}
The guide already tells players to paste the one-line installers, but `master` (0.1.0) doesn't have `install.ps1` or `install.sh` in its root yet. Both one-liners fail with a 404 error until 0.2.0 is released.
{% endhint %}

Release 0.2.0 in this order:

1. **Finish Steam Deck support** ([#11](https://github.com/friessssss/DewDrop/pull/11)). It needs a review and the on-device checks in [Testing on a Steam Deck](steam-deck-testing.md). Then merge it into `development`. If 0.2.0 goes out without it, its docs stay on its branch, so the guide doesn't promise Steam Deck support either.
2. **Bump the version** to 0.2.0 in a pull request into `development`:

   ```bash
   node tools/bump-version.mjs minor
   ```
3. **Release:** open the pull request from `development` into `master`, titled "Release 0.2.0", and merge it with a merge commit, as in [Make a release](#make-a-release).
4. **Check it worked.** Both one-liner addresses should now answer with `200` instead of `404`. GitHub caches these files for a few minutes, so try again shortly if you still get `404`:

   ```bash
   curl -sI https://raw.githubusercontent.com/friessssss/DewDrop/master/install.sh | head -n 1
   curl -sI https://raw.githubusercontent.com/friessssss/DewDrop/master/install.ps1 | head -n 1
   ```

   Then add a short summary of what's new to the top of the release notes.
5. **Update the pages that say 0.2.0 isn't out yet,** in a pull request into `development`:
   * `README.md`: under **What's new in 0.2.0**, the "isn't released yet" paragraph, the **Status** column and the "Still to do" line; under **Install**, the note about the 404 error.
   * `docs/README.md`: the hint under **What's new in 0.2.0**.
   * `docs/help/troubleshooting.md`: the "404" entry under **The installer fails to start**, and its line in the list at the top.
   * `docs/contributing/README.md` (**What's in progress**) and this page's **The next release**.

## How CI builds the mod without the game

`DewDrop.Mod` builds against the game's and SMAPI's DLLs, and CI runners have no game install. So CI checks out [StardewModders/mod-reference-assemblies](https://github.com/StardewModders/mod-reference-assemblies): metadata-only copies of those DLLs, with the types and methods but none of the code, made for building mods in CI. It passes the folder to the packaging script:

```bash
node tools/package-release.mjs --game-path=.game-refs
```

The reference assemblies are pinned to one commit in both `build.yml` and `release.yml`. A DewDrop built against them is the same as one built against the game, except that it records the SMAPI version of the reference assemblies (4.4.0 at the time of writing) instead of yours. SMAPI loads mods built against older versions, so that doesn't matter to players.

When a game or SMAPI update adds something DewDrop uses, move the pin forward: find the newest commit of that repository and change the `ref:` in both workflows to it.

`build-mod` runs on every pull request and every push to `development`, and attaches the two zips to the run as **DewDrop-zips**. Download them from the run's summary page to try a pull request's build without building it yourself. You need to be signed in to GitHub to download them.

## If something goes wrong

* **The `version` check fails.** Its message says why: the two files disagree, the version is already released, or it isn't higher than the latest release. Fix it with `node tools/bump-version.mjs` in a pull request into `development`. The pull request into `master` picks the change up.
* **The Release workflow fails after the merge.** Nothing is published until its last step. If the failure was a hiccup (a download timed out, say), open the failed run in the **Actions** tab and click **Re-run jobs**. Otherwise fix the problem on `development` and release again with a new pull request. If a tag or a half-made release was left behind, delete both on GitHub first, or the next run stops because the version is already released.
* **You need to package by hand,** for example to test the installer: see [Packaging a release](building.md#packaging-a-release).
