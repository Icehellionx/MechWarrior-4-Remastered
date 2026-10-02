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

The application CI also builds the native input policy and actual upstream wrapper tests from pinned source. Hosted MSVC revisions vary, so that job explicitly uses a research build and uploads no binaries. It cannot qualify a release DLL. To verify a local qualified payload at every title boundary, run the Core suite with `-- --input-adapter-root <directory-containing-qualified-dinput.dll>`; it tests installation paths, acceptance and same-size corruption rejection.

## Release package

The release build requires the exact pinned dgVoodoo archive and source checkout, a dinputto8 checkout with its pinned Logging submodule, and either a locally built or exact qualified Black Knight capture bundle. The script validates all hashes, publishes self-contained applications, rebuilds the presentation add-on and modified DirectInput adapter, runs native input regressions, assembles an allowlisted payload including the qualified manuals, compiles Inno Setup, and emits a SHA-256 sidecar. Input build evidence stays in ignored `.local/input-release-build-*` directories. A supplied `DinputTo8AdapterPath` is only an optional reference hash check; it never replaces the fresh source build.

```powershell
& tools/package/build-release.ps1 `
    -OutputDirectory output/0.6.39 `
    -Version 0.6.39 `
    -BlackKnightCaptureBundle <qualified-capture-directory> `
    -DgVoodooArchive <dgVoodoo2_87_5.zip> `
    -DgVoodooSourceRoot <pinned-dgVoodoo-source> `
    -DinputTo8SourceRoot <pinned-dinputto8-source>
```

Before publishing, build twice and compare the complete output byte-for-byte, run `tools/assert-release-tree.ps1`, perform representative clean install/repair/uninstall smokes, and scan the exact setup plus installed tree with current Microsoft Defender intelligence. Do not publish from a dirty or partially qualified payload.

## Exact-byte release reproduction

.NET application informational versions include the Git build revision. For the qualified `0.6.47.5` C/D setup, build HEAD was `8954eb4fa38a758cc7f84747508f6a9cbf55e7e7`; the qualification commit records the tested source changes afterward. To reproduce those exact bytes, use a disposable checkout at that build HEAD and apply the source diff through tag `v0.6.47.5` without committing it, then run the declared build command with the pinned inputs/toolchain. A normal build directly at the tag includes different Git revision metadata. The unrelated local dgVoodoo experiment was excluded from the source diff; the unchanged HEAD builder independently reproduced the exact packaged add-on.
