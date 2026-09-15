namespace MW4Remastered.Core.Media;

public static class MediaCatalog
{
    public static IReadOnlyList<MediaLayoutDefinition> Layouts { get; } = new[]
    {
        new MediaLayoutDefinition(
            "vengeance-disc-1",
            MediaProduct.Vengeance,
            1,
            "MechWarrior 4: Vengeance Disc 1",
            new[] { "MW4.EXE", "MW4.ICD", "RESOURCE/CORE.MW4", "RESOURCE/PROPS.MW4", "RESOURCE/TEXTURES.MW4" }),
        new MediaLayoutDefinition(
            "vengeance-disc-2",
            MediaProduct.Vengeance,
            2,
            "MechWarrior 4: Vengeance Disc 2",
            new[] { "AUTORUN2.EXE", "RESOURCE/MAPS/ALPINE01.MW4", "RESOURCE/MAPS/URBAN06.MW4", "RESOURCE/MISSIONS/0001.MW4" }),
        new MediaLayoutDefinition(
            "black-knight-disc-1",
            MediaProduct.BlackKnight,
            1,
            "MechWarrior 4: Black Knight",
            new[] { "MW4X/MW4X.EXE", "RESOURCE/COREX.MW4", "RESOURCE/PROPSX.MW4", "CONTENT/MOVIES/GAMEOP_1.MPG" }),
        new MediaLayoutDefinition(
            "mercenaries-disc-1",
            MediaProduct.Mercenaries,
            1,
            "MechWarrior 4: Mercenaries Disc 1",
            new[] { "MW4MERCS.EXE", "MW4MERCS.ICD", "MSGAME.CAB", "MW4/MW4.EXE" }),
        new MediaLayoutDefinition(
            "mercenaries-disc-2",
            MediaProduct.Mercenaries,
            2,
            "MechWarrior 4: Mercenaries Disc 2",
            new[] { "AUTORUN2.EXE", "CONTENT/MOVIES/GAMEOPEN.MPG", "RESOURCE/MAPS/ARCTIC05.MW4", "RESOURCE/MISSIONS/AVALON01.MW4" }),
        new MediaLayoutDefinition(
            "inner-sphere-mech-pak",
            MediaProduct.InnerSphereMechPak,
            1,
            "Inner Sphere Mech Pak",
            new[] { "MPISETUP.DLL", "RESOURCE/MAPS/COLSM01.MW4", "RESOURCE/MAPS/GAGE.MW4", "GOODIES/PATCH3/MW4P3/ENGLISH/MW4.RTP" }),
        new MediaLayoutDefinition(
            "clan-mech-pak",
            MediaProduct.ClanMechPak,
            1,
            "Clan Mech Pak",
            new[] { "MPCSETUP.DLL", "RESOURCE/MAPS/FACT01.MW4", "RESOURCE/MAPS/NGOTH.MW4", "GOODIES/PATCH3/MW4P3/ENGLISH/MW4.RTP" }),
    };

    public static IReadOnlySet<string> ForbiddenRootNames { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "CRACK",
        "NOCD",
        "RAZOR1911",
    };

    public static IReadOnlySet<string> ForbiddenFileNames { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "SECDRV.SYS",
        "CDAC14BA.DLL",
        "CDAC21BA.DLL",
        "SCSHD.CSA",
        "SCSHD.EXE",
    };
}
