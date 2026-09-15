namespace MW4Remastered.Core;

public sealed record ProductStatus(ProductDefinition Product, bool IsInstalled, string? LaunchPath);

public sealed class InstallStatusReader
{
    private readonly string installationRoot;

    public InstallStatusReader(string installationRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(installationRoot);
        this.installationRoot = Path.GetFullPath(installationRoot);
    }

    public IReadOnlyList<ProductStatus> Read()
    {
        return ProductCatalog.All.Select(ReadProduct).ToArray();
    }

    private ProductStatus ReadProduct(ProductDefinition product)
    {
        if (product.Kind == ProductKind.OptionalPack)
        {
            // Pack status remains false until the installer owns a manifest verifier.
            // A registry value or marker file alone must never claim installed content.
            return new ProductStatus(product, false, null);
        }

        foreach (var executable in product.ExecutableCandidates)
        {
            var candidate = Path.Combine(installationRoot, product.Id, executable);
            if (File.Exists(candidate))
            {
                return new ProductStatus(product, true, candidate);
            }
        }

        return new ProductStatus(product, false, null);
    }
}
