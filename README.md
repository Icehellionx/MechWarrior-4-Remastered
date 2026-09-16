# MechWarrior 4 Remastered

An early-stage preservation project for an all-in-one Windows installer and launcher covering MechWarrior 4: Vengeance, Black Knight, and Mercenaries, with optional Inner Sphere and Clan Mech Pak support.

The project does not distribute original game ISOs, serial keys, opaque cracks, or extracted proprietary game files. Users will supply supported original media or recognized archival layouts. No public build exists yet.

Current planning and status live in [`active/HANDOFF.md`](active/HANDOFF.md) and [`active/PRODUCTION_PLAN.md`](active/PRODUCTION_PLAN.md).

The current source includes a read-only media-intake installer shell. It can validate supported folders, ISOs, and ISO-containing ZIPs, but installation remains deliberately locked until the patch/no-disc and transaction-lifetime gates are complete.

```powershell
dotnet build src/MW4Remastered.Installer/MW4Remastered.Installer.csproj --configuration Release
dotnet build src/MW4Remastered.Launcher/MW4Remastered.Launcher.csproj --configuration Release
dotnet run --project tests/MW4Remastered.Core.Tests/MW4Remastered.Core.Tests.csproj --configuration Release
```
