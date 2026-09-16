# Releases and operational checkpoints

## Current baseline

- Public release: none.
- Product source/build: reproducible local `0.1.0` setup proof created; not published.
- GitHub remote: `https://github.com/Icehellionx/MechWarrior-4-Remastered`; `main` is the initial development branch.
- Media support: all seven supplied ISO layouts are structurally recognized; Vengeance, Black Knight, and Mercenaries have qualified media-only install/launch paths on this device.
- Compatibility: source-owned exact-input transforms cover Vengeance and Mercenaries, while Black Knight uses a reproducibly source-built loader/helper. Official patch levels, broad runtime behavior, and pack entitlement remain unqualified.
- Packaging: pinned Inno Setup 7.1.0 builds a per-user, non-elevating setup with standard uninstall and an in-setup media stage. Two clean final-flow builds produced the same 72,289,562-byte executable and SHA-256 `1b7455ed7180f638a6e7c24b8d118a8ed4954f693a6bb7720658f8c544aa1994`. The subsequent clarified-scope flow build is 72,297,599 bytes with SHA-256 `a2b00bacf81b4ea1533ef8bb24170e48f04b5650942eb436bfdd05830a1e307d` and passed current Defender qualification. A disposable install/start/uninstall smoke removed package-owned shell files and preserved an unowned synthetic save.
- Antivirus: the current `0.3.0` setup and qualified game trees passed bounded Defender scans with engine `1.1.26080.3` / intelligence `1.459.226.0`. A historical ignored SafeDiscLoader 1 evaluation was detected and quarantined, so that opaque path is permanently excluded. Signing/SmartScreen reputation, repair UI, wider hardware, and remaining pack gates stay open.

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
