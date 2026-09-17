# Third-party notices

## SafeDiscLoader2

Black Knight PR1 uses a modified, source-built copy of SafeDiscLoader2 from commit `f27286a363aa675a0422141cb96fc8619cf8b9d8` only during setup-owned scratch processing. Setup exact-hash validates the capture DLL, uses it to obtain the already decrypted image from the user's exact official PR1 executable, then deterministically produces the static installed `MW4x.exe`. The capture DLL, configuration, and mapped image are deleted before commit and are never runtime files.

- Upstream: <https://github.com/nckstwrt/SafeDiscLoader2>
- License: GPL-3.0-only
- Packaged license: `Compatibility/BlackKnightPr1Capture/SafeDiscLoader2-LICENSE.txt`
- Exact corresponding source: `Compatibility/BlackKnightPr1Capture/SafeDiscLoader2-source-f27286a363aa675a0422141cb96fc8619cf8b9d8.zip`
- Exact project patch: `Compatibility/BlackKnightPr1Capture/SafeDiscLoader2-MW4-BlackKnight-PR1-Capture.patch`

The upstream elevated `VersionInjector` project is neither built nor distributed. SafeDiscLoader2's internal child-process technique is confined to setup scratch and never participates in ordinary game launch. There is no installed launch helper, proxy DLL, service, driver, or launch-time elevation; the launcher starts the deterministic static `MW4x.exe` directly as the current user.

## dgVoodoo2

MechWarrior 4: Vengeance, Black Knight, and Mercenaries use the stock x86 DirectX compatibility DLLs from dgVoodoo2 2.86.5 for modern D3D11 presentation, aspect-preserving scaling, and cursor stability. The project keeps the original cinematic path enabled; interactive cinematic qualification is tracked separately from this notice.

- Project: <https://github.com/dege-diosg/dgVoodoo2>
- Version: 2.86.5
- Complete evaluated archive SHA-256: `76b6893a0be81e3905a03f30f25202d6dc6128c3b8f7a21f2c33bcfabfa75ddf`
- Redistribution: the official dgVoodoo readme permits individual files to ship with a game or game mod. Standalone redistribution requires the complete ZIP; this project does not distribute dgVoodoo as a general-purpose framework.
- Exact provenance and component hashes are recorded in `third_party/dgVoodoo2.lock.json`.
