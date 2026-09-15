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

## Best resume path

1. Finish the governance/root repository baseline and verify the auxiliary router.
2. Produce a hash-and-structure inventory of supported local media without tracking proprietary contents.
3. Research MW4 release/patch history, modern-Windows failure modes, open-source compatibility projects, and pack-installer architecture using primary sources where possible.
4. Record ADRs for redistribution/media policy, implementation stack, install layout, compatibility baseline, and save-preserving uninstall.
5. Implement the smallest vertical slice: detect/validate one Vengeance media layout, stage a disposable install tree, apply a reproducible official patch/no-disc strategy, and verify rollback—before building full UI.
6. Extend the shared contract to Black Knight, Mercenaries, and optional packs, then add launcher presentation and manuals.

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
- GitHub CLI device authorization succeeded for `Icehellionx`; the public repository was created and the initial `main` branch pushed.
- No product build, install, launch, uninstall, antivirus, or field verification has run.
