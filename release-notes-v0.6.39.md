# MechWarrior 4 Remastered v0.6.39

The first public candidate combines Vengeance, Black Knight, Mercenaries, the Inner Sphere Mech Pak, and the Clan Mech Pak behind one media-first installer and launcher.

## Highlights

- Installs supported games from user-supplied original ISOs or ZIP archives.
- Preserves strict 4:3 geometry in a borderless full-screen window without changing the physical desktop resolution.
- Uses the largest monitor-fitting 4:3 internal render resolution with 4× MSAA, 16× anisotropic filtering, 32-bit color, and Ultra High-equivalent game detail defaults.
- Removes the dgVoodoo watermark and gameplay-only top/left presentation seam.
- Keeps the picture visible and stable during Alt-Tab while releasing and recapturing the cursor cleanly.
- Applies a proportional widescreen treatment only to the live-action portion of Vengeance's opening; menus, gameplay, and every other video retain their original aspect ratio.
- Enables validated Inner Sphere and Clan Mech Pak chassis, stock loadouts, weapons, and subsystems while preserving the original model loader.
- Includes cleaned manuals and cover art for all three games.
- Creates a desktop shortcut by default and provides ownership-safe uninstall behavior.

## Verification

- Setup size: `174,447,393` bytes.
- SHA-256: `97f893791c0d68329c2a930a91c5248053c05c18b8e9b410100b6d9118cd7e7d`.
- Two clean package builds were byte-identical.
- Core smoke and all self-contained contract suites passed.
- The exact setup produced no new Microsoft Defender detection with engine `1.1.26080.3` and intelligence `1.459.273.0`.

## Known limitations

- The installer is unsigned and may display an Unknown publisher or SmartScreen reputation warning.
- Broader GPU, multi-monitor, regional-media, campaign, and multiplayer coverage remains welcome.
- Unrecognized or modified media fails closed.
