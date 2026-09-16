using MW4Remastered.Core;
using MW4Remastered.Core.Install;
using MW4Remastered.Core.Media;

namespace MW4Remastered.Installer;

internal sealed record InstallWorkerArguments(string DestinationPath, string LogPath, IReadOnlyList<string> MediaPaths)
{
    public static InstallWorkerArguments Parse(IReadOnlyList<string> args)
    {
        if (args.Count == 0 || !args[0].Equals("--install-worker", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("This component is an internal worker and must be started by MechWarrior 4 Remastered Setup.");

        string? destination = null;
        string? log = null;
        var media = new List<string>();
        for (var index = 1; index < args.Count; index++)
        {
            if (index + 1 >= args.Count) throw new ArgumentException($"Missing value for worker argument: {args[index]}");
            var name = args[index];
            var value = args[++index];
            if (name.Equals("--destination", StringComparison.OrdinalIgnoreCase)) destination = value;
            else if (name.Equals("--log", StringComparison.OrdinalIgnoreCase)) log = value;
            else if (name.Equals("--media", StringComparison.OrdinalIgnoreCase)) media.Add(value);
            else throw new ArgumentException($"Unknown worker argument: {name}");
        }

        if (string.IsNullOrWhiteSpace(destination) || !Path.IsPathFullyQualified(destination))
            throw new ArgumentException("Setup did not provide an absolute installation destination.");
        if (string.IsNullOrWhiteSpace(log) || !Path.IsPathFullyQualified(log))
            throw new ArgumentException("Setup did not provide an absolute worker log path.");
        if (media.Count == 0) throw new ArgumentException("Setup did not provide any original media.");
        return new InstallWorkerArguments(Path.GetFullPath(destination), Path.GetFullPath(log),
            media.Select(Path.GetFullPath).ToArray());
    }
}

internal sealed class InstallWorker
{
    public int Run(InstallWorkerArguments args)
    {
        ArgumentNullException.ThrowIfNull(args);
        TryAppendLog(args.LogPath, "Starting contained game installation.");
        var sourceSessions = new MediaSourceSessionFactory();
        var inspection = new MediaInspectionService();
        var inspector = new MediaSourceInspector(inspection, sourceSessions);
        var selection = new MediaSelectionSet();
        foreach (var path in args.MediaPaths)
        {
            TryAppendLog(args.LogPath, $"Inspecting selected source: {path}");
            selection.Add(path, inspector.Inspect(path));
        }

        var planner = new InstallDestinationPlanner();
        var preliminary = planner.Plan(selection.Current, args.DestinationPath);
        var alreadyReady = GetReadyProductIds(preliminary.RootPath);
        var plan = planner.Plan(selection.Current, args.DestinationPath, alreadyReady);
        ValidatePlan(selection.Current, plan, alreadyReady);

        var sessionFactory = new MediaSelectionSessionFactory(sourceSessions, inspection);
        var coordinator = new GameInstallationCoordinator();
        var installedThisRun = new List<string>();
        try
        {
            using var media = sessionFactory.Open(selection.Current);
            foreach (var product in plan.Products.Where(item => !alreadyReady.Contains(item.ProductId)))
            {
                TryAppendLog(args.LogPath, $"Installing {product.DisplayName}.");
                var progress = new Progress<GameInstallationProgress>(value =>
                    TryAppendLog(args.LogPath, $"{value.ProductId}: {value.Stage} - {value.Message}"));
                coordinator.Install(CreateInstallRequest(product.ProductId, media), product.DestinationPath, progress);
                installedThisRun.Add(product.DestinationPath);
            }
        }
        catch
        {
            RollBack(installedThisRun, args.LogPath);
            throw;
        }

        var finalReady = GetReadyProductIds(plan.RootPath);
        if (plan.Products.Any(item => !finalReady.Contains(item.ProductId)))
        {
            RollBack(installedThisRun, args.LogPath);
            throw new InvalidDataException("One or more selected games failed final ownership verification.");
        }
        TryAppendLog(args.LogPath, "All selected games installed and verified.");
        return 0;
    }

    internal static void TryAppendLog(string? path, string message)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            File.AppendAllText(path, $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}");
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException) { }
    }

    private static void ValidatePlan(MediaSelectionSnapshot selection, InstallDestinationPlan plan, HashSet<string> alreadyReady)
    {
        if (!plan.HasSelectedGames) throw new InvalidDataException("The selected files do not contain a complete supported game media set.");
        if (plan.BlockedProducts.Count > 0)
            throw new InvalidDataException(string.Join("; ", plan.BlockedProducts.Select(item =>
                $"{item.DisplayName} requires {string.Join(" + ", item.MissingDependencyIds)}")));
        if (!plan.HasEnoughSpace) throw new IOException("The selected installation destination does not have enough free space.");
        if (plan.Products.Any(item => item.ProductId is not ("vengeance" or "black-knight" or "mercenaries")))
            throw new InvalidDataException("The media selection includes an unsupported game installation path.");
        var packSelected = selection.Capabilities.Any(item => item.Kind == ProductKind.OptionalPack && item.IsComplete);
        var vengeanceSelected = selection.Capabilities.Any(item => item.ProductId == "vengeance" && item.IsComplete);
        if (packSelected && !vengeanceSelected)
            throw new InvalidDataException("Mech Pak installation requires both Vengeance discs in the same setup run.");
        var conflict = plan.Products.FirstOrDefault(item => !alreadyReady.Contains(item.ProductId) &&
            (Directory.Exists(item.DestinationPath) || File.Exists(item.DestinationPath)));
        if (conflict is not null) throw new IOException($"The game destination already exists and is not a verified installation: {conflict.DestinationPath}");
    }

    private static GameInstallRequest CreateInstallRequest(string productId, IMediaSelectionSession media) => productId switch
    {
        "vengeance" => new VengeanceInstallRequest(media.GetRoot("vengeance-disc-1"), media.GetRoot("vengeance-disc-2"),
            new[] { "inner-sphere-mech-pak", "clan-mech-pak" }.Where(media.Layouts.ContainsKey).Select(media.GetRoot).ToArray()),
        "black-knight" => new BlackKnightInstallRequest(media.GetRoot("black-knight-disc-1")),
        "mercenaries" => new MercenariesInstallRequest(media.GetRoot("mercenaries-disc-1"), media.GetRoot("mercenaries-disc-2")),
        _ => throw new InvalidOperationException($"Unsupported product: {productId}"),
    };

    private static HashSet<string> GetReadyProductIds(string root) => new InstallStatusReader(root).Read()
        .Where(item => item.State == ProductInstallState.Ready).Select(item => item.Product.Id)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static void RollBack(IEnumerable<string> installedPaths, string logPath)
    {
        var uninstaller = new OwnedInstallUninstaller();
        foreach (var path in installedPaths.Reverse())
        {
            var result = uninstaller.Remove(path);
            TryAppendLog(logPath, result.Status == InstallRemovalStatus.Removed
                ? $"Rolled back incomplete setup payload: {path}"
                : $"Rollback blocked for {path}: {string.Join("; ", result.Issues)}");
        }
    }
}
