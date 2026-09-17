using System.Security.Cryptography;

namespace MW4Remastered.Core.Install;

public static class LegacyPresentationCompatibility
{
    private static readonly IReadOnlyDictionary<string, string> DgVoodooFiles =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["DDraw.dll"] = "62f1e1b2ac5196f4a74b35b898ba8644322f976026324e2f767a4096bfb58748",
            ["D3DImm.dll"] = "f51507acbb1c5510ab72881eefde5e4dfe6376667546e86ddcf62e6a9ee4923f",
            ["D3D8.dll"] = "72bd6b84face40b928dfd5d1ee6d30f2ce8671919d0264b24513e25a2c647fd3",
            ["D3D9.dll"] = "b7401378b2b8e8c18c88a033e77c3ee99f4c7d2ac8cfcc949d79c1dd7fa99767",
        };
    private const string ConfigName = "dgVoodoo.conf";

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
        _ = RequireRegularFile(fullRoot, ConfigName);
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
