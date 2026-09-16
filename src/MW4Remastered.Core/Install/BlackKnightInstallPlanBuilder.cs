using MW4Remastered.Core.Media;

namespace MW4Remastered.Core.Install;

public sealed class BlackKnightInstallPlanBuilder
{
    public const string QualifiedLoaderSha256 = "ff530c8144ebf82b3f32951c8cd3b1085b6527b46d55348bb1c7f8ef7f83296a";
    public const string QualifiedLoaderConfigurationSha256 = "8208a30c47f55c3f497689d7ab9ed1e88c9c7d11e028ee14d8508df15f6f2cbd";
    public const string QualifiedLoaderLicenseSha256 = "81cbae84a29ce7e770bf2bc7b178e50bda0ce8de6067aba661b0bc7b05b562f8";
    public const string QualifiedSourceArchiveName = "SafeDiscLoader2-source-f27286a363aa675a0422141cb96fc8619cf8b9d8.zip";
    public const string QualifiedSourceArchiveSha256 = "e96558eee5570e4085fcb9b36cbc3c30ae3ff3b2e2c02a67d54c008c920f0490";
    public const string QualifiedLocalPatchName = "SafeDiscLoader2-MW4-BlackKnight.patch";
    public const string QualifiedLocalPatchSha256 = "286de58683edd45065f884b201109815b7252a6d8b3baf896e0a4ea68b03dadb";

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

    private readonly QualifiedCompatibilityPayload compatibility;
    private readonly MediaInspectionService inspection;
    private readonly DirectoryMediaInventory inventory;
    private readonly InstallManifestVerifier manifestVerifier;

    public BlackKnightInstallPlanBuilder()
        : this(CreatePackagedPayload(), new MediaInspectionService(), new DirectoryMediaInventory(), new InstallManifestVerifier())
    {
    }

    public BlackKnightInstallPlanBuilder(
        QualifiedCompatibilityPayload compatibility,
        MediaInspectionService inspection,
        DirectoryMediaInventory inventory,
        InstallManifestVerifier? manifestVerifier = null)
    {
        this.compatibility = compatibility ?? throw new ArgumentNullException(nameof(compatibility));
        this.inspection = inspection ?? throw new ArgumentNullException(nameof(inspection));
        this.inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        this.manifestVerifier = manifestVerifier ?? new InstallManifestVerifier();
    }

    public InstallPlan Build(string discRoot, string baseVengeanceRoot, string preparedEulaPath)
    {
        RequireLayout(discRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseVengeanceRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(preparedEulaPath);
        var verifiedBase = manifestVerifier.Verify(baseVengeanceRoot, InstallVerificationScope.OwnedFiles);
        if (!verifiedBase.IsValid || verifiedBase.Manifest is null ||
            !verifiedBase.Manifest.ProductId.Equals("vengeance", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Black Knight requires a verified Remastered Vengeance base installation: " +
                string.Join("; ", verifiedBase.Issues));
        }

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
                    expansionFiles.Add(new InstallFile(discRoot, path, fileName));
                }
            }
        }

        var preparedEula = Path.GetFullPath(preparedEulaPath);
        expansionFiles.Add(new InstallFile(Path.GetDirectoryName(preparedEula)!, Path.GetFileName(preparedEula), "EBUEula.dll"));
        expansionFiles.AddRange(compatibility.ValidateAndCreateInstallFiles());
        var expansionDestinations = expansionFiles.Select(file => file.DestinationRelativePath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var baseRoot = Path.GetFullPath(baseVengeanceRoot);
        var files = verifiedBase.Manifest.Files
            .Where(file => !expansionDestinations.Contains(file.Path))
            .Select(file => new InstallFile(baseRoot, file.Path, file.Path))
            .ToList();
        files.AddRange(expansionFiles);
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

    public static QualifiedCompatibilityPayload CreatePackagedPayload(string? bundleRoot = null)
    {
        var root = string.IsNullOrWhiteSpace(bundleRoot)
            ? Path.Combine(AppContext.BaseDirectory, "Compatibility", "BlackKnight")
            : Path.GetFullPath(bundleRoot);
        return new QualifiedCompatibilityPayload(
            root,
            new[]
            {
                new QualifiedCompatibilityFile("version.dll", "version.dll", QualifiedLoaderSha256),
                new QualifiedCompatibilityFile("version.json", "version.json", QualifiedLoaderConfigurationSha256),
                new QualifiedCompatibilityFile("SafeDiscLoader2-LICENSE.txt", "Licenses/SafeDiscLoader2-GPL-3.0.txt", QualifiedLoaderLicenseSha256),
            },
            new[]
            {
                new QualifiedCompatibilitySupportFile(QualifiedSourceArchiveName, QualifiedSourceArchiveSha256),
                new QualifiedCompatibilitySupportFile(QualifiedLocalPatchName, QualifiedLocalPatchSha256),
            },
            requireExactInventory: true);
    }

}
