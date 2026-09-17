# Releases and operational checkpoints

## Current baseline

- Public release: none.
- Product source/build: internal candidate `0.6.7` compiles with the corrected page lifecycle, verified Black Knight PR1 runtime, exact setup-accepted EULA transform, consumed-registration validation, embedded exact official Mercenaries PR1 payload, setup foreground recovery, and repair-tolerant shell uninstall. It still requires elevated packaged ISO/ZIP, focus, uninstall, and actual menu/gameplay acceptance before distribution. Internal candidates through `0.6.6` are superseded and must not be distributed.
- GitHub remote: `https://github.com/Icehellionx/MechWarrior-4-Remastered`; `main` is the initial development branch.
- Media support: all seven supplied ISO layouts are structurally recognized. Fresh Vengeance Patch 3/Inner Sphere/Black Knight PR1 coordinator output exact-verified; Vengeance and Black Knight each produced their correctly titled windows when launched with installer-owned 32-bit registration. A fresh Mercenaries PR1 worker run committed and exact-verified 211 files, passed Defender, produced one responsive correctly titled window with no visible legacy error dialog, and passed ownership-safe removal with save/settings preservation. The elevated packaged ISO/ZIP path and actual menus/gameplay remain open.
- Staged AIO integration: the exact `0.6.4` packaged payload's self-contained worker installed all three titles together from extracted original-media copies plus the original PR1 ZIP. Launcher status returned three Ready games, three present manuals, Inner Sphere Ready, Clan Missing, and three present media icons. The launcher stayed responsive and the complete staged application/game tree passed Defender; ownership-safe cleanup removed both game trees and their matching registrations while retaining the shell. Pixel-level launcher acceptance remains open because native screenshot automation was unavailable.
- Compatibility: Black Knight's invalid mapped-image/static-PE experiment is removed. Official PR1 uses a hosted source-built app-local DLL with no injector/helper/UAC. The unpatched retail route remains unsupported and is rejected until its Vengeance Patch 2 prerequisite is implemented.
- Packaging: pinned Inno Setup 7.1.0 built internal `0.6.7` (171,556,050 bytes) SHA-256 `c9a3ff437987f737d822382c78eec20b13595104d1820ab70216a47dfdf77e09`. It remains internal until the complete elevated media-first setup, foreground behavior, repair/unified uninstall, and actual-menu three-game acceptance path passes. Bundled official-update redistribution rights also remain unresolved.
- Antivirus: the hosted Black Knight runtime, exact setup-accepted EULA module, fresh complete Vengeance/Black Knight/Inner Sphere and Mercenaries PR1 installed trees, staged application payload, and exact final `0.6.7` setup passed Defender with zero target detections under real-time protection. Signing/SmartScreen reputation, repair UI, wider hardware, joystick compatibility, and in-game pack visibility remain open.

## Release checklist

- [ ] Reconcile `HANDOFF.md`, `PRODUCTION_PLAN.md`, and this file.
- [ ] Resolve repository ownership and review branch, remote, and unrelated changes.
- [ ] Compile every project-owned component from source.
- [ ] Run focused tests for every changed contract.
- [ ] Build from declared inputs into a clean output directory.
- [ ] Smoke supported Vengeance, Black Knight, Mercenaries, Inner Sphere, and Clan combinations available.
- [ ] Exercise failure rollback, repair, uninstall, and save/config preservation.
- [ ] Verify final checksums and payload manifest/allowlist.
- [ ] Confirm ISOs, archives, serials, raw cracks, proprietary extracted trees, credentials, reports, and local artifacts are absent.
- [ ] Scan setup, extracted payload, and installed trees with current Defender intelligence and inspect new detection events.
- [ ] Record Defender engine/intelligence versions and fail the release if any target is detected or remediated; never add exclusions or suppress warnings.
- [ ] Record Windows/GPU/input/media coverage and explicit gaps.
- [ ] Keep version metadata, tag, README, installer display, checksums, and release notes synchronized.
- [ ] Publish immutable assets; never replace an asset under an existing version.
- [ ] Record rollback instructions and retain the prior known-good release/checksum.

Compile, local smoke, antivirus scan, field qualification, signing, legal provenance, and publication are separate claims.
