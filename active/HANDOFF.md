# Session handoff

Updated: 2026-09-15.

## Current state

- This is a newly initialized MechWarrior 4 Remastered workspace derived only in governance shape from the MechWarrior 3 Remastered project. It now has a narrow launcher, a read-only installer intake shell, media recognition, manual pipeline, release-tree policy, and rollback-safe staging cores for all three games; no public installer package or qualified runtime baseline exists yet.
- Local inputs include two-disc Vengeance media, Black Knight media, two-disc Mercenaries media, Inner Sphere and Clan Mech Pak media, archival ZIP variants, patch/fix archives, and three raw PDF manuals. They are evidence/user inputs and are intentionally ignored.
- The intended product is one MW4-themed installer and launcher for Vengeance, Black Knight, and Mercenaries. Optional Inner Sphere and Clan packs are selected/detected at install time and surfaced as status in the launcher.
- Raw manuals now have a reproducible cleanup and verification pipeline; generated PDFs remain ignored until redistribution rights are established.
- The desired app includes supported media detection, reproducible no-disc/compatibility transforms, per-game launch buttons/icons, pack status, manual links, repair/configuration, diagnostics, and a safe uninstaller that preserves user-owned saves/configuration by explicit policy.
- The user's freeware statement is not treated as sufficient redistribution authorization. Until reliable primary evidence says otherwise, releases require user-supplied media and exclude ISOs, serials, extracted game trees, and third-party cracks.
- Inner Sphere and Clan setup/DRM components are 32-bit, but their C-Dilla activation stack explicitly references `CDILLA16.EXE`. Disc/readme/archived-support evidence now separates plain resource payload, required Vengeance 3/Black Knight 1 patching, and SafeCast entitlement; the clean entitlement replacement still needs controlled before/after proof.
- Root `.env` is local-only. Its configured auxiliary model names are recorded without credentials in `active/AUXILIARY_MODELS.md`.
- The root Git repository is published publicly at `Icehellionx/MechWarrior-4-Remastered`; local `main` tracks `origin/main`.
- The .NET 10 WinForms launcher now models three games and two optional packs through a UI-free core catalog. It enables game launch only for a manifest-verified installation, reports an unverified executable as needing repair, opens a cleaned manual when present, and leaves unfinished settings/diagnostics/uninstall controls visibly disabled. Pack status deliberately remains false until a payload-manifest verifier exists.
- The launcher now embeds the user-provided original Vengeance icon treatment with only the exact silver/gray `R` glyph from the installed MW3 Remastered launcher composited at native 32×32 scale. The nearest-neighbor preview master and Windows icon live in `assets/branding/`; the underlying game-art redistribution status remains a release gate.
- A media catalog and directory inspector recognize both Vengeance discs, Black Knight, both Mercenaries discs, and both Mech Paks. Unsafe paths fail closed; recognized media reports crack/DRM paths for mandatory exclusion.
- An ownership-aware ISO media session now validates regular `.iso` inputs, refuses pre-attached images, mounts explicitly read-only through a bounded Windows PowerShell adapter, validates the returned root, and dismounts only its owned image. ADR 0004 records the replaceable backend choice.
- Archival ZIP input now has an ISO-only extractor: it validates every entry, rejects unsafe/link/duplicate paths, enforces count and expanded-size limits, reports but never writes non-ISO files, verifies its exact extracted inventory, and commits atomically. ADR 0005 records why already-extracted trees and generic archive unpacking are excluded.
- `MediaSourceInspector` now composes directory inspection, owned ISO sessions, and ISO-only ZIP extraction behind one read-only call suitable for installer UI. The media probe is only a thin reporter over that service.
- `MediaSelectionSet` atomically converts recognized inspections into readiness for the three games and two optional packs. Its latest valid source wins per layout, failed mixed-media intake leaves prior state unchanged, and exclusion counts remain visible.
- The first MW4-styled WinForms installer shell now accepts multiple ISO/ZIP files or mounted folders, inspects them off the UI thread, displays five readiness cards and exact source evidence, reports excluded content, and can reopen/revalidate all selected media. Installation remains visibly locked because the permanent patch/no-disc path and install planning UX are not qualified.
- `MediaSourceSessionFactory` keeps directory/ISO/ZIP roots usable for an explicit lifetime and releases all owned mounts/scratch in reverse order. `MediaSelectionSessionFactory` reopens only current evidence, re-recognizes every expected layout, and closes earlier sources if a later source is missing or changed.
- A release-tree policy rejects disc images, secrets/keys, crack directories, legacy DRM files, reparse points, and executable/DLL/script content not named by an explicit allowlist.
- The manual pipeline reproducibly creates three ignored local outputs. Black Knight is 36 portrait pages with the front cover first and separated back cover last; Vengeance is 98 cropped 611.76×342-point spreads; Mercenaries preserves its 19 original pages. Two consecutive runs produced identical hashes and every output page was rendered for review.
- The Vengeance install plan now stages 231 allowlisted files from both discs plus one exact-hash user-supplied version-2.0 executable. It restores patch-relevant 8.3 names, excludes setup/SafeDisc content, clears read-only media attributes, commits atomically, and writes a repair/uninstall ownership manifest.
- A real 1.04 GB disposable Vengeance tree exists under ignored `staging/vengeance-baseline`; independent manifest verification passes. It has not been launched.
- Black Knight now uses the same transaction contract. Its plan consumes the recognized disc, flattens runtime files from `MW4X`, restores installed root names, excludes setup/DirectX/SafeDisc components, and accepts only the exact-hash local replacement executable. A real 138-file, 563,490,697-byte payload tree plus ownership manifest exists under ignored `staging/black-knight-baseline`; verification passes and it has not been launched.
- Mercenaries now uses a containment-first cabinet extractor plus the shared transaction. The extractor preflights all archive paths, accepts only `GAME/` payload entries, verifies the exact extracted inventory, and atomically commits the payload root. The install plan combines 85 cabinet files with allowlisted Disc 1 runtime files and Disc 2 content, restores installer-era names, and exact-hash gates the local version-`50.07.01.2105` executable. A real 209-file, 1,206,212,339-byte payload tree plus manifest exists under ignored `staging/mercenaries-baseline`; verification passes and it has not been launched.
- `GameInstallationCoordinator` now provides one UI-independent orchestration path for all three games. It reports validation/extraction/planning/commit/verification stages, owns Mercenaries cabinet scratch cleanup on success or failure, delegates title allowlists to the existing builders, and returns success only after exact-tree verification. The development install probe now consumes this service instead of duplicating orchestration.
- Pack evidence now shows the assets are 20 ordinary `RESOURCE` files, not 16-bit-encrypted data. The 32-bit setup/DRM stack invokes `CDILLA16.EXE`, on-disc documentation requires Vengeance 3 or Black Knight 1, and archived Microsoft support treats missing logos/content as a SafeCast licence problem. The replacement entitlement contract remains unqualified.
- `MechPakResourceOverlayPlanBuilder` captures the two exact 10-file resource allowlists, accepts only structurally recognized pack media, and targets only Vengeance or Black Knight. It excludes patches, setup, cracks, and DRM and is not yet connected to a merge transaction.
- The uninstaller core removes only manifest-owned files, preserves unowned saves/configuration, blocks before mutation on modified owned files, and rolls back move-phase failures. ADR 0003 records the policy.
- Manifest verification has explicit scopes: exact-tree verification rejects all unexpected files for staging/release gates, while owned-file verification powers launcher health so user-created saves/configuration do not disable a valid install. Both scopes still reject missing or modified owned game files.

