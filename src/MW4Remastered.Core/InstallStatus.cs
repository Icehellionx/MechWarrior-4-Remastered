using MW4Remastered.Core.Install;

namespace MW4Remastered.Core;

public enum ProductInstallState
{
    Missing,
    Ready,
    NeedsRepair,
}

public sealed record ProductStatus(
    ProductDefinition Product,
    ProductInstallState State,
    string? LaunchPath,
    string? CompatibilityLaunchPath,
    string? ManualPath,
    string? InstallPath,
    string? Detail)
{
    public bool IsInstalled => State == ProductInstallState.Ready;
}

public sealed class InstallStatusReader
{
    private readonly string installationRoot;
    private readonly InstallManifestVerifier verifier;

    public InstallStatusReader(string installationRoot)
        : this(installationRoot, new InstallManifestVerifier())
    {
    }

    public InstallStatusReader(string installationRoot, InstallManifestVerifier verifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(installationRoot);
        this.installationRoot = Path.GetFullPath(installationRoot);
        this.verifier = verifier ?? throw new ArgumentNullException(nameof(verifier));
    }

    public IReadOnlyList<ProductStatus> Read()
    {
        return ProductCatalog.All.Select(ReadProduct).ToArray();
    }

    private ProductStatus ReadProduct(ProductDefinition product)
    {
        var manualPath = FindManual(product);
        if (product.Kind == ProductKind.OptionalPack)
        {
            var vengeanceRoot = Path.Combine(installationRoot, "vengeance");
            var packVerification = verifier.Verify(vengeanceRoot, InstallVerificationScope.OwnedFiles);
            var installed = packVerification.IsValid && packVerification.Manifest is not null &&
                packVerification.Manifest.ProductId.Equals("vengeance", StringComparison.OrdinalIgnoreCase) &&
                (packVerification.Manifest.HasComponent(product.Id) || MechPakInstalledEvidence.IsPresent(packVerification.Manifest, product.Id));
            return installed
                ? new ProductStatus(product, ProductInstallState.Ready, null, null, manualPath, vengeanceRoot, "Verified pack payload")
                : new ProductStatus(product, ProductInstallState.Missing, null, null, manualPath, null, "Pack payload not installed");
        }

        var physicalProductId = product.Id.Equals("black-knight", StringComparison.OrdinalIgnoreCase)
            ? "vengeance"
            : product.Id;
        var productRoot = Path.Combine(installationRoot, physicalProductId);
        if (!Directory.Exists(productRoot))
        {
            return new ProductStatus(product, ProductInstallState.Missing, null, null, manualPath, null, "Game files not found");
        }
        var executable = product.ExecutableCandidates
            .Select(name => Path.Combine(productRoot,
                name.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar)))
            .FirstOrDefault(File.Exists);

        var verification = verifier.Verify(productRoot, InstallVerificationScope.OwnedFiles);
        var ownsComponent = verification.Manifest is not null && verification.Manifest.HasComponent(product.Id);
        if (executable is null || !verification.IsValid ||
            !string.Equals(verification.Manifest?.ProductId, physicalProductId, StringComparison.OrdinalIgnoreCase) ||
            !ownsComponent)
        {
            var detail = executable is null ? "Game executable is missing"
                : verification.Issues.FirstOrDefault() ?? "Ownership manifest identifies a different product";
            return new ProductStatus(product, ProductInstallState.NeedsRepair, null, null, manualPath, productRoot, detail);
        }

        // Runtime helpers are intentionally unsupported. Setup prepares every
        // compatibility artifact up front and normal launch executes the game directly.
        return new ProductStatus(product, ProductInstallState.Ready, executable, null, manualPath, productRoot, "Verified installation");
    }

    private string? FindManual(ProductDefinition product)
    {
        if (product.ManualFileName is null) return null;
        var candidates = new[]
        {
            Path.Combine(installationRoot, "Manuals", product.ManualFileName),
            Path.Combine(installationRoot, product.Id, "Manuals", product.ManualFileName),
        };
        return candidates.FirstOrDefault(File.Exists);
    }
}
