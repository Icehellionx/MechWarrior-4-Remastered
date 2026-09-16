using System.Security.Cryptography;
using MW4Remastered.Core.Media;

namespace MW4Remastered.Core.Install;

public sealed class MercenariesInstallPlanBuilder
{
    public const string QualifiedCompatibilityExecutableSha256 = MercenariesRetailExecutableTransform.OutputSha256;

    private static readonly IReadOnlyDictionary<string, string> DiscOneRootFiles =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["00000409.016"] = "00000409.016",
            ["00000409.256"] = "00000409.256",
            ["ARTPCLNT.DLL"] = "ArtpClnt.dll",
            ["AUTOCO_1.EXE"] = "AutoConfig.exe",
            ["BLADE.DLL"] = "Blade.dll",
            ["EBUEULA.DLL"] = "EBUEula.dll",
            ["EULA.RTF"] = "EULA.rtf",
            ["GUNDLL.DLL"] = "GunDll.dll",
            ["GUNTIC_1.DLL"] = "GunTicket.dll",
            ["LANGUAGE.DLL"] = "Language.dll",
            ["MCP.DLL"] = "MCP.dll",
            ["MECH4MER.ICO"] = "Mech4Merc.ico",
            ["MFC42.DLL"] = "MFC42.dll",
            ["MISSIO_1.DLL"] = "MissionLang.dll",
            ["MOTD.TXT"] = "MOTD.txt",
            ["MSVCIRT.DLL"] = "MSVCIRT.dll",
            ["MSVCRT.DLL"] = "MSVCRT.dll",
            ["MW4STATS.DLL"] = "MW4Stats.dll",
            ["NFMEDI_1.EXE"] = "NFMEditor.exe",
            ["SCRIPT_1.DLL"] = "ScriptStrings.dll",
            ["SERVER_1.TXT"] = "ServerCycle.txt",
            ["VIDEOC_1.TXT"] = "VideoCard.txt",
            ["WARRANTY.RTF"] = "Warranty.rtf",
            ["ZLOGLIB.DLL"] = "ZLogLib.dll",
        };

    private readonly string compatibilityExecutableSha256;
    private readonly MediaInspectionService inspection;
    private readonly DirectoryMediaInventory inventory;

    public MercenariesInstallPlanBuilder()
        : this(QualifiedCompatibilityExecutableSha256, new MediaInspectionService(), new DirectoryMediaInventory())
    {
    }

    public MercenariesInstallPlanBuilder(
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

    public InstallPlan Build(string discOneRoot, string discTwoRoot, string cabinetPayloadRoot, string compatibilityExecutablePath)
    {
        RequireLayout(discOneRoot, "mercenaries-disc-1");
        RequireLayout(discTwoRoot, "mercenaries-disc-2");
        var replacement = RequireQualifiedExecutable(compatibilityExecutablePath);

        var cabinetRoot = Path.GetFullPath(cabinetPayloadRoot);
        var cabinetInventory = inventory.Read(cabinetRoot);
        if (!cabinetInventory.Contains("RESOURCE/CORE.MW4", StringComparer.OrdinalIgnoreCase) ||
            !cabinetInventory.Contains("RESOURCE/PROPS.MW4", StringComparer.OrdinalIgnoreCase) ||
            !cabinetInventory.Contains("RESOURCE/TEXTURES.MW4", StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Mercenaries cabinet payload is missing required core archives.");
        }

        var files = new List<InstallFile>();
        foreach (var path in inventory.Read(discOneRoot))
        {
            if (DiscOneRootFiles.TryGetValue(path, out var destination))
            {
                files.Add(new InstallFile(discOneRoot, path, destination));
            }
            else if (path.StartsWith("FONTS/", StringComparison.OrdinalIgnoreCase) ||
                     path.StartsWith("RESOURCE/", StringComparison.OrdinalIgnoreCase))
            {
                files.Add(new InstallFile(discOneRoot, path, path));
            }
        }

        files.AddRange(cabinetInventory.Select(path => new InstallFile(cabinetRoot, path, path)));

        foreach (var path in inventory.Read(discTwoRoot))
        {
            if (path.StartsWith("CONTENT/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("RESOURCE/", StringComparison.OrdinalIgnoreCase))
            {
                files.Add(new InstallFile(discTwoRoot, path, MapDiscTwoPath(path)));
            }
        }

        files.Add(new InstallFile(Path.GetDirectoryName(replacement)!, Path.GetFileName(replacement), "MW4Mercs.exe"));
        return new InstallPlan("mercenaries", files);
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

    private string RequireQualifiedExecutable(string path)
    {
        var executable = Path.GetFullPath(path);
        if (!File.Exists(executable)) throw new FileNotFoundException("Mercenaries compatibility executable does not exist.", executable);
        if ((File.GetAttributes(executable) & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidDataException("Mercenaries compatibility executable cannot be a reparse point.");
        }

        using var stream = File.OpenRead(executable);
        var actualHash = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
        if (!string.Equals(actualHash, compatibilityExecutableSha256, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Unsupported Mercenaries compatibility executable SHA-256: {actualHash}");
        }
        return executable;
    }

    private static string MapDiscTwoPath(string path)
    {
        if (path.StartsWith("CONTENT/MERCSS_1/", StringComparison.OrdinalIgnoreCase))
        {
            return "Content/MercsShellScripts/" + path["CONTENT/MERCSS_1/".Length..];
        }
        if (string.Equals(path, "CONTENT/GAMETY_1.H", StringComparison.OrdinalIgnoreCase))
        {
            return "Content/GameTypes.h";
        }
        return path;
    }
}
