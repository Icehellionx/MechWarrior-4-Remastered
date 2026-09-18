# 0006 — Non-elevating compatibility launch boundary

- Status: Superseded by ADR 0012
- Date: 2026-09-15

## Context and evidence

MechWarrior 4's retail executables use obsolete SafeDisc generations that cannot use the removed `secdrv` kernel service on current Windows. The evaluated GPL-3.0 SafeDiscLoader2 release can load its compatibility DLL into the original Vengeance executable, but its `VersionInjector` project explicitly declares `RequireAdministrator`. A bounded local smoke test therefore produced repeated UAC interaction while evaluating relaunch behavior. That is incompatible with an all-in-one launcher whose installed games should start normally.

A project-owned 32-bit proof-of-contract helper was then built with an explicit `asInvoker` manifest. It created an original Vengeance process suspended, loaded only the adjacent compatibility DLL, resumed the process, exited successfully, and reached the same responsive Vengeance CD-check dialog without elevation. This proves elevation is not inherently required for same-user, same-integrity launch-time loading. It does not yet qualify the compatibility DLL or solve Vengeance's remaining raw optical-media check.

Follow-up testing also reproduced Windows installer-detection heuristics: legacy executables without a requested-execution-level manifest were elevated based partly on names such as `patch` and `unSafeDisc`, while a disposable copy with an embedded `asInvoker` manifest ran at the caller's integrity level. The project must therefore declare the privilege contract explicitly in every executable; relying on filenames or SDK defaults is not acceptable.

## Historical decision

- Installed game launch must run at the calling user's integrity level and must never request administrator elevation.
- Installer, launcher, and compatibility helper all embed `asInvoker` manifests. Any future privileged operation must be isolated behind an explicit, narrow broker and must never be inherited by normal game launch.
- The compatibility launch helper is constrained to the three adjacent product executable names and the adjacent `version.dll`; it does not accept arbitrary target processes or DLL paths.
- The helper creates its child suspended, loads compatibility before original protection startup, terminates the child on any pre-resume failure, and closes every process/thread handle it owns.
- The upstream `VersionInjector.exe` is evaluation evidence only and is excluded from product packages.
- The compatibility DLL is qualified for the recognized Black Knight revision under ADR 0007. Vengeance and Mercenaries remain outside that decision because their SafeDisc 1 loader/ICD design requires a different transform.
- No release or support workflow may advise disabling antivirus, adding exclusions, or approving recurring UAC prompts.

## Alternatives considered

- Ship the upstream elevated injector: rejected because every game launch would cross an unnecessary administrator boundary.
- Run the entire launcher elevated: rejected because it expands the blast radius of UI, document, and game-process actions.
- Ship opaque replacement executables: rejected as the default because provenance, reproducibility, licensing, and malware review are weaker than a source-built compatibility path.
- Modify protected executables in place: deferred until a narrow, deterministic transform can be proven across known media revisions.

## Historical consequences

- The helper route was later rejected in field testing and removed. ADR 0012 replaces it with setup-only capture and a deterministic static Black Knight runtime image; ordinary launch now starts the game executable directly without a helper, proxy DLL, injection, or elevation.

## Rollback

Remove the helper from packaging and restore direct executable launch. Media-derived game files remain untouched, so no game-tree reversal is required beyond the ownership-aware uninstall/repair transaction.

## Verification

- Release build completed with zero warnings and errors for `win-x86`.
- Embedded manifest extraction confirmed `requestedExecutionLevel level="asInvoker"`.
- A bounded Vengeance smoke test returned helper exit code `0`, produced a responsive game process, and reached the expected CD-check dialog without an elevation request.
- `tests/ApplicationPrivilegeBoundary.Tests.ps1` locks explicit `asInvoker` manifests for both the installer and launcher so Windows installer-name heuristics cannot silently reintroduce UAC.
- ADR 0012 and the current core/packaging contracts verify that the superseding setup-only capture artifacts never enter the installed game runtime.
