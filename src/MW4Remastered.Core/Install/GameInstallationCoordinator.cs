namespace MW4Remastered.Core.Install;

public abstract record GameInstallRequest(string ProductId);

public sealed record VengeanceInstallRequest(
    string DiscOneRoot,
    string DiscTwoRoot,
    IReadOnlyList<string>? MechPakRoots = null,
    string? BlackKnightDiscRoot = null,
    string? PresentationCompatibilityRoot = null)
    : GameInstallRequest("vengeance");

public sealed record MercenariesInstallRequest(
    string DiscOneRoot,
    string DiscTwoRoot,
    string? PointReleaseRoot = null,
    string? PresentationCompatibilityRoot = null)
    : GameInstallRequest("mercenaries");

public enum GameInstallationStage
{
    Validating,
    Extracting,
    Transforming,
    Planning,
    Committing,
    Verifying,
    Completed,
}

public sealed record GameInstallationProgress(GameInstallationStage Stage, string ProductId, string Message);

public sealed record GameInstallationResult(string ProductId, string DestinationPath, InstallManifest Manifest);

public sealed record PreparedInstallInputs(
    string? VengeanceExecutablePath = null,
    string? VengeancePatch3PayloadRoot = null,
    string? MercenariesCabinetPayloadRoot = null,
    string? MercenariesExecutablePath = null,
    string? BlackKnightEulaPath = null,
    string? MercenariesPr1PayloadRoot = null,
    IReadOnlyList<InstallFile>? BlackKnightCompatibilityFiles = null);

public interface IGameInstallPlanFactory
{
    InstallPlan Build(GameInstallRequest request, PreparedInstallInputs? preparedInputs = null);
}

public sealed class GameInstallPlanFactory : IGameInstallPlanFactory
{
    private readonly VengeanceInstallPlanBuilder vengeance;
    private readonly BlackKnightInstallPlanBuilder blackKnight;
    private readonly MercenariesInstallPlanBuilder mercenaries;
    private readonly MechPakResourceOverlayPlanBuilder mechPaks;

    public GameInstallPlanFactory()
        : this(new VengeanceInstallPlanBuilder(), new BlackKnightInstallPlanBuilder(), new MercenariesInstallPlanBuilder(),
            new MechPakResourceOverlayPlanBuilder())
    {
    }

    public GameInstallPlanFactory(
        VengeanceInstallPlanBuilder vengeance,
        BlackKnightInstallPlanBuilder blackKnight,
        MercenariesInstallPlanBuilder mercenaries,
        MechPakResourceOverlayPlanBuilder? mechPaks = null)
    {
        this.vengeance = vengeance ?? throw new ArgumentNullException(nameof(vengeance));
        this.blackKnight = blackKnight ?? throw new ArgumentNullException(nameof(blackKnight));
        this.mercenaries = mercenaries ?? throw new ArgumentNullException(nameof(mercenaries));
        this.mechPaks = mechPaks ?? new MechPakResourceOverlayPlanBuilder();
    }

    public InstallPlan Build(GameInstallRequest request, PreparedInstallInputs? preparedInputs = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        return request switch
        {
            VengeanceInstallRequest input when !string.IsNullOrWhiteSpace(preparedInputs?.VengeanceExecutablePath) =>
                BuildVengeance(input, preparedInputs),
            VengeanceInstallRequest => throw new ArgumentException("Vengeance installation requires an internally prepared executable.", nameof(preparedInputs)),
            MercenariesInstallRequest input when !string.IsNullOrWhiteSpace(preparedInputs?.MercenariesCabinetPayloadRoot) &&
                !string.IsNullOrWhiteSpace(preparedInputs.MercenariesExecutablePath) =>
                BuildMercenaries(input, preparedInputs),
            MercenariesInstallRequest => throw new ArgumentException(
                "Mercenaries installation requires an extracted cabinet payload and internally prepared executable.",
                nameof(preparedInputs)),
            _ => throw new ArgumentException($"Unsupported game install request: {request.GetType().Name}", nameof(request)),
        };
    }

