# Historical work ledger

Completed implementation, verification, durable decisions, and disproven investigations belong here in reverse chronological order. Current state and next work stay in `active/`.

## Startup movie suppression retired — 2026-09-17

- Field reproduction tied Vengeance's black/flickering intro and `DDERR_SURFACELOST` to the wrapper activation profile, not a missing codec.
- Vengeance visibly played its original intro using the qualified MW3-derived DDrawCompat DLL with `AltTabFix=keepvidmem(1)`.
- Mercenaries remained unstable with the MW3-derived DLL but visibly played its original intro using clean upstream DDrawCompat v0.7.1 with `AltTabFix=noactivateapp(0)`.
- `/gosnovideo` was removed from all three launch profiles. The prior video-suppression baseline and shared-wrapper assumption are historical only.
- Built internal setup `0.6.16` with exact title-specific wrapper/source payloads; release-tree policy and current Defender passed. A full seven-ISO worker exact-verified both game trees before the expected existing-registration safety rollback and detached every image.

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
- Added `MediaSourceInspector` as the UI-facing composition boundary for directories, ISOs, and ISO-containing ZIPs; refactored the media probe to a thin reporter and re-ran the real ZIP smoke with identical recognition and cleanup results.

## Installer media intake shell — 2026-09-15

- Added an atomic media-selection model that maps seven layouts to complete/incomplete game and optional-pack capability state, tracks current source evidence, and reports excluded content.
- Added a MW4-styled WinForms installer shell with multi-file ISO/ZIP and mounted-folder intake, background inspection, five readiness cards, a validated-media ledger, and explicit prohibited-content reporting.
- Kept installation mutation visibly locked until selected sources can be reopened with transaction-owned lifetimes and the permanent patch/no-disc method is qualified.
- Built the installer and launcher in Release with zero warnings/errors and passed the full synthetic core regression suite; native WinForms visual inspection remains unautomated.

## Transaction-length selected media — 2026-09-15

- Refactored inspection onto one reusable media-session boundary that keeps read-only ISO mounts and ISO-only ZIP scratch alive until explicit disposal.
- Added selection-wide reopen/revalidation that matches archive entry plus structural layout, rejects changed media before installation, and unwinds already-opened sources if later input fails.
- Wired installer-side revalidation without enabling file installation.
- Proved lifetime and cleanup synthetically, then recognized the real Black Knight archival ZIP with no residual scratch or attached image.

## Mech Pak 64-bit failure boundary — 2026-09-15

- Inventoried both pack discs read-only and established that their game data is 20 ordinary resource files, not 16-bit-encrypted assets.
- Correlated on-disc setup/readme evidence with archived Microsoft support: installation combines resource overlay, Vengeance 3 or Black Knight 1 patching, and a distinct SafeCast/C-Dilla entitlement required for pack logos/content.
- Confirmed the protection stack references a 16-bit helper while the setup engines and payload are 32-bit; this explains the 64-bit boundary more precisely without running legacy DRM.
- Rejected a generic Mercenaries overlay because four same-named staged files differ from the older pack media; the clean entitlement replacement and game-visible proof remain open.
- Added a payload-only overlay planner with exact per-pack resource allowlists and explicit Vengeance/Black Knight targeting; no legacy setup, protection, patch executable, or crack enters the plan.
- Added a verified-install overlay transaction with collision refusal, atomic manifest replacement, rollback, save preservation, and uninstall ownership; production pack installation remains disabled pending patch/entitlement proof.

## Media-only Black Knight compatibility — 2026-09-16

- Rejected upstream `VersionInjector.exe` because its embedded privilege contract requires administrator elevation, then built a constrained project-owned x86 `asInvoker` helper for adjacent MW4 targets and `version.dll` only.
- Pinned SafeDiscLoader2 v1.3 GPL source, built only its loader project, normalized non-semantic PE timestamps, packaged exact corresponding source, and proved byte-identical artifacts across two clean GitHub-hosted Windows builds.
- Replaced Black Knight's opaque replacement-executable input with the untouched media executable plus exact-hash internal helper/DLL/license payload.
- Installed and exactly verified a fresh 142-file Black Knight tree from media alone, launched a responsive game window with no mounted disc or UAC, and passed a current Defender scan.
- Repeated uninstall on a disposable copy: all 143 owned entries were removed while the sole synthetic user configuration file was preserved.

