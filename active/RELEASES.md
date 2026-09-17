# Releases and operational checkpoints

## Current baseline

- Public release: none.
- Product source/build: every `/gosnovideo` default is retired. DDrawCompat proved Vengeance and Mercenaries cinematics can render but required incompatible per-title binaries/settings and excluded Black Knight. The 2026-09-17 ecosystem re-audit selects exact-hash dgVoodoo2 2.86.5 as the primary presentation candidate after a Defender-clean three-title load smoke. No current setup is approved for handoff until all three intros, menus, pilot creation, one mission each, and Alt-Tab pass interactively under that stack.
- Internal staged payload `0.6.18` contains the exact locked dgVoodoo DLLs/profile, passed release-tree policy and current Defender, and completed bounded responsive launches for all three titles. It is not a compiled user-facing setup and is not approved for handoff because the interactive presentation/gameplay gates remain open.
- Internal setups `0.6.16` (SHA-256 `1762572a25d6d5fb57c738a1a3bfa1e46085ec74279f0a9cf7b85335725ce437`) and `0.6.17` (SHA-256 `2d055ca9e0ab2ec9d8bb115f267a216c20837ba6596ccfe1339ca9e078d8e8ba`) are superseded and must not be offered because they package the DDrawCompat experiment. `0.6.17` retains useful ownership-safe compatibility migration code; its setup/release tree passed policy and Defender engine `1.1.26080.3`, intelligence `1.459.256.0`.
- GitHub remote: `https://github.com/Icehellionx/MechWarrior-4-Remastered`; `main` is the initial development branch.
- Media support: all seven supplied ISO layouts are structurally recognized. Fresh Vengeance Patch 3/Inner Sphere/Black Knight PR1 coordinator output exact-verified; Vengeance and Black Knight each produced their correctly titled windows when launched with installer-owned 32-bit registration. A fresh Mercenaries PR1 worker run committed and exact-verified 211 files, passed Defender, produced one responsive correctly titled window with no visible legacy error dialog, and passed ownership-safe removal with save/settings preservation. The elevated packaged ISO/ZIP path and actual menus/gameplay remain open.
- Staged AIO integration: the exact `0.6.4` packaged payload's self-contained worker installed all three titles together from extracted original-media copies plus the original PR1 ZIP. Launcher status returned three Ready games, three present manuals, Inner Sphere Ready, Clan Missing, and three present media icons. The launcher stayed responsive and the complete staged application/game tree passed Defender; ownership-safe cleanup removed both game trees and their matching registrations while retaining the shell. Pixel-level launcher acceptance remains open because native screenshot automation was unavailable.
- Compatibility: Black Knight's invalid mapped-image/static-PE experiment is removed. Official PR1 uses a hosted source-built app-local DLL with no injector/helper/UAC. The unpatched retail route remains unsupported and is rejected until its Vengeance Patch 2 prerequisite is implemented.
- Packaging: pinned Inno Setup 7.1.0 built internal `0.6.9` (171,551,426 bytes) SHA-256 `8e0a413c8084b85b5a1fbdeda3b616bbbd60ecd2bd3329a4f5875ab511cefe68`. It remains internal until pilot/gameplay, foreground/fullscreen behavior, repair/unified uninstall, and redistribution gates pass.
- Antivirus: the exact final `0.6.9` setup produced zero target detections with real-time protection enabled and Defender intelligence `1.459.248.0`. Signing/SmartScreen reputation, wider hardware, joystick compatibility, and in-game pack visibility remain open.

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
