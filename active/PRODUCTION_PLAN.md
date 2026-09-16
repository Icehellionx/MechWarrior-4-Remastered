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
- [x] Extend the shared transaction contract to Black Knight and Mercenaries, including contained cabinet extraction for Mercenaries.
- [x] Diagnose the Inner Sphere and Clan 64-bit failure boundary from installer/payload evidence: plain resource payload plus official game patch plus obsolete SafeCast/C-Dilla entitlement; replacement remains unqualified.
- [ ] Install optional packs through a tested extraction/configuration path.
- [x] Persist an ownership manifest sufficient for repair and safe file uninstall.
- [x] Add a read-only installer intake shell with atomic multi-source readiness for all games and packs; keep mutation locked pending patch and source-lifetime qualification.
- [x] Reopen and re-recognize selected directory/ISO/ZIP sources under one disposable transaction lifetime with success and failure cleanup.

## 3. Launcher and user experience

Outcome: one MW4-styled launcher makes installed capabilities obvious and starts each title reliably.

- [x] Choose the application stack and record an ADR.
- [ ] Create launcher visual direction from legally usable MW4-era references without copying unlicensed web art.
- [x] Show Vengeance, Black Knight, and Mercenaries launch actions only for manifest-verified installations; distinguish repair-required trees.
- [ ] Show Inner Sphere and Clan pack status with clear installed/missing states.
- [ ] Complete settings, diagnostics, repair, and uninstall actions. Manual opening is wired for installed cleaned outputs.
- [x] Create and wire the requested Vengeance remaster icon with the MW3 launcher's silver/gray lower-right `R`; retain original-art licensing as a release gate.

## 4. Manuals

Outcome: readable manuals are generated reproducibly from user-local scans and packaged only when redistribution rights permit.

- [x] Inspect page geometry and render every raw manual.
- [x] Split Black Knight's combined cover, move the back cover to the end, and verify page order.
- [x] Crop Vengeance pages to content bounds without clipping art, text, folios, or bleed.
- [x] Confirm Mercenaries needs no geometry cleanup.
- [x] Add page-count, dimensions, content-presence, deterministic-output, and visual render checks.

## 5. Compatibility qualification

Outcome: all installed titles and optional packs work on supported modern Windows configurations.

- [ ] Establish clean baselines for video, audio, input, movies, configuration, saves, and multiplayer behavior.
- [ ] Evaluate maintained open-source wrappers/fixes with license and revision records.
- [ ] Add only evidence-backed compatibility changes with disable/rollback paths.
- [x] Prove a non-elevating, disc-free Black Knight process launch from the untouched ISO executable through a constrained project helper and pinned open-source loader release.
- [ ] Test representative Intel/AMD/NVIDIA systems and current supported Windows versions.

## 6. Release and operations

Outcome: a clean machine can install, launch, repair, and uninstall a trustworthy package.

- [ ] Build reproducibly from declared inputs.
- [ ] Smoke every available game/pack/media combination.
- [ ] Preserve saves/configuration through uninstall/reinstall according to documented policy.
- [ ] Verify checksums and payload manifests.
- [ ] Scan setup, extracted payload, and installed trees with current Defender intelligence.
- [ ] Document signing, SmartScreen, rollback, support bundles, and coverage gaps.

## Next slice

```text
User-facing goal: require only Black Knight ISO/ISO-ZIP input and install a disc-free, non-elevating launch path.
Owning boundary: compatibility payload, Black Knight install planning, and launch orchestration.
Smallest verifiable slice: reproduce the pinned GPL loader DLL from source, stage the untouched media executable plus constrained helper/DLL as owned files, and launch via the helper from a fresh disposable install.
Focused evidence: upstream revision/license, reproducible build command and hashes, original executable hash, ownership manifest, explicit `asInvoker` manifest, Defender scan, and bounded process/window result.
Smoke/regression evidence: existing media/transaction/coordinator tests plus helper boundary test and a fresh Black Knight install/launch/uninstall smoke.
Rollback or disable path: compatibility files are manifest-owned; uninstall removes them while preserving user data, and direct launch remains disabled if the bundle is missing or modified.
Hardware coverage gap: menu presence is proven only on the current Windows device; gameplay, renderer, input, audio, saves, and additional GPUs remain unqualified.
```
