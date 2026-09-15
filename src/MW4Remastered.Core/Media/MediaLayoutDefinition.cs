namespace MW4Remastered.Core.Media;

public enum MediaProduct
{
    Vengeance,
    BlackKnight,
    Mercenaries,
    InnerSphereMechPak,
    ClanMechPak
}

public sealed record MediaLayoutDefinition(
    string Id,
    MediaProduct Product,
    int DiscNumber,
    string DisplayName,
    IReadOnlyList<string> RequiredPaths);
