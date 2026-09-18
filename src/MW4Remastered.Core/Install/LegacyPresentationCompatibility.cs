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
            ["SampleAddon.dll"] = "33e7fae1c1cb2d297c05c14b5d4f886676fcd492c44eab0602d8e4465bc71ef0",
            ["SampleAddon.ini"] = "f21bb13f1e5ecb33595677ed5f8eba156576fcb2f2f5123138147809ae9b9edb",
            ["DirtyGlass.png"] = "dc507d14880cde567b192aaf444769586a906c0165ec2432ee43d0cead4fcbc5",
        };
    private const string ConfigName = "dgVoodoo.conf";
    private const string ConfigSha256 = "72b27b7bbebb7d1a3a3dd136b83f88c79aebcb20ba8edcc20a4703a40c0300a6";

    public static IReadOnlyList<InstallFile> CreateVengeanceFiles(string root, bool includeBlackKnight = false)
    {
        var sourceRoot = Validate(root);
        var files = CreateFiles(sourceRoot, "").ToList();
        if (includeBlackKnight)
        {
            files.AddRange(CreateFiles(sourceRoot, "MW4X"));
        }
        return files;
    }

    public static IReadOnlyList<InstallFile> CreateMercenariesFiles(string root)
    {
        var sourceRoot = Validate(root);
        return CreateFiles(sourceRoot, "");
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
