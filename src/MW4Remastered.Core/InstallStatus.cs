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
    string? ManualPath,
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
            // Pack status remains missing until pack-specific payload evidence is defined.
            // A registry value or marker file alone must never claim installed content.
            return new ProductStatus(product, ProductInstallState.Missing, null, manualPath, "Pack payload not installed");
        }

        var productRoot = Path.Combine(installationRoot, product.Id);
        var executable = product.ExecutableCandidates
            .Select(name => Path.Combine(productRoot, name))
            .FirstOrDefault(File.Exists);
        if (executable is null)
        {
            return new ProductStatus(product, ProductInstallState.Missing, null, manualPath, "Game files not found");
        }

        var verification = verifier.Verify(productRoot);
        if (!verification.IsValid || !string.Equals(verification.Manifest?.ProductId, product.Id, StringComparison.OrdinalIgnoreCase))
        {
            var detail = verification.Issues.FirstOrDefault() ?? "Ownership manifest identifies a different product";
            return new ProductStatus(product, ProductInstallState.NeedsRepair, null, manualPath, detail);
        }

        return new ProductStatus(product, ProductInstallState.Ready, executable, manualPath, "Verified installation");
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
