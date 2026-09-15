# Project brief: MechWarrior 4 Remastered

Build an all-in-one Windows installer and launcher modeled on the successful operating approach of MechWarrior 3 Remastered, while designing MW4-specific boundaries from scratch.

The product should install from user-selected Vengeance, Black Knight, and Mercenaries ISOs or recognized archival ZIP layouts; optionally install Inner Sphere and Clan Mech Paks; integrate required official patches, no-disc behavior, and modern-Windows fixes reproducibly; show game launch icons and pack status; list cleaned manuals; provide an MW4-appropriate visual identity and remaster icon; and include repair and safe uninstall.

Raw media and source scans remain local. The release must not include ISOs, serials, opaque cracks, or proprietary extracted trees without independently documented redistribution rights.

Manual work:

- Black Knight: split the combined front/back cover and place the back cover last.
- Vengeance: crop oversized white page canvases to the original content geometry.
- Mercenaries: inspect and change only if evidence warrants it.

Known investigation: Inner Sphere and Clan pack installation reportedly fails on 64-bit systems. Determine the actual installer/payload cause and build a tested compatibility path rather than relying on the tentative explanation supplied with the report.
