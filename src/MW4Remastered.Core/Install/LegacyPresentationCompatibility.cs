using System.Security.Cryptography;

namespace MW4Remastered.Core.Install;

public static class LegacyPresentationCompatibility
{
    public const string DllSha256 = "b589c27402c283f699857aec26948b33595ee93f645891ec4f9607254148b509";

    public static IReadOnlyList<InstallFile> CreateVengeanceFiles(string root)
    {
        var (sourceRoot, dllName, configName) = Validate(root);
        var files = new List<InstallFile>
        {
            new(sourceRoot, dllName, "ddraw.dll"),
            new(sourceRoot, configName, "DDrawCompat-MW4.ini"),
        };
        return files;
    }

    public static IReadOnlyList<InstallFile> CreateMercenariesFiles(string root)
    {
        var (sourceRoot, dllName, configName) = Validate(root);
        return
        [
            new InstallFile(sourceRoot, dllName, "ddraw.dll"),
            new InstallFile(sourceRoot, configName, "DDrawCompat-MW4Mercs.ini"),
        ];
    }

    private static (string Root, string DllName, string ConfigName) Validate(string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        var fullRoot = Path.GetFullPath(root);
        if (!Directory.Exists(fullRoot)) throw new DirectoryNotFoundException($"Presentation compatibility root is missing: {fullRoot}");
        if ((File.GetAttributes(fullRoot) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("Presentation compatibility root cannot be a reparse point.");

        const string dllName = "ddraw.dll";
        const string configName = "DDrawCompat-MW4.ini";
        var dll = RequireRegularFile(fullRoot, dllName);
        _ = RequireRegularFile(fullRoot, configName);
        using var stream = File.OpenRead(dll);
        var actual = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
        if (!actual.Equals(DllSha256, StringComparison.Ordinal))
            throw new InvalidDataException($"Unsupported DDrawCompat DLL SHA-256: {actual}");
        return (fullRoot, dllName, configName);
    }

    private static string RequireRegularFile(string root, string name)
    {
        var path = Path.Combine(root, name);
        StagedInstallTransaction.RejectContainedFilePath(root, path);
        if (!File.Exists(path)) throw new FileNotFoundException($"Presentation compatibility file is missing: {name}", path);
        return path;
    }
}
