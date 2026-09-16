namespace MW4Remastered.Core.Install;

// Original Black Knight media stores long installed names in its setup table.
// This reviewed map reproduces those names without executing the legacy installer.
public static class BlackKnightMediaPathMap
{
    private static readonly IReadOnlyDictionary<string, string> Destinations =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["CONTENT/GAMETY_1.H"] = "Content/GameTypesX.h",
            ["CONTENT/MOVIES/CLOSEX_1.MPG"] = "Content/Movies/Close x.mpg",
            ["CONTENT/MOVIES/GAMEOP_1.MPG"] = "Content/Movies/GameOpenx.mpg",
            ["CONTENT/SHELLS_1/FILES/STUTTE_1.WAV"] = "Content/ShellScriptsX/Files/StutterShark_music.wav",
            ["RESOURCE/MISSIONS/CANTIN_1.MW4"] = "Resource/Missions/cantinasiege.mw4",
            ["RESOURCE/MISSIONS/CANTIN_1.NFX"] = "Resource/Missions/CantinaSiege.nfx",
            ["RESOURCE/MISSIONS/CANTIN_1.TGA"] = "Resource/Missions/CantinaSiege.tga",
            ["RESOURCE/MISSIONS/CANYON_1.MW4"] = "Resource/Missions/canyonhold.mw4",
            ["RESOURCE/MISSIONS/CANYON_1.NFX"] = "Resource/Missions/CanyonHold.nfx",
            ["RESOURCE/MISSIONS/CANYON_1.TGA"] = "Resource/Missions/CanyonHold.tga",
            ["RESOURCE/MISSIONS/EDITOR_1.MW4"] = "Resource/Missions/editorxtemplate.mw4",
            ["RESOURCE/MISSIONS/GHOSTH_1.MW4"] = "Resource/Missions/ghosthighway.mw4",
            ["RESOURCE/MISSIONS/GHOSTH_1.NFX"] = "Resource/Missions/GhostHighway.nfx",
            ["RESOURCE/MISSIONS/GHOSTH_1.TGA"] = "Resource/Missions/GhostHighway.tga",
            ["RESOURCE/MISSIONS/LAKESI_1.MW4"] = "Resource/Missions/lakesidehold.mw4",
            ["RESOURCE/MISSIONS/LAKESI_1.NFX"] = "Resource/Missions/LakesideHold.nfx",
            ["RESOURCE/MISSIONS/LAKESI_1.TGA"] = "Resource/Missions/lakesidehold.tga",
            ["RESOURCE/MISSIONS/MECHWO_1.MW4"] = "Resource/Missions/mechworks.mw4",
            ["RESOURCE/MISSIONS/MECHWO_1.NFX"] = "Resource/Missions/MechWorks.nfx",
            ["RESOURCE/MISSIONS/MECHWO_1.TGA"] = "Resource/Missions/MechWorks.tga",
            ["RESOURCE/MISSIONS/MINERL_1.MW4"] = "Resource/Missions/minerl01_miners.mw4",
            ["RESOURCE/MISSIONS/MINERL_2.MW4"] = "Resource/Missions/minerl02_garrote.mw4",
            ["RESOURCE/MISSIONS/MINERL_3.MW4"] = "Resource/Missions/minerl03_safeguard.mw4",
            ["RESOURCE/MISSIONS/MINERL_4.MW4"] = "Resource/Missions/minerl04_seekanddestroy.mw4",
            ["RESOURCE/MISSIONS/MOUNTN_1.MW4"] = "Resource/Missions/mountn01_betrayal.mw4",
            ["RESOURCE/MISSIONS/MOUNTN_2.MW4"] = "Resource/Missions/mountn02_depot.mw4",
            ["RESOURCE/MISSIONS/MOUNTN_3.MW4"] = "Resource/Missions/mountn03_prison.mw4",
            ["RESOURCE/MISSIONS/MOUNTN_4.MW4"] = "Resource/Missions/mountn05_assault.mw4",
            ["RESOURCE/MISSIONS/RUBBLE_1.MW4"] = "Resource/Missions/rubblegiant.mw4",
            ["RESOURCE/MISSIONS/RUBBLE_1.NFX"] = "Resource/Missions/RubbleGiant.nfx",
            ["RESOURCE/MISSIONS/RUBBLE_1.TGA"] = "Resource/Missions/RubbleGiant.tga",
            ["RESOURCE/MISSIONS/RUIN01_1.MW4"] = "Resource/Missions/ruin01_stagingground.mw4",
            ["RESOURCE/MISSIONS/RUIN02_1.MW4"] = "Resource/Missions/ruin02_checkpointcheckout.mw4",
            ["RESOURCE/MISSIONS/RUIN03_1.MW4"] = "Resource/Missions/ruin03_lionsmouth.mw4",
            ["RESOURCE/MISSIONS/RUIN04_1.MW4"] = "Resource/Missions/ruin04_revenge.mw4",
            ["RESOURCE/MISSIONS/SC89E3_1.MW4"] = "Resource/Missions/scrub06_canyonambush.mw4",
            ["RESOURCE/MISSIONS/SCD90F_1.MW4"] = "Resource/Missions/scrub05_killcasey.mw4",
            ["RESOURCE/MISSIONS/SCRUB0_1.MW4"] = "Resource/Missions/scrub01_breakout.mw4",
            ["RESOURCE/MISSIONS/SCRUB0_2.MW4"] = "Resource/Missions/scrub02_modl.mw4",
            ["RESOURCE/MISSIONS/SCRUB0_3.MW4"] = "Resource/Missions/scrub03_clearskies.mw4",
            ["RESOURCE/MISSIONS/SCRUB0_4.MW4"] = "Resource/Missions/scrub04_mechheist.mw4",
            ["RESOURCE/MISSIONS/SPACEP_1.MW4"] = "Resource/Missions/spaceport.mw4",
            ["RESOURCE/MISSIONS/SPACEP_1.NFX"] = "Resource/Missions/Spaceport.nfx",
            ["RESOURCE/MISSIONS/SPACEP_1.TGA"] = "Resource/Missions/Spaceport.tga",
            ["RESOURCE/MISSIONS/SPACEP_2.MW4"] = "Resource/Missions/spaceportsiege.mw4",
            ["RESOURCE/MISSIONS/SPACEP_2.NFX"] = "Resource/Missions/SpaceportSiege.nfx",
            ["RESOURCE/MISSIONS/SPACEP_2.TGA"] = "Resource/Missions/SpaceportSiege.tga",
            ["RESOURCE/MISSIONS/STRONG_1.TXT"] = "Resource/Missions/strongholds.txt",
            ["RESOURCE/MISSIONS/VOLCAN_1.MW4"] = "Resource/Missions/volcan01_holdout.mw4",
            ["RESOURCE/MISSIONS/VOLCAN_2.MW4"] = "Resource/Missions/volcan02_gauntlet.mw4",
            ["RESOURCE/MISSIONS/VOLCAN_3.MW4"] = "Resource/Missions/volcan03_pillage.mw4",
            ["RESOURCE/MISSIONS/VOLCAN_4.MW4"] = "Resource/Missions/volcan04_moneyshot.mw4",
            ["RESOURCE/TEXTUR_1.MW4"] = "Resource/texturesx.mw4",
            ["RESOURCE/VARIAN_1/VARIAN_1.TXT"] = "Resource/Variantsx/variantsx.txt",
        };

    public static string Map(string sourceRelativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceRelativePath);
        return Destinations.TryGetValue(sourceRelativePath, out var destination)
            ? destination
            : sourceRelativePath;
    }
}
