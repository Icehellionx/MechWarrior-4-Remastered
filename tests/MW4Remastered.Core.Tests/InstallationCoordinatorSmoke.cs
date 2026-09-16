using System.IO.Compression;
using System.Security.Cryptography;
using MW4Remastered.Core.Install;
using MW4Remastered.Core.Media;

internal static class InstallationCoordinatorSmoke
{
    public static void Run(List<string> failures)
    {
        var root = Path.Combine(Path.GetTempPath(), "mw4-install-coordinator-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var inputs = PrepareInputs(root);
            var progress = new RecordingProgress();
            var coordinator = CreateCoordinator(inputs);

            var vengeance = coordinator.Install(
                new VengeanceInstallRequest(inputs.VengeanceDiscOne, inputs.VengeanceDiscTwo, inputs.VengeanceExecutable),
                Path.Combine(root, "installed", "vengeance"), progress);
            Check(vengeance.Manifest.ProductId == "vengeance" && vengeance.Manifest.Files.Count > 0, "coordinator installs and verifies Vengeance", failures);

            var blackKnight = coordinator.Install(
                new BlackKnightInstallRequest(inputs.BlackKnightDisc, inputs.BlackKnightExecutable),
                Path.Combine(root, "installed", "black-knight"), progress);
            Check(blackKnight.Manifest.ProductId == "black-knight" && blackKnight.Manifest.Files.Count > 0, "coordinator installs and verifies Black Knight", failures);

            var mercenaries = coordinator.Install(
                new MercenariesInstallRequest(inputs.MercenariesDiscOne, inputs.MercenariesDiscTwo, inputs.MercenariesExecutable),
                Path.Combine(root, "installed", "mercenaries"), progress);
            Check(mercenaries.Manifest.ProductId == "mercenaries" && mercenaries.Manifest.Files.Count > 0, "coordinator extracts, installs, and verifies Mercenaries", failures);
            Check(!Directory.EnumerateDirectories(Path.Combine(root, "installed"), ".mercenaries-cabinet-*").Any(), "coordinator removes Mercenaries cabinet scratch after success", failures);
            Check(progress.Events.Count(item => item.Stage == GameInstallationStage.Completed) == 3, "coordinator reports completion for all three games", failures);

            var failedDestination = Path.Combine(root, "failed", "mercenaries");
            var failed = false;
            try
            {
                new GameInstallationCoordinator(
                    new ThrowingPlanFactory(), new CabinetPayloadExtractor(), new StagedInstallTransaction(), new InstallManifestVerifier())
                    .Install(new MercenariesInstallRequest(inputs.MercenariesDiscOne, inputs.MercenariesDiscTwo, inputs.MercenariesExecutable), failedDestination);
            }
            catch (InvalidDataException)
            {
                failed = true;
            }
            Check(failed && !Directory.Exists(failedDestination), "coordinator leaves no install when planning fails", failures);
            Check(!Directory.EnumerateDirectories(Path.Combine(root, "failed"), ".mercenaries-cabinet-*").Any(), "coordinator removes Mercenaries cabinet scratch after failure", failures);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    private static GameInstallationCoordinator CreateCoordinator(FixtureInputs inputs)
    {
        var inspection = new MediaInspectionService();
        var inventory = new DirectoryMediaInventory();
        var plans = new GameInstallPlanFactory(
            new VengeanceInstallPlanBuilder(Hash(inputs.VengeanceExecutable), inspection, inventory),
            new BlackKnightInstallPlanBuilder(Hash(inputs.BlackKnightExecutable), inspection, inventory),
            new MercenariesInstallPlanBuilder(Hash(inputs.MercenariesExecutable), inspection, inventory));
        return new GameInstallationCoordinator(plans, new CabinetPayloadExtractor(), new StagedInstallTransaction(), new InstallManifestVerifier());
    }

    private static FixtureInputs PrepareInputs(string root)
    {
        var vengeanceDiscOne = Path.Combine(root, "vengeance-1");
        var vengeanceDiscTwo = Path.Combine(root, "vengeance-2");
        var blackKnightDisc = Path.Combine(root, "black-knight");
        var mercenariesDiscOne = Path.Combine(root, "mercenaries-1");
        var mercenariesDiscTwo = Path.Combine(root, "mercenaries-2");
        CreateLayout("vengeance-disc-1", vengeanceDiscOne);
        CreateLayout("vengeance-disc-2", vengeanceDiscTwo);
        CreateLayout("black-knight-disc-1", blackKnightDisc);
        CreateLayout("mercenaries-disc-1", mercenariesDiscOne);
        CreateLayout("mercenaries-disc-2", mercenariesDiscTwo);

        var cabinet = Path.Combine(mercenariesDiscOne, "MSGAME.CAB");
        File.Delete(cabinet);
        var cabinetSource = Path.Combine(root, "cabinet-source");
        Write(cabinetSource, "GAME/RESOURCE/CORE.MW4", "core");
        Write(cabinetSource, "GAME/RESOURCE/PROPS.MW4", "props");
        Write(cabinetSource, "GAME/RESOURCE/TEXTURES.MW4", "textures");
        ZipFile.CreateFromDirectory(cabinetSource, cabinet, CompressionLevel.NoCompression, includeBaseDirectory: false);

        var vengeanceExecutable = Write(root, "replacements/vengeance/MW4.exe", "vengeance executable");
        var blackKnightExecutable = Write(root, "replacements/black-knight/MW4X.exe", "black knight executable");
        var mercenariesExecutable = Write(root, "replacements/mercenaries/MW4Mercs.exe", "mercenaries executable");
        return new FixtureInputs(
            vengeanceDiscOne, vengeanceDiscTwo, vengeanceExecutable,
            blackKnightDisc, blackKnightExecutable,
            mercenariesDiscOne, mercenariesDiscTwo, mercenariesExecutable);
    }

    private static void CreateLayout(string layoutId, string root)
    {
        foreach (var path in MediaCatalog.Layouts.Single(item => item.Id == layoutId).RequiredPaths) Write(root, path, "fixture");
    }

    private static string Write(string root, string relativePath, string contents)
    {
        var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, contents);
        return path;
    }

    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();

    private static void Check(bool condition, string message, List<string> failures)
    {
        if (!condition) failures.Add("FAIL: " + message);
    }

    private sealed record FixtureInputs(
        string VengeanceDiscOne, string VengeanceDiscTwo, string VengeanceExecutable,
        string BlackKnightDisc, string BlackKnightExecutable,
        string MercenariesDiscOne, string MercenariesDiscTwo, string MercenariesExecutable);

    private sealed class RecordingProgress : IProgress<GameInstallationProgress>
    {
        public List<GameInstallationProgress> Events { get; } = new();
        public void Report(GameInstallationProgress value) => Events.Add(value);
    }

    private sealed class ThrowingPlanFactory : IGameInstallPlanFactory
    {
        public InstallPlan Build(GameInstallRequest request, string? mercenariesCabinetPayloadRoot = null) =>
            throw new InvalidDataException("Synthetic planning failure.");
    }
}
