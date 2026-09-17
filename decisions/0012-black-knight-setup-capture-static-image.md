# 0012 — Black Knight setup-only capture and static runtime image

- Status: Accepted
- Date: 2026-09-17
- Supersedes: the runtime-loader portions of ADR 0007
- Preserves: ADR 0011's shared Vengeance-family topology and official update chain

## Context and evidence

Black Knight PR1 remains protected by a SafeDisc generation that current 64-bit Windows cannot execute through the original driver. The apparent app-local SafeDiscLoader2 solution was not actually injection-free: upstream child-process handling injected the DLL into SafeDisc's temporary process, and field evidence recorded the resulting fault.

A first setup-captured static image removed runtime injection but still failed when the original video/presentation path gained focus. A 32-bit debugger recorded execution at removed address `0x008A3504`. Structural comparison found two independently significant defects: 32 direct branches into the removed SafeDisc tail were absent from the transform map, and the rebuilt imports used a different FirstThunk slot order from the established PR1 game image. Existing code references those slots directly, so API-set equivalence was insufficient.

The historical PR1 replacement executable was used only as ignored, Defender-scanned structural evidence. It is not a source, packaged input, runtime dependency, or redistributable project asset.

## Decision

- Apply official Black Knight PR1 from exact qualified user media within the shared Vengeance-family staging tree.
- Use the exact source-built GPL capture DLL only inside setup-owned scratch to obtain the already decrypted mapped image. Contain the protected launcher and inherited temporary child in an owned kill-on-close Windows job so scratch cleanup cannot race or orphan the SafeDisc process family. Do not build or use `VersionInjector`, a project injector, a launch helper, a service, a driver, or broad process-name termination.
- Reconstruct a static five-section game executable using the qualified import descriptor and FirstThunk slot ordering, not merely the same set of APIs.
- Repair exactly 127 qualified branches out of the removed SafeDisc tail and scan all executable code for residual direct `E8` or `E9` targets in `0x008A3000..0x008A7FFF`. Any residual target fails installation closed.
- Truncate both SafeDisc-only tails and exact-hash gate the resulting 3,743,744-byte `MW4x.exe` as `7761c41d03e52f33091c6e9055ba4bb545be83d22082dbfed53b01faa630442b`.
- Delete the capture DLL, configuration, mapped image, and capture scratch before manifest generation. Install only the deterministic static executable and the separately qualified setup-accepted EULA module.
- Start the installed executable directly as the current user. Runtime must not depend on capture artifacts, injection, mounted media, UAC, or the historical comparison executable.

## Alternatives considered

- Launch-time app-local SafeDiscLoader2: rejected because it injects into the temporary protected process and was field-proven unstable.
- Keep static v2 `04fe9acf…`: rejected because it missed 32 tail branches and changed effective import-slot semantics.
- Ship the historical replacement executable: rejected for provenance, redistribution, reproducibility, and security-review reasons.
- Suppress original videos: rejected because cinematics are part of the preserved presentation and do not solve the executable defect.

## Consequences

- The setup compatibility package retains GPL license, exact corresponding source, and the project capture patch, but none of those files enter the installed game manifest.
- A compiler or capture-build hash change requires renewed source/provenance verification and setup-stage testing; a static output change requires a new ADR or explicit revision of this decision.
- Deterministic output and a responsive timed process are necessary but not sufficient. Focused intro-to-menu, Alt-Tab, pilot creation, mission load, full coordinator cleanup, packaged Defender scan, repair, and uninstall remain release gates.
- Retail Black Knight remains unsupported until its separate Vengeance Patch 2 prerequisite is implemented and qualified.

## Rollback

Disable Black Knight selection and launch. Do not restore launch-time injection or the static v2 image. Vengeance and Mercenaries remain independent capabilities under the existing shared-tree and ownership contracts.

## Verification

- Two captures with different resolved API addresses normalize to byte-identical v3 output.
- Static tests assert the exact 127-entry branch map, established import order, five-section result, output length/hash, and rejection of a synthetic residual tail target.
- A production-shaped disposable tree with setup-accepted EULA, corrected installed filenames, dgVoodoo2, and original videos enabled stayed correctly titled and responsive for 60 seconds across the prior fault boundary.
- A full Vengeance + Inner Sphere + Black Knight coordinator run installed and exact-verified 387 owned files, left no capture artifact or temporary process, and ownership-safe uninstall removed every owned path while preserving a synthetic unowned save.
- Interactive and complete packaged evidence remains open and must not be inferred from the timed smoke.
