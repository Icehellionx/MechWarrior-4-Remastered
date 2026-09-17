namespace MW4Remastered.Core.Install;

// Original Vengeance media stores several installed long names only in the
// InstallShield setup table. Copying the ISO tree literally leaves scripts
// asking for files that do not exist (for example Burnloop_lr_15.avi).
public static class VengeanceMediaPathMap
{
    private static readonly IReadOnlyDictionary<string, string> Destinations =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["CONTENT/MOVIES/BURNLO_1.AVI"] = "Content/Movies/Burnloop_lr_15.avi",
            ["CONTENT/MOVIES/CLOSEA_1.MPG"] = "Content/Movies/Close a.mpg",
            ["CONTENT/MOVIES/CLOSEB_1.MPG"] = "Content/Movies/Close b.mpg",
            ["CONTENT/MOVIES/CLOSIN_1.MPG"] = "Content/Movies/Closinga1.mpg",
            ["CONTENT/SHELLS_1/FILES/STUTTE_1.WAV"] = "Content/ShellScripts/Files/StutterShark_music.wav",
            ["CONTENT/TEXTURES/CUSTOM_1/CSTMDCAL.TXT"] = "Content/Textures/customdecals/CSTMDCAL.TXT",
            ["RESOURCE/MISSIONS/CENTRA_1.TGA"] = "Resource/Missions/centralpark.tga",
            ["RESOURCE/MISSIONS/EDITOR_1.MW4"] = "Resource/Missions/editortemplate.mw4",
            ["RESOURCE/MISSIONS/FROSTB_1.TGA"] = "Resource/Missions/frostbite.tga",
            ["RESOURCE/MISSIONS/GATORB_1.TGA"] = "Resource/Missions/gatorbait.tga",
            ["RESOURCE/MISSIONS/INNERC_1.TGA"] = "Resource/Missions/innercity.tga",
            ["RESOURCE/MISSIONS/PALACE_1.TGA"] = "Resource/Missions/PalaceGates.tga",
            ["RESOURCE/MISSIONS/TIMBER_1.TGA"] = "Resource/Missions/timberline.tga",
        };

    public static string Map(string sourceRelativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceRelativePath);
        if (Destinations.TryGetValue(sourceRelativePath, out var destination)) return destination;
        if (sourceRelativePath.StartsWith("CONTENT/SHELLS_1/", StringComparison.OrdinalIgnoreCase))
            return "Content/ShellScripts/" + sourceRelativePath["CONTENT/SHELLS_1/".Length..];
        if (sourceRelativePath.StartsWith("CONTENT/TEXTURES/CUSTOM_1/", StringComparison.OrdinalIgnoreCase))
            return "Content/Textures/customdecals/" + sourceRelativePath["CONTENT/TEXTURES/CUSTOM_1/".Length..];
        return sourceRelativePath;
    }

    public static OwnedPathMigrationPlan CreateOwnedMigration(string installRoot, InstallManifest manifest)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(installRoot);
        ArgumentNullException.ThrowIfNull(manifest);
        var root = Path.GetFullPath(installRoot);
        var owned = manifest.Files.Select(file => Normalize(file.Path)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var files = new List<InstallFile>();
        var retired = new List<string>();
        foreach (var entry in Destinations)
        {
            var source = Normalize(entry.Key);
            if (!owned.Contains(source)) continue;
            files.Add(new InstallFile(root, source, entry.Value));
            retired.Add(source);
        }
        return new OwnedPathMigrationPlan(files, retired);
    }

    private static string Normalize(string path) => path.Replace('\\', '/');
}

public sealed record OwnedPathMigrationPlan(
    IReadOnlyList<InstallFile> Files,
    IReadOnlyList<string> RetiredOwnedPaths);
