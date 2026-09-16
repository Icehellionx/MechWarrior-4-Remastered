# MW4 research baseline

Updated: 2026-09-15. This file separates observed evidence from hypotheses; it is not a compatibility claim.

## Observed local media evidence

Both local Inner Sphere and Clan Mech Pak ISOs contain `CDAC14BA.DLL`/`CDAC21BA.DLL`, `SCSHD.CSA`, `SCSHD.EXE`, and `SECDRV.SYS`, alongside pack-specific resource/map/mission payloads and official Patch 3 installers/data. This is consistent with C-Dilla/SafeCast-era DRM and contradicts the tentative “16-bit encryption” description as currently stated.

Static inspection narrows that diagnosis: the pack setup engines and protected components are 32-bit x86 PE files, but the Clan `SCSHD.exe` explicitly references `CDILLA16.EXE`, the `\\.\pipe\C-Dilla\Srv` service endpoint, `\\.\C-Dilla`, and C-Dilla licence registry keys. The likely 64-bit break is therefore an obsolete C-Dilla/SafeCast activation stack containing a 16-bit helper, not 16-bit encryption of the game assets. This remains a static-evidence conclusion until reproduced in an isolated reference environment.

The Inner Sphere image also contains a `Razor1911` directory. That directory is third-party crack material and is excluded from any automatic or release input. Its presence means the image cannot be treated as pristine retail media solely by filename.

Black Knight contains a conventional game payload (`MW4X`, `RESOURCE`, movies), `SECDRV.SYS`, and an official Vengeance Patch 2 payload. Its disc `MW4X.EXE` and supplied local replacement both report version `45.05.10.0701` and have the same length but different hashes. The replacement is accepted only by exact SHA-256 (`2a5b7f2f…`) and is not a project asset.

Vengeance Disc 1 contains a SafeDisc loader (`MW4.exe`, SHA-256 `b129d968…`) and the real version `01.06.11.0220` game image in `MW4.ICD`. The supplied replacement executable is version `01.20.07.2403`, matching the official 2.0 patch generation, and is accepted only by exact SHA-256 (`4f5a2add…`). It remains user-supplied local input, not a redistributable project asset. The version 2.0 RTP data names the installer mappings `AUTOCO~1.EXE` → `AutoConfig.exe`, `SCRIPT~1.DLL` → `ScriptStrings.dll`, and `MISSIO~1.DLL` → `MissionLang.dll`.

The two recognized Vengeance discs plus that exact replacement produced a 231-file, 1.04 GB disposable full-install tree without running legacy setup or SafeDisc. Its ownership manifest verifies every length and SHA-256; setup/DRM files are absent and optical-media read-only flags are cleared. Runtime behavior is still unqualified.

The recognized Black Knight disc plus its exact-hash replacement produced a 138-file, 563,490,697-byte payload tree without running setup or loading disc protection. The plan excludes `SECDRV.SYS`, `DRVMGT.DLL`, `DSETUP.DLL`, and setup executables; every owned hash verifies and runtime behavior remains unqualified.

The supplied Mercenaries Disc 2 is UDF media that `bsdtar` did not enumerate but Windows mounted successfully. It contains the expected movie/map/mission payload plus a third-party `Crack/` directory and `SECDRV.SYS`; both are detected and excluded by policy. All seven images match distinct structural descriptors through read-only Windows mounts.

## External evidence to verify further

- Microsoft archived support article 325999 describes pack visibility as dependent on install order, game presence, and registry/install state.
- Microsoft community reports show Vengeance failing after pack installation on 64-bit Windows and recovering when packs are removed; these are useful reports, not root-cause proof.
- The 2010 free release was specifically a Microsoft-cleared MekTek distribution of MechWarrior 4: Mercenaries. That does not by itself prove the original Vengeance, Black Knight, retail Mercenaries, packs, or arbitrary mirrors remain freely redistributable today.

## Working hypotheses

1. Pack setup/activation fails because obsolete C-Dilla/SafeCast components are incompatible with or blocked on 64-bit/current Windows.
2. A safe path may extract only validated Microsoft pack payloads, apply the matching official patch resources, and reproduce necessary registry/configuration state without installing DRM drivers.
3. Exact per-game core/resource differences and install-order effects must be derived from clean before/after trees, not assumed from community replacement files.
4. The official Patch 3 RTP payload may contain the shared mech assets while pack-specific registry/licence state controls visibility. This must be proven from transformed trees; filenames and patch size are not enough.

## Open-source patch-tool evaluation

`bwrsandman/rtptool` revision `258d1750340917bcf1361a177b90abd16de40453` is MIT-licensed and conceptually fits the desired checksum-verified transform. Evaluation found that version 0.1.1 does not parse the supplied files: the 2.09 Vengeance payload misparses a negative entry count and the pack payload reports format version `0x025a`, newer than the tool's supported `0x0209`. Its CLI also joins untrusted record names directly to output paths. Do not vendor or invoke it in the product without format fixes, path containment, adversarial tests, and independent output comparison.

## Falsifying checks

- Inventory and hash both pack images and compare their non-DRM payloads.
- Inspect executable architecture/imports and installer metadata in disposable extraction.
- Diff clean Vengeance and Black Knight trees before/after each pack on a controlled 32-bit reference environment if needed.
- Trace registry/file writes from the original pack setup in an isolated VM.
- Prove each direct-extraction candidate produces the same owned files and game-visible pack markers without loading/installing legacy drivers.
