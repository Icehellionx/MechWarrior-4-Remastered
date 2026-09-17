# Releases and operational checkpoints

## Current baseline

- Public release: none.
- Product source/build: internal `0.5.7` is invalid and must not be distributed. Internal candidate `0.6.1` compiles with the corrected page lifecycle and Black Knight runtime path, but still requires packaged media-flow/uninstall and interactive menu/gameplay acceptance before distribution.
- GitHub remote: `https://github.com/Icehellionx/MechWarrior-4-Remastered`; `main` is the initial development branch.
- Media support: all seven supplied ISO layouts are structurally recognized. Fresh Vengeance Patch 3/Inner Sphere/Black Knight PR1 coordinator output exact-verified; Vengeance and Black Knight each produced their correctly titled windows when launched with installer-owned 32-bit registration. Mercenaries PR1 has prior production-worker evidence but needs a fresh packaged rerun.
- Compatibility: Black Knight's invalid mapped-image/static-PE experiment is removed. Official PR1 uses a hosted source-built app-local DLL with no injector/helper/UAC. The unpatched retail route remains unsupported and is rejected until its Vengeance Patch 2 prerequisite is implemented.
- Packaging: pinned Inno Setup 7.1.0 built internal `0.6.1` SHA-256 `b226b6c08b9049dcdb69fef5be42588077779182690477c0e9c7e2bcb356a091`. It remains internal until the complete media-first setup, repair, unified uninstall, and dialog-aware three-game acceptance path passes.
- Antivirus: the hosted Black Knight runtime, fresh complete Vengeance/Black Knight/Inner Sphere installed tree, staged application payload, and exact `0.6.1` setup passed Defender engine `1.1.26080.3`, intelligence `1.459.239.0`, with no new detection/remediation event. Signing/SmartScreen reputation, repair UI, wider hardware, joystick compatibility, and in-game pack visibility remain open.

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
