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

        var registration = new LegacyGameRegistration();
        foreach (var planned in plan.Products)
        {
            foreach (var componentId in planned.EffectiveComponents)
            {
                var product = ProductCatalog.All.Single(item => item.Id == componentId);
                if (product.Kind != ProductKind.Game) continue;
                registration.ValidateAvailable(product, planned.DestinationPath);
            }
        }

        var sessionFactory = new MediaSelectionSessionFactory(sourceSessions, inspection);
        var coordinator = new GameInstallationCoordinator();
        var installedThisRun = new List<string>();
        try
        {
            using var media = sessionFactory.Open(selection.Current);
            foreach (var product in plan.Products.Where(item => !IsInstalled(item)))
            {
                var wasInstalled = File.Exists(Path.Combine(product.DestinationPath, InstallManifest.RelativePath));
                TryAppendLog(args.LogPath, $"Installing {product.DisplayName}.");
                var progress = new Progress<GameInstallationProgress>(value =>
                    TryAppendLog(args.LogPath, $"{value.ProductId}: {value.Stage} - {value.Message}"));
                coordinator.Install(CreateInstallRequest(product, media, args.DestinationPath), product.DestinationPath, progress);
                if (!wasInstalled) installedThisRun.Add(product.DestinationPath);
            }
        }
        catch
        {
            RollBack(installedThisRun, args.LogPath);
            throw;
        }

        var finalReady = GetReadyProductIds(plan.RootPath);
        if (plan.Products.Any(item => !IsInstalled(item)))
        {
            RollBack(installedThisRun, args.LogPath);
            throw new InvalidDataException("One or more selected games or Mech Paks failed final ownership verification.");
        }
        var statuses = new InstallStatusReader(plan.RootPath).Read()
            .Where(item => item.Product.Kind == ProductKind.Game && finalReady.Contains(item.Product.Id))
            .ToDictionary(item => item.Product.Id, StringComparer.OrdinalIgnoreCase);
        var configuration = new LegacyGameConfiguration();
        foreach (var status in statuses.Values)
        {
            configuration.EnsureDefaults(status, configuration.ResolveResolution());
            TryAppendLog(args.LogPath, $"Prepared modern graphics configuration for {status.Product.DisplayName}.");
        }
        var compatibilityReplacement = new OwnedInstallFileReplacementTransaction();
        var compatibilityRoot = Path.Combine(args.DestinationPath, "Compatibility", "dgVoodoo2");
        var inputCompatibilityRoot = Path.Combine(args.DestinationPath, "Compatibility", "dinputto8");
        var registeredThisRun = new List<ProductStatus>();
        try
        {
            foreach (var componentId in plan.Products.SelectMany(product => product.EffectiveComponents).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (ProductCatalog.All.Single(item => item.Id == componentId).Kind != ProductKind.Game) continue;
                var status = statuses[componentId];
                registration.Ensure(status);
                if (!alreadyReady.Contains(componentId)) registeredThisRun.Add(status);
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
            if (statuses.TryGetValue("vengeance", out var movieStatus) &&
                !string.IsNullOrWhiteSpace(movieStatus.InstallPath))
            {
                var verified = new InstallManifestVerifier().Verify(movieStatus.InstallPath, InstallVerificationScope.OwnedFiles);
                if (!verified.IsValid || verified.Manifest is null)
                    throw new InvalidDataException("Vengeance movie correction requires a verified owned manifest.");
                var scratch = Path.Combine(Path.GetTempPath(), $"mw4-vengeance-movie-upgrade-{Guid.NewGuid():N}");
                try
                {
                    var replacements = new VengeanceMovieSurfaceTransform().CreateUpgradeReplacement(
                        movieStatus.InstallPath, verified.Manifest, scratch);
                    if (replacements.Count > 0)
                    {
                        compatibilityReplacement.Execute("vengeance", movieStatus.InstallPath, replacements);
                        TryAppendLog(args.LogPath, "Corrected retail Vengeance movie surface lifetime using the official Patch 3 behavior.");
                    }
                }
                finally
                {
                    if (Directory.Exists(scratch)) Directory.Delete(scratch, recursive: true);
                }
            }
            var mechPakUpgrade = new MechPakOwnedExecutableUpgrade();
            var mechPakArchiveUpgrade = new MechPakOwnedArchiveUpgrade();
            foreach (var productId in new[] { "vengeance", "mercenaries" })
            {
                if (!statuses.TryGetValue(productId, out var status) || string.IsNullOrWhiteSpace(status.InstallPath)) continue;
                var verified = new InstallManifestVerifier().Verify(
                    status.InstallPath, InstallVerificationScope.OwnedFiles);
                if (!verified.IsValid || verified.Manifest is null)
                    throw new InvalidDataException($"{status.Product.DisplayName} Mech Pak upgrade requires a verified owned manifest.");

                var scratch = Path.Combine(Path.GetTempPath(), $"mw4-mech-pak-upgrade-{Guid.NewGuid():N}");
                try
                {
                    var replacements = mechPakUpgrade.CreateReplacements(status.InstallPath, verified.Manifest, scratch)
                        .Concat(mechPakArchiveUpgrade.CreateReplacements(status.InstallPath, verified.Manifest, scratch))
                        .ToArray();
                    if (replacements.Length == 0) continue;
                    compatibilityReplacement.Execute(productId, status.InstallPath, replacements);
                    TryAppendLog(args.LogPath, $"Upgraded stock chassis and component Mech Pak ownership for {status.Product.DisplayName}.");
                }
                finally
                {
                    if (Directory.Exists(scratch)) Directory.Delete(scratch, recursive: true);
                }
            }
            if (statuses.TryGetValue("black-knight", out var blackKnightStatus) &&
                !string.IsNullOrWhiteSpace(blackKnightStatus.InstallPath))
            {
                var verified = new InstallManifestVerifier().Verify(
                    blackKnightStatus.InstallPath, InstallVerificationScope.OwnedFiles);
                if (!verified.IsValid || verified.Manifest is null)
                    throw new InvalidDataException("Black Knight DPI migration requires a verified owned manifest.");
                var scratch = Path.Combine(Path.GetTempPath(), $"mw4-black-knight-dpi-upgrade-{Guid.NewGuid():N}");
                try
                {
                    var replacements = new BlackKnightDpiManifestTransform().CreateUpgradeReplacement(
                        blackKnightStatus.InstallPath, verified.Manifest, compatibilityRoot, scratch);
                    if (replacements.Count > 0)
                    {
                        compatibilityReplacement.Execute("vengeance", blackKnightStatus.InstallPath, replacements);
                        TryAppendLog(args.LogPath, "Embedded the system-DPI-aware manifest in the owned Black Knight executable.");
                    }
                }
                finally
                {
                    if (Directory.Exists(scratch)) Directory.Delete(scratch, recursive: true);
                }
            }
            foreach (var productId in new[] { "vengeance", "mercenaries" })
            {
                if (!statuses.TryGetValue(productId, out var status) || string.IsNullOrWhiteSpace(status.InstallPath)) continue;
                var hasBlackKnight = statuses.ContainsKey("black-knight");
                var files = productId == "vengeance"
                    ? LegacyPresentationCompatibility.CreateVengeanceFiles(
                        compatibilityRoot, includeBlackKnight: hasBlackKnight)
                        .Concat(LegacyInputCompatibility.CreateFiles(inputCompatibilityRoot, hasBlackKnight))
                        .ToArray()
                    : LegacyPresentationCompatibility.CreateMercenariesFiles(compatibilityRoot)
                        .Concat(LegacyInputCompatibility.CreateMercenariesFiles(inputCompatibilityRoot))
                        .ToArray();
                var retiredProfiles = productId == "vengeance"
                    ? new[] { "DDrawCompat-MW4.ini" }
                    : new[] { "DDrawCompat-MW4Mercs.ini" };
                compatibilityReplacement.Migrate(productId, status.InstallPath, files, retiredProfiles);
                TryAppendLog(args.LogPath, $"Verified current presentation and input compatibility for {status.Product.DisplayName}.");
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
        if (!plan.HasSelectedGames)
        {
            var blackKnightDisc = selection.Capabilities.Any(item => item.ProductId == "black-knight" && item.IsComplete);
            if (blackKnightDisc)
                throw new InvalidDataException("Black Knight is an expansion. Select both Vengeance discs and the Black Knight disc together; the Inner Sphere or Clan Mech Pak disc is also required for its official Point Release 1 update.");
            throw new InvalidDataException("The selected files do not contain a complete supported game media set. Vengeance and Mercenaries each require both original discs.");
        }
        if (plan.BlockedProducts.Count > 0)
            throw new InvalidDataException(string.Join("; ", plan.BlockedProducts.Select(item =>
                $"{item.DisplayName} requires {string.Join(" + ", item.MissingDependencyIds)}")));
        if (!plan.HasEnoughSpace) throw new IOException("The selected installation destination does not have enough free space.");
        foreach (var product in plan.Products)
        {
            if (!File.Exists(Path.Combine(product.DestinationPath, InstallManifest.RelativePath))) continue;
            var prior = new InstallManifestVerifier().Verify(product.DestinationPath, InstallVerificationScope.OwnedFiles);
            if (!prior.IsValid || prior.Manifest is null)
                throw new InvalidDataException("The existing game has changed or damaged owned files. Repair or remove it before adding content; saves and settings have not been changed.");
            var missing = prior.Manifest.EffectiveComponents.Except(product.EffectiveComponents, StringComparer.OrdinalIgnoreCase).ToArray();
            if (missing.Length > 0 && !product.IsSatisfiedBy(prior.Manifest))
                throw new InvalidDataException($"To add content without removing installed components, also select the original media for: {string.Join(", ", missing)}. Include both game discs and all installed expansion/Mech Pak discs together.");
        }
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
            Path.Combine(applicationRoot, "Compatibility", "dgVoodoo2"),
            Path.Combine(applicationRoot, "Compatibility", "dinputto8")),
        "mercenaries" => new MercenariesInstallRequest(
            media.GetRoot("mercenaries-disc-1"),
            media.GetRoot("mercenaries-disc-2"),
            media.GetRoot("mercenaries-pr1"),
            Path.Combine(applicationRoot, "Compatibility", "dgVoodoo2"),
            new[] { "inner-sphere-mech-pak", "clan-mech-pak" }
                .Where(media.Layouts.ContainsKey)
                .Select(id => id == "inner-sphere-mech-pak" ? "inner-sphere" : "clan")
                .ToArray(),
            Path.Combine(applicationRoot, "Compatibility", "dinputto8")),
        _ => throw new InvalidOperationException($"Unsupported product: {product.ProductId}"),
    };

    private static HashSet<string> GetReadyProductIds(string root) => new InstallStatusReader(root).Read()
        .Where(item => item.State == ProductInstallState.Ready).Select(item => item.Product.Id)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static bool IsInstalled(InstallDestinationProduct product)
    {
        var verification = new InstallManifestVerifier().Verify(product.DestinationPath, InstallVerificationScope.OwnedFiles);
        return verification.IsValid && product.IsSatisfiedBy(verification.Manifest);
    }

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
