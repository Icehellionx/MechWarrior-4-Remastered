# Test strategy and verification map

## Evidence layers

| Layer | Required evidence |
|---|---|
| Static | Compilation, parser/config validation, license metadata, manifest schema |
| Focused | Media recognition, transform hashes, path containment, crop geometry, save classification |
| Component | Mount/archive lifetime, staging/rollback, registry/config ownership, process classification |
| Smoke | Clean disposable install, launch where permitted, repair, uninstall, save preservation |
| Regression | Three games, both packs, disc/archive variants, success/failure cleanup |
| Release | Reproducible build, payload allowlist, checksums, Defender scans |
| Field | Sanitized logs from representative Windows/GPU/input configurations |

## Initial verification commands

```powershell
node --test tools/agent/aux-model.test.mjs
node tools/agent/aux-model.mjs --check
git status --short --ignored
dotnet run --project tests/MW4Remastered.Core.Tests/MW4Remastered.Core.Tests.csproj --configuration Release
dotnet build src/MW4Remastered.Installer/MW4Remastered.Installer.csproj --configuration Release
dotnet build src/MW4Remastered.Launcher/MW4Remastered.Launcher.csproj --configuration Release
& tests/ReleaseTreePolicy.Tests.ps1
# Elevated, local-media-only gate:
& tests/MediaRecognitionSmoke.ps1 -Images $knownMediaImages
# Local manuals stay ignored:
python tools/manuals/clean_manuals.py Manuals output/pdf
```

Native WinForms visual and interaction smoke remains a manual/automation coverage gap; compilation does not qualify visual layout or the eventual install transaction.

## Required contracts

- Input fixtures use synthetic trees or hashes/metadata; tests never copy proprietary game payloads into source.
- Media matching rejects unknown or partially matching revisions safely.
- Binary transforms require exact input and output hashes and fail without mutation on mismatch.
- Extraction prevents traversal, links/reparse escape, device paths, and writes outside staging.
- Media selected during intake is re-opened and structurally re-recognized under the install transaction lifetime; changed or missing sources fail before mutation and release earlier resources.
- Install commits atomically where practical and removes partial state after failure.
- Additive overlays require a verified matching base, refuse all destination replacement, atomically update the ownership manifest, and roll new files back if manifest commit or final verification fails.
- Uninstall validates the exact install root and ownership manifest before deletion, rejects links/reparse points, preserves only documented user data, and retains recovery data if restore fails.
- Launcher status is derived from verified files/configuration, not registry keys alone.
- Manual transforms assert page count/order/dimensions and render all pages for visual QA.
- Diagnostics redact serials, credentials, private paths, and media content.
- Release tests block ISOs, BIN/CUE/MDF/MDS, serial files, raw cracks, `.env`, dumps, and unintended executables.

## Behavioral slice record

```text
Behavior changed:
Owning boundary:
Focused command and assertion:
Primary smoke path:
Adjacent games/packs/media paths:
Cleanup/failure paths:
Release scan required? why/why not:
Affected hardware evidence required? why/why not:
```
