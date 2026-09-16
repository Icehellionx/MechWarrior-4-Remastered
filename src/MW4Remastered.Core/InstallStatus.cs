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
            // Pack status remains missing until pack-specific payload evidence is defined.
            // A registry value or marker file alone must never claim installed content.
            return new ProductStatus(product, ProductInstallState.Missing, null, null, manualPath, null, "Pack payload not installed");
        }

        var productRoot = Path.Combine(installationRoot, product.Id);
        if (!Directory.Exists(productRoot))
        {
            return new ProductStatus(product, ProductInstallState.Missing, null, null, manualPath, null, "Game files not found");
        }
        var executable = product.ExecutableCandidates
            .Select(name => Path.Combine(productRoot, name))
            .FirstOrDefault(File.Exists);

        var verification = verifier.Verify(productRoot, InstallVerificationScope.OwnedFiles);
        if (executable is null || !verification.IsValid || !string.Equals(verification.Manifest?.ProductId, product.Id, StringComparison.OrdinalIgnoreCase))
        {
            var detail = executable is null ? "Game executable is missing"
                : verification.Issues.FirstOrDefault() ?? "Ownership manifest identifies a different product";
            return new ProductStatus(product, ProductInstallState.NeedsRepair, null, null, manualPath, productRoot, detail);
        }

        const string compatibilityRelativePath = "MW4RemasteredCompatLauncher.exe";
        var compatibilityLaunchPath = verification.Manifest!.Files.Any(file =>
            file.Path.Equals(compatibilityRelativePath, StringComparison.OrdinalIgnoreCase))
            ? Path.Combine(productRoot, compatibilityRelativePath)
            : null;
        return new ProductStatus(product, ProductInstallState.Ready, executable, compatibilityLaunchPath, manualPath, productRoot, "Verified installation");
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
