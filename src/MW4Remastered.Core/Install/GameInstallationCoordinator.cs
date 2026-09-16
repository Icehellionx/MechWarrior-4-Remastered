namespace MW4Remastered.Core.Install;

public abstract record GameInstallRequest(string ProductId);

public sealed record VengeanceInstallRequest(string DiscOneRoot, string DiscTwoRoot, string CompatibilityExecutablePath)
    : GameInstallRequest("vengeance");

public sealed record BlackKnightInstallRequest(string DiscRoot)
    : GameInstallRequest("black-knight");

public sealed record MercenariesInstallRequest(string DiscOneRoot, string DiscTwoRoot, string CompatibilityExecutablePath)
    : GameInstallRequest("mercenaries");

public enum GameInstallationStage
{
    Validating,
    Extracting,
    Planning,
    Committing,
    Verifying,
    Completed,
}

public sealed record GameInstallationProgress(GameInstallationStage Stage, string ProductId, string Message);

public sealed record GameInstallationResult(string ProductId, string DestinationPath, InstallManifest Manifest);

public interface IGameInstallPlanFactory
{
    InstallPlan Build(GameInstallRequest request, string? mercenariesCabinetPayloadRoot = null);
}

public sealed class GameInstallPlanFactory : IGameInstallPlanFactory
{
    private readonly VengeanceInstallPlanBuilder vengeance;
    private readonly BlackKnightInstallPlanBuilder blackKnight;
    private readonly MercenariesInstallPlanBuilder mercenaries;

    public GameInstallPlanFactory()
        : this(new VengeanceInstallPlanBuilder(), new BlackKnightInstallPlanBuilder(), new MercenariesInstallPlanBuilder())
    {
    }

    public GameInstallPlanFactory(
        VengeanceInstallPlanBuilder vengeance,
        BlackKnightInstallPlanBuilder blackKnight,
        MercenariesInstallPlanBuilder mercenaries)
    {
        this.vengeance = vengeance ?? throw new ArgumentNullException(nameof(vengeance));
        this.blackKnight = blackKnight ?? throw new ArgumentNullException(nameof(blackKnight));
        this.mercenaries = mercenaries ?? throw new ArgumentNullException(nameof(mercenaries));
    }

    public InstallPlan Build(GameInstallRequest request, string? mercenariesCabinetPayloadRoot = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        return request switch
        {
            VengeanceInstallRequest input => vengeance.Build(input.DiscOneRoot, input.DiscTwoRoot, input.CompatibilityExecutablePath),
            BlackKnightInstallRequest input => blackKnight.Build(input.DiscRoot),
            MercenariesInstallRequest input when !string.IsNullOrWhiteSpace(mercenariesCabinetPayloadRoot) =>
                mercenaries.Build(input.DiscOneRoot, input.DiscTwoRoot, mercenariesCabinetPayloadRoot, input.CompatibilityExecutablePath),
            MercenariesInstallRequest => throw new ArgumentException("Mercenaries installation requires an extracted cabinet payload.", nameof(mercenariesCabinetPayloadRoot)),
            _ => throw new ArgumentException($"Unsupported game install request: {request.GetType().Name}", nameof(request)),
        };
    }
}

public sealed class GameInstallationCoordinator
{
    private readonly IGameInstallPlanFactory plans;
    private readonly CabinetPayloadExtractor cabinetExtractor;
    private readonly StagedInstallTransaction transaction;
    private readonly InstallManifestVerifier verifier;

    public GameInstallationCoordinator()
        : this(new GameInstallPlanFactory(), new CabinetPayloadExtractor(), new StagedInstallTransaction(), new InstallManifestVerifier())
    {
    }

    public GameInstallationCoordinator(
        IGameInstallPlanFactory plans,
        CabinetPayloadExtractor cabinetExtractor,
        StagedInstallTransaction transaction,
        InstallManifestVerifier verifier)
    {
        this.plans = plans ?? throw new ArgumentNullException(nameof(plans));
        this.cabinetExtractor = cabinetExtractor ?? throw new ArgumentNullException(nameof(cabinetExtractor));
        this.transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
        this.verifier = verifier ?? throw new ArgumentNullException(nameof(verifier));
    }

    public GameInstallationResult Install(
        GameInstallRequest request,
        string destinationPath,
        IProgress<GameInstallationProgress>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        var destination = Path.GetFullPath(destinationPath);
        Report(GameInstallationStage.Validating, "Validating selected media and destination.");

        string? cabinetPayload = null;
        try
        {
            if (request is MercenariesInstallRequest mercenaries)
            {
                Report(GameInstallationStage.Extracting, "Extracting the validated Mercenaries cabinet payload.");
                var parent = Directory.GetParent(destination)?.FullName
                    ?? throw new InvalidDataException("Install destination must have a parent directory.");
                cabinetPayload = Path.Combine(parent, $".mercenaries-cabinet-{Guid.NewGuid():N}");
                var cabinet = Path.Combine(Path.GetFullPath(mercenaries.DiscOneRoot), "MSGAME.CAB");
                cabinetExtractor.ExtractGamePayload(cabinet, cabinetPayload);
            }

            Report(GameInstallationStage.Planning, "Building the exact game payload plan.");
            var plan = plans.Build(request, cabinetPayload);
            if (!string.Equals(plan.ProductId, request.ProductId, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"Install plan product '{plan.ProductId}' does not match request '{request.ProductId}'.");
            }

            Report(GameInstallationStage.Committing, "Staging and atomically committing owned files.");
            var manifest = transaction.Execute(plan, destination);
            Report(GameInstallationStage.Verifying, "Verifying every committed owned file.");
            var verification = verifier.Verify(destination, InstallVerificationScope.ExactTree);
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
        }

        void Report(GameInstallationStage stage, string message) =>
            progress?.Report(new GameInstallationProgress(stage, request.ProductId, message));
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
