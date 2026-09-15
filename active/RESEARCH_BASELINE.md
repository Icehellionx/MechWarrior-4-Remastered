# MW4 research baseline

Updated: 2026-09-15. This file separates observed evidence from hypotheses; it is not a compatibility claim.

## Observed local media evidence

Both local Inner Sphere and Clan Mech Pak ISOs contain `CDAC14BA.DLL`/`CDAC21BA.DLL`, `SCSHD.CSA`, `SCSHD.EXE`, and `SECDRV.SYS`, alongside pack-specific resource/map/mission payloads and official Patch 3 installers/data. This is consistent with C-Dilla/SafeCast-era DRM and contradicts the tentative “16-bit encryption” description as currently stated.

The Inner Sphere image also contains a `Razor1911` directory. That directory is third-party crack material and is excluded from any automatic or release input. Its presence means the image cannot be treated as pristine retail media solely by filename.

Black Knight contains a conventional game payload (`MW4X`, `RESOURCE`, movies), `SECDRV.SYS`, and an official Patch 2 payload.

The supplied Mercenaries Disc 2 is UDF media that `bsdtar` did not enumerate but Windows mounted successfully. It contains the expected movie/map/mission payload plus a third-party `Crack/` directory and `SECDRV.SYS`; both are detected and excluded by policy. All seven images match distinct structural descriptors through read-only Windows mounts.

## External evidence to verify further

- Microsoft archived support article 325999 describes pack visibility as dependent on install order, game presence, and registry/install state.
- Microsoft community reports show Vengeance failing after pack installation on 64-bit Windows and recovering when packs are removed; these are useful reports, not root-cause proof.
- The 2010 free release was specifically a Microsoft-cleared MekTek distribution of MechWarrior 4: Mercenaries. That does not by itself prove the original Vengeance, Black Knight, retail Mercenaries, packs, or arbitrary mirrors remain freely redistributable today.

## Working hypotheses

1. Pack setup/activation fails because obsolete C-Dilla/SafeCast components are incompatible with or blocked on 64-bit/current Windows.
2. A safe path may extract only validated Microsoft pack payloads, apply the matching official patch resources, and reproduce necessary registry/configuration state without installing DRM drivers.
3. Exact per-game core/resource differences and install-order effects must be derived from clean before/after trees, not assumed from community replacement files.

## Falsifying checks

- Inventory and hash both pack images and compare their non-DRM payloads.
- Inspect executable architecture/imports and installer metadata in disposable extraction.
- Diff clean Vengeance and Black Knight trees before/after each pack on a controlled 32-bit reference environment if needed.
- Trace registry/file writes from the original pack setup in an isolated VM.
- Prove each direct-extraction candidate produces the same owned files and game-visible pack markers without loading/installing legacy drivers.
