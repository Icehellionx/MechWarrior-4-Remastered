# Technical debt and risk backlog

The first media/install core now exists. This ledger tracks both design risks and implementation debt.

## Complexity gates

| File category | Soft review | Hard ceiling for new feature growth |
|---|---:|---:|
| Project-owned C# | 450 lines | 650 lines |
| Project-owned PowerShell | 300 lines | 500 lines |
| Project-owned C/C++ | 500 lines | 800 lines |
| Project-owned JavaScript/TypeScript | 350 lines | 550 lines |
| Markdown operating documents | 250 lines | 450 lines |

A hard-ceiling file requires an extraction plan naming the contract, callers, parity tests, expected reduction, and protected behavior.

## Active risks

1. The legal history of “freeware” MW4 releases is easy to overstate; media must remain user-supplied until rights are documented.
2. The local input set contains opaque no-CD/fix archives and a serial-key file. They are evidence only, never automatic release inputs.
3. Multi-disc and archive variants may differ in executable revision or modifications despite similar names.
4. Disc/readme/archived-support evidence separates plain Mech Pak resources, required official game patches, and C-Dilla/SafeCast entitlement with a 16-bit helper. Patch 3 and an Inner Sphere payload now launch without the obsolete components, but game-visible chassis/variant proof and any remaining entitlement marker are still required.
5. Copying the mature MW3 launcher's structure wholesale would carry title-specific assumptions and historical complexity into a cleaner project.
6. Manual cleanup can silently clip or reorder content without full-page render comparison.
7. Vengeance and Mercenaries now bypass their crashing legacy joystick enumeration and obsolete AutoConfig, and start windowed to avoid unreliable exclusive-fullscreen initialization. Keyboard/menu launch is proven, but controller support, deliberate fullscreen switching, mission entry, and repeated-launch coverage remain open before deciding whether an open-source input/display compatibility layer is warranted.
7. Uninstall is destructive and must be designed from a precise ownership/save inventory before implementation.
8. The evaluated MIT RTPatch parser does not understand the supplied patch variants and its CLI lacks record-path containment. It is evaluation evidence only, not an accepted dependency.
9. The current Vengeance 8.3 restoration map is sufficient for staging and Patch 2 naming, but every inferred name must be compared with a legacy-setup reference tree before release qualification.
10. The install-root manifest is path-contained and hash-validating but not cryptographically anchored outside the user-writable tree. Before release, bind it to a protected external install record or constrain removal against an independently trusted product manifest.
11. The former Black Knight replacement-executable path is retired. Keep regression tests and release policy preventing opaque replacements from re-entering the media-only source-built path.
12. Mercenaries no longer accepts the opaque local replacement executable. The exact retail `50.06.09.3002` loader/ICD/DPLAYER set is transformed by reviewed managed source into a deterministic, Defender-clean executable that passed a bounded launch smoke. Official patch provenance and broader runtime qualification remain open; the adjacent `mercpr1.exe` was not executed or included.
13. Mercenaries cabinet extraction currently relies on the Windows-provided `tar.exe`/libarchive surface. Release qualification must pin the supported Windows behavior or replace it with a reviewed, licensed in-process cabinet reader.
14. The selected launcher icon derives from original Vengeance artwork supplied by the user. It is technically isolated and documented, but redistribution rights must be resolved before it enters a public release payload.
15. Pack installation uses a source-owned, `asInvoker` x86 host to load only the exact official Patch 3 engine and payload supplied on recognized user media inside coordinator-owned scratch. The engine/payload are never distributed or installed, and every retained output is exact-hash validated. Preserve that containment and cleanup boundary; the local Clan image has zero-byte Patch 3 sidecars, so a Clan-only selection cannot patch retail Vengeance unless another selected pack source supplies the qualified Patch 3 components.
16. `VengeanceExecutableTransform.cs` exceeds the C# hard ceiling. Extraction plan: move `MutablePe32` plus PE normalization into `SafeDiscPeImage`, and import/pointer repair into `SafeDisc15020ImportRepair`; callers remain the exact-input Vengeance, Patch 3, and Mercenaries transforms; protect byte-for-byte output hashes, wrong-input rejection, cipher vectors, and all three launch smokes; target fewer than 450 lines in the title transform file without changing any public request contract.
17. `LegacyGameRegistration` encodes unverified HKCU values, including full executable paths where original setup evidence indicates an install-directory `EXE Path`, and a hard-coded Black Knight `CDPath`. It must not return to release policy until an original-installer registry diff establishes exact 32-bit HKLM product/DirectPlay records and ownership-safe removal.
17. Vengeance currently presents its original first-run EULA before play. Evaluate presenting the applicable original terms once inside setup and recording acceptance for each installed title. Do not auto-accept or suppress the original dialog until the terms, registry marker, and consent UX are explicitly validated.
18. Black Knight's former injected helper is gone, but the replacement app-local DLL path is not runtime-qualified: the incorrect-install dialog passed the old responsive-process smoke. Reproduce the original shared Vengeance/`MW4X` topology and official PR1 state, then reassess the compatibility DLL with dialog-aware menu/gameplay evidence. Do not stack a renderer wrapper onto this unresolved boundary.

## Debt-negative rules

- Start with narrow contracts and tests before UI breadth.
- Prefer declarative edition/media data over per-title branching.
- Avoid catch-all `helpers`, `utils`, `manager`, `common`, and `misc` modules.
- Keep third-party code pinned and auditable; do not vendor an entire project for one small function.
- Do not add an alternative install/launch path without retiring or clearly isolating the first.