## Best resume path

1. Qualify a reproducible official 2.0/3.0 RTPatch transform against the staged Vengeance tree. The evaluated MIT parser is not yet compatible or path-safe enough.
2. Compare official-patch outputs and version resources with the supplied exact-hash 2.0/3.0 executables without publishing those binaries.
3. Derive pack payload/entitlement effects from Patch 3 plus controlled before/after trees, avoiding C-Dilla installation.
4. Record ADRs for the permanent patch/no-disc method, compatibility baseline, and remaining registry/save-location ownership.
5. Extend the installer intake shell with destination/free-space planning, cancellation, compatibility-input selection, and `GameInstallationCoordinator` progress before enabling installation.

## Known constraints and risks

- Licensing/freeware history may differ between Mercenaries releases and the original retail titles.
- Multiple archival ZIP/ISO layouts may contain different revisions or modified executables; filenames alone are not trusted.
- Opaque no-CD archives are high risk. Prefer known-input binary transforms or openly licensed source-based compatibility when legally and technically viable.
- Legacy setup/bootstrap executables may be 16-bit even when game payloads are 32-bit; direct payload extraction may be safer than emulating installers, but must preserve registry/configuration prerequisites.
- Hardware graphics, input, video, DRM/disc checks, and multiplayer behavior remain entirely unqualified.

