# Building MechWarrior 4 Remastered

The public repository intentionally excludes original game media, generated installers, extracted game trees, local evidence, and downloaded third-party archives.

## Prerequisites

- Windows 10 or Windows 11 x64.
- .NET SDK `10.0.300` as pinned by `global.json`.
- PowerShell 7.
- Inno Setup 7.1.0 for a setup EXE.
- Visual Studio 2022 Build Tools with the MSVC x86/x64 workload for the source-built compatibility components.
- Python and the packages in `tools/manuals/requirements.txt` when regenerating local manual outputs.
- Exact third-party source/archive inputs identified by the lock files under `third_party`.
- User-local original media and manual inputs in ignored directories.

## Application verification

```powershell
dotnet run --project tests/MW4Remastered.Core.Tests/MW4Remastered.Core.Tests.csproj --configuration Release

Get-ChildItem tests -File -Filter '*.Tests.ps1' | ForEach-Object {
    & $_.FullName
}
```

`MediaRecognitionSmoke.ps1` is input-driven and separately accepts paths to supported media images.

## Release package

The release build requires the exact pinned dgVoodoo archive and source checkout plus either a locally built or exact qualified Black Knight capture bundle. The script validates all hashes, publishes self-contained applications, rebuilds the presentation add-on, assembles an allowlisted payload including the qualified manuals, compiles Inno Setup, and emits a SHA-256 sidecar.

```powershell
& tools/package/build-release.ps1 `
    -OutputDirectory output/0.6.39 `
    -Version 0.6.39 `
    -BlackKnightCaptureBundle <qualified-capture-directory> `
    -DgVoodooArchive <dgVoodoo2_87_5.zip> `
    -DgVoodooSourceRoot <pinned-dgVoodoo-source>
```

Before publishing, build twice and compare the complete output byte-for-byte, run `tools/assert-release-tree.ps1`, perform representative clean install/repair/uninstall smokes, and scan the exact setup plus installed tree with current Microsoft Defender intelligence. Do not publish from a dirty or partially qualified payload.