    private InstallPlan BuildMercenaries(MercenariesInstallRequest input, PreparedInstallInputs preparedInputs)
    {
        var basePlan = mercenaries.Build(
            input.DiscOneRoot,
            input.DiscTwoRoot,
            preparedInputs.MercenariesCabinetPayloadRoot!,
            preparedInputs.MercenariesExecutablePath!);
        var files = basePlan.Files.ToList();
        if (!string.IsNullOrWhiteSpace(preparedInputs.MercenariesPr1PayloadRoot))
        {
            var patchFiles = OfficialMercenariesPr1Transform.CreateInstallFiles(preparedInputs.MercenariesPr1PayloadRoot);
            var destinations = patchFiles.Select(file => file.DestinationRelativePath).ToHashSet(StringComparer.OrdinalIgnoreCase);
            files = files.Where(file => !destinations.Contains(file.DestinationRelativePath)).ToList();
            files.AddRange(patchFiles);
        }
        if (!string.IsNullOrWhiteSpace(input.PresentationCompatibilityRoot))
        {
            files.AddRange(LegacyPresentationCompatibility.CreateMercenariesFiles(input.PresentationCompatibilityRoot));
        }
        return new InstallPlan(basePlan.ProductId, files, basePlan.Components);
    }

    private InstallPlan BuildVengeance(VengeanceInstallRequest input, PreparedInstallInputs preparedInputs)
    {
        var basePlan = vengeance.Build(input.DiscOneRoot, input.DiscTwoRoot, preparedInputs.VengeanceExecutablePath!);
        var packRoots = input.MechPakRoots?.Where(root => !string.IsNullOrWhiteSpace(root)).ToArray() ?? [];
        var files = basePlan.Files.ToList();
        if (packRoots.Length > 0 && string.IsNullOrWhiteSpace(preparedInputs.VengeancePatch3PayloadRoot))
        {
            throw new ArgumentException("Vengeance Mech Paks require an internally prepared Patch 3 payload.", nameof(preparedInputs));
        }
        if (packRoots.Length > 0)
        {
            var patchFiles = OfficialVengeancePatch3Transform.CreateInstallFiles(preparedInputs.VengeancePatch3PayloadRoot!);
            var patchDestinations = patchFiles.Select(file => file.DestinationRelativePath)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            files = files.Where(file => !patchDestinations.Contains(file.DestinationRelativePath)).ToList();
            files.AddRange(patchFiles);
        }

        var installedPacks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var packRoot in packRoots)
        {
            var overlay = mechPaks.Build(packRoot, "vengeance");
            if (!installedPacks.Add(overlay.PackProductId))
            {
                throw new InvalidDataException($"Mech Pak '{overlay.PackProductId}' was selected more than once.");
            }
            var collision = overlay.Files.FirstOrDefault(candidate => files.Any(existing =>
                existing.DestinationRelativePath.Equals(candidate.DestinationRelativePath, StringComparison.OrdinalIgnoreCase)));
            if (collision is not null)
            {
                throw new InvalidDataException($"Mech Pak payload collides with the patched Vengeance tree: {collision.DestinationRelativePath}");
            }
            files.AddRange(overlay.Files);
        }

        var components = new List<string> { "vengeance" };
        components.AddRange(installedPacks);
        if (!string.IsNullOrWhiteSpace(input.BlackKnightDiscRoot))
        {
            if (string.IsNullOrWhiteSpace(preparedInputs.BlackKnightEulaPath))
            {
                throw new ArgumentException("Black Knight requires an internally prepared EULA module.", nameof(preparedInputs));
            }
            var expansionFiles = blackKnight.BuildOverlay(input.BlackKnightDiscRoot, preparedInputs.BlackKnightEulaPath);
            var collision = expansionFiles.FirstOrDefault(candidate => files.Any(existing =>
                existing.DestinationRelativePath.Equals(candidate.DestinationRelativePath, StringComparison.OrdinalIgnoreCase)));
            if (collision is not null)
            {
                throw new InvalidDataException($"Black Knight payload collides with the Vengeance family tree: {collision.DestinationRelativePath}");
            }
            files.AddRange(expansionFiles);
            components.Add("black-knight");

            if (preparedInputs.BlackKnightCompatibilityFiles is null)
            {
                throw new ArgumentException("Black Knight requires an internally prepared compatibility payload.", nameof(preparedInputs));
            }
            var patchDestinations = preparedInputs.BlackKnightCompatibilityFiles
                .Select(file => file.DestinationRelativePath)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            files = files.Where(file => !patchDestinations.Contains(file.DestinationRelativePath)).ToList();
            files.AddRange(preparedInputs.BlackKnightCompatibilityFiles);
        }

