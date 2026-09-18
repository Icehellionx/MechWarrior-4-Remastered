# Production plan

This plan begins intentionally uncompleted. Checkmarks require retained evidence; completed detail moves to `archive/HISTORY.md`.

## Immediate polish priorities

The `0.6.22` field run established the first all-three-games-playable baseline. Work now proceeds in this order, without regressing original cinematics, 4:3 artwork, or media-only installation:

0. [x] Remove the Defender-triggering whole-disc Mech Pak ZIP path. Setup now reads supported pack ISOs in memory, writes only exact allowlisted resources and official patch inputs, and never materializes or mounts their legacy setup/DRM content. Real Inner Sphere projection, a complete five-ZIP install, and Defender scans of projected and installed trees pass. Reproducible setup packaging remains the release gate.

1. [x] Proportionally zoom only Vengeance's live-action opening movie until its pillar bars are gone and its letterbox bars are as thin as the target monitor permits. The source-built dgVoodoo add-on requires both exact process `MW4.exe` and an exclusive-open conflict on `Content/Movies/GAMEOPEN.MPG`, derives the movie's 30:17 source geometry, and center-covers to the active monitor without stretching or rewriting media. Exact-window captures prove the preceding startup clip and following menu remain strict 4:3. Black Knight, Mercenaries, all other videos, menus, and gameplay bypass this gate.
2. [x] Re-crop the Vengeance manual to the cover artwork's true first-page bounds, apply that page size consistently to the remaining pages, regenerate the launcher cover, update the deterministic lock, and render every page for clipping/order review.
3. [ ] Ship a maximum-quality, monitor-adaptive renderer profile for all three titles. Source detects the physical pixel dimensions of the monitor containing the foreground launcher, derives the largest fitting 4:3 render surface, and uses the same captured result for the launch request and guarded INI; `1920x1440` is only the result for a `2560x1440` monitor. Setup `0.6.39` also upgrades pre-existing low-detail pages to MW4's Ultra High-equivalent game-owned settings without preventing later user changes. The profile preserves desktop mode, strict 4:3 geometry, 4x MSAA, 16x anisotropic filtering, and 32-bit color. Automated geometry, low-to-ultra upgrade, core smoke, focused contracts, reproducible packaging, and Defender gates pass; moved-launcher multi-monitor and packaged all-title gameplay proof remain before completion.
4. [x] Remove the field-proven green seam only during gameplay and in-engine 3D. Retained runtime logs proved the shell and scene both present from `800x600`, invalidating the former high-resolution classifier. The shipped correction gates a five-source-pixel top/left crop on dgVoodoo's active legacy Direct3D-device lifecycle and anchors the image bottom-right; menus and startup videos remain on the untouched 2D path. The `0.6.36` packaged-launcher mission test field-confirmed that the top/left seam is gone without regressing 4:3 presentation.
5. [ ] Convert installed Inner Sphere and Clan resource evidence into game-visible Mech Pak activation. `0.6.37` proved the executable correction installed exactly but stock Cauldron-Born still failed because its loadout references separately flagged component records. Exact archive inspection found four Clan SMRMs and Inner Sphere Heavy Gauss in `weaponstable.mpt`, plus Clan Enhanced Optics and Inner Sphere IFF Jammer in `subsystemtable.mpt`. `0.6.38` unlocks selected-media records across those tables plus both chassis tables and includes an exact-hash owned archive migration for existing installs; the user field-confirmed stock Cauldron-Born selection no longer raises the entitlement dialog. Generated Vengeance and Black Knight archives contain zero remaining selected-pack flags. Broaden coverage to both families, all three titles, all eight chassis, campaign, and multiplayer without altering campaign markets.
6. [ ] Match the required capture-friendly Alt-Tab behavior in all three titles. On focus loss, the borderless game picture must remain visible, unchanged, and non-minimized behind other windows; screenshots and video capture must retain the same geometry. Alt-Tab or a single click must return cleanly, with the cursor released while inactive and recaptured exactly once when active. Current source finds the exact PID's largest visible legacy window, restores it without activation, pins its stabilized rectangle, and clips only while foreground. A disposable-launcher startup-presentation probe passed every state/geometry/display/cursor check on the current system, using exact-window capture only. Keep this item open until the same matrix passes during gameplay in all three titles.
7. [x] Complete end-user attribution. Package a readable credits/third-party notice covering incorporated upstream/runtime and build-time work, exact versions/revisions where pinned, author/project, license/provenance links, and local use/modification; expose it from the installed folder and add a compact launcher Credits action beside Uninstall.

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
- [x] Remove the failed app-local PR1 runtime and field-disproven static v2/v3 outputs. Apply official `45.30.04.1908`, capture its decrypted user-media image only during setup with the exact source-built GPL DLL, reproduce the established PR1 import layout, exact-correct 110 reviewed operands, repair all 127 tail branches, reject residual removed-tail references, and emit deterministic static v4 `b31bd051…`. Reject retail until its separate Vengeance Patch 2 prerequisite is implemented.
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
- [x] Field-qualify installation, launch, pilot creation, and playable game entry for Vengeance, Black Knight, and Mercenaries on the current NVIDIA/Windows test system using setup `0.6.22`.
- [ ] Qualify the polished presentation profile: native-monitor 3D scaling, maximum safe in-game detail, 4x MSAA, 16x anisotropic filtering, 32-bit color, Vengeance-only widescreen intro framing, non-minimizing Alt-Tab, and clean return in all three titles. Post-UAC topmost Setup behavior remains a separate user-visible gate.

