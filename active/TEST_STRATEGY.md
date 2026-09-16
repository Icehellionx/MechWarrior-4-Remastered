# Test strategy and verification map

## Evidence layers

| Layer | Required evidence |
|---|---|
| Static | Compilation, parser/config validation, license metadata, manifest schema |
| Focused | Media recognition, transform hashes, path containment, crop geometry, save classification |
| Component | Mount/archive lifetime, staging/rollback, registry/config ownership, process classification |
| Smoke | Clean disposable install, launch where permitted, repair, uninstall, save preservation |
| Regression | Three games, both packs, disc/archive variants, success/failure cleanup |
| Release | Reproducible build, payload allowlist, checksums, Defender scans |
| Field | Sanitized logs from representative Windows/GPU/input configurations |

## Initial verification commands

```powershell
node --test tools/agent/aux-model.test.mjs
node tools/agent/aux-model.mjs --check
git status --short --ignored
dotnet run --project tests/MW4Remastered.Core.Tests/MW4Remastered.Core.Tests.csproj --configuration Release
dotnet build src/MW4Remastered.Installer/MW4Remastered.Installer.csproj --configuration Release
dotnet build src/MW4Remastered.Launcher/MW4Remastered.Launcher.csproj --configuration Release
& tests/PackagingContract.Tests.ps1
& tests/UserFlowContract.Tests.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tests/ApplicationPrivilegeBoundary.Tests.ps1
& tests/ReleaseTreePolicy.Tests.ps1
& tools/security/scan-with-defender.ps1 -Path <assembled-release-tree>
# Elevated, local-media-only gate:
& tests/MediaRecognitionSmoke.ps1 -Images $knownMediaImages
# Local manuals stay ignored:
python tools/manuals/clean_manuals.py Manuals output/pdf
python tools/manuals/render_covers.py output/pdf output/manual-covers
python tools/manuals/verify_manuals.py Manuals output/pdf tools/manuals/manuals.lock.json --cover-directory output/manual-covers
```

Native WinForms rendering is captured with the actual window renderer to check hierarchy, clipping, and disabled states. Automated clicking through the full interactive package/media flow remains a coverage gap; compilation or a static capture alone does not qualify interaction behavior.

`.github/workflows/application-ci.yml` runs the synthetic core suite, both application builds, privilege checks, release-tree policy, packaging contract, and compatibility-source contract on Windows. It uploads no binaries; packaging still requires separately qualified compatibility evidence and a current local Defender scan.

## Required contracts

- Input fixtures use synthetic trees or hashes/metadata; tests never copy proprietary game payloads into source.
- Media matching rejects unknown or partially matching revisions safely.
- Binary transforms require exact input and output hashes and fail without mutation on mismatch.
- Official Patch 3 application accepts only the locked user-media engine/payload hashes, runs in contained scratch through the source-owned `asInvoker` x86 host, exact-validates every result, and excludes the engine, RTP payload, raw ICD, C-Dilla, ARTP client, and patch utilities from the installed tree.
- Internal compatibility bundles require exact inventory and hashes for installed binaries/notices plus any distribution-only corresponding source; extra files fail even when installed destinations are otherwise allowlisted.
- Extraction prevents traversal, links/reparse escape, device paths, and writes outside staging.
- Media selected during intake is re-opened and structurally re-recognized under the install transaction lifetime; changed or missing sources fail before mutation and release earlier resources.
- Cancellation is cooperative at bounded copy/mount/inspection boundaries and must release owned mounts and scratch before completion is reported.
- Install commits atomically where practical and removes partial state after failure.
- Reinstall after ownership-safe uninstall must preserve non-colliding logs, settings, saves, screenshots, and mods byte-for-byte while atomically committing a fresh owned payload. A preserved path that collides with an owned payload path must fail before mutation.
- Install cancellation is accepted through bounded staging operations and immediately before commit; after atomic commit, exact-tree verification is non-cancellable and must finish.
- Additive overlays require a verified matching base, refuse all destination replacement, atomically update the ownership manifest, and roll new files back if manifest commit or final verification fails.
- A Vengeance installation with one or both Mech Paks must exactly verify the combined ownership manifest, derive both launcher indicators from exact file evidence, launch without mounted media/elevation, and uninstall all owned pack files while preserving an unowned save.
- Uninstall validates the exact install root and ownership manifest before deletion, rejects links/reparse points, preserves only documented user data, and retains recovery data if restore fails.
- Launcher removal may target a verified or repair-required product directory, but never a merely inferred path; whole-application removal is owned separately by the standard package uninstaller.
- The package installs per user without elevation, contains no broad uninstall-delete rule, and must preserve unowned game/save/configuration files during shell uninstall.
- Two clean release builds from identical declared inputs must produce byte-identical setup executables and checksum sidecars.
- Launcher status is derived from verified files/configuration, not registry keys alone.
- Manual transforms assert page count/order/dimensions and render all pages for visual QA.
- Release assembly exact-hash validates and packages exactly the three cleaned manuals plus their three deterministic cover PNGs; launcher contract tests retain a distinct icon for every installed game and load the corresponding cover for every manual.
- Every executable's non-system runtime imports must be represented by the title install plan. Fresh corrected Vengeance, Black Knight, and Mercenaries trees must remain responsive for at least 30 seconds before a package is handed to field testing. A responsive window alone is insufficient: any delayed `STOP`, `EXCEPTION`, fullscreen, or incorrect-install log is a failure.
- Vengeance and Mercenaries package smokes must exercise the real launch orchestrator, verify the narrow per-user compatibility registration, and prove that uninstall removes only the matching project-owned records.
- Unified uninstall must close the launcher and invoke the standard shell uninstaller in visible-progress silent mode after owned-game removal; field acceptance must confirm the launcher, manuals, shortcuts, and registration disappear after the asynchronous handoff.
- Setup must remain the only visible installation UI. Contract tests reject `Application.Run`/`InstallerForm`, require a hidden synchronous worker invocation, and an unattended package smoke supplies media through the same Inno page state before verifying the installed shell and game manifest.
- Setup errors must retain the worker log outside Inno's self-deleting temporary directory and display that durable path.
- Diagnostics redact serials, credentials, private paths, and media content.
- Release tests block ISOs, BIN/CUE/MDF/MDS, serial files, raw cracks, `.env`, dumps, and unintended executables.
- Defender qualification scans every assembled package, extracted package tree, and representative installed tree with the current engine/intelligence. A clean exit alone is insufficient because exit `0` can also mean successful remediation; the scan gate also rejects new matching detection/remediation events.

## Behavioral slice record

```text
Behavior changed:
Owning boundary:
Focused command and assertion:
Primary smoke path:
Adjacent games/packs/media paths:
Cleanup/failure paths:
Release scan required? why/why not:
Affected hardware evidence required? why/why not:
```
