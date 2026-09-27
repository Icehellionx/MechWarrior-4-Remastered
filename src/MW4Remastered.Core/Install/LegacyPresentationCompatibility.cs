using System.Security.Cryptography;

namespace MW4Remastered.Core.Install;

public static class LegacyPresentationCompatibility
{
    private static readonly IReadOnlyDictionary<string, string> DgVoodooFiles =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["DDraw.dll"] = "612a24408a090a3c6f3886557fa18034ee742e94ad0a40ebdf854d2816176c2e",
            ["D3DImm.dll"] = "93c534f2d17419ea78f15551f7e0aac78b3c503733a840914fa063708a5afe8e",
            ["D3D8.dll"] = "d03e2562178db1fcf3493fc0a0b23a465e35c55d73adb3ec095be866cd662704",
            ["D3D9.dll"] = "6a0ca214784be04b7c8b547105aa9d79acf4dc26c0b6f8702b437ddca54058b2",
            ["SampleAddon.dll"] = "24e6fe3e7eea55aa2271223bf08e444597e58580f78126ea440db0e2cefb254b",
            ["SampleAddon.ini"] = "f21bb13f1e5ecb33595677ed5f8eba156576fcb2f2f5123138147809ae9b9edb",
            ["DirtyGlass.png"] = "dc507d14880cde567b192aaf444769586a906c0165ec2432ee43d0cead4fcbc5",
        };
    private const string ConfigName = "dgVoodoo.conf";
    public const string ConfigSha256 = "7ea9e4576a421157927de2d41551e3fdef8b4c76cd3adcc2d02ef19706f249a4";
    private static readonly IReadOnlyDictionary<string, string> MovieDecoderFiles =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Issue10DecoderBlock.dll"] = "17ddfcafeea049c43a2cf37ffb20432e6f64fbad5a58040ca10f298fa455a1dc",
            ["MW4.exe.manifest"] = "9312eaf3454beacc1dc1c9145a0b1184b5898ea982ecbb00bf4ecc155c200302",
            ["MW4Mercs.exe.manifest"] = "b7654d9771cb18a416de5fd03fbf6ad0fb1fa91d099b1e1973f9d1e84e972304",
        };
    private static readonly IReadOnlyDictionary<string, string> MovieManifestByExecutable =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["MW4.exe"] = "MW4.exe.manifest",
            ["MW4Mercs.exe"] = "MW4Mercs.exe.manifest",
        };

    public static IReadOnlyList<InstallFile> CreateVengeanceFiles(string root, bool includeBlackKnight = false)
    {
        var sourceRoot = Validate(root);
        var files = CreateGameFiles(sourceRoot, "MW4.exe", "").ToList();
        if (includeBlackKnight)
        {
            files.AddRange(CreateGameFiles(sourceRoot, "MW4x.exe", "MW4X"));
        }
        return files;
    }

    public static IReadOnlyList<InstallFile> CreateMercenariesFiles(string root)
    {
        var sourceRoot = Validate(root);
        return CreateGameFiles(sourceRoot, "MW4Mercs.exe", "");
    }

    private static IReadOnlyList<InstallFile> CreateGameFiles(string sourceRoot, string executableName, string destinationRoot)
    {
        var files = CreateFiles(sourceRoot, destinationRoot).ToList();
        // Black Knight played its UI movie on the affected PC, so leave its decoder selection intact.
        if (MovieManifestByExecutable.TryGetValue(executableName, out var manifestName))
        {
            files.Add(new InstallFile(sourceRoot, "Issue10DecoderBlock.dll", Combine(destinationRoot, "Issue10DecoderBlock.dll")));
            files.Add(new InstallFile(sourceRoot, manifestName, Combine(destinationRoot, manifestName)));
        }
        return files;
    }

    private static IReadOnlyList<InstallFile> CreateFiles(string sourceRoot, string destinationRoot)
    {
        var files = DgVoodooFiles.Keys
            .Select(name => new InstallFile(sourceRoot, name, Combine(destinationRoot, name)))
            .ToList();
        files.Add(new InstallFile(sourceRoot, ConfigName, Combine(destinationRoot, ConfigName)));
        return files;
    }

    private static string Validate(string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        var fullRoot = Path.GetFullPath(root);
        if (!Directory.Exists(fullRoot)) throw new DirectoryNotFoundException($"Presentation compatibility root is missing: {fullRoot}");
        if ((File.GetAttributes(fullRoot) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("Presentation compatibility root cannot be a reparse point.");

        foreach (var expected in DgVoodooFiles)
        {
            var dll = RequireRegularFile(fullRoot, expected.Key);
            using var stream = File.OpenRead(dll);
            var actual = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
            if (!actual.Equals(expected.Value, StringComparison.Ordinal))
                throw new InvalidDataException($"Unsupported dgVoodoo2 file SHA-256 for {expected.Key}: {actual}");
        }
        foreach (var expected in MovieDecoderFiles)
        {
            var file = RequireRegularFile(fullRoot, expected.Key);
            using var stream = File.OpenRead(file);
            var actual = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
            if (!actual.Equals(expected.Value, StringComparison.Ordinal))
                throw new InvalidDataException($"Unsupported movie decoder compatibility file SHA-256 for {expected.Key}: {actual}");
        }
        var config = RequireRegularFile(fullRoot, ConfigName);
        using (var stream = File.OpenRead(config))
        {
            var actual = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
            if (!actual.Equals(ConfigSha256, StringComparison.Ordinal))
                throw new InvalidDataException($"Unsupported dgVoodoo2 profile SHA-256: {actual}");
        }
        return fullRoot;
    }

    private static string Combine(string root, string name) =>
        string.IsNullOrWhiteSpace(root) ? name : $"{root}/{name}";

    private static string RequireRegularFile(string root, string name)
    {
        var path = Path.Combine(root, name);
        StagedInstallTransaction.RejectContainedFilePath(root, path);
        if (!File.Exists(path)) throw new FileNotFoundException($"Presentation compatibility file is missing: {name}", path);
        return path;
    }
}
