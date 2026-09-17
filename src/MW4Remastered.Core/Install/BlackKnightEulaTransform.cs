using System.Security.Cryptography;

namespace MW4Remastered.Core.Install;

public interface IBlackKnightEulaTransform
{
    string Transform(string discRoot, string outputDirectory);
}

public sealed class BlackKnightEulaTransform : IBlackKnightEulaTransform
{
    public const string InputSha256 = "78739a21c7537c3894b46ffe14f04b9a418477a12e00967f82b89310b3707e00";
    public const string OutputSha256 = InputSha256;

    public string Transform(string discRoot, string outputDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(discRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        var source = Path.Combine(Path.GetFullPath(discRoot), "MW4X", "EBUEULA.DLL");
        if (!File.Exists(source)) throw new FileNotFoundException("Black Knight EULA module is missing from selected media.", source);
        if ((File.GetAttributes(source) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("Black Knight EULA module cannot be a reparse point.");

        using var input = File.OpenRead(source);
        var inputHash = Convert.ToHexString(SHA256.HashData(input)).ToLowerInvariant();
        if (!inputHash.Equals(InputSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Unsupported Black Knight EBUEULA.DLL SHA-256: {inputHash}");

        var outputRoot = Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(outputRoot);
        var output = Path.Combine(outputRoot, "EBUEULA.DLL");
        File.Copy(source, output);
        return output;
    }
}
