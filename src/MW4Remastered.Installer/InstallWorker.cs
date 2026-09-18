using MW4Remastered.Core;
using MW4Remastered.Core.Install;
using MW4Remastered.Core.Launch;
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
        TryStartLog(args.LogPath, "Starting contained game installation.");
        var sourceSessions = new MediaSourceSessionFactory();
        var inspection = new MediaInspectionService();
        var inspector = new MediaSourceInspector(inspection, sourceSessions);
        var selection = new MediaSelectionSet();
        var bundledMercenariesUpdate = Path.Combine(args.DestinationPath, "Updates", "MercenariesPR1");
        if (Directory.Exists(bundledMercenariesUpdate))
        {
            TryAppendLog(args.LogPath, "Inspecting bundled, hash-qualified Mercenaries Point Release 1 payload.");
            selection.Add(bundledMercenariesUpdate, inspector.Inspect(bundledMercenariesUpdate));
        }
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
            foreach (var product in plan.Products.Where(item => item.EffectiveComponents.Any(component => !alreadyReady.Contains(component))))
            {
                TryAppendLog(args.LogPath, $"Installing {product.DisplayName}.");
                var progress = new Progress<GameInstallationProgress>(value =>
                    TryAppendLog(args.LogPath, $"{value.ProductId}: {value.Stage} - {value.Message}"));
                coordinator.Install(CreateInstallRequest(product, media, args.DestinationPath), product.DestinationPath, progress);
                installedThisRun.Add(product.DestinationPath);
            }
        }
        catch
        {
            RollBack(installedThisRun, args.LogPath);
            throw;
        }

        var finalReady = GetReadyProductIds(plan.RootPath);
        if (plan.Products.Any(item => item.EffectiveComponents.Any(component => !finalReady.Contains(component))))
        {
            RollBack(installedThisRun, args.LogPath);
            throw new InvalidDataException("One or more selected games failed final ownership verification.");
        }
        var statuses = new InstallStatusReader(plan.RootPath).Read()
            .Where(item => item.Product.Kind == ProductKind.Game && finalReady.Contains(item.Product.Id))
            .ToDictionary(item => item.Product.Id, StringComparer.OrdinalIgnoreCase);
        var configuration = new LegacyGameConfiguration();
        foreach (var status in statuses.Values)
        {
            configuration.Ensure(status);
            TryAppendLog(args.LogPath, $"Prepared modern graphics configuration for {status.Product.DisplayName}.");
        }
        var registration = new LegacyGameRegistration();
        var compatibilityReplacement = new OwnedInstallFileReplacementTransaction();
        var compatibilityRoot = Path.Combine(args.DestinationPath, "Compatibility", "dgVoodoo2");
        var registeredThisRun = new List<ProductStatus>();
        try
        {
            foreach (var componentId in plan.Products.SelectMany(product => product.EffectiveComponents).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var status = statuses[componentId];
                registration.Ensure(status);
                registeredThisRun.Add(status);
                TryAppendLog(args.LogPath, $"Registered {status.Product.DisplayName} for non-elevated launch.");
            }
            if (statuses.TryGetValue("vengeance", out var vengeanceStatus) &&
                !string.IsNullOrWhiteSpace(vengeanceStatus.InstallPath))
            {
                var verified = new InstallManifestVerifier().Verify(
                    vengeanceStatus.InstallPath, InstallVerificationScope.OwnedFiles);
                if (!verified.IsValid || verified.Manifest is null)
                    throw new InvalidDataException("Vengeance media-path migration requires a verified owned manifest.");
                var migration = VengeanceMediaPathMap.CreateOwnedMigration(
                    vengeanceStatus.InstallPath, verified.Manifest);
                if (migration.Files.Count > 0)
                {
                    compatibilityReplacement.Migrate(
                        "vengeance",
                        vengeanceStatus.InstallPath,
                        migration.Files,
                        migration.RetiredOwnedPaths);
                    TryAppendLog(args.LogPath, "Restored Vengeance long filenames from the original setup table.");
                }
            }
            foreach (var productId in new[] { "vengeance", "mercenaries" })
            {
                if (!statuses.TryGetValue(productId, out var status) || string.IsNullOrWhiteSpace(status.InstallPath)) continue;
                var files = productId == "vengeance"
                    ? LegacyPresentationCompatibility.CreateVengeanceFiles(
                        compatibilityRoot,
                        includeBlackKnight: statuses.ContainsKey("black-knight"))
                    : LegacyPresentationCompatibility.CreateMercenariesFiles(compatibilityRoot);
                var retiredProfiles = productId == "vengeance"
                    ? new[] { "DDrawCompat-MW4.ini" }
                    : new[] { "DDrawCompat-MW4Mercs.ini" };
                compatibilityReplacement.Migrate(productId, status.InstallPath, files, retiredProfiles);
                TryAppendLog(args.LogPath, $"Verified current presentation compatibility for {status.Product.DisplayName}.");
            }
        }
        catch
        {
            foreach (var status in registeredThisRun.AsEnumerable().Reverse()) registration.RemoveOwned(status);
            RollBack(installedThisRun, args.LogPath);
            throw;
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
        if (plan.Products.Any(item => item.ProductId is not ("vengeance" or "mercenaries")))
            throw new InvalidDataException("The media selection includes an unsupported game installation path.");
        var packSelected = selection.Capabilities.Any(item => item.Kind == ProductKind.OptionalPack && item.IsComplete);
        var vengeanceSelected = selection.Capabilities.Any(item => item.ProductId == "vengeance" && item.IsComplete);
        if (packSelected && !vengeanceSelected)
            throw new InvalidDataException("Mech Pak installation requires both Vengeance discs in the same setup run.");
        var blackKnightSelected = selection.Capabilities.Any(item => item.ProductId == "black-knight" && item.IsComplete);
        if (blackKnightSelected && !packSelected)
            throw new InvalidDataException(
                "Black Knight requires either the Inner Sphere or Clan Mech Pak media in this setup run because those original discs contain the official Black Knight Point Release 1 update.");
        var mercenariesSelected = selection.Capabilities.Any(item => item.ProductId == "mercenaries" && item.IsComplete);
        if (mercenariesSelected && !selection.Layouts.Any(item => item.Layout.Id.Equals("mercenaries-pr1", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidDataException("The setup package is missing its qualified Mercenaries Point Release 1 payload. Download a complete installer package and try again.");
        var conflict = plan.Products.FirstOrDefault(item =>
            item.EffectiveComponents.Any(component => !alreadyReady.Contains(component)) && File.Exists(item.DestinationPath));
        if (conflict is not null) throw new IOException($"The game destination is an existing file: {conflict.DestinationPath}");
    }

    private static void TryStartLog(string path, string message)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            File.WriteAllText(path, $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}");
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException) { }
    }

    private static GameInstallRequest CreateInstallRequest(
        InstallDestinationProduct product,
        IMediaSelectionSession media,
        string applicationRoot) => product.ProductId switch
    {
        "vengeance" => new VengeanceInstallRequest(media.GetRoot("vengeance-disc-1"), media.GetRoot("vengeance-disc-2"),
            new[] { "inner-sphere-mech-pak", "clan-mech-pak" }.Where(media.Layouts.ContainsKey).Select(media.GetRoot).ToArray(),
            product.EffectiveComponents.Contains("black-knight", StringComparer.OrdinalIgnoreCase)
                ? media.GetRoot("black-knight-disc-1")
                : null,
            Path.Combine(applicationRoot, "Compatibility", "dgVoodoo2")),
        "mercenaries" => new MercenariesInstallRequest(
            media.GetRoot("mercenaries-disc-1"),
            media.GetRoot("mercenaries-disc-2"),
            media.GetRoot("mercenaries-pr1"),
            Path.Combine(applicationRoot, "Compatibility", "dgVoodoo2"),
            new[] { "inner-sphere-mech-pak", "clan-mech-pak" }
                .Where(media.Layouts.ContainsKey)
                .Select(id => id == "inner-sphere-mech-pak" ? "inner-sphere" : "clan")
                .ToArray()),
        _ => throw new InvalidOperationException($"Unsupported product: {product.ProductId}"),
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
