# MechWarrior 4 Remastered v0.6.44

This release fixes installation from a normal double-click. Users no longer need to know that Setup must be manually started with **Run as administrator**.

## Installation fix

- Setup explicitly requests Windows elevation for its hidden original-media installation worker.
- Windows automatically presents the normal User Account Control prompt when required.
- The worker independently verifies its administrator token and fails immediately with a clear diagnostic if elevation is ever bypassed.
- The launcher and all three games continue to run normally without administrator privileges.

The exact installer uploaded with this release was launched normally—without manually selecting **Run as administrator**—and completed Vengeance, Black Knight, and both Mech Paks through final verification. The user also installed and confirmed this exact local artifact before publication.

## Integrity

- File: `MechWarrior-4-Remastered-Setup-0.6.44.exe`
- Size: `174,471,026` bytes
- SHA-256: `a3fdde1fbe6265c51c9a15258ea8ec79272a8e6430b9585bb6257275f5fed786`
- Defender: no threat detected with engine `1.1.26080.3`, intelligence `1.459.273.0`

The installer is unsigned, so Windows may show an Unknown publisher or SmartScreen reputation warning. Verify the checksum; do not disable antivirus or SmartScreen globally.

The installer does not include the games. You must provide supported original ISO files or archival ZIPs for the games and optional Mech Paks you own. Manuals are included in the installed launcher.
