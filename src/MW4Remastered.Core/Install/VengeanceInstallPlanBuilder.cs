using System.Security.Cryptography;
using MW4Remastered.Core.Media;

namespace MW4Remastered.Core.Install;

public sealed class VengeanceInstallPlanBuilder
{
    public const string QualifiedCompatibilityExecutableSha256 = VengeanceRetailExecutableTransform.OutputSha256;

    private static readonly IReadOnlyDictionary<string, string> DiscOneRootFiles =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["AUTOCO_1.EXE"] = "AutoConfig.exe",
            ["BLADE.DLL"] = "Blade.dll",
            ["DSETUP.DLL"] = "DSetup.dll",
            ["EBUEULA.DLL"] = "EBUEula.dll",
            ["EULA.RTF"] = "EULA.rtf",
            ["LANGUAGE.DLL"] = "Language.dll",
            ["MCP.DLL"] = "MCP.dll",
            ["MECH4.ICO"] = "Mech4.ico",
            ["MFC42.DLL"] = "MFC42.dll",
            ["MISSIO_1.DLL"] = "MissionLang.dll",
            ["MOTD.TXT"] = "MOTD.txt",
            ["MSVCIRT.DLL"] = "MSVCIRT.dll",
            ["MSVCRT.DLL"] = "MSVCRT.dll",
            ["README.RTF"] = "Readme.rtf",
            ["SCRIPT_1.DLL"] = "ScriptStrings.dll",
            ["SERVER_1.TXT"] = "servercycle.txt",
            ["SYSOPS_1.RTF"] = "SysOps.rtf",
            ["ZLOGLIB.DLL"] = "ZLogLib.dll",
            ["ZNMATCH.DLL"] = "ZNMatch.dll",
            ["ZONENET.DLL"] = "ZoneNet.dll",
        };

    private readonly string compatibilityExecutableSha256;
    private readonly MediaInspectionService inspection;
    private readonly DirectoryMediaInventory inventory;

    public VengeanceInstallPlanBuilder()
        : this(QualifiedCompatibilityExecutableSha256, new MediaInspectionService(), new DirectoryMediaInventory())
    {
    }

    public VengeanceInstallPlanBuilder(
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

    public InstallPlan Build(string discOneRoot, string discTwoRoot, string compatibilityExecutablePath)
    {
        RequireLayout(discOneRoot, "vengeance-disc-1");
        RequireLayout(discTwoRoot, "vengeance-disc-2");

        var compatibilityExecutable = Path.GetFullPath(compatibilityExecutablePath);
        if (!File.Exists(compatibilityExecutable))
        {
            throw new FileNotFoundException("Vengeance compatibility executable does not exist.", compatibilityExecutable);
        }
        if ((File.GetAttributes(compatibilityExecutable) & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidDataException("Vengeance compatibility executable cannot be a reparse point.");
        }
        var actualHash = ComputeSha256(compatibilityExecutable);
        if (!string.Equals(actualHash, compatibilityExecutableSha256, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Unsupported Vengeance compatibility executable SHA-256: {actualHash}");
        }

        var files = new List<InstallFile>();
        foreach (var path in inventory.Read(discOneRoot))
        {
            if (DiscOneRootFiles.TryGetValue(path, out var destination))
            {
                files.Add(new InstallFile(discOneRoot, path, destination));
            }
            else if (path.StartsWith("CONTENT/", StringComparison.OrdinalIgnoreCase) ||
                     path.StartsWith("RESOURCE/", StringComparison.OrdinalIgnoreCase))
            {
                files.Add(new InstallFile(discOneRoot, path, VengeanceMediaPathMap.Map(path)));
            }
        }

        foreach (var path in inventory.Read(discTwoRoot))
        {
            if (path.StartsWith("RESOURCE/", StringComparison.OrdinalIgnoreCase))
            {
                files.Add(new InstallFile(discTwoRoot, path, VengeanceMediaPathMap.Map(path)));
            }
        }

        files.Add(new InstallFile(
            Path.GetDirectoryName(compatibilityExecutable)!,
            Path.GetFileName(compatibilityExecutable),
            "MW4.exe"));

        return new InstallPlan("vengeance", files);
    }

    private void RequireLayout(string root, string expectedLayoutId)
    {
        var recognition = inspection.InspectDirectory(root);
        if (recognition.Layout?.Id != expectedLayoutId ||
            recognition.Status is not (MediaRecognitionStatus.Recognized or MediaRecognitionStatus.RecognizedWithExcludedContent))
        {
            throw new InvalidDataException($"Expected {expectedLayoutId}, but media recognition returned {recognition.Status} ({recognition.Layout?.Id ?? "unknown"}).");
        }
    }

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }
}