        if (!string.IsNullOrWhiteSpace(input.PresentationCompatibilityRoot))
        {
            files.AddRange(LegacyPresentationCompatibility.CreateVengeanceFiles(input.PresentationCompatibilityRoot));
        }

        return new InstallPlan("vengeance", files, components);
    }
}

public sealed class GameInstallationCoordinator
{
    private readonly IGameInstallPlanFactory plans;
    private readonly CabinetPayloadExtractor cabinetExtractor;
    private readonly StagedInstallTransaction transaction;
    private readonly InstallManifestVerifier verifier;
    private readonly IVengeanceExecutableTransform vengeanceTransform;
    private readonly IMercenariesExecutableTransform mercenariesTransform;
    private readonly OfficialVengeancePatch3Transform vengeancePatch3Transform;
    private readonly VengeancePatch3RetailInputBuilder vengeancePatch3Inputs;
    private readonly OfficialMercenariesPr1Transform mercenariesPr1Transform;
    private readonly IBlackKnightEulaTransform blackKnightEulaTransform;
    private readonly IBlackKnightPr1Transform blackKnightPr1Transform;
    private readonly string patchHostPath;
    private readonly string blackKnightRuntimeDllPath;

    public GameInstallationCoordinator()
        : this(new GameInstallPlanFactory(), new CabinetPayloadExtractor(), new StagedInstallTransaction(), new InstallManifestVerifier(),
            new VengeanceRetailExecutableTransform(), new MercenariesRetailExecutableTransform(),
            new OfficialVengeancePatch3Transform(), new VengeancePatch3RetailInputBuilder(), new OfficialMercenariesPr1Transform())
    {
    }

    public GameInstallationCoordinator(
        IGameInstallPlanFactory plans,
        CabinetPayloadExtractor cabinetExtractor,
        StagedInstallTransaction transaction,
        InstallManifestVerifier verifier,
        IVengeanceExecutableTransform? vengeanceTransform = null,
        IMercenariesExecutableTransform? mercenariesTransform = null,
        OfficialVengeancePatch3Transform? vengeancePatch3Transform = null,
        VengeancePatch3RetailInputBuilder? vengeancePatch3Inputs = null,
        OfficialMercenariesPr1Transform? mercenariesPr1Transform = null,
        string? patchHostPath = null,
        IBlackKnightEulaTransform? blackKnightEulaTransform = null,
        IBlackKnightPr1Transform? blackKnightPr1Transform = null,
        string? blackKnightRuntimeDllPath = null)
    {
        this.plans = plans ?? throw new ArgumentNullException(nameof(plans));
        this.cabinetExtractor = cabinetExtractor ?? throw new ArgumentNullException(nameof(cabinetExtractor));
        this.transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
        this.verifier = verifier ?? throw new ArgumentNullException(nameof(verifier));
        this.vengeanceTransform = vengeanceTransform ?? new VengeanceRetailExecutableTransform();
        this.mercenariesTransform = mercenariesTransform ?? new MercenariesRetailExecutableTransform();
        this.vengeancePatch3Transform = vengeancePatch3Transform ?? new OfficialVengeancePatch3Transform();
        this.vengeancePatch3Inputs = vengeancePatch3Inputs ?? new VengeancePatch3RetailInputBuilder();
        this.mercenariesPr1Transform = mercenariesPr1Transform ?? new OfficialMercenariesPr1Transform();
        this.blackKnightEulaTransform = blackKnightEulaTransform ?? new BlackKnightEulaTransform();
        this.blackKnightPr1Transform = blackKnightPr1Transform ?? new OfficialBlackKnightPr1Transform();
        this.patchHostPath = Path.GetFullPath(patchHostPath ?? Path.Combine(AppContext.BaseDirectory, "MW4RemasteredRtpPatchHost.exe"));
        this.blackKnightRuntimeDllPath = Path.GetFullPath(blackKnightRuntimeDllPath ?? Path.Combine(AppContext.BaseDirectory, "BlackKnightRuntime.dll"));
    }

