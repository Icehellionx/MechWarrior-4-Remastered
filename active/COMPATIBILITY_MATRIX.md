# Compatibility qualification matrix

Updated: 2026-09-16.

This matrix separates protection removal, launch privilege, and actual game-runtime qualification. A title is not release-ready merely because its process starts.

| Product/media revision | Protection evidence | Media-only route | Current result | Release gate |
|---|---|---|---|---|
| Vengeance Disc 1, `01.06.11.0220` | SafeDisc 1.5-era 344,863-byte loader plus 3,498,100-byte `MW4.ICD`; `DPLAYERX.DLL` is 138,752 bytes and `DRVMGT.DLL` is 34,304 bytes | Deterministically unwrap the user-media ICD, then apply official patches | SafeDiscLoader2 injection reaches the original CD error but cannot bypass SafeDisc 1; this is outside its documented 2.0–4.9 range | Reimplement/qualify a hash-gated SafeDisc 1 transform; compare exact output with known local evidence; apply official patch chain |
| Black Knight Disc 1, `45.05.10.0701` | Integrated SafeDisc 2.30-era executable; original SHA-256 `fa62e84b64a8ad2758b4d12213873d84b0004107176499d195f70e2bdaf3d6b7` | Original ISO executable plus source-built SafeDiscLoader2 DLL and project-owned non-elevating helper | **Media-only install and disc-free process launch passed**: two clean CI builds were byte-identical; a fresh 142-file manifest-owned tree launched a responsive `MechWarrior Black Knight` window after 10 seconds with no mounted ISO or UAC; current Defender found no threats | Verify menu/gameplay/config/save and representative hardware; qualify official Black Knight patch requirements before release |
| Mercenaries Disc 1, `50.06.09.3002` | SafeDisc 1.5-era 344,863-byte loader plus 3,895,418-byte `MW4MERCS.ICD`; contemporary protection tables also identify 1.50.020 | Deterministically unwrap the user-media ICD, then apply the qualified official update | Not runtime-tested through the new boundary | Same SafeDisc 1 transform gate as Vengeance, followed by Mercenaries-specific patch/runtime smoke |
| Inner Sphere / Clan Mech Paks | Plain resource payload plus SafeDisc-protected CD check and obsolete SafeCast/C-Dilla entitlement installer | Copy exact resource allowlist after required official game patch; replace entitlement effect without running C-Dilla | Resource overlay and rollback transactions pass; runtime visibility is not qualified | Controlled before/after entitlement evidence and in-game chassis/variant verification |

## Non-negotiable launch contract

- End users supply only original ISOs or ZIPs whose payload is those ISOs.
- Normal game launches run `asInvoker`; recurring UAC is a defect.
- No release includes `VersionInjector.exe`, legacy unwrappers, opaque fixed executables, serial material, SafeDisc drivers, or C-Dilla/SafeCast installers.
- Compatibility failures produce diagnostics. They never instruct users to disable Defender/antivirus or add exclusions.
- External compatibility code is pinned by repository revision, license, source build, output hash, and per-title smoke evidence before packaging.

## Evaluated upstream

- SafeDiscLoader2: `https://github.com/nckstwrt/SafeDiscLoader2`, GPL-3.0, evaluated release `v1.3` / commit `f27286a363aa675a0422141cb96fc8619cf8b9d8`, release ZIP SHA-256 `dd36f3b7c4eeaa022f6728f0f69feec09d76c8b892db1c614c8ff02d269d09a7`.
- Qualified source build: CI runs `35048315727` and `35048453580` at project commit `01ec0ec713f24d8f4e93761bec36427cec493383`; normalized `version.dll` SHA-256 `f26710840b1b6b0537c05b3e97c63171708c129b76a77ec3191d5652ee024959`; project helper SHA-256 `b80b440bf349438527e28547bf40276defc309c64b972400acf625a974eaab8d`.
- Evaluated release `version.dll`: 79,872 bytes, SHA-256 `2e9ca4bb2c650dafe2556176aeeef2fb5c4d733e398b45e467fae10c3195035b`.
- Upstream `VersionInjector.exe` is intentionally rejected because its project declares `RequireAdministrator`. Its evaluated hash is retained only as local evidence and must not enter a package.
- The historical unSafeDisc 1.5.5 binary has unclear redistribution/source terms and failed to initialize in a current-Windows oracle attempt. It is not a dependency candidate; it is at most behavioral evidence for an independently reviewable transform.
