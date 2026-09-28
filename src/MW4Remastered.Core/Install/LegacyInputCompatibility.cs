using System.Security.Cryptography;

namespace MW4Remastered.Core.Install;

/// <summary>Installs the exact reviewed DirectInput adapter beside each game executable.</summary>
public static class LegacyInputCompatibility
{
    public const string AdapterSha256 = "d190a049cbad89a87191abfc4dfd58d057ec1b89f0bb53c2b018485c3b9048d4";
    public const long AdapterLength = 272_384;

    public static IReadOnlyList<InstallFile> CreateFiles(string root, bool includeBlackKnight)
    {
        var source = Validate(root);
        var files = new List<InstallFile> { new(source, "dinput.dll", "dinput.dll") };
        if (includeBlackKnight)
            files.Add(new InstallFile(source, "dinput.dll", "MW4X/dinput.dll"));
        return files;
    }

    public static IReadOnlyList<InstallFile> CreateMercenariesFiles(string root) =>
        [new InstallFile(Validate(root), "dinput.dll", "dinput.dll")];

    private static string Validate(string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        var fullRoot = Path.GetFullPath(root);
        if (!Directory.Exists(fullRoot)) throw new DirectoryNotFoundException($"Input compatibility root is missing: {fullRoot}");
        if ((File.GetAttributes(fullRoot) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("Input compatibility root cannot be a reparse point.");
        var file = Path.Combine(fullRoot, "dinput.dll");
        StagedInstallTransaction.RejectContainedFilePath(fullRoot, file);
        if (!File.Exists(file)) throw new FileNotFoundException("DirectInput adapter is missing.", file);
        var info = new FileInfo(file);
        if (info.Length != AdapterLength)
            throw new InvalidDataException($"DirectInput adapter size mismatch: {info.Length}");
        using var stream = File.OpenRead(file);
        var actual = Convert.ToHexString(SHA256.HashData(stream));
        if (!actual.Equals(AdapterSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Unsupported DirectInput adapter SHA-256: {actual}");
        return fullRoot;
    }
}
