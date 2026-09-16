# 0006 — Non-elevating compatibility launch boundary

- Status: Accepted
- Date: 2026-09-15

## Context and evidence

MechWarrior 4's retail executables use obsolete SafeDisc generations that cannot use the removed `secdrv` kernel service on current Windows. The evaluated GPL-3.0 SafeDiscLoader2 release can load its compatibility DLL into the original Vengeance executable, but its `VersionInjector` project explicitly declares `RequireAdministrator`. A bounded local smoke test therefore produced repeated UAC interaction while evaluating relaunch behavior. That is incompatible with an all-in-one launcher whose installed games should start normally.

A project-owned 32-bit proof-of-contract helper was then built with an explicit `asInvoker` manifest. It created an original Vengeance process suspended, loaded only the adjacent compatibility DLL, resumed the process, exited successfully, and reached the same responsive Vengeance CD-check dialog without elevation. This proves elevation is not inherently required for same-user, same-integrity launch-time loading. It does not yet qualify the compatibility DLL or solve Vengeance's remaining raw optical-media check.

## Decision

- Installed game launch must run at the calling user's integrity level and must never request administrator elevation.
- Installer, repair, and uninstaller operations may elevate only when their selected destination or owned system changes require it. Their privilege is not inherited by normal game launch.
- The compatibility launch helper is constrained to the three adjacent product executable names and the adjacent `version.dll`; it does not accept arbitrary target processes or DLL paths.
- The helper creates its child suspended, loads compatibility before original protection startup, terminates the child on any pre-resume failure, and closes every process/thread handle it owns.
- The upstream `VersionInjector.exe` is evaluation evidence only and is excluded from product packages.
- The compatibility DLL remains an unqualified external dependency until its pinned source, GPL obligations, reproducible build, title-specific configuration, Defender result, and disc-free smoke tests are all recorded.
- No release or support workflow may advise disabling antivirus, adding exclusions, or approving recurring UAC prompts.

## Alternatives considered

- Ship the upstream elevated injector: rejected because every game launch would cross an unnecessary administrator boundary.
- Run the entire launcher elevated: rejected because it expands the blast radius of UI, document, and game-process actions.
- Ship opaque replacement executables: rejected as the default because provenance, reproducibility, licensing, and malware review are weaker than a source-built compatibility path.
- Modify protected executables in place: deferred until a narrow, deterministic transform can be proven across known media revisions.

## Consequences

- Packaging must produce and retain a 32-bit helper even if the main launcher is 64-bit.
- Launch orchestration will eventually target the helper rather than the protected game executable directly, while installation health continues to hash both original media-derived files and project compatibility files.
- Injection-sensitive security products may still block the technique. That must be treated as a failed compatibility path with honest diagnostics, never as a request to weaken endpoint security.
- The current helper is not wired into production installation or launch until the DLL boundary and remaining CD checks pass.

## Rollback

Remove the helper from packaging and restore direct executable launch. Media-derived game files remain untouched, so no game-tree reversal is required beyond the ownership-aware uninstall/repair transaction.

## Verification

- Release build completed with zero warnings and errors for `win-x86`.
- Embedded manifest extraction confirmed `requestedExecutionLevel level="asInvoker"`.
- A bounded Vengeance smoke test returned helper exit code `0`, produced a responsive game process, and reached the expected CD-check dialog without an elevation request.
- `tests/CompatLauncherBoundary.Tests.ps1` locks the architecture, privilege level, target/DLL constraints, least-access source rule, and failed-child cleanup contract.
