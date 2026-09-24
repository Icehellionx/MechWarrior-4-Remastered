# MechWarrior 4 Remastered v0.6.46

This update targets the reported ultrawide edge crop and Mercenaries mouse capture conflict. It still builds the games from media you provide; no game discs or extracted game assets are included.

- The launcher fits the game's **client area** to the current monitor. Some framed game windows have an outer frame larger than the display; fitting that outer frame had reduced the playable client area and could clip its edges.
- The launcher now leaves a game or dgVoodoo cursor clip alone. It no longer reapplies its own clip every 25 ms over the game's capture and releases only a clip it actually owns.
- The gameplay-only top/left seam trim is reduced from five to two source pixels. Menus and cinematics use the unchanged 4:3 path.
- Display Settings includes layout previews for standard 16:9 screens (1600×900, 1920×1080, 2560×1440, 3840×2160), 16:10 screens (1920×1200, 2560×1600), and ultrawide screens (2560×1080, 3440×1440, 5120×2160). Each shows the centered 4:3 picture without technical pillarbox measurements. The game uses your **current Windows monitor mode** and automatically picks a game-safe 4:3 render size at launch. Preview choices do not switch resolution. This release does not extend the 3D world into the pillarboxes.

The real lifecycle guard passed framed and borderless window component tests and cursor-clip ownership tests. Standard 1080p and ultrawide 1440p Settings previews were rendered and checked locally. Two declared-source setup builds were byte-identical. The exact setup, staged payload, and installed tree passed Defender scans; an in-place C: install from all five supplied ZIPs verified all three games and both Mech Paks, and the installed launcher displayed them as Ready. The user reports no drift in a local mission test of the corrected launcher. The affected 5120×2160 display and Windows 10/RX 6750 XT mouse configuration are unavailable locally, so [ultrawide issue #7](https://github.com/Icehellionx/MechWarrior-4-Remastered/issues/7) and [mouse issue #8](https://github.com/Icehellionx/MechWarrior-4-Remastered/issues/8) remain open for those configurations.

If a reinstall has trouble in a previously used folder, first back up saves/settings you want to keep, uninstall the old copy, delete the leftover installation folder, and retry. Please [report new-version trouble in Issues](https://github.com/Icehellionx/MechWarrior-4-Remastered/issues/new?template=bug_report.yml) and use the [legacy v0.6.45.9012 installer](https://github.com/Icehellionx/MechWarrior-4-Remastered/releases/download/v0.6.45.9012/MechWarrior-4-Remastered-Setup-0.6.45.9012.exe) meanwhile. If a particular ISO/ZIP/BIN revision fails, please reach out directly so I can work with you to obtain that revision for testing. Do not post game media, serial keys, or personal paths in a public issue.

## Integrity

- File: `MechWarrior-4-Remastered-Setup-0.6.46.exe`
- Size: `174,480,354` bytes
- SHA-256: `469d3b7e0c451f81697f692484a075906141efbc602c45625ed33f93d77d2e02`

The installer is unsigned, so Windows may show an Unknown publisher or SmartScreen reputation warning. Verify the checksum; do not disable antivirus or SmartScreen globally.
