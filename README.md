# MechWarrior 4 Remastered

An early-stage preservation project for an all-in-one Windows installer and launcher covering MechWarrior 4: Vengeance, Black Knight, and Mercenaries, with optional Inner Sphere and Clan Mech Pak support.

The project does not distribute original game ISOs, serial keys, opaque cracks, or extracted proprietary game files. The release goal is that users supply only supported original ISOs or ZIP files containing those ISOs; patching, compatibility, no-disc operation, launcher setup, repair, and uninstall are then handled by the project. No public build exists yet.

Current planning and status live in [`active/HANDOFF.md`](active/HANDOFF.md) and [`active/PRODUCTION_PLAN.md`](active/PRODUCTION_PLAN.md).

The current source includes a media-first installer that validates supported folders, ISOs, and ISO-containing ZIPs before mutation. Vengeance is rebuilt through its exact official Patch 3 path. Mercenaries requires its original two-disc set plus the supported fix ZIP containing the exact official Point Release 1 updater; setup extracts only that update, applies it in contained scratch, and derives the disc-free executable from qualified inputs without retaining the adjacent historical no-CD file. Black Knight is modeled in Vengeance's original `MW4X` subtree, but its PR1 disc-check path is not yet qualified. The two Mech Paks are recognized and staged through their qualified resource path, while in-game entitlement/visibility remains a release gate.

```powershell
dotnet build src/MW4Remastered.Installer/MW4Remastered.Installer.csproj --configuration Release
dotnet build src/MW4Remastered.Launcher/MW4Remastered.Launcher.csproj --configuration Release
dotnet build src/MW4Remastered.CompatLauncher/MW4Remastered.CompatLauncher.csproj --configuration Release
dotnet run --project tests/MW4Remastered.Core.Tests/MW4Remastered.Core.Tests.csproj --configuration Release
./tests/CompatLauncherBoundary.Tests.ps1
./tests/CompatibilitySourceBuild.Tests.ps1
```
