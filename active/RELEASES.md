# Releases and operational checkpoints

## Current baseline

- Public release: none.
- Product source/build: not yet created.
- GitHub remote: `https://github.com/Icehellionx/MechWarrior-4-Remastered`; `main` is the initial development branch.
- Media support: local candidate inputs inventoried by filename/size only; no fingerprint is qualified.
- Compatibility: unqualified.
- Antivirus, hardware, install, launch, repair, and uninstall gates: not run.

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
- [ ] Record Windows/GPU/input/media coverage and explicit gaps.
- [ ] Keep version metadata, tag, README, installer display, checksums, and release notes synchronized.
- [ ] Publish immutable assets; never replace an asset under an existing version.
- [ ] Record rollback instructions and retain the prior known-good release/checksum.

Compile, local smoke, antivirus scan, field qualification, signing, legal provenance, and publication are separate claims.