## 4. Manuals

Outcome: readable manuals are generated reproducibly from user-local scans and packaged only when redistribution rights permit.

- [x] Inspect page geometry and render every raw manual.
- [x] Split Black Knight's combined cover, move the back cover to the end, and verify page order.
- [x] Re-crop Vengeance to the cover's exact visible page bounds and apply the resulting 509.8 x 323-point geometry consistently across all 98 pages; regenerate the cover, exact-lock the new PDF, and visually inspect every rendered page. The earlier 342-point heuristic was field-rejected for retaining a bottom white strip.
- [x] Confirm Mercenaries needs no geometry cleanup.
- [x] Add page-count, dimensions, content-presence, deterministic-output, and visual render checks.
- [x] Generate, exact-hash, package, and visually verify one launcher cover thumbnail per manual.

## 5. Compatibility qualification

Outcome: all installed titles and optional packs work on supported modern Windows configurations.

- [ ] Establish clean baselines for video, audio, input, movies, configuration, saves, and multiplayer behavior.
- [x] Retire the rejected DDrawCompat presentation branch after dgVoodoo2 superseded it; remove its build script, patch, profiles, and lock from the production repository while retaining the negative result in history.
- [x] Re-audit current MW4 community recipes and maintained compatibility projects; identify dgVoodoo2 as the convergent Windows/Lutris path, record exact 2.86.5 archive and x86 DLL hashes/terms, pass Defender, and complete bounded three-title load smokes.
- [x] Use an exact-hash dgVoodoo2 import/profile and preserve a collision-safe, rollback-capable owned-install migration path that removes the exact old DDrawCompat profile names from existing installs.
- [x] Reproduce all 13 reviewed Vengeance InstallShield long-name mappings, including `Burnloop_lr_15.avi`, and route existing verified installs through the ownership-safe migration transaction so Vengeance and Black Knight can cross the shared intro-to-menu asset boundary.
- [x] Interactively qualify setup `0.6.22` through original startup movies, menus, pilot creation, and playable game entry for all three titles on the current NVIDIA/Windows system.
- [ ] Qualify the new presentation profile through the same paths plus non-minimizing Alt-Tab and return, screenshot layering, and one retained save/load cycle per title.
- [ ] Field-prove the elevated setup's exact executable firewall pairs: allow inbound on Private, explicitly block inbound on Public, produce no first-launch security prompt on the active Public test network, keep silent installs opt-in, and remove only project-named rules during uninstall.
- [ ] Verify Black Knight startup video, all in-engine cinematics, and real Alt-Tab return using the final title-specific package.
- [ ] Add only evidence-backed compatibility changes with disable/rollback paths.
- [ ] Qualify or reject official `dinputto8` v1.1.100.0 on the exact final all-title trees. Black Knight loads it without `/gosNoJoystick`, but pilot creation, two consecutive missions, in-mission rebinding, joystick/HOTAS, keyboard/mouse, and the upstream MW4 crash report remain open; do not package it yet.
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
User-facing goal: preserve the proven all-playable baseline while correcting presentation and making validated Mech Paks genuinely selectable.
Owning boundary: exact manual geometry, dgVoodoo aspect/upscale profile, exact-hash archive flags, post-recognition executable ownership gate, and field-visible game behavior.
Smallest verifiable slice: package the post-`0.6.33` opening correction, confirm strict 4:3 outside the sole Vengeance live-action segment, then field-capture an actual mission to judge the five-pixel top/left seam crop.
Focused evidence: the embedded Microsoft/FASA splash is centered and uncropped at 4:3; only the later 30:17 live-action segment covers the final display target without stretching; menus and all other videos remain 4:3; an actual mission capture has no top/left seam; installed profiles report `FullScreenMode=false`, centered `borderless, fullscreensize`, `stretched_ar`, `Resolution=unforced`, `ExtraEnumeratedResolutions=max_4_3`, 4x MSAA, 16x filtering, and both watermark families disabled.
Smoke/regression evidence: `0.6.23` was field-rejected for a 342-point manual crop, global 16:9 stretching, and table-visible but PID-gated pack chassis. `0.6.24` was blocked at the real destination by dead project registration values. `0.6.25` fixed stale-registration replacement but was field-rejected for global stretch, incomplete pack ownership, and minimizing Alt-Tab. The source correction now passes the presentation contract, synthetic selected/unselected pack tests, and exact transforms of all three real final executables; interactive proof remains open.
Rollback or disable path: retain the last title-specific DDrawCompat payloads only as A/B evidence; never restore `/gosnovideo` as a release default or introduce injectors, opaque no-CD files, codec packs, or Defender exclusions.
Hardware coverage gap: only this NVIDIA/Windows multi-monitor device is available; gameplay, input, audio completeness, saves, multiplayer, and additional GPUs remain unqualified.
```
