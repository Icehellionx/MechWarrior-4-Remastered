using System.Security.Cryptography;
using MW4Remastered.Core.Media;

namespace MW4Remastered.Core.Install;

public sealed class BlackKnightInstallPlanBuilder
{
    public const string QualifiedCompatibilityExecutableSha256 = "2a5b7f2f408d3ae9b103e42fabac2aba26df9721b292ba0537a97a3784c1bb31";

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
            "DRVMGT.DLL",
            "DSETUP.DLL",
            "MW4X.EXE",
            "SECDRV.SYS",
        };

    private readonly string compatibilityExecutableSha256;
    private readonly MediaInspectionService inspection;
    private readonly DirectoryMediaInventory inventory;

    public BlackKnightInstallPlanBuilder()
        : this(QualifiedCompatibilityExecutableSha256, new MediaInspectionService(), new DirectoryMediaInventory())
    {
    }

    public BlackKnightInstallPlanBuilder(
        string compatibilityExecutableSha256,
        MediaInspectionService inspection,
        DirectoryMediaInventory inventory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(compatibilityExecutableSha256);
        if (compatibilityExecutableSha256.Length != 64 || !compatibilityExecutableSha256.All(Uri.IsHexDigit))
        {
            throw new ArgumentException("Expected compatibility executable hash must be a SHA-256 hex string.", nameof(compatibilityExecutableSha256));
        }

        this.compatibilityExecutableSha256 = compatibilityExecutableSha256.ToLowerInvariant();
        this.inspection = inspection ?? throw new ArgumentNullException(nameof(inspection));
        this.inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
    }

    public InstallPlan Build(string discRoot, string compatibilityExecutablePath)
    {
        RequireLayout(discRoot);
        var replacement = RequireQualifiedExecutable(compatibilityExecutablePath);
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

        files.Add(new InstallFile(Path.GetDirectoryName(replacement)!, Path.GetFileName(replacement), "MW4X.exe"));
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

    private string RequireQualifiedExecutable(string path)
    {
        var executable = Path.GetFullPath(path);
        if (!File.Exists(executable)) throw new FileNotFoundException("Black Knight compatibility executable does not exist.", executable);
        if ((File.GetAttributes(executable) & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidDataException("Black Knight compatibility executable cannot be a reparse point.");
        }

        using var stream = File.OpenRead(executable);
        var actualHash = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
        if (!string.Equals(actualHash, compatibilityExecutableSha256, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Unsupported Black Knight compatibility executable SHA-256: {actualHash}");
        }
        return executable;
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
