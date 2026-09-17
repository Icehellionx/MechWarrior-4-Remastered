# 0007 — Media-derived Black Knight compatibility bundle

- Status: Superseded by ADR 0011
- Date: 2026-09-16

## Context and evidence

> Correction (2026-09-16): later field testing showed that the responsive window classified below was the game's `incorrectly installed` dialog. The tested executable was the retail `45.05.10.0701` build in a flattened standalone tree, while the official PR1 produces `45.30.04.1908` under the original shared `Vengeance\MW4X` topology. The bundle remains research evidence only and is suspended from installation policy.

Black Knight's original `MW4X.EXE` is an integrated SafeDisc 2.30-era executable that cannot use the removed SafeDisc driver on current Windows. The previously staged replacement executable was opaque and would have required an extra user-supplied file, contrary to the ISO-only product contract.

SafeDiscLoader2 v1.3 is GPL-3.0-only source at pinned commit `f27286a363aa675a0422141cb96fc8619cf8b9d8`. Its loader DLL works with the untouched Black Knight disc executable, while its upstream elevated injector is unnecessary and remains excluded under ADR 0006.

Two clean GitHub-hosted Windows builds of project commit `01ec0ec713f24d8f4e93761bec36427cec493383` produced byte-identical compatibility artifacts. The normalized loader is SHA-256 `f26710840b1b6b0537c05b3e97c63171708c129b76a77ec3191d5652ee024959`; the self-contained project helper is `b80b440bf349438527e28547bf40276defc309c64b972400acf625a974eaab8d`; and the corresponding-source archive is `78ae295db0382f498829546ff7272db5eb2e713c20eb72ab3552ceaf8477cd17`.

## Decision

- Black Knight installation consumes only recognized original media plus a project-packaged, exact-hash compatibility bundle.
- The original media-derived `MW4X.EXE` and required `DRVMGT.DLL` remain unchanged.
- The bundle contains only the project-owned `asInvoker` helper, source-built `version.dll`, and GPL license in the installed tree. Release artifacts must also carry the exact corresponding source archive.
- The loader is built from the pinned upstream commit by the declared CI workflow. Non-semantic PE timestamps are normalized, and the helper excludes repository revision metadata so rebuilds are deterministic.
- Installation fails closed if any compatibility payload hash differs. Launch uses the helper only when it is manifest-owned and verifies successfully.
- `VersionInjector.exe`, `unSafedisc155.exe`, replacement game executables, obsolete drivers, and original media are not release payloads.

## Alternatives considered

- Continue requiring a local replacement executable: rejected because it violates the ISO-only goal and lacks adequate provenance.
- Package the upstream release DLL and injector: rejected because the injector requests elevation and the build would not be controlled by this project.
- Patch the original executable in place: deferred for SafeDisc 1 titles, but unnecessary for this qualified Black Knight path.

## Consequences

- The public package must satisfy GPL source and notice obligations for the included DLL.
- A Black Knight install owns 142 files for the currently recognized media revision, including the three installed compatibility files.
- Hash changes require a new qualification record, two clean reproducibility builds, launch smoke, and Defender scan.
- Menu, gameplay, renderer, audio, input, configuration, and save coverage remain separate release gates.

## Rollback

Remove the compatibility bundle from packaging and disable Black Knight launch. Ownership-aware uninstall removes the bundle while preserving unowned user files; original media-derived files require no reverse patch.

## Verification

- CI runs `35048315727` and `35048453580` completed successfully at the same project commit and produced byte-identical packaged files.
- A fresh install from the recognized extracted ISO tree plus the internal bundle committed and exactly verified 142 manifest-owned files.
- With no image mounted, the installed helper launched a responsive window titled `MechWarrior Black Knight` after ten seconds without UAC.
- Current Microsoft Defender scanning of the full fresh install reported no threats.
- Uninstall from a disposable copy removed all 143 owned entries, including the manifest, while preserving the sole synthetic unowned configuration file.
