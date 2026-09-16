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

## Scaffold origin

- The document layout and agentic workflow were adapted from the MechWarrior 3 Remastered project.
- No MW3 compatibility baseline, native binary, configuration, qualified behavior, or release claim carries into this project automatically.
