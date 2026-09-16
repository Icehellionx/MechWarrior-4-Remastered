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
4. Static evidence identifies a C-Dilla 16-bit helper in the pack activation path, but the exact failure and clean replacement contract still need controlled before/after proof.
5. Copying the mature MW3 launcher's structure wholesale would carry title-specific assumptions and historical complexity into a cleaner project.
6. Manual cleanup can silently clip or reorder content without full-page render comparison.
7. Uninstall is destructive and must be designed from a precise ownership/save inventory before implementation.
8. The evaluated MIT RTPatch parser does not understand the supplied patch variants and its CLI lacks record-path containment. It is evaluation evidence only, not an accepted dependency.
9. The current Vengeance 8.3 restoration map is sufficient for staging and Patch 2 naming, but every inferred name must be compared with a legacy-setup reference tree before release qualification.
10. The install-root manifest is path-contained and hash-validating but not cryptographically anchored outside the user-writable tree. Before release, bind it to a protected external install record or constrain removal against an independently trusted product manifest.
11. The Black Knight replacement executable is a hash-gated local input with matching reported version/length but different bytes from the disc executable. Provenance, malware scanning, and runtime qualification are required before it can be recommended; it must never enter the public payload by accident.

## Debt-negative rules

- Start with narrow contracts and tests before UI breadth.
- Prefer declarative edition/media data over per-title branching.
- Avoid catch-all `helpers`, `utils`, `manager`, `common`, and `misc` modules.
- Keep third-party code pinned and auditable; do not vendor an entire project for one small function.
- Do not add an alternative install/launch path without retiring or clearly isolating the first.
