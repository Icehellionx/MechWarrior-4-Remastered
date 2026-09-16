using MW4Remastered.Core.Media;

namespace MW4Remastered.Core.Install;

public sealed class BlackKnightInstallPlanBuilder
{
    public const string QualifiedLoaderSha256 = "18f13072cf42fbacb5b228864bdb4a67fb1dff8c7ca7362054525816f8a4565a";
    public const string QualifiedLaunchHelperSha256 = "2f15bf4b8f8ae5d20d74a6e7fa9dbee98cc52793e16a9f8e98e8333f20d7c25f";
    public const string QualifiedLoaderLicenseSha256 = "81cbae84a29ce7e770bf2bc7b178e50bda0ce8de6067aba661b0bc7b05b562f8";

    private static readonly IReadOnlyDictionary<string, string> RootFiles =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["AUTOCO_1.EXE"] = "AutoConfig.exe",
            ["EULAX.RTF"] = "EulaX.rtf",
            ["MECH4X.ICO"] = "Mech4X.ico",
            ["MISSIO_1.DLL"] = "MissionLang.dll",
            ["NFXEDI_1.EXE"] = "NFXEditor.exe",
            ["READMEX.RTF"] = "ReadmeX.rtf",
            ["SCRIPT_1.DLL"] = "ScriptStrings.dll",
            ["SERVER_1.TXT"] = "servercycle.txt",
            ["WARRANTY.RTF"] = "Warranty.rtf",
        };

    private static readonly IReadOnlySet<string> ExcludedMw4XFiles =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "DSETUP.DLL",
            "SECDRV.SYS",
        };

    private readonly QualifiedCompatibilityPayload compatibility;
    private readonly MediaInspectionService inspection;
    private readonly DirectoryMediaInventory inventory;

    public BlackKnightInstallPlanBuilder()
        : this(CreatePackagedPayload(), new MediaInspectionService(), new DirectoryMediaInventory())
    {
    }

    public BlackKnightInstallPlanBuilder(
        QualifiedCompatibilityPayload compatibility,
        MediaInspectionService inspection,
        DirectoryMediaInventory inventory)
    {
        this.compatibility = compatibility ?? throw new ArgumentNullException(nameof(compatibility));
        this.inspection = inspection ?? throw new ArgumentNullException(nameof(inspection));
        this.inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
    }

    public InstallPlan Build(string discRoot)
    {
        RequireLayout(discRoot);
        var files = new List<InstallFile>();

        foreach (var path in inventory.Read(discRoot))
        {
            if (RootFiles.TryGetValue(path, out var destination))
            {
                files.Add(new InstallFile(discRoot, path, destination));
            }
            else if (path.StartsWith("CONTENT/", StringComparison.OrdinalIgnoreCase) ||
                     path.StartsWith("FONTS/", StringComparison.OrdinalIgnoreCase) ||
                     path.StartsWith("RESOURCE/", StringComparison.OrdinalIgnoreCase))
            {
                files.Add(new InstallFile(discRoot, path, MapDiscPath(path)));
            }
            else if (path.StartsWith("MW4X/", StringComparison.OrdinalIgnoreCase))
            {
                var fileName = path["MW4X/".Length..];
                if (!fileName.Contains('/') && !ExcludedMw4XFiles.Contains(fileName))
                {
                    files.Add(new InstallFile(discRoot, path, fileName));
                }
            }
        }

        files.AddRange(compatibility.ValidateAndCreateInstallFiles());
        return new InstallPlan("black-knight", files);
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

    private static QualifiedCompatibilityPayload CreatePackagedPayload()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "Compatibility", "BlackKnight");
        return new QualifiedCompatibilityPayload(root, new[]
        {
            new QualifiedCompatibilityFile("MW4RemasteredCompatLauncher.exe", "MW4RemasteredCompatLauncher.exe", QualifiedLaunchHelperSha256),
            new QualifiedCompatibilityFile("version.dll", "version.dll", QualifiedLoaderSha256),
            new QualifiedCompatibilityFile("SafeDiscLoader2-LICENSE.txt", "Licenses/SafeDiscLoader2-GPL-3.0.txt", QualifiedLoaderLicenseSha256),
        });
    }

    private static string MapDiscPath(string path)
    {
        if (path.StartsWith("CONTENT/SHELLS_1/", StringComparison.OrdinalIgnoreCase))
        {
            return "Content/ShellScripts/" + path["CONTENT/SHELLS_1/".Length..];
        }
        if (path.StartsWith("CONTENT/TEXTURES/CUSTOM_1/", StringComparison.OrdinalIgnoreCase))
        {
            return "Content/Textures/customdecals/" + path["CONTENT/TEXTURES/CUSTOM_1/".Length..];
        }
        return path;
    }
}
