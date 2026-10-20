DewDrop installer
=================

DewDrop is a mod manager that lives inside Stardew Valley: a Mods button on the title screen where you can
turn mods on and off, browse and install mods from Nexus Mods, and keep separate mod profiles.

This installer does the setup for you. It finds your game, installs SMAPI (the mod loader DewDrop needs) if
you don't have it yet, and puts DewDrop in your Mods folder.


How to install
--------------
On macOS or Linux, the quickest way is to skip this zip: open Terminal, paste this line and press Return.

  curl -fsSL https://raw.githubusercontent.com/friessssss/DewDrop/master/install.sh | bash

Otherwise, first extract this zip (right-click > Extract All on Windows; double-click on macOS). Then:

  Windows   Double-click "Install on Windows.bat".
            If Windows shows "Windows protected your PC", click "More info", then "Run anyway".

  macOS     Double-click "Install on macOS.command".
            If macOS says it can't be opened, click Done (not Move to Trash), then open
            System Settings > Privacy & Security, scroll down and click Open Anyway.
            (Or use the Terminal line above, which macOS doesn't block.)

  Linux     Double-click "Install on Linux.sh". If it opens in a text editor, right-click an empty spot in
            this folder, choose Open in Terminal, and run:  bash "Install on Linux.sh"

Answer the questions (pressing Enter picks the recommended answer), then start the game and click the new
Mods button on the title screen.

On Windows with the Steam version, the installer may also ask whether Steam should start the game in your
DewDrop profile. If you say yes, DewDrop finishes this the next time you quit the game: Steam closes and
opens again once. You can change it any time in the game, in Mods > Account > "Start in my profile from Steam".


Prefer to install by hand?
--------------------------
Download the plain "DewDrop-<version>.zip" instead and drag its DewDrop folder into your game's Mods folder.
You need SMAPI 4.0 or later: https://smapi.io


Help
----
Guide:   https://github.com/friessssss/DewDrop#readme
Issues:  https://github.com/friessssss/DewDrop/issues
Log:     the installer keeps a log, DewDrop-installer.log, in your temp folder. Attach it if you ask for help.
