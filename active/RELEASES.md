# Releases and operational checkpoints

## Current baseline

- Public release: none.
- Product source/build: internal `0.5.7` candidate compiled; not published.
- GitHub remote: `https://github.com/Icehellionx/MechWarrior-4-Remastered`; `main` is the initial development branch.
- Media support: all seven supplied ISO layouts are structurally recognized; Vengeance, Black Knight, and Mercenaries have qualified media-only install/launch paths on this device.
- Compatibility: source-owned exact-input transforms cover Vengeance and Mercenaries. Black Knight now composes the verified Vengeance base during setup and uses a normally loaded, reproducibly source-built app-local DLL with a narrow signature-checked OEP patch; the injector and runtime helper are absent. The official user-media Patch 3 path and exact Inner Sphere/Clan overlays are qualified to responsive launch on this device; in-game chassis/variant visibility and broad runtime behavior remain unqualified.
- Packaging: pinned Inno Setup 7.1.0 builds one media-first wizard. Setup requests the single UAC consent needed for ISO mounting, runs the source-owned worker synchronously and hidden, performs transforms and registration before success, and starts the optional post-install launcher with the original user token. Internal `0.5.7` is 166,365,015 bytes with SHA-256 `493c08007885c34cc8035ed96627b44a73ba971c8b563a7cf8af509922f3a787`. A full worker-level seven-media install and runtime smoke passed; the compiled-package smoke is pending because an unrelated active Inno/VS Code updater held the machine-wide setup gate.
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
