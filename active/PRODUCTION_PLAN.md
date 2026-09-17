# Production plan

This plan begins intentionally uncompleted. Checkmarks require retained evidence; completed detail moves to `archive/HISTORY.md`.

## 1. Evidence and legal boundary

Outcome: supported inputs and publishable outputs are explicit and defensible.

- [x] Hash and structurally inventory each local ISO/archive/manual without tracking proprietary data.
- [ ] Distinguish original retail, official patches, Microsoft-released Mercenaries freeware material, community releases, and third-party cracks by source and license.
- [x] Define the release payload allowlist and forbidden-file scanner.
- [x] Record a media/redistribution ADR.

## 2. Reproducible installation core

Outcome: user media becomes a verified, rollback-safe installation without invoking unsupported legacy setup paths where avoidable.

- [x] Detect supported Vengeance media layouts and stage a disposable install.
- [x] Implement the proven official update chain: Vengeance Patch 3, Black Knight PR1 in the shared `MW4X` topology, and Mercenaries PR1 before any disc-check transform.
- [x] Reimplement and independently vector-test the SafeDisc 1 eight-byte block cipher, title-specific 1.50.20 page-local second layers, and exact-input Vengeance/Mercenaries PE/import/pointer repair transforms.
- [x] Extend the shared transaction contract to Black Knight and Mercenaries, including contained cabinet extraction for Mercenaries.
- [x] Diagnose the Inner Sphere and Clan 64-bit failure boundary from installer/payload evidence: plain resource payload plus official game patch plus obsolete SafeCast/C-Dilla entitlement; replacement remains unqualified.
- [x] Install optional packs from exact-hash media through the Patch 3 and resource-overlay path without invoking C-Dilla/SafeCast; in-game chassis/variant visibility remains a separate qualification gate.
- [x] Persist an ownership manifest sufficient for repair and safe file uninstall.
- [x] Add atomic multi-source readiness for all games and packs, and enable dependency-ordered mutation for all three qualified game paths.
- [x] Reopen and re-recognize selected directory/ISO/ZIP sources under one disposable transaction lifetime with success and failure cleanup.
- [x] Add read-only destination planning with contained per-game folders, conservative space budgets, current-volume capacity, and unsafe-root rejection.
- [x] Add cooperative cancellation for installer media inspection/revalidation with owned mount and ZIP scratch cleanup.
- [x] Encode the original product topology: Vengeance base, Black Knight dependent expansion, independent Mercenaries, and Vengeance-dependent Mech Paks.
- [x] Apply that topology to destination planning and remove the obsolete hidden Black Knight-only installer execution path.
- [x] Collect a neutral multi-file ISO/ZIP set in the package wizard before Install and hand it directly to intake validation without a second picker.
- [x] Remove the second visible WinForms installer; the package wizard now owns the entire visible flow and invokes only a synchronous hidden install worker.
- [x] Remove the caller-supplied Vengeance replacement executable from installation requests; confine the qualified retail media-derived transform to coordinator-owned scratch and fail closed on unknown revisions or output hashes.
- [x] Remove the caller-supplied Mercenaries replacement executable; derive its disc-free executable from exact original-media inputs inside coordinator-owned scratch and fail closed on unknown revisions or output hashes.
- [x] Prove official Vengeance Patch 3 applies directly to retail media, apply it through a constrained source-owned host, retain only exact qualified outputs, and remove the obsolete C-Dilla and ARTP imports/call sites from the generated executable.
- [x] Recognize the exact official Mercenaries PR1 updater from the supported fix ZIP, exclude its adjacent historical no-CD payload, apply PR1 in contained scratch, and derive an exact-hash source-owned `50.07.01.2105` executable before commit.
- [x] Make original Mercenaries media sufficient for the internal AIO flow by build-time extracting and hash-locking only the two qualified official PR1 payload files; public redistribution rights remain an explicit release gate.
- [x] Remove the failed app-local PR1 runtime. Apply official `45.30.04.1908`, capture its decrypted user-media image only during setup with the exact source-built GPL DLL, reproduce the established PR1 import-slot layout, repair all 127 tail branches, reject residual references to the removed SafeDisc tail, and emit deterministic static executable `7761c41d…`; a production-shaped 60-second launch crossed the prior focused-transition failure without runtime compatibility files. Reject retail until its separate Vengeance Patch 2 prerequisite is implemented.
- [x] Apply official Black Knight PR1 and Mercenaries PR1 in contained reference trees and retain exact pre/post manifests as ignored evidence.
- [x] Replace separate Vengeance/Black Knight destinations with one atomic schema-2 Vengeance-family manifest and preserve the original `MW4X` subdirectory.
- [x] Remove the superseded retail Black Knight proxy DLL from production package staging and constrain the RTP host to the three qualified official-update hashes.