## Explicit no-UAC application boundary — 2026-09-16

- Reproduced Windows installer-detection elevation on unmanifested legacy executables with setup-like names and proved that embedding `requestedExecutionLevel="asInvoker"` removes the prompt without changing the operation.
- Added checked-in `asInvoker` manifests to the installer and launcher, complementing the existing non-elevating compatibility helper, and added a regression test that locks all three privilege contracts.
- Applied official Vengeance Patch 2 through a disposable non-elevating wrapper around its 32-bit patch engine; the resulting SafeDisc 1 loader/ICD still performs raw-sector verification, so the ordinary ISO and volume-label emulation are not sufficient for disc-free launch.

## Defender-clean release boundary — 2026-09-16

- Traced the observed warning to Defender's `Program:Script/Wacapew.A!ml` detection of an ignored historical SafeDiscLoader 1 DLL and a disposable executable that embedded it; both were quarantined and neither was tracked or packaged.
- Removed the exact opaque/generated evaluation binaries plus compiled dump/patch research outputs while retaining source evidence and all user media.
- Expanded the release-tree denylist to reject known injectors, unwrappers, patch engines, dump tools, and opaque compatibility binaries even if explicitly executable-allowlisted.
- Added a Defender scan gate that records engine/intelligence versions and checks new detection/remediation events as well as scan exit status, without exclusions or disabled remediation.

## Installer destination planning — 2026-09-16

- Added a read-only destination planner that includes only media-complete games, maps them to contained title directories, and combines conservative per-title budgets with a 512 MiB reserve.
- Added storage-capacity injection and focused tests for complete/incomplete selection, sufficient/insufficient space, containment, and rejection of relative or filesystem-root destinations.
- Added installer destination selection and live required/available capacity status while keeping file installation locked behind the remaining compatibility gates.

## Cancellable media intake — 2026-09-16

- Added a visible Cancel action for installer inspection and revalidation, with cancellation propagated through source sessions, ISO mounting, selection reopen, and chunked ZIP extraction.
- Preserved cleanup ownership on cancellation: a just-mounted image is dismounted, partial ZIP staging is removed, and the UI reports completion only after cleanup unwinds.
- Added focused regressions for cancellation before archive mutation and cancellation arriving during ISO mount.

## Exact Black Knight compatibility bundle — 2026-09-16

- Extended compatibility validation to cover the GPL corresponding-source archive alongside the project helper, source-built loader, and license while keeping the source archive out of the game directory.
- Added exact-inventory rejection for missing, modified, unexpected, or reparse bundle content and regression coverage proving extra files fail closed.
- Added atomic bundle assembly from reproducible CI evidence, reproduced all four locked hashes from run `35048453580`, and passed a current Defender scan of the assembled bundle.

## Installer-driven Black Knight installation — 2026-09-16

- Wired only the qualified Black Knight path into the installer: complete media, exact compatibility bundle, contained unused destination, and sufficient space are all required before the action enables.
- Reopened only Black Knight source evidence for the operation lifetime, surfaced coordinator progress, added chunk-cancellable staging/cabinet process control, and preserved the atomic commit plus mandatory post-commit verification boundary.
- Added coordinator/transaction cancellation regressions, started the rebuilt installer responsively with the exact bundle present, and passed a current Defender scan of that combined output tree.

## Launcher game removal — 2026-09-16

- Unified installer and launcher product directories on the catalog IDs so newly installed Black Knight is immediately discoverable by the launcher.
- Exposed per-game ownership-safe removal for ready and repair-required directories, with confirmation, off-UI-thread hashing/removal, preserved-data reporting, and refreshed status.
- Kept whole-application uninstall explicitly pending until packaging owns a complete shell-file manifest; the launcher does not broadly delete its own directory.

## Reproducible per-user package — 2026-09-16

