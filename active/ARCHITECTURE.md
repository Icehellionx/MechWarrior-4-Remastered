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

- `MediaInspectionService` and `MediaCatalog` own structural recognition and prohibited-content reporting for seven supplied disc layouts plus the qualified Mercenaries PR1 payload layout.
- `OwnedIsoMediaSessionFactory` owns read-only ISO mount lifetime and refuses pre-attached images; `PowerShellDiskImageBackend` is the replaceable initial Windows adapter.
- `IsoArchiveExtractor` validates complete ZIP inventories but materializes only bounded ISO entries; non-ISO serial/crack/manual files are reported and never extracted.
- `MercenariesPr1ArchiveExtractor` recognizes only the exact official `mercpr1.exe` nested in the supported historical fix ZIP, retains only its qualified RTP engine/payload, reports adjacent files as excluded, and rejects same-size counterfeits before mutation.
- `MediaSourceInspector` is the UI-facing read-only application service that normalizes directory, ISO, and ISO-containing ZIP recognition while owning all temporary extraction and mount lifetimes.
- `MediaSelectionSet` owns atomic source acceptance, latest-source evidence per recognized layout, three-game disc completeness, optional-pack presence, and excluded-content counts. It holds evidence only; it never mounts, extracts, or installs.
- `MediaSourceSessionFactory` provides transaction-length directory/ISO/ZIP roots and owns every mount and ZIP scratch directory until disposal. `MediaSelectionSessionFactory` reopens a complete selection, re-recognizes every expected layout, and fails closed if media changed after intake.
- Media intake and revalidation carry cooperative cancellation through source sessions, ISO mounting, and chunked ZIP extraction. Cancellation after mount still dismounts the owned image, and archive/session cleanup retains its existing failure semantics.
- `VengeanceInstallPlanBuilder` and `MercenariesInstallPlanBuilder` own their full-install allowlists, name restoration, and exact internally prepared executable hash gates. Their transform interfaces are the only coordinator boundaries permitted to produce those executables: callers supply only the two media roots, output must remain inside coordinator-owned scratch, and unknown revisions fail closed.
- Vengeance and Black Knight are one physical install transaction and one schema-2 ownership manifest. The manifest's component list independently enables the two launcher operations; root expansion tools live beside Vengeance while Black Knight runtime files remain under `MW4X`. A Black Knight-only add-on is blocked until the installer can rebuild the complete aggregate safely.
- Mercenaries production installation requires the recognized PR1 source. `OfficialMercenariesPr1Transform` stages the retail install plan in owned scratch, applies the exact official update through the constrained x86 RTP host, validates all changed outputs, retains only the nine qualified data updates plus a source-owned managed PR1 executable, and overlays those files before the normal atomic commit.
- `OfficialBlackKnightPr1Transform` stages the complete Vengeance-family plan and applies the exact official PR1 from qualified Mech Pak media. `BlackKnightRuntimeCompatibility` then installs an exact-hash, source-built app-local DLL beside the untouched protected executable. The launcher starts `MW4x.exe` directly; there is no injector, helper process, driver, or launch elevation. Retail Black Knight remains unsupported until the separate Vengeance Patch 2 dependency is implemented and qualified.
- `StagedInstallTransaction` owns contained copy, writable normalization, manifest generation, atomic directory commit, and pre-commit rollback. A manifestless product directory left by ownership-safe uninstall may be replaced atomically: non-colliding user files are copied into staging, the old tree remains as a rollback sibling until the swap succeeds, and any collision with a fresh owned path fails without mutation.
- `InstallManifestVerifier` independently rejects missing, extra, changed, unsafe, or reparse-point content.
- `OwnedInstallUninstaller` removes only verified owned files, preserves unowned content, and blocks before mutation on modified owned files.
- The launcher's single uninstall action first refuses any repair-required tree, then delegates verified game removal to `OwnedInstallUninstaller` and finally starts the standard Inno package uninstaller. Game removal preserves unowned saves/configuration; package uninstall separately owns only application-shell files and shortcuts.
- `GameInstallationCoordinator` is the UI-independent application service for all three games. It reports common stages, delegates title policy to plan builders, owns Vengeance/Mercenaries transform and Mercenaries-cabinet scratch lifetimes, rejects transform output outside owned scratch, commits through the shared transaction, and requires exact-tree verification before success.
- `InstallDestinationPlanner` maps Vengeance plus Black Knight to one contained Vengeance-family operation and keeps Mercenaries independent. It returns explicit blocked-product records, applies conservative budgets plus a fixed reserve, rejects unsafe roots, and does not permit a partial Black Knight overlay onto an installed Vengeance tree.
- `MechPakResourceOverlayPlanBuilder` owns only the exact 10-file resource allowlist for each retail pack and permits only documented Vengeance/Black Knight targets. It deliberately does not own official patch transforms, entitlement replacement, merge transactions, or Mercenaries behavior.
- `OwnedInstallOverlayTransaction` adds non-colliding files to an already verified matching game tree, atomically replaces its ownership manifest, rolls payload back on manifest failure, preserves unowned user data, and leaves recovery state when rollback itself fails. It does not decide whether a pack is runtime-visible.
- `MW4Remastered.InstallProbe` is a development smoke entry point, not the installer UI.
- The Inno package owns the first-run media page: it collects one or more neutral ISO/ZIP paths before Install, requires a selection, and forwards paths only as arguments to `MW4Remastered.Installer`. Setup requests one administrator consent before mutation because Windows ISO inspection/mounting requires it on supported systems; no installed launcher or game requests elevation.
- Inno Setup is the sole visible installation UI and owns media selection, destination selection, progress, completion, and rollback reporting. `MW4RemasteredInstallWorker.exe` has no UI: Setup invokes it synchronously and hidden with the already-selected media/destination, and it inspects, transactionally reopens, plans, installs in dependency order through `GameInstallationCoordinator`, verifies every manifest-owned tree, and rolls back titles committed earlier in the same failed run. Only the launcher may be offered after that worker returns success.
- The worker resets and writes its diagnostic log under `{app}\Logs`; setup failure messages point to this retained path rather than an Inno temporary directory that disappears when the wizard closes.
- The launcher presents one fixed three-column operation grid: games above their manuals, compact verified pack indicators below, and one lower-corner uninstall action. Installed-tree hashing runs after the window is shown so a large game never delays visible startup.
- Manual tiles load deterministic first-page cover PNGs beside the exact packaged PDFs; the generated letter-book image is only a missing/damaged-asset fallback.
- `LegacyGameRegistration` is suspended evidence, not the product registration contract. Static setup resources show original 32-bit HKLM product and DirectPlay records and indicate that `EXE Path` may mean the install directory, while the experiment writes a full executable under HKCU and invents one `CDPath`. A controlled original-installer registry diff must define exact keys, value kinds, semantics, ownership, and removal before registration code is restored to setup.
- All three titles use non-elevating, title-specific startup profiles that skip obsolete joystick enumeration and AutoConfig but preserve original cinematics. The DDrawCompat split remains implemented rollback evidence, not the next release boundary: current community/upstream evidence and a three-title local load smoke select exact-hash dgVoodoo2 as the primary candidate. The intended common profile uses stock x86 DirectX DLLs, the cursor system hook, a 30 FPS presentation cap, aspect-preserving scaling, and no watermark; per-title differences remain allowed when interactive evidence requires them. A 30-second launch guard repairs only required resolution/depth keys in the title's actual executable directory if MW4 rewrites its user-owned INI.
- First-launch network authorization is a setup boundary. If multiplayer support needs Windows Defender Firewall rules, setup must request that choice explicitly, create only executable-scoped project-owned rules, record their identities, and remove only those rules during uninstall. The launcher must never trigger an unexpected firewall consent dialog during normal game launch.
- Interactive setup defaults its clearly described firewall option on; unattended setup defaults it off unless `/ALLOWPRIVATEFIREWALL=1` is supplied. Created inbound rules apply only to private network profiles, disable edge traversal, use project-specific names, and are deleted by exact name during reinstall/uninstall.
- `tools/package/build-release.ps1` publishes self-contained installer/launcher/RTP-host executables, builds the pinned GPL Black Knight app-local DLL with corresponding source, applies release-tree policy before and after packaging, compiles Inno setup, and emits its SHA-256 sidecar. Setup owns the one deliberate elevation boundary; launcher/game execution remains `asInvoker`. No new package is releasable before Defender, full install/uninstall, and dialog-aware runtime qualification pass.

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
- Cancellation is honored before atomic install commit; once a completed tree is renamed into place, final verification runs to completion so cancellation cannot strand an unverified committed result.
- Shared game behavior is parameterized; edition-specific differences live in data or named contracts.
- No-disc support is reproducible, narrowly documented, hash-gated, and never sourced from an unverified opaque executable at release time.
- Setup is the single deliberate elevation boundary. Its completion action uses the original user token, and normal launcher/game execution never elevates or performs install-time mutation.
- Optional pack status reflects verified installed payloads, not only registry residue.
- Package uninstall owns only application-shell files. Per-game uninstall validates its ownership manifest and preserves unowned saves/configuration.
- Build, smoke, antivirus, hardware, and legal/provenance claims remain separate.

## Decision rule

Add an ADR under `decisions/` when choosing the implementation stack, repository split, redistribution boundary, supported media fingerprints, patch/no-disc method, privilege model, install/save layout, compatibility wrapper, signing strategy, or a permanent exception to the shared three-game contract.
