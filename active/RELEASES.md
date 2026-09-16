# Releases and operational checkpoints

## Current baseline

- Public release: none.
- Product source/build: reproducible local `0.1.0` setup proof created; not published.
- GitHub remote: `https://github.com/Icehellionx/MechWarrior-4-Remastered`; `main` is the initial development branch.
- Media support: all seven supplied ISO layouts are structurally recognized; Vengeance, Black Knight, and Mercenaries have qualified media-only install/launch paths on this device.
- Compatibility: source-owned exact-input transforms cover Vengeance and Mercenaries, while Black Knight uses a reproducibly source-built loader/helper. The official user-media Patch 3 path and exact Inner Sphere/Clan overlays are qualified to responsive Vengeance launch on this device; in-game chassis/variant visibility and broad runtime behavior remain unqualified.
- Packaging: pinned Inno Setup 7.1.0 builds a per-user, non-elevating setup whose wizard is the only visible installer. Setup collects all selected ISOs or ISO-containing ZIPs before Install and runs the source-owned installation worker synchronously and hidden. Internal `0.5.2`, built from source commit `ec40669`, is 169,840,365 bytes with SHA-256 `6ee1bd2411cab95617d9a1f22ee69338e8e27c7e47dd25e596e4ca36c3e709ec`. A full packaged Mercenaries two-disc smoke completed through that single setup process, installed all three manuals, corrected the setup-table filenames, and removed the game plus launcher shell through the unified uninstall path. It includes the source-built Patch 3 host but not the proprietary engine or payload. Interactive packaged-flow acceptance, signing/SmartScreen reputation, wider hardware, and in-game content visibility remain open.
- Antivirus: the final `0.5.2` setup passed a bounded Defender scan with antivirus and real-time protection enabled and intelligence `1.459.226.0`; no recent detection was reported. Corrected Black Knight and Mercenaries trees also passed earlier bounded scans and opened responsive windows; Mercenaries now launches with `/gosnojoystick` to bypass its crashing legacy joystick enumeration on this device. A historical ignored SafeDiscLoader 1 evaluation was detected and quarantined, so that opaque path is permanently excluded. Signing/SmartScreen reputation, repair UI, wider hardware, joystick compatibility, and in-game pack-visibility gates stay open.

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