    public GameInstallationResult Install(
        GameInstallRequest request,
        string destinationPath,
        IProgress<GameInstallationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        cancellationToken.ThrowIfCancellationRequested();
        var destination = Path.GetFullPath(destinationPath);
        Report(GameInstallationStage.Validating, "Validating selected media and destination.");

        string? cabinetPayload = null;
        string? vengeanceTransformScratch = null;
        string? vengeanceExecutable = null;
        string? vengeancePatch3Scratch = null;
        string? vengeancePatch3Payload = null;
        string? vengeancePatch3OfficialPayload = null;
        string? mercenariesTransformScratch = null;
        string? mercenariesExecutable = null;
        string? mercenariesPr1Scratch = null;
        string? mercenariesPr1Payload = null;
        string? blackKnightTransformScratch = null;
        string? blackKnightEula = null;
        string? blackKnightPr1Scratch = null;
        IReadOnlyList<InstallFile>? blackKnightCompatibilityFiles = null;
        try
        {
            if (request is VengeanceInstallRequest vengeance)
            {
                Report(GameInstallationStage.Transforming, "Producing the Vengeance executable from validated original media.");
                var parent = Directory.GetParent(destination)?.FullName
                    ?? throw new InvalidDataException("Install destination must have a parent directory.");
                vengeanceTransformScratch = Path.Combine(parent, $".vengeance-transform-{Guid.NewGuid():N}");
                var prepared = vengeanceTransform.Transform(vengeance.DiscOneRoot, vengeanceTransformScratch, cancellationToken);
                vengeanceExecutable = ValidatePreparedVengeanceExecutable(prepared, vengeanceTransformScratch);

                var packRoots = vengeance.MechPakRoots?.Where(root => !string.IsNullOrWhiteSpace(root)).ToArray() ?? [];
                if (packRoots.Length > 0)
                {
                    var patchMediaRoot = packRoots.FirstOrDefault(vengeancePatch3Transform.IsQualifiedPatchMedia)
                        ?? throw new InvalidDataException(
                            "Selected Mech Pak media does not contain the qualified official Patch 3 payload. Add complete Inner Sphere media or another supported pack disc containing Patch 3.");
                    Report(GameInstallationStage.Transforming, "Applying official Vengeance Patch 3 in contained scratch for the selected Mech Pak payload.");
                    vengeancePatch3Scratch = Path.Combine(parent, $".vengeance-patch3-{Guid.NewGuid():N}");
                    var retailPlan = plans.Build(
                        vengeance with { MechPakRoots = null, BlackKnightDiscRoot = null },
                        new PreparedInstallInputs(VengeanceExecutablePath: vengeanceExecutable));
                    var retailInputs = vengeancePatch3Inputs.Build(
                        retailPlan,
                        vengeance.DiscOneRoot,
                        Path.Combine(vengeancePatch3Scratch, "retail"),
                        cancellationToken);
                    var patchResult = vengeancePatch3Transform.Transform(
                        retailInputs,
                        patchMediaRoot,
                        patchHostPath,
                        Path.Combine(vengeancePatch3Scratch, "transform"),
                        cancellationToken);
                    vengeancePatch3Payload = patchResult.PayloadRoot;
                    vengeancePatch3OfficialPayload = patchResult.OfficialPayloadRoot;
                }

                if (!string.IsNullOrWhiteSpace(vengeance.BlackKnightDiscRoot))
                {
                    if (packRoots.Length == 0 && blackKnightPr1Transform is OfficialBlackKnightPr1Transform)
                        throw new InvalidDataException(
                            "Black Knight currently requires qualified Mech Pak media containing the official Vengeance Patch 3 and Black Knight Point Release 1 payloads.");

                    Report(GameInstallationStage.Transforming, "Recording setup-time license acceptance for Black Knight.");
                    blackKnightTransformScratch = Path.Combine(parent, $".black-knight-transform-{Guid.NewGuid():N}");
                    blackKnightEula = blackKnightEulaTransform.Transform(vengeance.BlackKnightDiscRoot, blackKnightTransformScratch);

                    blackKnightPr1Scratch = Path.Combine(parent, $".black-knight-pr1-{Guid.NewGuid():N}");
                    if (packRoots.Length > 0)
                    {
                        Report(GameInstallationStage.Transforming, "Applying official Black Knight Point Release 1 and its app-local compatibility runtime.");
                        var aggregatePlan = plans.Build(vengeance, new PreparedInstallInputs(
                            vengeanceExecutable,
                            vengeancePatch3Payload,
                            BlackKnightEulaPath: blackKnightEula,
                            BlackKnightCompatibilityFiles: []));
                        var blackKnightPatchMedia = blackKnightPr1Transform is OfficialBlackKnightPr1Transform officialPr1
                            ? packRoots.FirstOrDefault(officialPr1.IsQualifiedPatchMedia)
                            : packRoots.First();
                        var pr1 = blackKnightPr1Transform.Transform(
                            aggregatePlan,
                            vengeancePatch3OfficialPayload ?? throw new InvalidDataException("Black Knight PR1 requires the official Vengeance Patch 3 intermediate payload."),
                            blackKnightPatchMedia
                                ?? throw new InvalidDataException("Selected Mech Pak media does not contain the qualified official Black Knight Point Release 1 payload."),
                            patchHostPath,
                            blackKnightRuntimeDllPath,
                            blackKnightPr1Scratch,
                            cancellationToken);
                        if (!pr1.TransformId.Equals(OfficialBlackKnightPr1Transform.TransformId, StringComparison.Ordinal) &&
                            blackKnightPr1Transform is OfficialBlackKnightPr1Transform)
                            throw new InvalidDataException("Black Knight PR1 transform returned an unexpected identity.");
                        blackKnightCompatibilityFiles = pr1.InstallFiles;
                    }
                    else
                    {
                        // Synthetic test transforms can exercise orchestration without proprietary patch media.
                        var aggregatePlan = plans.Build(vengeance, new PreparedInstallInputs(
                            vengeanceExecutable,
                            BlackKnightEulaPath: blackKnightEula,
                            BlackKnightCompatibilityFiles: []));
                        blackKnightCompatibilityFiles = blackKnightPr1Transform.Transform(
                            aggregatePlan, string.Empty, vengeance.BlackKnightDiscRoot,
                            patchHostPath, blackKnightRuntimeDllPath, blackKnightPr1Scratch,
                            cancellationToken).InstallFiles;
                    }
                }
            }

            if (request is MercenariesInstallRequest mercenaries)
            {
                var parent = Directory.GetParent(destination)?.FullName
                    ?? throw new InvalidDataException("Install destination must have a parent directory.");
                Report(GameInstallationStage.Transforming, "Producing the Mercenaries executable from validated original media.");
                mercenariesTransformScratch = Path.Combine(parent, $".mercenaries-transform-{Guid.NewGuid():N}");
                var prepared = mercenariesTransform.Transform(
                    mercenaries.DiscOneRoot,
                    mercenariesTransformScratch,
                    cancellationToken);
                mercenariesExecutable = ValidatePreparedExecutable(
                    prepared.ExecutablePath,
                    prepared.TransformId,
                    mercenariesTransformScratch,
                    "Mercenaries");

                Report(GameInstallationStage.Extracting, "Extracting the validated Mercenaries cabinet payload.");
                cabinetPayload = Path.Combine(parent, $".mercenaries-cabinet-{Guid.NewGuid():N}");
                var cabinet = Path.Combine(Path.GetFullPath(mercenaries.DiscOneRoot), "MSGAME.CAB");
                cabinetExtractor.ExtractGamePayload(cabinet, cabinetPayload, cancellationToken);

                if (!string.IsNullOrWhiteSpace(mercenaries.PointReleaseRoot))
                {
                    Report(GameInstallationStage.Transforming, "Applying official Mercenaries Point Release 1 in contained scratch.");
                    mercenariesPr1Scratch = Path.Combine(parent, $".mercenaries-pr1-{Guid.NewGuid():N}");
                    var retailPlan = plans.Build(
                        mercenaries with { PointReleaseRoot = null },
                        new PreparedInstallInputs(
                            MercenariesCabinetPayloadRoot: cabinetPayload,
                            MercenariesExecutablePath: mercenariesExecutable));
                    var patchResult = mercenariesPr1Transform.Transform(
                        retailPlan,
                        mercenaries.DiscOneRoot,
                        mercenaries.PointReleaseRoot,
                        patchHostPath,
                        mercenariesPr1Scratch,
                        cancellationToken);
                    mercenariesPr1Payload = patchResult.PayloadRoot;
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            Report(GameInstallationStage.Planning, "Building the exact game payload plan.");
            var plan = plans.Build(request, new PreparedInstallInputs(
                vengeanceExecutable,
                vengeancePatch3Payload,
                cabinetPayload,
                mercenariesExecutable,
                blackKnightEula,
                mercenariesPr1Payload,
                blackKnightCompatibilityFiles));
            if (!string.Equals(plan.ProductId, request.ProductId, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"Install plan product '{plan.ProductId}' does not match request '{request.ProductId}'.");
            }

            Report(GameInstallationStage.Committing, "Staging and atomically committing owned files.");
            var preservingExistingFiles = Directory.Exists(destination);
            var manifest = transaction.Execute(plan, destination, cancellationToken);
            Report(GameInstallationStage.Verifying, "Verifying every committed owned file.");
            var verification = verifier.Verify(destination, preservingExistingFiles
                ? InstallVerificationScope.OwnedFiles
                : InstallVerificationScope.ExactTree);
            if (!verification.IsValid)
            {
                throw new InvalidDataException("Committed installation failed verification: " + string.Join("; ", verification.Issues));
            }

            Report(GameInstallationStage.Completed, "Installation completed and verified.");
            return new GameInstallationResult(request.ProductId, destination, manifest);
        }
        finally
        {
            if (cabinetPayload is not null) RemoveScratchTree(cabinetPayload);
            if (mercenariesTransformScratch is not null) RemoveScratchTree(mercenariesTransformScratch);
            if (mercenariesPr1Scratch is not null) RemoveScratchTree(mercenariesPr1Scratch);
            if (vengeancePatch3Scratch is not null) RemoveScratchTree(vengeancePatch3Scratch);
            if (vengeanceTransformScratch is not null) RemoveScratchTree(vengeanceTransformScratch);
            if (blackKnightTransformScratch is not null) RemoveScratchTree(blackKnightTransformScratch);
            if (blackKnightPr1Scratch is not null) RemoveScratchTree(blackKnightPr1Scratch);
        }

        void Report(GameInstallationStage stage, string message) =>
            progress?.Report(new GameInstallationProgress(stage, request.ProductId, message));
    }

    private static string ValidatePreparedVengeanceExecutable(PreparedVengeanceExecutable prepared, string scratchDirectory)
    {
        ArgumentNullException.ThrowIfNull(prepared);
        ArgumentException.ThrowIfNullOrWhiteSpace(prepared.TransformId);
        var scratch = Path.GetFullPath(scratchDirectory);
        var executable = Path.GetFullPath(prepared.ExecutablePath);
        var relative = Path.GetRelativePath(scratch, executable);
        if (Path.IsPathRooted(relative) || relative.Equals("..", StringComparison.Ordinal) ||
            relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new InvalidDataException("The Vengeance transform returned an executable outside its owned scratch directory.");
        }
        if (!File.Exists(executable))
        {
            throw new FileNotFoundException("The Vengeance transform did not produce an executable.", executable);
        }
        if ((File.GetAttributes(executable) & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidDataException("The prepared Vengeance executable cannot be a reparse point.");
        }
        return executable;
    }

    private static string ValidatePreparedExecutable(
        string executablePath,
        string transformId,
        string scratchDirectory,
        string productName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(transformId);
        var scratch = Path.GetFullPath(scratchDirectory);
        var executable = Path.GetFullPath(executablePath);
        var relative = Path.GetRelativePath(scratch, executable);
        if (Path.IsPathRooted(relative) || relative.Equals("..", StringComparison.Ordinal) ||
            relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"The {productName} transform returned an executable outside its owned scratch directory.");
        }
        if (!File.Exists(executable))
        {
            throw new FileNotFoundException($"The {productName} transform did not produce an executable.", executable);
        }
        if ((File.GetAttributes(executable) & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidDataException($"The prepared {productName} executable cannot be a reparse point.");
        }
        return executable;
    }

    private static void RemoveScratchTree(string path)
    {
        if (!Directory.Exists(path)) return;
        foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }
        Directory.Delete(path, recursive: true);
    }
}
