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
7. Vengeance, Black Knight, and Mercenaries bypass their crashing legacy joystick enumeration and obsolete AutoConfig. Setup and a bounded launch guard maintain required 1024×768×32 INI keys without owning the mutable user file; Black Knight correctly stores `optionsx.ini` beside `MW4X.exe`. A pinned source-built DDrawCompat profile presents Vengeance and Mercenaries borderlessly and a narrow local patch retries transient surface loss. Fresh media-derived Vengeance and Mercenaries passed 2560×1440 presentation and Alt-Tab. Black Knight passed launch and Alt-Tab only in its 800×600 native window; a future scaler must not inject/proxy `ddraw.dll` beside its PR1 runtime. Pilot creation, controller support, mission entry, and repeated launches remain open.
8. Startup FMV is now visibly proven for Vengeance and Mercenaries through different exact-hash DDrawCompat variants and activation profiles. Remaining debt is final-package Alt-Tab/menu repetition, Black Knight video playback on its native windowed path, and in-engine cinematic coverage. Do not install a system codec pack; the machine already supplies the required x86 DirectShow/MPEG components.
9. Uninstall is destructive and must be designed from a precise ownership/save inventory before implementation.
10. The evaluated MIT RTPatch parser does not understand the supplied patch variants and its CLI lacks record-path containment. It is evaluation evidence only, not an accepted dependency.
9. The current Vengeance 8.3 restoration map is sufficient for staging and Patch 2 naming, but every inferred name must be compared with a legacy-setup reference tree before release qualification.
10. The install-root manifest is path-contained and hash-validating but not cryptographically anchored outside the user-writable tree. Before release, bind it to a protected external install record or constrain removal against an independently trusted product manifest.
11. The former Black Knight replacement-executable path is retired. Keep regression tests and release policy preventing opaque replacements from re-entering the media-only source-built path.
12. Mercenaries no longer accepts the opaque local replacement executable. The exact retail `50.06.09.3002` loader/ICD/DPLAYER set is transformed by reviewed managed source into a deterministic, Defender-clean executable that passed a bounded launch smoke. Internal setup now includes only the exact qualified engine/RTP files extracted at build time from official `mercpr1.exe`; the adjacent historical no-CD executable is excluded. Official patch redistribution rights and broader runtime qualification remain open before public release.
13. Mercenaries cabinet extraction currently relies on the Windows-provided `tar.exe`/libarchive surface. Release qualification must pin the supported Windows behavior or replace it with a reviewed, licensed in-process cabinet reader.
14. The selected launcher icon derives from original Vengeance artwork supplied by the user. It is technically isolated and documented, but redistribution rights must be resolved before it enters a public release payload.
15. Pack installation uses a source-owned, `asInvoker` x86 host to load only the exact official Patch 3 engine and payload supplied on recognized user media inside coordinator-owned scratch. The engine/payload are never distributed or installed, and every retained output is exact-hash validated. Preserve that containment and cleanup boundary; the local Clan image has zero-byte Patch 3 sidecars, so a Clan-only selection cannot patch retail Vengeance unless another selected pack source supplies the qualified Patch 3 components.
16. `VengeanceExecutableTransform.cs` exceeds the C# hard ceiling. Extraction plan: move `MutablePe32` plus PE normalization into `SafeDiscPeImage`, and import/pointer repair into `SafeDisc15020ImportRepair`; callers remain the exact-input Vengeance, Patch 3, and Mercenaries transforms; protect byte-for-byte output hashes, wrong-input rejection, cipher vectors, and all three launch smokes; target fewer than 450 lines in the title transform file without changing any public request contract.
17. `LegacyGameRegistration` encodes unverified HKCU values, including full executable paths where original setup evidence indicates an install-directory `EXE Path`, and a hard-coded Black Knight `CDPath`. It must not return to release policy until an original-installer registry diff establishes exact 32-bit HKLM product/DirectPlay records and ownership-safe removal.
17. Resolved for the supported paths: setup presents and requires explicit original-license acceptance; Vengeance/Mercenaries bypass their obsolete post-setup calls, and Black Knight installs exact-input accepted-result `EBUEula.dll` `d150fceb…`. Dialog-aware fresh-tree probes show no EULA. Actual menu/gameplay acceptance remains a release gate.
18. Black Knight's failed setup-capture/static-PE path is removed. PR1 requires runtime-generated SafeDisc trampolines, so the supported route is the exact protected executable plus pinned source-built app-local DLL. Package, repair, uninstall, retail, menu/gameplay, and representative-hardware proof remain mandatory; never reintroduce `VersionInjector`, remote-thread injection, or a launch helper.
19. Official Black Knight PR1 is sourced only from qualified Mech Pak media. Retail Black Knight also requires the official Vengeance Patch 2 state from its disc; that separate chain is not implemented, and a base-retail fallback crashed. Setup therefore rejects Black Knight without a qualified pack until Patch 2 is source-owned and runtime-qualified; never fetch an unpinned mirror.

## Debt-negative rules

- Start with narrow contracts and tests before UI breadth.
- Prefer declarative edition/media data over per-title branching.
- Avoid catch-all `helpers`, `utils`, `manager`, `common`, and `misc` modules.
- Keep third-party code pinned and auditable; do not vendor an entire project for one small function.
- Do not add an alternative install/launch path without retiring or clearly isolating the first.
