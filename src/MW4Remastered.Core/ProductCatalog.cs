namespace MW4Remastered.Core;

public enum ProductKind
{
    Game,
    OptionalPack
}

public sealed record ProductDefinition(
    string Id,
    string DisplayName,
    ProductKind Kind,
    string[] ExecutableCandidates,
    string? ManualFileName);

public static class ProductCatalog
{
    public static IReadOnlyList<ProductDefinition> All { get; } = new[]
    {
        new ProductDefinition("vengeance", "Vengeance", ProductKind.Game,
            new[] { "MW4.exe" }, "MechWarrior 4 Vengeance Manual.pdf"),
        new ProductDefinition("black-knight", "Black Knight", ProductKind.Game,
            new[] { "MW4X/MW4X.exe" }, "MechWarrior 4 Black Knight Manual.pdf"),
        new ProductDefinition("mercenaries", "Mercenaries", ProductKind.Game,
            new[] { "MW4Mercs.exe" }, "MechWarrior 4 Mercenaries Manual.pdf"),
        new ProductDefinition("inner-sphere", "Inner Sphere Mech Pak", ProductKind.OptionalPack,
            Array.Empty<string>(), null),
        new ProductDefinition("clan", "Clan Mech Pak", ProductKind.OptionalPack,
            Array.Empty<string>(), null),
    };
}

public static class ProductDependencies
{
    private static readonly IReadOnlyDictionary<string, string[]> RequiredBaseProducts =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["vengeance"] = Array.Empty<string>(),
            ["black-knight"] = new[] { "vengeance" },
            ["mercenaries"] = Array.Empty<string>(),
            ["inner-sphere"] = new[] { "vengeance" },
            ["clan"] = new[] { "vengeance" },
        };

    public static IReadOnlyList<string> GetRequiredBaseProducts(string productId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productId);
        return RequiredBaseProducts.TryGetValue(productId, out var required)
            ? required
            : throw new ArgumentOutOfRangeException(nameof(productId), productId, "Unknown product.");
    }

    public static bool AreSatisfied(string productId, IEnumerable<string> installedProductIds)
    {
        ArgumentNullException.ThrowIfNull(installedProductIds);
        var installed = installedProductIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return GetRequiredBaseProducts(productId).All(installed.Contains);
    }
}