## Last verification

- Governance intake completed; local inputs enumerated without opening or publishing content.
- Auxiliary router syntax and four unit tests passed. Ollama was reachable with all nine configured local model names installed; Featherless roles are configured but were not live-billed.
- The launcher/core Release build completed with zero warnings/errors, and the synthetic core smoke test passed.
- The installer/core Release build completed with zero warnings/errors. Synthetic selection tests cover incomplete/complete two-disc readiness, multi-ISO ZIPs, optional packs, exclusion accounting, latest-source replacement, and atomic rejection of mixed unknown media.
- Synthetic source-session tests prove ISO/ZIP resources live until disposal, normal and partial-failure cleanup, selection-wide reopen, changed-media rejection, and cleanup when a later source disappears.
- The refactored real Black Knight archival-ZIP smoke recognized the expected layout and two prohibited SafeDisc paths, returned exit 0, left no new media-session scratch, and confirmed the standalone local image was detached.
- Launcher process-boundary tests confirm that an unmanifested executable cannot be launched and that a verified game starts with its own directory as the working directory. The available automation surface could not capture native WinForms windows, so visual inspection remains an explicit UI coverage gap.
- A local-only SHA-256 inventory recorded all 23 media/manual inputs under ignored `.local/`; no source media was changed.
- ISO directory inspection confirmed C-Dilla/SafeCast and SafeDisc-era files on both pack discs plus directly accessible content/patch payloads. The direct-extraction hypothesis remains unqualified.
- Read-only pack inventory confirmed 10 plain resource files (34,434,947 bytes) for Inner Sphere and 10 (20,285,495 bytes) for Clan. No Vengeance/Black Knight path collisions exist; four of six Mercenaries name collisions differ by hash, so a generic overlay is rejected.
- Synthetic pack-overlay tests prove exact 10-file selection, crack/DRM/RTP exclusion, Vengeance/Black Knight targeting, Mercenaries refusal, and partial-payload rejection.
- The real-media recognition smoke passed all seven supplied ISOs and confirmed every owned mount was detached afterward.
- The new C# owned-ISO boundary independently mounted and recognized the Black Knight image, reported both prohibited SafeDisc paths, and left the image detached. Synthetic lifetime tests cover pre-attached refusal and failed-root cleanup.
- The composed ZIP media probe extracted only `MW4BK.iso` from the real Black Knight archive, recognized the expected layout, and left no new scratch directory. Synthetic tests prove non-ISO exclusion and traversal rejection.
- Synthetic transaction tests cover containment, duplicate destinations, writable normalization, rollback, ownership manifests, tamper detection, and unexpected-file detection.
- Real Vengeance staging committed 231 files from two read-only mounted discs and the exact version-2.0 replacement input. Manifest verification passed, no staged files remained read-only, and both owned images were detached.
- Real Black Knight staging committed 138 payload files from its read-only mounted disc and exact-hash local replacement input. Manifest verification passed, no staged file remained read-only, no forbidden setup/DRM file entered the tree, and the image was detached.
- Real Mercenaries cabinet extraction committed and exactly re-inventoried 85 files. The subsequent two-disc transaction committed 209 payload files, verified its manifest, left no read-only or forbidden files, and detached both owned images.
- Synthetic coordinator smoke installs and verifies all three games through the common application service, observes completion progress for each, and proves Mercenaries cabinet scratch cleanup after both success and injected planning failure.
- A real-tree uninstall smoke removed all 232 owned payload/manifest files from a disposable 1.04 GB copy while preserving an unowned synthetic pilot save; the exact disposable target was then removed.
- Release-tree policy tests passed their safe baseline and rejected synthetic ISO, unallowlisted executable, SafeDisc driver, and crack-directory fixtures.
- All three manual outputs passed page-count/geometry/render checks and deterministic SHA-256 comparison. Final full contact-sheet review found no clipped or misordered pages.
- GitHub CLI device authorization succeeded for `Icehellionx`; the public repository was created and the initial `main` branch pushed.
- No game launch, permanent install, registry mutation, antivirus, or field/hardware verification has run. Uninstall is qualified only at the file-ownership core/smoke level, not through a packaged UI.
