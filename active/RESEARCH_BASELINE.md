# MW4 research baseline

Updated: 2026-09-16. This file separates observed evidence from hypotheses; it is not a compatibility claim.

## Observed local media evidence

Both local Inner Sphere and Clan Mech Pak ISOs contain `CDAC14BA.DLL`/`CDAC21BA.DLL`, `SCSHD.CSA`, `SCSHD.EXE`, and `SECDRV.SYS`, alongside pack-specific resource/map/mission payloads and official Patch 3 installers/data. This is consistent with C-Dilla/SafeCast-era DRM and contradicts the tentative “16-bit encryption” description as currently stated.

Static inspection narrows that diagnosis: the pack setup engines and protected components are 32-bit x86 PE files, but the Clan `SCSHD.exe` explicitly references `CDILLA16.EXE`, the `\\.\pipe\C-Dilla\Srv` service endpoint, `\\.\C-Dilla`, and C-Dilla licence registry keys. The likely 64-bit break is therefore an obsolete C-Dilla/SafeCast activation stack containing a 16-bit helper, not 16-bit encryption of the game assets. This remains a static-evidence conclusion until reproduced in an isolated reference environment.

The actual pack data is directly readable and not encrypted: Inner Sphere exposes 10 `RESOURCE` files totaling 34,434,947 bytes, and Clan exposes 10 totaling 20,285,495 bytes. Each contains two maps plus mission metadata/assets. Neither pack payload collides with the staged Vengeance or Black Knight trees. Six names collide with Mercenaries, but four of those Mercenaries files differ by hash; blindly overlaying the retail pack payload onto Mercenaries would therefore overwrite later/different content and is not acceptable.

Both on-disc readmes say the packs add content to Vengeance and Black Knight, require Vengeance, must be reinstalled after Black Knight to become visible there, and automatically apply Vengeance Patch 3 or Black Knight Patch 1. The setup DLLs look up `HKLM\SOFTWARE\Microsoft\Microsoft Games\MechWarrior Vengeance\Exe Path`, while the disc-protection layer separately requests a product key and SafeCast/C-Dilla licensing. This establishes three distinct contracts: plain resource overlay, correct official game patch level, and entitlement/visibility.

The Inner Sphere image also contains a `Razor1911` directory. That directory is third-party crack material and is excluded from any automatic or release input. Its presence means the image cannot be treated as pristine retail media solely by filename.

Black Knight contains a conventional game payload (`MW4X`, `RESOURCE`, movies), `SECDRV.SYS`, and an official Vengeance Patch 2 payload. Its disc `MW4X.EXE` and historical local replacement both report version `45.05.10.0701` and have the same length but different hashes. The replacement was useful comparison evidence but is no longer accepted by the install plan and is not a project asset.

Vengeance Disc 1 contains a SafeDisc loader (`MW4.exe`, SHA-256 `b129d968…`) and the real version `01.06.11.0220` game image in `MW4.ICD`. A previously supplied replacement executable is version `01.20.07.2403`, matching the official 2.0 patch generation, but it is now comparison evidence only and is not an installer input. The version 2.0 RTP data names the installer mappings `AUTOCO~1.EXE` → `AutoConfig.exe`, `SCRIPT~1.DLL` → `ScriptStrings.dll`, and `MISSIO~1.DLL` → `MissionLang.dll`.

The original retail loader/ICD/DPLAYER set is sufficient for a disc-free version `01.06.11.0220` executable. Static and emulated analysis derived the four exact block-cipher words after validating 192 of 194 legacy imports. The reviewed managed implementation removes the 1.50.20 page-local second layer, repairs imports and protected pointers, canonicalizes inert legacy padding, normalizes the raw end, and changes one uniquely matched conditional disc-check branch. Its deterministic 3,497,984-byte result has SHA-256 `48ba4baf0894f0c14c44ddaf503cf953f59be3b919817156f1f11ea9b0df0de4` and opened a responsive `MechWarrior Vengeance` window without mounted media or UAC. Exact input hashes fail closed, and no historical unwrapper or emulator is a product dependency.

