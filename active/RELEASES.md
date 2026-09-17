# Releases and operational checkpoints

## Current baseline

- Public release: none.
- Product source/build: internal `0.5.7` is invalid and must not be distributed; its wizard has a field-confirmed startup failure. Source contains the page-lifecycle correction, but no replacement candidate will be built until runtime reference parity passes.
- GitHub remote: `https://github.com/Icehellionx/MechWarrior-4-Remastered`; `main` is the initial development branch.
- Media support: all seven supplied ISO layouts are structurally recognized and install transactions verify structurally, but no title currently has a qualified runtime path. Field testing reached the incorrect-install dialog in all three games.
- Compatibility: exact-input transform and source-build work remains reproducible research evidence. Established implementations show the missing qualification order: reference install state, official Vengeance Patch 3 / Black Knight PR1 / Mercenaries PR1, disc-check handling, then renderer/input fixes. The former responsive-window evidence is downgraded because the error dialog met that gate.
- Packaging: pinned Inno Setup 7.1.0 builds one media-first wizard. The `0.5.7` binary (SHA-256 `493c08007885c34cc8035ed96627b44a73ba971c8b563a7cf8af509922f3a787`) is retained only as failed evidence and must not be offered to users. The source page-lifecycle fix requires static/package verification after runtime qualification.
- Antivirus: the exact `0.5.7` setup, staged application payload, and fresh three-game/two-pack installed tree passed bounded Defender scans with engine `1.1.26080.3` and intelligence `1.459.239.0`; no matching detection or remediation event was created. Signing/SmartScreen reputation, repair UI, wider hardware, joystick compatibility, and in-game pack visibility remain open.

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
