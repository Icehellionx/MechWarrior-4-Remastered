using MW4Remastered.Core.Media;

namespace MW4Remastered.Core.Install;

public sealed class BlackKnightInstallPlanBuilder
{
    private static readonly IReadOnlyDictionary<string, string> RootFiles =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["AUTOCO_1.EXE"] = "AutoConfigx.exe",
            ["EULAX.RTF"] = "EulaX.rtf",
            ["MECH4X.ICO"] = "Mech4X.ico",
            ["MISSIO_1.DLL"] = "MissionLangx.dll",
            ["NFXEDI_1.EXE"] = "NFXEditor.exe",
            ["READMEX.RTF"] = "ReadmeX.rtf",
            ["SCRIPT_1.DLL"] = "ScriptStringsx.dll",
            ["SERVER_1.TXT"] = "servercyclex.txt",
            ["WARRANTY.RTF"] = "Warranty.rtf",
        };

    private static readonly IReadOnlySet<string> ExcludedMw4XFiles =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SECDRV.SYS",
            "EBUEULA.DLL",
        };

    private readonly MediaInspectionService inspection;
    private readonly DirectoryMediaInventory inventory;

    public BlackKnightInstallPlanBuilder()
        : this(new MediaInspectionService(), new DirectoryMediaInventory())
    {
    }

    public BlackKnightInstallPlanBuilder(
        MediaInspectionService inspection,
        DirectoryMediaInventory inventory)
    {
        this.inspection = inspection ?? throw new ArgumentNullException(nameof(inspection));
        this.inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
    }

    public IReadOnlyList<InstallFile> BuildOverlay(string discRoot, string preparedEulaPath)
    {
        RequireLayout(discRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(preparedEulaPath);

        var expansionFiles = new List<InstallFile>();

        foreach (var path in inventory.Read(discRoot))
        {
            if (RootFiles.TryGetValue(path, out var destination))
            {
                expansionFiles.Add(new InstallFile(discRoot, path, destination));
            }
            else if (path.StartsWith("CONTENT/", StringComparison.OrdinalIgnoreCase) ||
                     path.StartsWith("FONTS/", StringComparison.OrdinalIgnoreCase) ||
                     path.StartsWith("RESOURCE/", StringComparison.OrdinalIgnoreCase))
            {
                expansionFiles.Add(new InstallFile(discRoot, path, BlackKnightMediaPathMap.Map(path)));
            }
            else if (path.StartsWith("MW4X/", StringComparison.OrdinalIgnoreCase))
            {
                var fileName = path["MW4X/".Length..];
                if (!fileName.Contains('/') && !ExcludedMw4XFiles.Contains(fileName))
                {
                    expansionFiles.Add(new InstallFile(discRoot, path, "MW4X/" + fileName));
                }
            }
        }

        var preparedEula = Path.GetFullPath(preparedEulaPath);
        expansionFiles.Add(new InstallFile(Path.GetDirectoryName(preparedEula)!, Path.GetFileName(preparedEula), "MW4X/EBUEula.dll"));
        return expansionFiles;
    }

    private void RequireLayout(string root)
    {
        var recognition = inspection.InspectDirectory(root);
        if (recognition.Layout?.Id != "black-knight-disc-1" ||
            recognition.Status is not (MediaRecognitionStatus.Recognized or MediaRecognitionStatus.RecognizedWithExcludedContent))
        {
            throw new InvalidDataException($"Expected black-knight-disc-1, but media recognition returned {recognition.Status} ({recognition.Layout?.Id ?? "unknown"}).");
        }
    }

}