## 3. Launcher and user experience

Outcome: one MW4-styled launcher makes installed capabilities obvious and starts each title reliably.

- [x] Choose the application stack and record an ADR.
- [x] Create a compact MW4 visual direction using the user-approved icon and the proven MW3 launcher hierarchy without adding unlicensed web art.
- [x] Show Vengeance, Black Knight, and Mercenaries launch actions only for manifest-verified installations; distinguish repair-required trees.
- [x] Show Inner Sphere and Clan pack status from exact installed-file evidence with clear installed/missing states.
- [ ] Complete settings, diagnostics, and repair actions. Manual opening is wired for packaged cleaned outputs.
- [x] Consolidate the supported games' first-run EULA state into one explicit setup acceptance and exact transforms/registration so gameplay is not interrupted by legacy dialogs.
- [x] Expose one launcher uninstall action that removes verified owned game payloads, closes the launcher, and visibly hands off removal of the application shell, manuals, shortcuts, and registration.
- [x] Keep shell uninstall available when an old or damaged game tree lacks verifiable ownership, preserve uncertain files, and remove the shared Vengeance/Black Knight physical tree only once.
- [x] Create and wire the requested Vengeance remaster icon with the MW3 launcher's silver/gray lower-right `R`; retain original-art licensing as a release gate.
- [x] Replace generic letter/manual markers with deterministic cover thumbnails generated from and packaged beside each cleaned manual.
- [x] Reproduce and fix the field-confirmed incorrect-install failures; candidate `0.6.7` installed and reached the real menu in all three titles.
- [ ] Field-qualify pilot creation after setup seeds and the bounded launch guard maintains each title's actual INI location. The final dgVoodoo profile must prove aspect-preserving fullscreen/borderless presentation and Alt-Tab for all three titles; Black Knight is no longer intentionally forced windowed. Post-UAC topmost Setup behavior remains a user-visible gate.

## 4. Manuals

Outcome: readable manuals are generated reproducibly from user-local scans and packaged only when redistribution rights permit.

- [x] Inspect page geometry and render every raw manual.
- [x] Split Black Knight's combined cover, move the back cover to the end, and verify page order.
- [x] Crop Vengeance pages to content bounds without clipping art, text, folios, or bleed.
- [x] Confirm Mercenaries needs no geometry cleanup.
- [x] Add page-count, dimensions, content-presence, deterministic-output, and visual render checks.
- [x] Generate, exact-hash, package, and visually verify one launcher cover thumbnail per manual.

## 5. Compatibility qualification

Outcome: all installed titles and optional packs work on supported modern Windows configurations.

