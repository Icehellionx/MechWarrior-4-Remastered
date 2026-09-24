using MW4Remastered.Core;
using MW4Remastered.Core.Install;
using MW4Remastered.Core.Launch;

internal static class GraphicsSettingsSmoke
{
    public static void Run(List<string> failures)
    {
        var sourceProfile = Path.Combine(Directory.GetCurrentDirectory(), "assets", "compatibility", "dgVoodoo-MW4.conf");
        var requestedParent = Environment.GetEnvironmentVariable("MW4_CROSS_VOLUME_SMOKE_ROOT");
        var parent = Path.GetFullPath(string.IsNullOrWhiteSpace(requestedParent) ? Path.GetTempPath() : requestedParent);
        if (!Directory.Exists(parent)) throw new DirectoryNotFoundException("Graphics smoke parent must already exist.");
        var root = Path.GetFullPath(Path.Combine(parent, "mw4-graphics-settings-" + Guid.NewGuid().ToString("N")));
        var parentPrefix = parent.EndsWith(Path.DirectorySeparatorChar) ? parent : parent + Path.DirectorySeparatorChar;
        if (!root.StartsWith(parentPrefix,
                StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Graphics smoke root escaped its explicit parent.");
        if (!string.IsNullOrWhiteSpace(requestedParent) &&
            Path.GetPathRoot(root)!.Equals(Path.GetPathRoot(sourceProfile), StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Cross-volume graphics smoke must use a different volume than source media.");
        try
        {
            Check(new DriveStorageCapacityReader().GetAvailableBytes(Path.Combine(root, "planned")) > 0,
                "destination capacity resolves on the selected volume", failures);
            var app = Path.Combine(root, "app");
            var compatibility = Path.Combine(app, "Compatibility", "dgVoodoo2");
            Directory.CreateDirectory(compatibility);
            File.Copy(sourceProfile, Path.Combine(compatibility, "dgVoodoo.conf"));
            var resolutionPreference = new ConfiguredGameResolutionProvider(app,
                new FixedResolutionProvider(new GameResolution(1920, 1440)));
            Check(resolutionPreference.GetResolution() == new GameResolution(1600, 1200),
                "gameplay resolution defaults to a mode accepted by the original HUD", failures);
            resolutionPreference.Save(new GameResolution(1024, 768));
            Check(resolutionPreference.ReadSaved() == new GameResolution(1024, 768) &&
                  resolutionPreference.GetResolution() == new GameResolution(1024, 768),
                "selected gameplay resolution persists for launch", failures);
            resolutionPreference.Save(new GameResolution(1600, 1200));
            Check(new ConfiguredGameResolutionProvider(app,
                    new FixedResolutionProvider(new GameResolution(1024, 768))).GetResolution() ==
                  new GameResolution(1600, 1200),
                "explicit 1200p render resolution remains selected above monitor size", failures);
            Check(GameResolutionPreset.BestFitting(new GameResolution(1200, 900)) == new GameResolution(1024, 768) &&
                  GameResolutionPreset.BestFitting(new GameResolution(1440, 1080)) == new GameResolution(1024, 768) &&
                  GameResolutionPreset.BestFitting(new GameResolution(1920, 1440)) == new GameResolution(1600, 1200) &&
                  GameResolutionPreset.BestFitting(new GameResolution(2880, 2160)) == new GameResolution(1600, 1200) &&
                  !GameResolutionPreset.IsSelectable(new GameResolution(1200, 900)) &&
                  !GameResolutionPreset.IsSelectable(new GameResolution(1280, 960)) &&
                  !GameResolutionPreset.IsSelectable(new GameResolution(1440, 1080)) &&
                  !GameResolutionPreset.IsSelectable(new GameResolution(1920, 1440)) &&
                  !GameResolutionPreset.IsSelectable(new GameResolution(2880, 2160)),
                "monitor equivalents use modes whose heights are accepted by all three HUD tables", failures);
            var ultrawide = GameDisplayGeometry.ForMonitor(5120, 2160);
            var smallerUltrawide = GameDisplayGeometry.ForMonitor(3440, 1440);
            var hdUltrawide = GameDisplayGeometry.ForMonitor(2560, 1080);
            Check(ultrawide.Gameplay == new GameResolution(2880, 2160) &&
                  ultrawide.LeftPillarboxWidth == 1120 && ultrawide.RightPillarboxWidth == 1120 &&
                  smallerUltrawide.Gameplay == new GameResolution(1920, 1440) &&
                  smallerUltrawide.LeftPillarboxWidth == 760 && smallerUltrawide.RightPillarboxWidth == 760 &&
                  hdUltrawide.Gameplay == new GameResolution(1440, 1080) &&
                  hdUltrawide.LeftPillarboxWidth == 560 && hdUltrawide.RightPillarboxWidth == 560 &&
                  GameDisplayPreset.UltrawideChoices.Select(choice => choice.Monitor).SequenceEqual(
                  [new GameResolution(2560, 1080), new GameResolution(3440, 1440), new GameResolution(5120, 2160)]),
                "ultrawide monitors calculate centered 4:3 presentation bounds without unsafe game render heights", failures);
            var standardLayouts = new[]
            {
                (Monitor: new GameResolution(1600, 900), Image: new GameResolution(1200, 900), Pillar: 200),
                (Monitor: new GameResolution(1920, 1080), Image: new GameResolution(1440, 1080), Pillar: 240),
                (Monitor: new GameResolution(2560, 1440), Image: new GameResolution(1920, 1440), Pillar: 320),
                (Monitor: new GameResolution(3840, 2160), Image: new GameResolution(2880, 2160), Pillar: 480),
                (Monitor: new GameResolution(1920, 1200), Image: new GameResolution(1600, 1200), Pillar: 160),
                (Monitor: new GameResolution(2560, 1600), Image: new GameResolution(2132, 1599), Pillar: 214),
            };
            Check(GameDisplayPreset.StandardChoices.Select(choice => choice.Monitor)
                      .SequenceEqual(standardLayouts.Select(layout => layout.Monitor)) &&
                  GameDisplayPreset.Choices.Count == standardLayouts.Length + GameDisplayPreset.UltrawideChoices.Count &&
                  standardLayouts.All(layout =>
                  {
                      var bounds = GameDisplayGeometry.ForMonitor(layout.Monitor.Width, layout.Monitor.Height);
                      return bounds.Gameplay == layout.Image &&
                             bounds.LeftPillarboxWidth == layout.Pillar &&
                             bounds.RightPillarboxWidth == layout.Pillar;
                  }),
                "standard 16:9 and 16:10 presets remain alongside ultrawide previews with centered 4:3 bounds", failures);
            var unsupportedRejected = false;
            try { resolutionPreference.Save(new GameResolution(1440, 1080)); }
            catch (ArgumentOutOfRangeException) { unsupportedRejected = true; }
            Check(unsupportedRejected, "field-crashing gameplay mode cannot be newly selected", failures);
            File.WriteAllText(Path.Combine(compatibility, "gameplay-resolution.txt"), "1440x1080");
            Check(resolutionPreference.ReadSaved() == new GameResolution(1440, 1080) &&
                  resolutionPreference.GetResolution() == new GameResolution(1600, 1200),
                "previously saved unsupported mode safely falls back without deleting the preference", failures);
            var narrowMonitor = new ConfiguredGameResolutionProvider(app,
                new FixedResolutionProvider(new GameResolution(1440, 1080)));
            Check(narrowMonitor.GetResolution() == new GameResolution(1024, 768),
                "1080p monitor cannot accidentally request the crashing 1440x1080 render mode", failures);
            resolutionPreference.Save(null);
            Check(resolutionPreference.GetResolution() == new GameResolution(1600, 1200),
                "automatic gameplay resolution restores the largest fitting accepted mode", failures);
            var source = Path.Combine(root, "source");
            Write(source, "MW4.exe", "vengeance fixture");
            Write(source, "MW4X/MW4X.exe", "black knight fixture");
            Write(source, "MW4Mercs.exe", "mercenaries fixture");
            var installed = Path.Combine(root, "installed");
            var vengeance = Path.Combine(installed, "vengeance");
            var mercenaries = Path.Combine(installed, "mercenaries");
            var transaction = new StagedInstallTransaction();
            transaction.Execute(new InstallPlan("vengeance", new[]
            {
                new InstallFile(source, "MW4.exe", "MW4.exe"),
                new InstallFile(source, "MW4X/MW4X.exe", "MW4X/MW4X.exe"),
                new InstallFile(compatibility, "dgVoodoo.conf", "dgVoodoo.conf"),
                new InstallFile(compatibility, "dgVoodoo.conf", "MW4X/dgVoodoo.conf"),
            }, new[] { "vengeance", "black-knight" }), vengeance);
            transaction.Execute(new InstallPlan("mercenaries", new[]
            {
                new InstallFile(source, "MW4Mercs.exe", "MW4Mercs.exe"),
                new InstallFile(compatibility, "dgVoodoo.conf", "dgVoodoo.conf"),
            }), mercenaries);
            Write(vengeance, "Saves/pilot.sav", "keep me");
            var statuses = new InstallStatusReader(installed).Read();
            var diagnosticService = new InstallationDiagnosticsService();
            var report = diagnosticService.Build(statuses);
            Check(report.Contains("Vengeance: Ready", StringComparison.Ordinal) &&
                  report.Contains("Black Knight: Ready", StringComparison.Ordinal) &&
                  report.Contains("Mercenaries: Ready", StringComparison.Ordinal) &&
                  report.Contains("dgVoodoo.conf SHA-256:", StringComparison.Ordinal) &&
                  !report.Contains(root, StringComparison.OrdinalIgnoreCase),
                "shareable diagnostics include game and profile evidence without private paths", failures);
            var service = new GraphicsSettingsService(app, new OwnedInstallFileReplacementTransaction(),
                new InstallManifestVerifier(), new IdleProcessState());
            Check(service.Read(statuses) == DisplayResampling.Lanczos3,
                "graphics settings identify the qualified default across three installed titles", failures);

            var vengeanceOnly = Path.Combine(root, "vengeance-only");
            transaction.Execute(new InstallPlan("vengeance", new[]
            {
                new InstallFile(source, "MW4.exe", "MW4.exe"),
                new InstallFile(compatibility, "dgVoodoo.conf", "dgVoodoo.conf"),
            }, new[] { "vengeance" }), Path.Combine(vengeanceOnly, "vengeance"));
            var soloStatuses = new InstallStatusReader(vengeanceOnly).Read();
            Check(soloStatuses.Single(item => item.Product.Id == "vengeance").State == ProductInstallState.Ready &&
                  soloStatuses.Single(item => item.Product.Id == "black-knight").State == ProductInstallState.Missing &&
                  service.Read(soloStatuses) == DisplayResampling.Lanczos3,
                "Vengeance-only installation does not misclassify absent Black Knight as damaged", failures);
            var expansionExecutable = Path.Combine(vengeance, "MW4X", "MW4X.exe");
            var expansionBytes = File.ReadAllBytes(expansionExecutable);
            File.Delete(expansionExecutable);
            Check(new InstallStatusReader(installed).Read().Single(item => item.Product.Id == "black-knight").State ==
                  ProductInstallState.NeedsRepair,
                "declared Black Knight component with a missing executable still requires repair", failures);
            File.WriteAllBytes(expansionExecutable, expansionBytes);
            var busyRejected = false;
            try
            {
                new GraphicsSettingsService(app, new OwnedInstallFileReplacementTransaction(),
                    new InstallManifestVerifier(), new BusyProcessState()).Apply(statuses, DisplayResampling.Bilinear);
            }
            catch (InvalidOperationException) { busyRejected = true; }
            Check(busyRejected && service.Read(statuses) == DisplayResampling.Lanczos3,
                "graphics settings reject updates while a game is running without changing profiles", failures);
            service.Apply(statuses, DisplayResampling.Bilinear);
            Check(service.Read(new InstallStatusReader(installed).Read()) == DisplayResampling.Bilinear &&
                  new InstallManifestVerifier().Verify(vengeance, InstallVerificationScope.OwnedFiles).IsValid &&
                  new InstallManifestVerifier().Verify(mercenaries, InstallVerificationScope.OwnedFiles).IsValid &&
                  File.ReadAllText(Path.Combine(vengeance, "Saves", "pilot.sav")) == "keep me",
                "one display choice updates all three profiles while preserving manifests and saves", failures);
            var advanced = new GraphicsChoices(DisplayResampling.PointSampled,
                TextureFiltering.Eight, DisplayAntialiasing.Two);
            service.ApplyChoices(new InstallStatusReader(installed).Read(), advanced);
            Check(service.ReadChoices(new InstallStatusReader(installed).Read()) == advanced &&
                  File.ReadAllText(Path.Combine(vengeance, "dgVoodoo.conf")).Contains("Filtering                           = 8", StringComparison.Ordinal) &&
                  File.ReadAllText(Path.Combine(mercenaries, "dgVoodoo.conf")).Contains("Antialiasing                        = 2x", StringComparison.Ordinal),
                "texture filtering and antialiasing round-trip across all three titles", failures);
            service.ApplyChoices(new InstallStatusReader(installed).Read(),
                advanced with { Resampling = DisplayResampling.Bilinear });

            var vengeanceManifest = Path.Combine(vengeance, InstallManifest.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            var rollbackObserved = false;
            using (File.Open(vengeanceManifest, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                try { service.Apply(new InstallStatusReader(installed).Read(), DisplayResampling.PointSampled); }
                catch (IOException) { rollbackObserved = true; }
            }
            Check(rollbackObserved && service.Read(new InstallStatusReader(installed).Read()) == DisplayResampling.Bilinear,
                "second-tree failure restores the first game's prior global setting", failures);

            var damagedProfile = Path.Combine(mercenaries, "dgVoodoo.conf");
            var originalProfile = File.ReadAllBytes(damagedProfile);
            File.WriteAllText(damagedProfile, "unqualified profile");
            var damagedRejected = false;
            var damagedReport = diagnosticService.Build(new InstallStatusReader(installed).Read());
            try { service.Apply(new InstallStatusReader(installed).Read(), DisplayResampling.PointSampled); }
            catch (InvalidOperationException) { damagedRejected = true; }
            Check(damagedRejected &&
                  File.ReadAllText(Path.Combine(vengeance, "dgVoodoo.conf")).Contains("= bilinear", StringComparison.Ordinal),
                "repair-required game blocks global settings before another tree changes", failures);
            Check(damagedReport.Contains("Mercenaries: NeedsRepair", StringComparison.Ordinal) &&
                  !damagedReport.Contains(root, StringComparison.OrdinalIgnoreCase) &&
                  !damagedReport.Contains("unqualified profile", StringComparison.Ordinal),
                "diagnostics categorize damaged installs without exporting paths or file contents", failures);
            File.WriteAllBytes(damagedProfile, originalProfile);

            service.ApplyChoices(new InstallStatusReader(installed).Read(), GraphicsChoices.Default);
            var defaultBytes = File.ReadAllBytes(sourceProfile);
            Check(File.ReadAllBytes(Path.Combine(vengeance, "dgVoodoo.conf")).AsSpan().SequenceEqual(defaultBytes) &&
                  File.ReadAllBytes(Path.Combine(vengeance, "MW4X", "dgVoodoo.conf")).AsSpan().SequenceEqual(defaultBytes) &&
                  File.ReadAllBytes(Path.Combine(mercenaries, "dgVoodoo.conf")).AsSpan().SequenceEqual(defaultBytes),
                "reset restores exact qualified profiles for every title", failures);
            Check(new OwnedInstallUninstaller().Remove(mercenaries).Status == InstallRemovalStatus.Removed &&
                  new OwnedInstallUninstaller().Remove(vengeance).Status == InstallRemovalStatus.Removed &&
                  File.ReadAllText(Path.Combine(vengeance, "Saves", "pilot.sav")) == "keep me",
                "global profile updates retain ownership-safe uninstall and preserve user saves", failures);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static void Write(string root, string relative, string text)
    {
        var path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }

    private static void Check(bool condition, string message, List<string> failures)
    {
        if (!condition) failures.Add("FAIL: " + message);
    }

    private sealed class IdleProcessState : IGameProcessState
    {
        public bool IsRunning(string executablePath) => false;
    }

    private sealed class FixedResolutionProvider(GameResolution resolution) : IGameResolutionProvider
    {
        public GameResolution GetResolution() => resolution;
    }

    private sealed class BusyProcessState : IGameProcessState
    {
        public bool IsRunning(string executablePath) => true;
    }
}
