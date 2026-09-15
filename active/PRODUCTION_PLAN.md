# Production plan

This plan begins intentionally uncompleted. Checkmarks require retained evidence; completed detail moves to `archive/HISTORY.md`.

## 1. Evidence and legal boundary

Outcome: supported inputs and publishable outputs are explicit and defensible.

- [ ] Hash and structurally inventory each local ISO/archive/manual without tracking proprietary data.
- [ ] Distinguish original retail, official patches, Microsoft-released Mercenaries freeware material, community releases, and third-party cracks by source and license.
- [ ] Define the release payload allowlist and forbidden-file scanner.
- [ ] Record a media/redistribution ADR.

## 2. Reproducible installation core

Outcome: user media becomes a verified, rollback-safe installation without invoking unsupported legacy setup paths where avoidable.

- [ ] Detect supported Vengeance media layouts and stage a disposable install.
- [ ] Determine required official patch level and implement exact-input transforms.
- [ ] Extend the shared contract to Black Knight and Mercenaries.
- [ ] Diagnose Inner Sphere and Clan pack failures on 64-bit Windows from installer/payload evidence.
- [ ] Install optional packs through a tested extraction/configuration path.
- [ ] Persist an ownership manifest sufficient for repair and safe uninstall.

## 3. Launcher and user experience

Outcome: one MW4-styled launcher makes installed capabilities obvious and starts each title reliably.

- [ ] Choose the application stack and record an ADR.
- [ ] Create launcher visual direction from legally usable MW4-era references without copying unlicensed web art.
- [ ] Show Vengeance, Black Knight, and Mercenaries actions only when installed.
- [ ] Show Inner Sphere and Clan pack status with clear installed/missing states.
- [ ] Add manual, settings, diagnostics, repair, and uninstall actions.
- [ ] Create a project-owned Vengeance-inspired remaster icon with a distinct `R` mark.

## 4. Manuals

Outcome: readable manuals are generated reproducibly from user-local scans and packaged only when redistribution rights permit.

- [ ] Inspect page geometry and render every raw manual.
- [ ] Split Black Knight's combined cover, move the back cover to the end, and verify page order.
- [ ] Crop Vengeance pages to content bounds without clipping art, text, folios, or bleed.
- [ ] Confirm whether Mercenaries needs cleanup.
- [ ] Add page-count, dimensions, text-presence, and visual render checks.

## 5. Compatibility qualification

Outcome: all installed titles and optional packs work on supported modern Windows configurations.

- [ ] Establish clean baselines for video, audio, input, movies, configuration, saves, and multiplayer behavior.
- [ ] Evaluate maintained open-source wrappers/fixes with license and revision records.
- [ ] Add only evidence-backed compatibility changes with disable/rollback paths.
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
User-facing goal: establish a trustworthy MW4 project baseline.
Owning boundary: governance, media catalog, repository hygiene.
Smallest verifiable slice: initialize Git; add ignores, agent router, model catalog, input inventory tooling, and research-backed first ADRs.
Focused evidence: docs contain no MW3 product claims; secrets/media remain untracked; router tests pass.
Smoke/regression evidence: not applicable until an installation core exists.
Rollback or disable path: documentation/source-only commit; no user media is mutated.
Hardware coverage gap: all runtime behavior remains unqualified.
```
