# Architecture and boundary contract

## Product shape

MechWarrior 4 Remastered is a preservation-oriented Windows installer and launcher. It consumes validated user-supplied media for Vengeance, Black Knight, and Mercenaries, optionally installs the Inner Sphere and Clan Mech Paks, applies narrowly scoped and reproducible compatibility changes, presents manuals, and can safely repair or uninstall its own installation.

## Planned ownership boundaries

| Boundary | Owns | Must not own |
|---|---|---|
| Media catalog | Known editions, disc/archive fingerprints, required files, provenance | Mounting, extraction, UI |
| Media access | ISO/archive inspection and bounded extraction sessions | Game-specific patch policy |
| Install planner | Selected games/packs, dependency rules, space/elevation plan | Filesystem mutation |
| Install transaction | Staging, commit, rollback, manifests, install record | Launcher presentation |
| Game transforms | Official patches and exact known-input executable/data transforms | Generic extraction or release packaging |
| Launcher UI | Installed-game actions, pack status, manuals, errors | Registry, process, patch, filesystem implementation |
| Launch orchestration | Per-title executable/config selection and process lifetime | Graphics-hook internals |
| Compatibility | Versioned wrappers/configuration and title-specific modern-Windows fixes | Installer UI or media ownership |
| Manuals pipeline | Deterministic crop/split/ordering and output validation | Raw-media redistribution decisions |
| Uninstall/repair | Ownership manifest, validation, save/config preservation, cleanup | Broad deletion outside the install root |
| Diagnostics | Sanitized logs and support bundles | Secrets, serials, raw media paths/content |
| Packaging/release | Declared inputs, payload allowlist, checksums, Defender gates | Silent source/media mutation |

## Dependency direction

```text
Launcher UI -> application services -> launch / manuals / repair / uninstall boundaries
Installer UI -> install planner -> media catalog + media access -> transforms -> transaction
Game process -> versioned compatibility boundary -> Windows graphics/audio/input APIs
Build and tests -> declared source/assets -> package manifest -> smoke-installed tree
```

## Invariants

- Original media, serials, cracks, and extracted proprietary game files are never committed or published.
- Every accepted input is identified by content/structure, not filename alone.
- Every mutation occurs in staging or under an exact validated install root and has cleanup/rollback ownership.
- Shared game behavior is parameterized; edition-specific differences live in data or named contracts.
- No-disc support is reproducible, narrowly documented, hash-gated, and never sourced from an unverified opaque executable at release time.
- Optional pack status reflects verified installed payloads, not only registry residue.
- User saves/configuration are inventoried before uninstall policy is implemented.
- Build, smoke, antivirus, hardware, and legal/provenance claims remain separate.

## Decision rule

Add an ADR under `decisions/` when choosing the implementation stack, repository split, redistribution boundary, supported media fingerprints, patch/no-disc method, privilege model, install/save layout, compatibility wrapper, signing strategy, or a permanent exception to the shared three-game contract.
