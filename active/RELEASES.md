# Releases and operational checkpoints

## Current baseline

- Public release: none.
- Product source/build: reproducible local `0.1.0` setup proof created; not published.
- GitHub remote: `https://github.com/Icehellionx/MechWarrior-4-Remastered`; `main` is the initial development branch.
- Media support: all seven supplied ISO layouts are structurally recognized; Vengeance, Black Knight, and Mercenaries have qualified media-only install/launch paths on this device.
- Compatibility: source-owned exact-input transforms cover Vengeance and Mercenaries, while Black Knight uses a reproducibly source-built loader/helper. The official user-media Patch 3 path and exact Inner Sphere/Clan overlays are qualified to responsive Vengeance launch on this device; in-game chassis/variant visibility and broad runtime behavior remain unqualified.
- Packaging: pinned Inno Setup 7.1.0 builds a per-user, non-elevating setup with standard uninstall and an in-setup media stage. Internal `0.4.0` is 93,938,661 bytes with SHA-256 `7deb2a9bb4dd41298c2bba6271317eae54e5104593ac1e3e5da135387062b1ee`; it includes the source-built Patch 3 host but not the proprietary engine or payload. The package and a 252-file Vengeance-plus-both-packs install passed exact verification and current Defender scanning. The installed game opened responsively without disc or UAC, and uninstall preserved an unowned save. Interactive packaged-flow acceptance, signing/SmartScreen reputation, wider hardware, and in-game content visibility remain open.
- Antivirus: the current `0.4.0` setup and qualified game trees, including Vengeance with both packs, passed bounded Defender scans with current intelligence. A historical ignored SafeDiscLoader 1 evaluation was detected and quarantined, so that opaque path is permanently excluded. Signing/SmartScreen reputation, repair UI, wider hardware, and in-game pack-visibility gates stay open.

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
