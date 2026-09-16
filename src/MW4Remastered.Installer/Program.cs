using MW4Remastered.Core.Install;
using MW4Remastered.Core.Launch;
using MW4Remastered.Core.Media;

namespace MW4Remastered.Installer;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        var initialMediaPaths = ParseMediaPaths(args);
        var mediaSessions = new MediaSourceSessionFactory();
        var mediaInspection = new MediaInspectionService();
        var processStarter = new SystemProcessStarter();
        var compatibilityRoot = Path.Combine(AppContext.BaseDirectory, "Compatibility", "BlackKnight");
        GameInstallationCoordinator? blackKnightInstaller = null;
        string compatibilityStatus;
        try
        {
            var compatibility = BlackKnightInstallPlanBuilder.CreatePackagedPayload(compatibilityRoot);
            compatibility.ValidateAndCreateInstallFiles();
            var plans = new GameInstallPlanFactory(
                new VengeanceInstallPlanBuilder(),
                new BlackKnightInstallPlanBuilder(compatibility, mediaInspection, new DirectoryMediaInventory()),
                new MercenariesInstallPlanBuilder());
            blackKnightInstaller = new GameInstallationCoordinator(
                plans, new CabinetPayloadExtractor(), new StagedInstallTransaction(), new InstallManifestVerifier());
            compatibilityStatus = "Qualified Black Knight compatibility bundle verified.";
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException or ArgumentException)
        {
            compatibilityStatus = $"Black Knight installation unavailable: {error.Message}";
        }
        Application.Run(new InstallerForm(
            new MediaSourceInspector(mediaInspection, mediaSessions),
            new MediaSelectionSet(),
            new MediaSelectionSessionFactory(mediaSessions, mediaInspection),
            new InstallDestinationPlanner(),
            blackKnightInstaller,
            compatibilityStatus,
            new InstalledLauncherOrchestrator(AppContext.BaseDirectory, processStarter),
            initialMediaPaths));
    }

    private static IReadOnlyList<string> ParseMediaPaths(IReadOnlyList<string> args)
    {
        var paths = new List<string>();
        for (var index = 0; index < args.Count - 1; index++)
        {
            if (!args[index].Equals("--media", StringComparison.OrdinalIgnoreCase)) continue;
            var path = args[++index];
            if (!string.IsNullOrWhiteSpace(path)) paths.Add(path);
        }
        return paths;
    }
}
