# 0009 — Per-user Inno Setup package

- Status: accepted
- Date: 2026-09-16

## Context and evidence

The project needs a conventional Windows setup and whole-application uninstaller without making the launcher self-delete, invoking a privileged helper, or broadly deleting media-derived game directories. Those patterns are difficult to audit and can resemble malware behavior. Normal installation must remain non-elevating, while game payload removal must retain the ownership-manifest and save-preservation guarantees from ADR 0003.

Inno Setup 7.1.0 supports per-user installation, an `asInvoker` setup, standard Add/Remove Programs registration, and deterministic file timestamps. Its license permits this non-commercial project use and redistribution subject to the recorded terms. The exact local compiler is pinned in `third_party/InnoSetup.lock.json`.

Two clean package builds from the same declared inputs produced byte-identical 72,276,429-byte setup executables with SHA-256 `83a396a6cc497440f171a9fffb68ba2788dad5843c93989b0fa462dfbbafed52`. A silent disposable install completed without elevation. Both installed applications started responsively. Standard uninstall removed every package-owned shell file while preserving an unowned synthetic `black-knight/Saves/pilot.sav`. The setup package and installed tree passed the current Defender gate.

## Decision

- Build the application shell as a single Inno Setup executable using the pinned compiler.
- Install per user under `{localappdata}\Programs\MechWarrior 4 Remastered` with `PrivilegesRequired=lowest`.
- Publish the .NET installer and launcher as self-contained single-file executables.
- Package only explicitly allowlisted shell files and the exact qualified Black Knight compatibility distribution bundle.
- Use Inno's standard uninstaller for package-owned shell files and shortcuts.
- Keep `[UninstallDelete]` empty. The package uninstaller must not recursively remove game directories, saves, configuration, or unknown files.
- Keep media-derived game removal in the launcher through `OwnedInstallUninstaller`, which validates the per-game ownership manifest and preserves unowned files.
- Emit a SHA-256 sidecar, run the release-tree policy over both staged payload and final output, and require Defender scanning of the setup, installed shell, and representative installed game trees.
- Treat code signing and SmartScreen reputation as separate release gates. An unsigned reputation warning is not a malware detection and must not be hidden.

## Alternatives considered

- A self-deleting launcher/helper was rejected because it complicates ownership and creates unnecessary security-tool optics.
- Broad scripted deletion was rejected because it cannot safely distinguish game payload from saves and user files.
- WiX was not selected for this slice because current binary distribution terms add a separate Open Source Maintenance Fee agreement; Inno supplies the needed conventional package boundary with a smaller source and operational surface.
- A machine-wide elevated install was rejected because the current product needs no privileged destination or system-wide component.

## Consequences

- Whole-application removal is visible in Windows Installed Apps and needs no custom elevation or deletion helper.
- Uninstalling the application shell can intentionally leave media-derived games and saves. Users remove those safely from the launcher before uninstalling the shell.
- The setup is currently unsigned, so SmartScreen reputation remains an explicit distribution gap even when Defender scanning is clean.
- Inno Setup version/license/compiler provenance is part of every qualified release record.

## Rollback

Remove the Inno script/build entry point and its package registration. Per-game manifests and ownership-safe removal remain independent. Any replacement packager must preserve the per-user, non-elevating, narrow-ownership, deterministic-build, checksum, and Defender contracts before adoption.

## Verification

```powershell
& tests/PackagingContract.Tests.ps1
& tools/package/build-release.ps1 -CompatibilityEvidenceDirectory <qualified-evidence> -OutputDirectory <new-output> -Version 0.1.0
& tools/security/scan-with-defender.ps1 -Path <new-output>
```

Qualification also requires a disposable install/start/uninstall smoke proving package files are removed and unowned save/configuration files remain.
