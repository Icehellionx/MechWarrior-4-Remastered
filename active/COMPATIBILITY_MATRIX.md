# Compatibility qualification matrix

Updated: 2026-09-16.

This matrix separates protection removal, launch privilege, and actual game-runtime qualification. A title is not release-ready merely because its process starts.

| Product/media revision | Protection evidence | Media-only route | Current result | Release gate |
|---|---|---|---|---|
| Vengeance Disc 1, `01.06.11.0220` | SafeDisc 1.50.20/r4 344,863-byte loader plus 3,498,100-byte `MW4.ICD`; official Patch 2 retains the loader size and produces a 3,616,885-byte ICD | Compare the source-owned exact-media transform against a known-good original-installer tree before applying later patches | The transform hash remains deterministic, but a responsive window was not proof of a valid launch. Field testing reached the incorrect-install dialog | Reference file/registry parity, actual menu entry, one gameplay path, Defender scan, and uninstall |
| Vengeance official Patch 3, `01.30.04.1908` | Exact 40,042,724-byte RTP from Inner Sphere media changes the SafeDisc loader metadata and adds C-Dilla/ARTP imports | Apply the exact-hash official payload through the source-owned x86 host, validate outputs, and compare against a reference installation | Patch 3 applies directly to retail without Patch 2, but field testing reached the incorrect-install dialog. The former responsive/empty-log result is not a runtime pass | Reference parity, actual menu and gameplay, pack visibility, and ownership-safe uninstall |
| Black Knight PR1, `45.30.04.1908` | Retail `45.05.10.0701` is integrated SafeDisc 2.30; exact official PR1 output is `MW4X/MW4x.exe` SHA-256 `be9c1573…` | Build the shared Vengeance/`MW4X` tree, apply exact official PR1, then evaluate disc-check handling against this revision only | Contained PR1 application passed with no missing/invalid inputs. The old retail separate-tree runtime evidence is invalid | Shared-tree/reference-registry parity, PR1-specific disc-check proof, actual menu/gameplay/config/save, no launch UAC, and representative hardware |
| Mercenaries PR1, `50.07.01.2105` | Exact official PR1 produces a 344,863-byte loader plus 3,915,897-byte `MW4MERCS.ICD` SHA-256 `aafff190…`; C-Dilla/title gates remain | Apply PR1 before deriving any disc-free executable, then compare registry and output against a reference installation | Contained PR1 application passed all 11 operations. The retail `50.06.09.3002` transformed runtime result is invalid product evidence | PR1 transform/output lock, reference registry/file parity, actual menu/gameplay/config/save, no launch UAC, and hardware qualification |
| Inner Sphere / Clan Mech Paks | Plain resource payload plus SafeDisc-protected CD check and obsolete SafeCast/C-Dilla entitlement installer | Copy exact resource allowlist after required official game patch; replace entitlement effect without running C-Dilla | Resource overlay/rollback passes; Patch 3 plus Inner Sphere resources reaches a responsive process without obsolete dependencies; runtime content visibility is not yet qualified | Controlled before/after entitlement evidence and in-game chassis/variant verification |

## Non-negotiable launch contract

- End users supply only original ISOs or ZIPs whose payload is those ISOs.
- Setup requests one administrator consent for ISO mounting and all install-time work. The installed launcher, compatibility host, and normal game launches run `asInvoker`; any launch-time UAC is a defect.
- No release includes `VersionInjector.exe`, legacy unwrappers, opaque fixed executables, serial material, SafeDisc drivers, or C-Dilla/SafeCast installers.
- Compatibility failures produce diagnostics. They never instruct users to disable Defender/antivirus or add exclusions.
- External compatibility code is pinned by repository revision, license, source build, output hash, and per-title smoke evidence before packaging.

## Evaluated upstream

- SafeDiscLoader2: `https://github.com/nckstwrt/SafeDiscLoader2`, GPL-3.0, evaluated release `v1.3` / commit `f27286a363aa675a0422141cb96fc8619cf8b9d8`, release ZIP SHA-256 `dd36f3b7c4eeaa022f6728f0f69feec09d76c8b892db1c614c8ff02d269d09a7`.
- Suspended patched source build: two clean local builds from upstream commit `f27286a363aa675a0422141cb96fc8619cf8b9d8` plus local patch SHA-256 `286de586…` were byte-identical; normalized `version.dll` is 82,944 bytes with SHA-256 `ff530c8144ebf82b3f32951c8cd3b1085b6527b46d55348bb1c7f8ef7f83296a`. It targeted the retail Black Knight revision and is not a current package input.
- dinputto8: `https://github.com/elishacloud/dinputto8`, zlib-licensed DirectInput 1-7 to DirectInput 8 adapter; recent MW4 guidance places it beside all three executables. It is an unqualified A/B-test candidate after setup/DRM parity, not a current dependency.
- Evaluated release `version.dll`: 79,872 bytes, SHA-256 `2e9ca4bb2c650dafe2556176aeeef2fb5c4d733e398b45e467fae10c3195035b`.
- Upstream `VersionInjector.exe` and the former project launch helper are intentionally rejected and removed. Neither is built or packaged.
- The historical unSafeDisc 1.5.5 binary has unclear redistribution/source terms and is not a dependency candidate. A disposable copy initialized on current Windows only after embedding an explicit `asInvoker` manifest, which is useful behavioral evidence but does not resolve its licensing, provenance, GUI-only operation, or suitability for distribution.
