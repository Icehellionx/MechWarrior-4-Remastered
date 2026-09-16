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
- [ ] Determine required official patch level and implement exact-input transforms.
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
- [x] Validate and assemble the exact internal Black Knight helper/loader/license/corresponding-source bundle from reproducible CI evidence.
- [x] Prove the Black Knight media transformation internally; keep it out of the primary product flow until the Vengeance base path and dependency-aware orchestration are qualified.

## 3. Launcher and user experience

Outcome: one MW4-styled launcher makes installed capabilities obvious and starts each title reliably.

- [x] Choose the application stack and record an ADR.
- [x] Create a compact MW4 visual direction using the user-approved icon and the proven MW3 launcher hierarchy without adding unlicensed web art.
- [x] Show Vengeance, Black Knight, and Mercenaries launch actions only for manifest-verified installations; distinguish repair-required trees.
- [x] Show Inner Sphere and Clan pack status from exact installed-file evidence with clear installed/missing states.
- [ ] Complete settings, diagnostics, and repair actions. Manual opening is wired for packaged cleaned outputs.
- [ ] Consolidate each game's first-run EULA into setup if the original terms permit one explicit installer acceptance to satisfy the retail runtimes; never silently mark acceptance without presenting the applicable terms.
- [x] Expose one launcher uninstall action that removes verified owned game payloads, closes the launcher, and visibly hands off removal of the application shell, manuals, shortcuts, and registration.
- [x] Create and wire the requested Vengeance remaster icon with the MW3 launcher's silver/gray lower-right `R`; retain original-art licensing as a release gate.
- [x] Replace generic letter/manual markers with deterministic cover thumbnails generated from and packaged beside each cleaned manual.
- [x] Reproduce and fix Vengeance/Mercenaries first-launch failures with narrow per-user legacy registration and a stable non-elevating windowed launch profile.

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
- [ ] Evaluate maintained open-source wrappers/fixes with license and revision records.
- [ ] Add only evidence-backed compatibility changes with disable/rollback paths.
- [x] Prove a non-elevating, disc-free Black Knight process launch from the untouched ISO executable through a constrained project helper and pinned open-source loader release.
- [x] Reproduce the Black Knight compatibility bundle across two clean Windows CI builds, exact-hash it into installation policy, and pass a fresh ISO-derived install/launch/Defender smoke.
- [ ] Replace or harden Black Knight's nondeterministic protected-helper runtime: repeated clean packaged launches now alternate between a responsive process, a delayed DirectDraw exclusive-mode stop, and a pre-menu NTDLL exit. Any replacement must remain source-built/pinned, non-elevating, Defender-clean, and compatible with the Vengeance dependency record.
- [x] Qualify fresh Vengeance and Mercenaries media-only coordinator installs through exact verification, bounded Defender scans, responsive disc-free launches, and ownership-safe uninstall.
- [ ] Test representative Intel/AMD/NVIDIA systems and current supported Windows versions.

## 6. Release and operations

Outcome: a clean machine can install, launch, repair, and uninstall a trustworthy package.

- [x] Build a byte-reproducible per-user setup from declared application and exact Black Knight compatibility inputs.
- [ ] Smoke every available game/pack/media combination.
- [x] Preserve saves/configuration through both ownership-safe game removal and standard shell-package uninstall smoke paths.
- [x] Permit reinstall over manifestless residue preserved by safe uninstall without deleting user data; reject owned-path collisions without mutation and retain setup failure logs after the wizard closes.
- [x] Emit and verify the setup checksum while enforcing payload and release-tree allowlists.
- [x] Scan the current setup and qualified Vengeance, Black Knight, and Mercenaries trees with current Defender intelligence.
- [ ] Document signing, SmartScreen, rollback, support bundles, and coverage gaps.

## Next slice

```text
User-facing goal: make setup 0.5.5's clean three-game install launch reliably, with Black Knight no longer dependent on a nondeterministic protected-process helper.
Owning boundary: Black Knight executable compatibility, Vengeance/expansion registration topology, renderer initialization, and exact release provenance.
Smallest verifiable slice: reproduce the clean-package Black Knight failure under a debugger/trace, select a source-auditable transform or wrapper strategy, and hold the real child process responsive with an empty log for repeated 60-second launches.
Focused evidence: exact media/helper hashes, crash or loader trace, deterministic output/bundle hashes, registry reads, process responsiveness, and clean game log.
Smoke/regression evidence: three consecutive fresh Black Knight launches plus Vengeance, Mercenaries, both-pack, Defender, and unified-uninstall regressions.
Rollback or disable path: retain the current untouched-media plus pinned helper bundle behind an explicit experimental gate until the replacement passes; never fall back to an opaque fixed executable.
Hardware coverage gap: only this NVIDIA/Windows device is available; gameplay, input, audio, saves, multiplayer, and additional GPUs remain unqualified.
```
