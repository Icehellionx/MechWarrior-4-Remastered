# Third-party notices

MechWarrior, BattleTech, the original games, manuals, and related artwork are the work and property of their respective original authors and rights holders, including Microsoft, FASA Interactive, and Cyberlore Studios. This preservation project does not claim authorship of those works and requires original user-supplied game media.

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

## Inno Setup

The all-in-one Windows setup is compiled with Inno Setup 7.1.0 by Jordan Russell and Martijn Laan. The project uses the unmodified compiler/runtime to package the project-owned launcher and media-processing workflow.

- Project: <https://jrsoftware.org/isinfo.php>
- Copyright: Jordan Russell and Martijn Laan
- Version used for the current internal package line: 7.1.0

## DiscUtils

Setup uses the MIT-licensed DiscUtils ISO9660 reader only for supported Mech Pak media. It reads the user-supplied image in memory and writes only the exact allowlisted resources and official update inputs; the legacy disc installer, autorun, serial, and DRM files are not materialized or executed.

- Project: <https://github.com/DiscUtils/DiscUtils>
- Version: 0.16.13
- Evaluated revision: `59d7cadab839c6d8dfcf52f8be5efe6d2ced190f`
- License: MIT; packaged as `DiscUtils-LICENSE.txt`
- Exact package hashes: `third_party/DiscUtils.lock.json`

## Build-time tools

The reproducible manual-cleanup pipeline uses PyMuPDF 1.28.2, Pillow 12.3.0, and NumPy 2.3.4. These tools produce the cleaned local PDF and thumbnail artifacts but are not installed with the launcher. Their respective upstream licenses and package metadata remain with the pinned local build environment.

## Mech Pak activation research acknowledgments

RivvidGunner's *MechWarrior 4: Vengeance - Mech Packs Fix* documented the practical modern-Windows symptom and the eight affected Inner Sphere and Clan chassis. Its author notes permit use as a base with credit. This project does not package that mod or its crack; it independently applies a narrow, exact-input transform to the original patched resource tables derived from the user's media.

- Author: RivvidGunner
- Reference: <https://www.nexusmods.com/mechwarrior4/mods/11>
- Local use: behavior/test oracle and affected-roster cross-check only; no files copied or distributed

HelmMemoryCore's *MechWarrior 4 Modding Tools* and the Star League Cache community documentation helped identify the `#VBD` archive family and the relevant table names. The release does not package those tools or their code. The installer uses a separately implemented, exact-hash-gated archive reader and table transform.

- Project: <https://github.com/HelmMemoryCore/Mechwarrior-4-ModdingTools>
- Evaluated revision: `673e3afd3da09b20cad3352519752e3914dce9d2`
- Local use: format/table research oracle only; no files copied or distributed
