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
4. Disc/readme/archived-support evidence separates plain Mech Pak resources, required official game patches, and C-Dilla/SafeCast entitlement with a 16-bit helper. The clean entitlement replacement still needs controlled before/after and game-visible proof; copying resources alone is not qualified.
5. Copying the mature MW3 launcher's structure wholesale would carry title-specific assumptions and historical complexity into a cleaner project.
6. Manual cleanup can silently clip or reorder content without full-page render comparison.
7. Uninstall is destructive and must be designed from a precise ownership/save inventory before implementation.
8. The evaluated MIT RTPatch parser does not understand the supplied patch variants and its CLI lacks record-path containment. It is evaluation evidence only, not an accepted dependency.
9. The current Vengeance 8.3 restoration map is sufficient for staging and Patch 2 naming, but every inferred name must be compared with a legacy-setup reference tree before release qualification.
10. The install-root manifest is path-contained and hash-validating but not cryptographically anchored outside the user-writable tree. Before release, bind it to a protected external install record or constrain removal against an independently trusted product manifest.
11. The Black Knight replacement executable is a hash-gated local input with matching reported version/length but different bytes from the disc executable. Provenance, malware scanning, and runtime qualification are required before it can be recommended; it must never enter the public payload by accident.
12. The Mercenaries replacement executable is likewise an opaque, hash-gated local input. Its `50.07.01.2105` version is newer than the disc's `50.06.09.3002`, but provenance, scan, and runtime evidence are still required. The adjacent `mercpr1.exe` was not executed or included.
13. Mercenaries cabinet extraction currently relies on the Windows-provided `tar.exe`/libarchive surface. Release qualification must pin the supported Windows behavior or replace it with a reviewed, licensed in-process cabinet reader.
14. The selected launcher icon derives from original Vengeance artwork supplied by the user. It is technically isolated and documented, but redistribution rights must be resolved before it enters a public release payload.

## Debt-negative rules

- Start with narrow contracts and tests before UI breadth.
- Prefer declarative edition/media data over per-title branching.
- Avoid catch-all `helpers`, `utils`, `manager`, `common`, and `misc` modules.
- Keep third-party code pinned and auditable; do not vendor an entire project for one small function.
- Do not add an alternative install/launch path without retiring or clearly isolating the first.
