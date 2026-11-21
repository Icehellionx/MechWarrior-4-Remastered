# MechWarrior 4 Remastered Steam Deck Installation Variant

This alpha packages the regular Windows Setup with an opt-in Steam launch script. It uses a separate Proton prefix and Wine’s built-in DirectDraw for gameplay. Start with Vengeance. The software-rendered lab reached the 1.0.2 Setup wizard under GE-Proton11-12. The earlier Vengeance intro and mission tests used Wine 11. SteamOS Game Mode, Deck controls, accelerated graphics, Black Knight and Mercenaries gameplay are open for feedback.

## Install in Desktop Mode

1. Extract the entire ZIP into `/home/deck/Games/MW4-Deck-Alpha`. In a terminal in that folder, run `sha256sum -c SHA256SUMS.txt` to verify the files.
2. Install the x86-64 build of [GE-Proton11-12](https://github.com/GloriousEggroll/proton-ge-custom/releases/tag/GE-Proton11-12) using the [upstream Steam installation instructions](https://github.com/GloriousEggroll/proton-ge-custom/blob/GE-Proton11-12/README.md#installation). Restart Steam.
3. Add `MechWarrior-4-Remastered-Setup.exe` as a non-Steam game. Under **Compatibility**, force GE-Proton11-12. Under **General → Launch Options**, paste:

   ```text
   bash "/home/deck/Games/MW4-Deck-Alpha/launch.sh" setup %command%
   ```

   Adjust the script path if you extracted the ZIP elsewhere. Keep `%command%` at the end.
4. Extract each original Vengeance disc into its own ordinary folder. In Setup, use **Add folder** for each disc separately. Use the folder picker for expansions and Mech Paks too. Select a destination of `C:\MechWarrior4-Deck-Alpha`. On Setup’s last page, clear the option to launch immediately.
5. In Steam, add the installed launcher as a second non-Steam game. Its default alpha path for the destination above is:

   ```text
   /home/deck/.local/share/mw4-remastered-deck-alpha/pfx/drive_c/MechWarrior4-Deck-Alpha/MW4RemasteredLauncher.exe
   ```

   Dolphin shows hidden folders with **Ctrl+H**. Force the same GE-Proton version on this shortcut and use these launch options:

   ```text
   bash "/home/deck/Games/MW4-Deck-Alpha/launch.sh" play %command%
   ```

## Play and report

Start the installed launcher in Desktop Mode for the first run. A mouse and keyboard or a Steam Input layout that maps to them can help navigate the Windows launcher and the game’s pilot-name prompt. Try Vengeance in Game Mode after a successful desktop launch.

In an [issue report](https://github.com/Icehellionx/MechWarrior-4-Remastered/issues/new), include the Deck model, SteamOS and Proton versions, game, media type, and last screen that worked. Say whether the original intro had picture and sound and whether controls worked in a mission. To collect a Proton log, add `--log` after `setup` or `play`; logs go in `~/.local/share/mw4-remastered-deck-alpha/logs`. Review logs for private paths and usernames before posting. Keep game media, installed game files and saves private.

The two shortcuts share the alpha prefix. Keep that directory if you want to retain the installed games, their registry settings and saves. Changes to the renderer apply only to the play shortcut. Use the normal Windows download for Windows.

## Build the alpha package

Run `python tools/steam-deck/build-alpha.py SETUP.exe SETUP_SHA256 MechWarrior-4-Steam-Deck-Alpha.zip` against the chosen released Setup and its published SHA-256. This packages the exact Setup bytes with this guide, the launch script and a checksum manifest. The original game discs are supplied by the user.
