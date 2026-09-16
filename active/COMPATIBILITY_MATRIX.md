# Compatibility qualification matrix

Updated: 2026-09-16.

This matrix separates protection removal, launch privilege, and actual game-runtime qualification. A title is not release-ready merely because its process starts.

| Product/media revision | Protection evidence | Media-only route | Current result | Release gate |
|---|---|---|---|---|
| Vengeance Disc 1, `01.06.11.0220` | SafeDisc 1.50.20/r4 344,863-byte loader plus 3,498,100-byte `MW4.ICD`; official Patch 2 retains the loader size and produces a 3,616,885-byte ICD | Unwrap the exact user-media ICD with the source-owned block/second-layer/PE transform, apply one reviewed disc-check branch change, then qualify later official patches separately | Managed exact-input transform produces deterministic SHA-256 `48ba4baf…`; its output opened a responsive `MechWarrior Vengeance` window without media or UAC and excludes legacy protection components from the install plan | Fresh coordinator install, exact-tree verification, Defender scan, launch, and uninstall; then official patch/runtime qualification |
| Vengeance official Patch 3, `01.30.04.1908` | Exact 40,042,724-byte RTP from Inner Sphere media changes the SafeDisc loader metadata and adds C-Dilla/ARTP imports | Apply the exact-hash official payload through the source-owned non-elevating x86 host in minimal scratch, validate all outputs, retain 11 files, and replace the obsolete EULA/media calls with call-convention-correct local results | Patch 3 applies directly to retail without Patch 2. Managed SHA-256 `9f0b9cab…`; a fresh Vengeance plus both-packs tree passed exact verification and remained responsive with an empty log | Prove pack chassis/variants visible in game and exercise packaged UI plus ownership-safe uninstall |
| Black Knight Disc 1, `45.05.10.0701` | Integrated SafeDisc 2.30-era executable; original SHA-256 `fa62e84b64a8ad2758b4d12213873d84b0004107176499d195f70e2bdaf3d6b7` | Compose the verified manifest-owned Vengeance base with Black Knight media; transform the exact EULA export after explicit setup acceptance; normally load a locally patched source-built SafeDiscLoader2 DLL that signature-checks and bypasses only two obsolete installed-media calls at OEP | **Fresh seven-media install and repeated launch passed**: final tree exactly verified; three consecutive 20-second launches each had one responsive `MechWarrior Black Knight` process and a zero-length log; no mounted media, helper, injector, launch UAC, or Defender detection | Verify menu/gameplay/config/save and representative hardware; qualify official Black Knight patch requirements before release |
| Mercenaries Disc 1, `50.06.09.3002` | SafeDisc 1.50.20 344,863-byte loader plus 3,895,418-byte `MW4MERCS.ICD` and C-Dilla title gate | Deterministically unwrap the exact user-media ICD, repair imports/pointers, remove the C-Dilla descriptor/title gate, and redirect only the protected setup-record roots to 32-bit per-user HKCU | Managed exact-input transform produces deterministic SHA-256 `039b5781…`; a packaged tree plus launcher-owned product record remained responsive with an empty log and no media/UAC | Official patch plus broader menu/gameplay/config/save/hardware qualification |
| Inner Sphere / Clan Mech Paks | Plain resource payload plus SafeDisc-protected CD check and obsolete SafeCast/C-Dilla entitlement installer | Copy exact resource allowlist after required official game patch; replace entitlement effect without running C-Dilla | Resource overlay/rollback passes; Patch 3 plus Inner Sphere resources reaches a responsive process without obsolete dependencies; runtime content visibility is not yet qualified | Controlled before/after entitlement evidence and in-game chassis/variant verification |

## Non-negotiable launch contract

- End users supply only original ISOs or ZIPs whose payload is those ISOs.
- Setup requests one administrator consent for ISO mounting and all install-time work. The installed launcher, compatibility host, and normal game launches run `asInvoker`; any launch-time UAC is a defect.
- No release includes `VersionInjector.exe`, legacy unwrappers, opaque fixed executables, serial material, SafeDisc drivers, or C-Dilla/SafeCast installers.
- Compatibility failures produce diagnostics. They never instruct users to disable Defender/antivirus or add exclusions.
- External compatibility code is pinned by repository revision, license, source build, output hash, and per-title smoke evidence before packaging.

## Evaluated upstream

- SafeDiscLoader2: `https://github.com/nckstwrt/SafeDiscLoader2`, GPL-3.0, evaluated release `v1.3` / commit `f27286a363aa675a0422141cb96fc8619cf8b9d8`, release ZIP SHA-256 `dd36f3b7c4eeaa022f6728f0f69feec09d76c8b892db1c614c8ff02d269d09a7`.
- Qualified patched source build: two clean local builds from upstream commit `f27286a363aa675a0422141cb96fc8619cf8b9d8` plus local patch SHA-256 `286de586…` were byte-identical; normalized `version.dll` is 82,944 bytes with SHA-256 `ff530c8144ebf82b3f32951c8cd3b1085b6527b46d55348bb1c7f8ef7f83296a`. The exact upstream source archive and local patch ship beside the GPL notice.
- Evaluated release `version.dll`: 79,872 bytes, SHA-256 `2e9ca4bb2c650dafe2556176aeeef2fb5c4d733e398b45e467fae10c3195035b`.
- Upstream `VersionInjector.exe` and the former project launch helper are intentionally rejected and removed. Neither is built or packaged.
- The historical unSafeDisc 1.5.5 binary has unclear redistribution/source terms and is not a dependency candidate. A disposable copy initialized on current Windows only after embedding an explicit `asInvoker` manifest, which is useful behavioral evidence but does not resolve its licensing, provenance, GUI-only operation, or suitability for distribution.
