# MechWarrior 4 Remastered v0.6.45.9012

This update improves original-media intake, makes display options available in the AIO launcher, and prepares exact game-specific Windows Firewall decisions during installation. It remains a preservation-oriented build from user-supplied media; no game discs or extracted game assets are included.

- Settings now opens promptly as one dialog and offers game render size, resampling, anisotropic filtering, and antialiasing for all installed titles. Automatic chooses the largest listed game-safe 4:3 render size that fits the monitor. The game image remains centered at its original aspect ratio. Experimental 900p/1080p/4K-equivalent modes were withdrawn after Vengeance crashed on entering Training; they are not represented as supported.
- Setup accepts structurally validated single-track MODE1/2352 CUE/BIN pairs alongside supported ISOs and ZIPs. Different archival revisions still require exact game-file and official-patch qualification and may be rejected safely.
- Setup creates exact-executable inbound firewall rules before first launch: optional Private-network multiplayer access and a Public-network block. The launcher remains a standard, unelevated application.
- Installation diagnostics and media errors are clearer, and repeated launcher status refreshes no longer re-hash the shared Vengeance/Black Knight/Mech Pak tree four times.

The exact installer installed Vengeance, Black Knight, Mercenaries, and both Mech Paks from the five locally available ZIPs at `F:\Mechwarrior Test`. Each game reached its first menu without a Windows Security firewall prompt. The installer and installed F: tree passed Microsoft Defender custom scans with engine `1.1.26080.3` and intelligence `1.459.366.0`. Full mission testing of all new display choices, first-launch mouse placement across monitor setups, alternate physical disc revisions, private-access-declined firewall behavior, and uninstall remain open. Widescreen extension during gameplay is not included.

If the new version gives you trouble, please [log the problem in Issues](https://github.com/Icehellionx/MechWarrior-4-Remastered/issues/new?template=bug_report.yml) and use the [legacy v0.6.44 installer](https://github.com/Icehellionx/MechWarrior-4-Remastered/releases/download/v0.6.44/MechWarrior-4-Remastered-Setup-0.6.44.exe) for now. If the issue is specific to your ISO/ZIP/BIN revision, please reach out to me directly so I can work with you to obtain your version for testing. Do not post game media, serial keys, or personal paths in a public issue.

## Integrity

- File: `MechWarrior-4-Remastered-Setup-0.6.45.9012.exe`
- Size: `174,497,579` bytes
- SHA-256: `99308010a9f824c6c2642be1188ef62fd2e72a60f18b483d459945488b8ebdcd`

The installer is unsigned, so Windows may show an Unknown publisher or SmartScreen reputation warning. Verify the checksum; do not disable antivirus or SmartScreen globally.
