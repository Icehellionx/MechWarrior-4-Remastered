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

## Implemented foundation

- `MediaInspectionService` and `MediaCatalog` own structural recognition and prohibited-content reporting for seven supplied disc layouts.
- `OwnedIsoMediaSessionFactory` owns read-only ISO mount lifetime and refuses pre-attached images; `PowerShellDiskImageBackend` is the replaceable initial Windows adapter.
- `IsoArchiveExtractor` validates complete ZIP inventories but materializes only bounded ISO entries; non-ISO serial/crack/manual files are reported and never extracted.
- `MediaSourceInspector` is the UI-facing read-only application service that normalizes directory, ISO, and ISO-containing ZIP recognition while owning all temporary extraction and mount lifetimes.
- `MediaSelectionSet` owns atomic source acceptance, latest-source evidence per recognized layout, three-game disc completeness, optional-pack presence, and excluded-content counts. It holds evidence only; it never mounts, extracts, or installs.
- `MediaSourceSessionFactory` provides transaction-length directory/ISO/ZIP roots and owns every mount and ZIP scratch directory until disposal. `MediaSelectionSessionFactory` reopens a complete selection, re-recognizes every expected layout, and fails closed if media changed after intake.
- Media intake and revalidation carry cooperative cancellation through source sessions, ISO mounting, and chunked ZIP extraction. Cancellation after mount still dismounts the owned image, and archive/session cleanup retains its existing failure semantics.
- `VengeanceInstallPlanBuilder` owns the current full-install allowlist, 8.3 name restoration, and exact compatibility-executable hash gate.
- `BlackKnightInstallPlanBuilder` owns the media-derived Black Knight payload plus an exact-hash internal compatibility bundle. It stages the untouched disc executable, required runtime files, project helper, source-built loader, and GPL notice; no replacement executable is accepted.
- `StagedInstallTransaction` owns contained copy, writable normalization, manifest generation, atomic directory commit, and pre-commit rollback.
- `InstallManifestVerifier` independently rejects missing, extra, changed, unsafe, or reparse-point content.
- `OwnedInstallUninstaller` removes only verified owned files, preserves unowned content, and blocks before mutation on modified owned files.
- `GameInstallationCoordinator` is the UI-independent application service for all three games. It reports common stages, delegates title policy to plan builders, owns Mercenaries cabinet scratch lifetime, commits through the shared transaction, and requires exact-tree verification before success.
- `InstallDestinationPlanner` maps only media-complete games to contained per-title directories, applies conservative per-title byte budgets plus a fixed safety reserve, rejects filesystem roots/reparse traversal, and reports current-volume capacity without mutating the destination.
- `MechPakResourceOverlayPlanBuilder` owns only the exact 10-file resource allowlist for each retail pack and permits only documented Vengeance/Black Knight targets. It deliberately does not own official patch transforms, entitlement replacement, merge transactions, or Mercenaries behavior.
- `OwnedInstallOverlayTransaction` adds non-colliding files to an already verified matching game tree, atomically replaces its ownership manifest, rolls payload back on manifest failure, preserves unowned user data, and leaves recovery state when rollback itself fails. It does not decide whether a pack is runtime-visible.
- `MW4Remastered.InstallProbe` is a development smoke entry point, not the installer UI.
- `MW4Remastered.CompatLauncher` is a 32-bit, `asInvoker`, fail-closed launch helper constrained to the three adjacent MW4 executable names and the adjacent compatibility DLL. Black Knight launch selects it only when the ownership manifest verifies the helper and original game files; unowned dropped helpers are ignored.
- `MW4Remastered.Installer` is the initial WinForms intake shell. It requests inspection through the application service, renders selection state, can transactionally reopen/revalidate the current selection, and previews a contained destination/free-space plan. Its install action remains locked until the permanent patch/no-disc contracts and execution UX are qualified.

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
- Normal game launch never elevates. Privileged install/repair/uninstall work cannot make the launcher or game inherit administrator integrity.
- Optional pack status reflects verified installed payloads, not only registry residue.
- User saves/configuration are inventoried before uninstall policy is implemented.
- Build, smoke, antivirus, hardware, and legal/provenance claims remain separate.

## Decision rule

Add an ADR under `decisions/` when choosing the implementation stack, repository split, redistribution boundary, supported media fingerprints, patch/no-disc method, privilege model, install/save layout, compatibility wrapper, signing strategy, or a permanent exception to the shared three-game contract.