The two recognized Vengeance discs plus that exact replacement produced a 231-file, 1.04 GB disposable full-install tree without running legacy setup or SafeDisc. Its ownership manifest verifies every length and SHA-256; setup/DRM files are absent and optical-media read-only flags are cleared. Runtime behavior is still unqualified.

The current Black Knight plan uses the recognized disc plus an exact-hash internal compatibility bundle, not a replacement game executable. It preserves the original `MW4X.EXE`, includes required `DRVMGT.DLL`, excludes `SECDRV.SYS`, `DSETUP.DLL`, and setup executables, and produced an exactly verified 142-file tree. The manifest-owned non-elevating helper and reproducibly source-built loader launched that tree to a responsive game window without mounted media or UAC; broader runtime behavior remains unqualified.

The supplied Mercenaries Disc 2 is UDF media that `bsdtar` did not enumerate but Windows mounted successfully. It contains the expected movie/map/mission payload plus a third-party `Crack/` directory and `SECDRV.SYS`; both are detected and excluded by policy. All seven images match distinct structural descriptors through read-only Windows mounts.

Mercenaries Disc 1 stores 85 `GAME/RESOURCE/...` payload entries in `MSGAME.CAB`; a containment-first extraction reproduced exactly those 85 files (615,038,509 bytes) without running setup. The disc SafeDisc image reports version `50.06.09.3002`; the exact-hash local replacement reports `50.07.01.2105` (`eff39b2f…`). Combining the verified cabinet root, allowlisted loose Disc 1 runtime files, and Disc 2 content produced a 209-file, 1,206,212,339-byte payload tree. C-Dilla, SafeDisc, setup, and Disc 2 crack files are absent; runtime behavior remains unqualified.

## External evidence

- [Archived Microsoft support article 325999](https://www.betaarchive.com/wiki/index.php/Microsoft_KB_Archive/325999) describes missing pack logos/content, install-order interactions, and an official CD-dependent SafeCast Repair Utility. It corroborates that payload presence and SafeCast entitlement are separate conditions.
- [Microsoft's surviving disc-check support article](https://support.microsoft.com/en-us/topic/error-message-or-the-game-stops-responding-on-the-loading-screen-when-you-start-a-microsoft-game-please-insert-the-correct-cd-rom-c9eb279d-e016-c079-6afa-5bb37dfc1018) names the Inner Sphere pack among protected products and directs users to launch it with Vengeance Disc 1 or Black Knight media.
- Microsoft community reports show Vengeance failing after pack installation on 64-bit Windows and recovering when packs are removed; these are useful reports, not root-cause proof.
- The 2010 free release was specifically a Microsoft-cleared MekTek distribution of MechWarrior 4: Mercenaries. That does not by itself prove the original Vengeance, Black Knight, retail Mercenaries, packs, or arbitrary mirrors remain freely redistributable today.

## Working hypotheses

1. A safe path may extract only validated Microsoft pack payloads, apply the matching official patch resources, and replace the obsolete entitlement check reproducibly without installing DRM drivers.
2. Exact per-game core/resource differences and install-order effects must be derived from clean before/after trees, not assumed from community replacement files.
3. The official Patch 3 RTP payload may contain shared support for the new assets while SafeCast licence state controls visibility. This must be proven from transformed trees; filenames and patch size are not enough.

## Open-source patch-tool evaluation

`bwrsandman/rtptool` revision `258d1750340917bcf1361a177b90abd16de40453` is MIT-licensed and conceptually fits the desired checksum-verified transform. Evaluation found that version 0.1.1 does not parse the supplied files: the 2.09 Vengeance payload misparses a negative entry count and the pack payload reports format version `0x025a`, newer than the tool's supported `0x0209`. Its CLI also joins untrusted record names directly to output paths. Do not vendor or invoke it in the product without format fixes, path containment, adversarial tests, and independent output comparison.

## Falsifying checks

- Diff clean Vengeance and Black Knight trees before/after each pack on a controlled 32-bit reference environment if needed.
- Trace registry/file writes from the original pack setup in an isolated VM.
- Prove each direct-extraction candidate produces the same owned files and game-visible pack markers without loading/installing legacy drivers.
