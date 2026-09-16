# MechWarrior 4 Remastered

An early-stage preservation project for an all-in-one Windows installer and launcher covering MechWarrior 4: Vengeance, Black Knight, and Mercenaries, with optional Inner Sphere and Clan Mech Pak support.

The project does not distribute original game ISOs, serial keys, opaque cracks, or extracted proprietary game files. The release goal is that users supply only supported original ISOs or ZIP files containing those ISOs; patching, compatibility, no-disc operation, launcher setup, repair, and uninstall are then handled by the project. No public build exists yet.

Current planning and status live in [`active/HANDOFF.md`](active/HANDOFF.md) and [`active/PRODUCTION_PLAN.md`](active/PRODUCTION_PLAN.md).

The current source includes a media-first installer that validates supported folders, ISOs, and ISO-containing ZIPs before mutation. Vengeance and Mercenaries install directly from their original two-disc sets through exact-input, source-owned SafeDisc 1.50.20 transforms; Black Knight installs through a reproducibly built, non-elevating compatibility bundle after its Vengeance dependency is satisfied. The two Mech Paks are recognized but remain explicitly disabled until their patch and entitlement paths are qualified.

```powershell
dotnet build src/MW4Remastered.Installer/MW4Remastered.Installer.csproj --configuration Release
dotnet build src/MW4Remastered.Launcher/MW4Remastered.Launcher.csproj --configuration Release
dotnet build src/MW4Remastered.CompatLauncher/MW4Remastered.CompatLauncher.csproj --configuration Release
dotnet run --project tests/MW4Remastered.Core.Tests/MW4Remastered.Core.Tests.csproj --configuration Release
./tests/CompatLauncherBoundary.Tests.ps1
./tests/CompatibilitySourceBuild.Tests.ps1
```
