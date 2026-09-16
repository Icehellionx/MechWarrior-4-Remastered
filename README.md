# MechWarrior 4 Remastered

An early-stage preservation project for an all-in-one Windows installer and launcher covering MechWarrior 4: Vengeance, Black Knight, and Mercenaries, with optional Inner Sphere and Clan Mech Pak support.

The project does not distribute original game ISOs, serial keys, opaque cracks, or extracted proprietary game files. The release goal is that users supply only supported original ISOs or ZIP files containing those ISOs; patching, compatibility, no-disc operation, launcher setup, repair, and uninstall are then handled by the project. No public build exists yet.

Current planning and status live in [`active/HANDOFF.md`](active/HANDOFF.md) and [`active/PRODUCTION_PLAN.md`](active/PRODUCTION_PLAN.md).

The current source includes a read-only media-intake installer shell. It can validate supported folders, ISOs, and ISO-containing ZIPs, but the public install action remains locked until the remaining patch/no-disc gates are complete. The development installation core now installs and launches Black Knight from original media alone through a reproducibly built, non-elevating compatibility bundle; Vengeance and Mercenaries still need a reproducible SafeDisc 1 transform before the full media-only contract is complete.

```powershell
dotnet build src/MW4Remastered.Installer/MW4Remastered.Installer.csproj --configuration Release
dotnet build src/MW4Remastered.Launcher/MW4Remastered.Launcher.csproj --configuration Release
dotnet build src/MW4Remastered.CompatLauncher/MW4Remastered.CompatLauncher.csproj --configuration Release
dotnet run --project tests/MW4Remastered.Core.Tests/MW4Remastered.Core.Tests.csproj --configuration Release
./tests/CompatLauncherBoundary.Tests.ps1
./tests/CompatibilitySourceBuild.Tests.ps1
```
