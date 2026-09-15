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
            new[] { "MW4X.exe" }, "MechWarrior 4 Black Knight Manual.pdf"),
        new ProductDefinition("mercenaries", "Mercenaries", ProductKind.Game,
            new[] { "MW4Mercs.exe" }, "MechWarrior 4 Mercenaries Manual.pdf"),
        new ProductDefinition("inner-sphere", "Inner Sphere Mech Pak", ProductKind.OptionalPack,
            Array.Empty<string>(), null),
        new ProductDefinition("clan", "Clan Mech Pak", ProductKind.OptionalPack,
            Array.Empty<string>(), null),
    };
}
