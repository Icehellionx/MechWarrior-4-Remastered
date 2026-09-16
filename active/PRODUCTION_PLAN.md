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
- [x] Add read-only destination planning with contained per-game folders, conservative space budgets, current-volume capacity, and unsafe-root rejection.
- [x] Add cooperative cancellation for installer media inspection/revalidation with owned mount and ZIP scratch cleanup.
- [x] Validate and assemble the exact internal Black Knight helper/loader/license/corresponding-source bundle from reproducible CI evidence.
- [x] Wire the qualified Black Knight media-only path into installer progress, cancellation, atomic commit, and final verification while leaving unqualified titles locked.

## 3. Launcher and user experience

Outcome: one MW4-styled launcher makes installed capabilities obvious and starts each title reliably.

- [x] Choose the application stack and record an ADR.
- [x] Create a compact MW4 visual direction using the user-approved icon and the proven MW3 launcher hierarchy without adding unlicensed web art.
- [x] Show Vengeance, Black Knight, and Mercenaries launch actions only for manifest-verified installations; distinguish repair-required trees.
- [ ] Show Inner Sphere and Clan pack status with clear installed/missing states.
- [ ] Complete settings, diagnostics, repair, and uninstall actions. Manual opening is wired for installed cleaned outputs.
- [x] Expose ownership-safe per-game removal in the launcher and provide a separate standard package uninstaller for application-shell files.
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
- [x] Reproduce the Black Knight compatibility bundle across two clean Windows CI builds, exact-hash it into installation policy, and pass a fresh ISO-derived install/launch/Defender smoke.
- [ ] Test representative Intel/AMD/NVIDIA systems and current supported Windows versions.

## 6. Release and operations

Outcome: a clean machine can install, launch, repair, and uninstall a trustworthy package.

- [x] Build a byte-reproducible per-user setup from declared application and exact Black Knight compatibility inputs.
- [ ] Smoke every available game/pack/media combination.
- [x] Preserve saves/configuration through both ownership-safe game removal and standard shell-package uninstall smoke paths.
- [x] Emit and verify the setup checksum while enforcing payload and release-tree allowlists.
- [x] Scan the current setup, extracted payload, installed shell, and qualified Black Knight tree with current Defender intelligence.
- [ ] Document signing, SmartScreen, rollback, support bundles, and coverage gaps.

## Next slice

```text
User-facing goal: require only Vengeance ISO/ISO-ZIP input and produce a disc-free executable from its original SafeDisc 1 loader/ICD pair.
Owning boundary: deterministic executable transform, official patch chain, and Vengeance install planning.
Smallest verifiable slice: implement or adopt suitably licensed source for a hash-gated ICD transform, compare it with known local evidence, and eliminate the extra replacement-executable input.
Focused evidence: exact loader/ICD input hashes, documented transform stages, deterministic output hash, current Defender scan, and a bounded non-elevating launch.
Smoke/regression evidence: transform unit vectors plus fresh Vengeance install/launch/repair/uninstall smoke from media alone.
Rollback or disable path: transforms operate in staging; failed or unknown revisions never modify media or an existing install.
Hardware coverage gap: Black Knight menu presence is proven only on the current Windows device; gameplay, renderer, input, audio, saves, and additional GPUs remain unqualified.
```
