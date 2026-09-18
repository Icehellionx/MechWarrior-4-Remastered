# Security policy

## Supported version

Security fixes target the latest release listed on the repository's Releases page.

## Reporting a vulnerability

Please use GitHub's private vulnerability reporting feature when available. If it is unavailable, open an issue that contains no exploit details or sensitive data and ask the maintainer for a private contact route.

Do not attach game media, serial keys, credentials, memory dumps, private filesystem paths, or unrelated logs.

## Installer safety

- The installer never asks users to disable antivirus, SmartScreen, or Windows security features.
- Original media is validated before product-specific transforms run.
- Media mounting, update application, executable transformation, registry changes, firewall rules, and uninstall are bounded and ownership-checked.
- Release packaging uses allowlists, pinned hashes, reproducible builds where practical, and a current Microsoft Defender scan.
- An unsigned SmartScreen reputation warning is not the same as a malware detection. Verify the published SHA-256 digest before running the installer.