- [ ] Establish clean baselines for video, audio, input, movies, configuration, saves, and multiplayer behavior.
- [x] Pin, source-build, patch narrowly, and record the 0BSD DDrawCompat-MW3 r18 revision used for MW4 borderless presentation; two clean qualified builds are byte-identical.
- [x] Resolve startup DirectShow movie-surface interoperability without a system codec pack: Vengeance uses the qualified fork with `keepvidmem(1)` and Mercenaries uses clean upstream v0.7.1 with `noactivateapp(0)`.
- [x] Re-audit current MW4 community recipes and maintained compatibility projects; identify dgVoodoo2 as the convergent Windows/Lutris path, record exact 2.86.5 archive and x86 DLL hashes/terms, pass Defender, and complete bounded three-title load smokes.
- [x] Replace the DDrawCompat release payload with an exact-hash dgVoodoo2 import/profile and preserve a collision-safe, rollback-capable owned-install migration path for existing installs.
- [x] Reproduce all 13 reviewed Vengeance InstallShield long-name mappings, including `Burnloop_lr_15.avi`, and route existing verified installs through the ownership-safe migration transaction so Vengeance and Black Knight can cross the shared intro-to-menu asset boundary.
- [ ] Interactively qualify the exact staged dgVoodoo profile across all three startup movies, menus, Alt-Tab, pilot creation, and one mission before building a user-facing setup.
- [x] Move optional multiplayer firewall authorization into the elevated setup as an explicit private-network choice, keep silent installs opt-in, and remove only project-named rules during uninstall.
- [ ] Verify Black Knight startup video, all in-engine cinematics, and real Alt-Tab return using the final title-specific package.
- [ ] Add only evidence-backed compatibility changes with disable/rollback paths.
- [x] Prove a non-elevating, disc-free Black Knight process/window launch after reproducing the official shared Vengeance/`MW4X` topology and Black Knight PR1 state. Dialog-aware menu/gameplay confirmation remains open.
- [x] Re-evaluate the locally patched Black Knight compatibility bundle against fresh officially patched reference trees before restoring only the exact PR1 path to installation policy.
- [ ] Replace timed responsive-process checks with dialog-aware menu and gameplay assertions for all three titles.
- [ ] Qualify fresh Vengeance and Mercenaries media-only coordinator installs through setup-state parity, menu/gameplay checks, exact verification, Defender scan, and ownership-safe uninstall.
- [ ] Test representative Intel/AMD/NVIDIA systems and current supported Windows versions.

## 6. Release and operations

Outcome: a clean machine can install, launch, repair, and uninstall a trustworthy package.

- [ ] Build a byte-reproducible per-user setup after the revised setup-only Black Knight capture/static transform passes full worker cleanup, Defender, repair/uninstall, and interactive menu/gameplay gates; the superseded launch-time loader is removed.
- [ ] Smoke every available game/pack/media combination.
- [x] Preserve saves/configuration through both ownership-safe game removal and standard shell-package uninstall smoke paths.
- [x] Permit reinstall over manifestless residue preserved by safe uninstall without deleting user data; reject owned-path collisions without mutation and retain setup failure logs after the wizard closes.
- [x] Emit and verify the setup checksum while enforcing payload and release-tree allowlists.
- [x] Scan the current setup and qualified Vengeance, Black Knight, and Mercenaries trees with current Defender intelligence.
- [ ] Document signing, SmartScreen, rollback, support bundles, and coverage gaps.

## Next slice

```text
User-facing goal: ship one media-first installer whose launcher plays every original cinematic and reaches stable menu/gameplay in each installed title.
Owning boundary: Black Knight setup-only image capture, deterministic static PE/import normalization, exact-hash dgVoodoo import/profile, launch arguments, owned manifests/migration, packaging provenance, and dialog-aware runtime proof.
Smallest verifiable slice: build the package from the now-complete official-PR1 capture/static coordinator path, prove no capture artifact enters its staged or installed tree, scan it, then cross Black Knight's original intro-to-menu boundary with the final launch profile.
Focused evidence: deterministic output hash from independent captures, five-section/clean-import PE assertions, scratch cleanup, absence of `version.dll` in the installed tree, positive menu capture, pilot creation, one mission, and real Alt-Tab return.
Smoke/regression evidence: candidate `04fe9acf…` is rejected after a focused transition reached omitted SafeDisc target `0x008A3504`. Two different mapped images normalize to the same 3,743,744-byte v3 output `7761c41d…`; a production-shaped tree with accepted EULA, corrected filenames, dgVoodoo, videos enabled, and `-noautoconfigx /gosNoJoystick` stayed correctly titled and responsive for 60 seconds. The complete Vengeance + Inner Sphere + Black Knight coordinator installed/exact-verified 387 owned files, leaked no capture artifact or temporary child, and uninstalled every owned file while preserving an unowned save. Final internal setup `0.6.20` and its 21-file unpacked payload passed release-tree policy and current Defender. Interactive focused intro/menu proof remains open.
Rollback or disable path: retain the last title-specific DDrawCompat payloads only as A/B evidence; never restore `/gosnovideo` as a release default or introduce injectors, opaque no-CD files, codec packs, or Defender exclusions.
Hardware coverage gap: only this NVIDIA/Windows multi-monitor device is available; gameplay, input, audio completeness, saves, multiplayer, and additional GPUs remain unqualified.
```
