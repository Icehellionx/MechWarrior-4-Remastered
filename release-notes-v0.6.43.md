# MechWarrior 4 Remastered v0.6.43

This release carries forward the field-confirmed installer, launcher, presentation, Alt-Tab, gameplay-seam, and Mech Pak fixes, and corrects the final high-resolution graphics configuration found during local release testing.

## Graphics and presentation

- Keeps the known physics-safe 30 FPS cap in both the wrapper and game launch configuration.
- Explicitly enables 4x MSAA and 16x anisotropic filtering.
- Uses Lanczos-3 final-image resampling.
- Chooses the largest fitting 4:3 render mode for the user's monitor and now advertises explicit high-resolution 4:3 fallbacks, including 1920x1440, so the requested mode is available to the original game.
- Preserves aspect-correct borderless presentation, black bars where required, cursor capture, Alt-Tab behavior, and the gameplay-only top/left seam correction.

The exact installer uploaded with this release was installed and confirmed running correctly from a local copy before publication. A wider range of GPUs, monitor layouts, and archival media variants remains welcome test coverage.

## Integrity

- File: `MechWarrior-4-Remastered-Setup-0.6.43.exe`
- Size: `174,462,561` bytes
- SHA-256: `860abdfae1281e99209f0a515838eed19fe220dcf4e1a38c4122ce37056edf87`
- Defender: no threat detected with engine `1.1.26080.3`, intelligence `1.459.273.0`

The installer is unsigned, so Windows may show an Unknown publisher or SmartScreen reputation warning. Verify the checksum; do not disable antivirus or SmartScreen globally.

The installer does not include the games. You must provide supported original ISO files or archival ZIPs for the games and optional Mech Paks you own. Manuals are included in the installed launcher.
