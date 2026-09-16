using MW4Remastered.Core.Install;
using MW4Remastered.Core.Media;

namespace MW4Remastered.Installer;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        var mediaSessions = new MediaSourceSessionFactory();
        var mediaInspection = new MediaInspectionService();
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
            compatibilityStatus));
    }
}
