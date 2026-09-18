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
7. Vengeance, Black Knight, and Mercenaries bypass their crashing legacy joystick enumeration and obsolete AutoConfig. Setup and a bounded launch guard maintain required 1024×768×32 INI keys without owning the mutable user file; Black Knight correctly stores `optionsx.ini` beside `MW4X.exe`. The DDrawCompat title split is superseded as a release direction. Exact dgVoodoo2 2.86.5 loaded in all three media-derived titles and removed Black Knight's no-proxy architectural exception in a bounded smoke, but every movie/menu/pilot/mission and Alt-Tab path still needs interactive proof before integration is qualified.
8. Startup FMV was visibly proven for Vengeance and Mercenaries through different DDrawCompat variants, demonstrating that the MPEG media and installed x86 DirectShow components work. Remaining debt is to reproduce those movies plus Black Knight's intro under the common dgVoodoo candidate, then verify in-engine cinematics. Do not install a system codec pack.
9. The exact staged dgVoodoo profile passes bounded all-title process smokes, but unattended screen capture cannot force these legacy top-level windows ahead of the user's foreground application because Windows correctly rejects background focus stealing. Treat this as an automation limitation, not visual acceptance; a foreground interactive run is still required.
10. Setup's private-only, no-edge-traversal firewall rules compile and have contract coverage, but the current automation token is not administrator-elevated even when it can run outside the workspace sandbox. Windows correctly rejected a temporary rule probe. Exercise add/query/delete through the real elevated setup and uninstall before release.
9. A new executable path can trigger Windows Defender Firewall's normal network-access prompt on first launch. Add an explicit setup-time multiplayer firewall choice with exact executable-scoped rule ownership and uninstall cleanup; do not suppress, broaden, or add exclusions to Windows security.
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
18. Black Knight's launch-time app-local DLL, static v2, and static v3 are disproven. V3's callsite classifier corrupted 110 IAT operands, including two entry-point MOV loads, and field testing reached a frame-one stack jump. V4 exact-validates those corrections, preserves the established import order, repairs all 127 tail branches, rejects residual removed-tail targets, removes both loader tails, and emits deterministic `b31bd051…`. Full v4 coordinator/package/Defender/repair/uninstall and focused intro/menu/Alt-Tab/gameplay proof remain mandatory. Never reintroduce `VersionInjector`, a project remote-thread injector, a launch helper, or broad process-name killing; ensure capture DLL/configuration/image cannot enter the installed manifest.
    - Hardening follow-up: `Process.Start` precedes `AssignProcessToJobObject`. The observed SafeDisc child starts several seconds later and the full coordinator proved containment, but suspended-create/assign/resume would eliminate the theoretical pre-assignment child race. Do not replace the proven path without its own full-worker and cleanup evidence.
19. Official Black Knight PR1 is sourced only from qualified Mech Pak media. Retail Black Knight also requires the official Vengeance Patch 2 state from its disc; that separate chain is not implemented, and a base-retail fallback crashed. Setup therefore rejects Black Knight without a qualified pack until Patch 2 is source-owned and runtime-qualified; never fetch an unpinned mirror.

## Debt-negative rules

- Start with narrow contracts and tests before UI breadth.
- Prefer declarative edition/media data over per-title branching.
- Avoid catch-all `helpers`, `utils`, `manager`, `common`, and `misc` modules.
- Keep third-party code pinned and auditable; do not vendor an entire project for one small function.
- Do not add an alternative install/launch path without retiring or clearly isolating the first.