- Added a pinned Inno Setup 7.1.0 package that installs the self-contained launcher/installer and exact Black Knight compatibility distribution per user without elevation.
- Kept standard package uninstall ownership separate from manifest-aware game removal; no broad uninstall-delete rule can remove media-derived game files or unowned saves/configuration.
- Produced byte-identical setup executables across clean paired builds, emitted the SHA-256 sidecar, and passed release-tree plus privilege-boundary contracts.
- Completed a disposable package install/start/uninstall smoke: both apps were responsive, all package-owned shell files were removed, and a synthetic Black Knight save remained.
- Scanned the final setup and representative installed trees with current Defender intelligence; no threat or matching remediation event was reported. Signing/SmartScreen reputation remains a separate release gap.
- Added hosted Windows application CI for synthetic core behavior, both application builds, non-elevation manifests, release-tree policy, packaging ownership, and pinned compatibility-source contracts; it deliberately publishes no binary artifact.
- The first hosted application-contract run, `35055297275`, passed every step for commit `9888e9a` in 50 seconds.

## Simplified setup and launcher flow — 2026-09-16

- Moved media intake from an optional post-install checkbox into the interactive setup sequence and made setup wait for that stage before reporting completion.
- Replaced the launcher card dashboard with the MW3-derived hierarchy requested by the user: three games, three manuals, compact pack indicators, one status line, and one lower-corner uninstall action.
- Replaced the unexplained `INSTALLATION LOCKED` state with numbered media/install actions and explicit reasons for unavailable titles, destinations, or compatibility support.
- Made installed-tree verification asynchronous so the native launcher window appears immediately; the packaged installed-state smoke showed it in 228 ms and kept it responsive while Black Knight was verified.
- Captured both real WinForms windows for visual QA, fixed unreadable disabled-state text and dead initial controls, and added a user-flow source contract to hosted application CI.
- Built the final package twice byte-identically at 72,289,562 bytes, scanned SHA-256 `1b7455ed7180f638a6e7c24b8d118a8ed4954f693a6bb7720658f8c544aa1994` clean with the current Defender gate, and preserved the existing Black Knight tree during the installed-shell update.
- Re-scanned the corrected installed shell plus preserved Black Knight tree; Defender reported no threat and no new detection/remediation event.
- Hosted Windows application-contract run `35061879507` passed all steps for redesign commit `11d0310` in 58 seconds.

## Setup-owned launch-ready 0.5.7 candidate — 2026-09-16

- Moved the sole required elevation boundary to outer setup after reproducing `Get-DiskImage` access denial without administrator consent; the post-install launcher explicitly returns to the original user token.
- Moved all legacy registration and EULA completion into setup and made launch validation read-only, so game clicks do no install-time work.
- Removed the Black Knight process-injection helper, its source project, publisher, and tests. A reproducibly source-built app-local DLL now signature-checks and patches only two obsolete installed-media calls at OEP.
- Made Black Knight a self-contained expansion tree by composing only verified manifest-owned Vengeance base files with expansion overrides and the exact setup-accepted EULA transform.
- Completed a fresh real seven-ISO worker install for all three games plus both packs. Vengeance, Black Knight, and Mercenaries were responsive with empty logs; Black Knight passed three consecutive single-process launches.
- Built internal setup `0.5.7` at 166,365,015 bytes, SHA-256 `493c08007885c34cc8035ed96627b44a73ba971c8b563a7cf8af509922f3a787`. Defender engine `1.1.26080.3`, intelligence `1.459.239.0`, reported no detection/remediation event for the exact setup, staged payload, or installed tree.
- Deferred only the compiled-package seven-media smoke because an unrelated active VS Code/Inno updater held Inno's setup gate; the unrelated process was not terminated.
- Passed hosted application contracts run `35158374157` and patched-source build run `35158191892`; recorded the newer hosted-toolset DLL separately instead of treating different compiler bytes as the runtime-qualified release payload.

## Scaffold origin

- The document layout and agentic workflow were adapted from the MechWarrior 3 Remastered project.
- No MW3 compatibility baseline, native binary, configuration, qualified behavior, or release claim carries into this project automatically.
