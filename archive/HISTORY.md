# Historical work ledger

Completed implementation, verification, durable decisions, and disproven investigations belong here in reverse chronological order. Current state and next work stay in `active/`.

## MechWarrior 4 governance reset — 2026-09-15

- Replaced inherited MechWarrior 3 runtime/release claims with an unqualified MW4 starting state.
- Defined user-supplied-media, three-game, two-optional-pack, manuals, launcher, repair, and uninstall scope.
- Made the freeware/redistribution claim an evidence question rather than a release assumption.
- Established planned ownership boundaries, testing layers, release gates, and initial risk backlog.
- Cataloged configured Featherless and Ollama model names without recording credentials.

## Initial application and evidence slice — 2026-09-15

- Initialized the root Git repository and added strict media/secret/build ignores.
- Added an SDK-style .NET 10 core and WinForms launcher scaffold with declarative definitions for Vengeance, Black Knight, Mercenaries, Inner Sphere, and Clan.
- Added synthetic status smoke coverage and refused to infer pack installation from an unverified marker.
- Hashed 23 local media/manual inputs into ignored local evidence and inspected pack/Black Knight ISO layouts without mutation.
- Confirmed local pack discs carry C-Dilla/SafeCast-era components, SafeDisc-era `SECDRV.SYS`, content resources, and official patch payloads.
- Created the public `Icehellionx/MechWarrior-4-Remastered` GitHub repository and pushed `main` after device authorization.

## Media and manuals foundation — 2026-09-15

- Added one shared structural recognition contract for seven Vengeance, Black Knight, Mercenaries, Inner Sphere, and Clan disc layouts.
- Verified every supplied ISO through ownership-aware read-only mounts; all mounts were released and crack/DRM paths were reported for exclusion.
- Added release-tree policy and synthetic rejection tests for media, key/secret, DRM, crack, reparse, and unallowlisted executable classes.
- Added a deterministic manual pipeline. Split and oriented Black Knight covers, cropped all Vengeance spreads to their content canvas, and preserved Mercenaries geometry.
- Rendered every final manual page and reproduced identical output hashes in a second build.

## Vengeance transactional staging — 2026-09-15

- Added a contained staging transaction with atomic commit, rollback, writable media normalization, per-file SHA-256 ownership manifest, and independent verifier.
- Added a Vengeance full-install plan that consumes both recognized discs, restores installer-era 8.3 names, hash-gates the supplied version-2.0 executable, and excludes setup/SafeDisc material.
- Staged and verified a real 231-file, 1.04 GB local Vengeance baseline without running legacy setup, SafeDisc, or writing registry/system state.
- Narrowed the Mech Pak 64-bit diagnosis to a 32-bit C-Dilla/SafeCast stack that explicitly references a 16-bit helper.
- Evaluated an MIT RTPatch parser and rejected its current revision for product use because it does not parse the supplied patch variants and lacks output-path containment.
- Added an ownership-only uninstaller with modified-file preflight, move-phase rollback, unowned save/config preservation, and a successful real-tree smoke on a disposable Vengeance copy.

## Black Knight transactional staging — 2026-09-15

- Added a Black Knight plan on the shared transaction boundary with structural media recognition and an exact-hash local replacement executable gate.
- Flattened the disc's `MW4X` runtime payload, restored installed names, and excluded legacy setup, DirectX setup, disc-management, and SafeDisc components.
- Staged a real 138-file, 563,490,697-byte payload tree, independently verified every manifest entry, confirmed all files writable and forbidden components absent, and detached the owned image.

## Mercenaries transactional staging — 2026-09-15

- Added a containment-first cabinet extractor that rejects non-`GAME/`, unsafe, and duplicate paths before extraction, verifies the exact produced inventory, and atomically commits the payload root.
- Added a Mercenaries plan that combines the verified cabinet payload with allowlisted files from both recognized discs, restores setup-era names, exact-hash gates a local replacement executable, and excludes C-Dilla, SafeDisc, setup, and crack paths.
- Extracted and verified all 85 real cabinet files, then staged a real 209-file, 1,206,212,339-byte payload tree; every owned hash verified, all files were writable, both images detached, and no forbidden component entered the tree.

## Launcher remaster mark — 2026-09-15

- Replaced the initial generated concept at the user's direction with the supplied original Vengeance icon treatment.
- Isolated the installed MW3 launcher's exact 6×7 silver/gray `R` glyph, composited it onto the MW4 artwork's native 32×32 grid without changing the remaining source cells, packaged a nearest-neighbor 256×256 Windows icon, and embedded it into the launcher build.

## Manifest health scopes — 2026-09-15

- Split manifest verification into exact-tree and owned-file scopes so staging/release gates remain strict while user-created saves/configuration do not incorrectly force launcher repair state.
- Added regression coverage proving unowned user data is permitted for launch health but reported by exact-tree verification; owned-file tampering remains a failure.

## Shared game-install coordination — 2026-09-15

- Added typed Vengeance, Black Knight, and Mercenaries requests behind one UI-independent installation coordinator with common progress stages and exact-tree completion verification.
- Made the coordinator own Mercenaries cabinet extraction scratch lifetime and proved cleanup after success and injected planning failure.
- Switched the development install probe to the coordinator so installer UI and smoke tooling can share one orchestration path.

## Owned ISO media sessions — 2026-09-15

- Added a read-only, ownership-aware ISO session that rejects pre-attached images and reparse inputs, validates the mounted root, and provides idempotent owned cleanup.
- Isolated inbox Windows storage cmdlets behind a bounded, encoded-command PowerShell adapter with no execution-policy bypass and a 30-second timeout.
- Extended the media probe to accept ISO paths and proved the new C# boundary against the real Black Knight image; recognition reported both SafeDisc exclusions and the image detached afterward.

## ISO-only archival ZIP media — 2026-09-15

- Added a bounded ZIP extractor that validates all entry paths but writes only ISO files, reports non-ISO entries without materializing them, rejects links/traversal/duplicates, and verifies the exact staged inventory before atomic commit.
- Composed ZIP extraction with owned read-only ISO sessions in the media probe.
- Proved the path against the real Black Knight archival ZIP: only `MW4BK.iso` was extracted, recognition succeeded with SafeDisc exclusions, and no probe scratch directory remained.

## Scaffold origin

- The document layout and agentic workflow were adapted from the MechWarrior 3 Remastered project.
- No MW3 compatibility baseline, native binary, configuration, qualified behavior, or release claim carries into this project automatically.
