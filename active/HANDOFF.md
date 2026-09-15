# Session handoff

Updated: 2026-09-15.

## Current state

- This is a newly initialized MechWarrior 4 Remastered workspace derived only in governance shape from the MechWarrior 3 Remastered project. No MW4 installer, launcher, patch pipeline, or qualified compatibility baseline exists yet.
- Local inputs include two-disc Vengeance media, Black Knight media, two-disc Mercenaries media, Inner Sphere and Clan Mech Pak media, archival ZIP variants, patch/fix archives, and three raw PDF manuals. They are evidence/user inputs and are intentionally ignored.
- The intended product is one MW4-themed installer and launcher for Vengeance, Black Knight, and Mercenaries. Optional Inner Sphere and Clan packs are selected/detected at install time and surfaced as status in the launcher.
- Raw manuals need a reproducible cleanup pass: Black Knight's combined front/back cover must be split and the back cover moved to the end; Vengeance pages must be cropped to their real content dimensions. Mercenaries requires inspection before declaring it unchanged.
- The desired app includes supported media detection, reproducible no-disc/compatibility transforms, per-game launch buttons/icons, pack status, manual links, repair/configuration, diagnostics, and a safe uninstaller that preserves user-owned saves/configuration by explicit policy.
- The user's freeware statement is not treated as sufficient redistribution authorization. Until reliable primary evidence says otherwise, releases require user-supplied media and exclude ISOs, serials, extracted game trees, and third-party cracks.
- Inner Sphere and Clan installers are reported to fail on 64-bit Windows. Cause and repair are unverified; investigate installer technology and payload layout before adopting the user's tentative “16-bit encryption” explanation.
- Root `.env` is local-only. Its configured auxiliary model names are recorded without credentials in `active/AUXILIARY_MODELS.md`.
- The root Git repository is published publicly at `Icehellionx/MechWarrior-4-Remastered`; local `main` tracks `origin/main`.
- An initial .NET 10 WinForms launcher scaffold now models three games and two optional packs through a UI-free core catalog. It only recognizes game executables; pack status deliberately remains false until a payload-manifest verifier exists.
- A media catalog and directory inspector recognize both Vengeance discs, Black Knight, both Mercenaries discs, and both Mech Paks. Unsafe paths fail closed; recognized media reports crack/DRM paths for mandatory exclusion.
- A release-tree policy rejects disc images, secrets/keys, crack directories, legacy DRM files, reparse points, and executable/DLL/script content not named by an explicit allowlist.
- The manual pipeline reproducibly creates three ignored local outputs. Black Knight is 36 portrait pages with the front cover first and separated back cover last; Vengeance is 98 cropped 611.76×342-point spreads; Mercenaries preserves its 19 original pages. Two consecutive runs produced identical hashes and every output page was rendered for review.

## Best resume path

1. Finish the governance/root repository baseline and verify the auxiliary router.
2. Research MW4 release/patch history, modern-Windows failure modes, open-source compatibility projects, and pack-installer architecture using primary sources where possible.
3. Record ADRs for install layout, compatibility baseline, and save-preserving uninstall.
4. Implement the next vertical slice: stage a disposable Vengeance install tree from the recognized two-disc layout, reproduce the installer mapping without executing obsolete setup/DRM code, apply a reproducible official patch/no-disc strategy, and verify rollback.
5. Extend the installation contract to Black Knight, Mercenaries, and optional packs, then connect launcher actions and cleaned manuals.

## Known constraints and risks

- Licensing/freeware history may differ between Mercenaries releases and the original retail titles.
- Multiple archival ZIP/ISO layouts may contain different revisions or modified executables; filenames alone are not trusted.
- Opaque no-CD archives are high risk. Prefer known-input binary transforms or openly licensed source-based compatibility when legally and technically viable.
- Legacy setup/bootstrap executables may be 16-bit even when game payloads are 32-bit; direct payload extraction may be safer than emulating installers, but must preserve registry/configuration prerequisites.
- Hardware graphics, input, video, DRM/disc checks, and multiplayer behavior remain entirely unqualified.

## Last verification

- Governance intake completed; local inputs enumerated without opening or publishing content.
- Auxiliary router syntax and four unit tests passed. Ollama was reachable with all nine configured local model names installed; Featherless roles are configured but were not live-billed.
- The launcher/core Release build completed with zero warnings/errors, and the synthetic core smoke test passed.
- A local-only SHA-256 inventory recorded all 23 media/manual inputs under ignored `.local/`; no source media was changed.
- ISO directory inspection confirmed C-Dilla/SafeCast and SafeDisc-era files on both pack discs plus directly accessible content/patch payloads. The direct-extraction hypothesis remains unqualified.
- The real-media recognition smoke passed all seven supplied ISOs and confirmed every owned mount was detached afterward.
- Release-tree policy tests passed their safe baseline and rejected synthetic ISO, unallowlisted executable, SafeDisc driver, and crack-directory fixtures.
- All three manual outputs passed page-count/geometry/render checks and deterministic SHA-256 comparison. Final full contact-sheet review found no clipped or misordered pages.
- GitHub CLI device authorization succeeded for `Icehellionx`; the public repository was created and the initial `main` branch pushed.
- No product build, install, launch, uninstall, antivirus, or field verification has run.
