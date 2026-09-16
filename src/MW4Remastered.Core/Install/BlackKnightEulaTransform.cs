using System.Security.Cryptography;

namespace MW4Remastered.Core.Install;

public interface IBlackKnightEulaTransform
{
    string Transform(string discRoot, string outputDirectory);
}

public sealed class BlackKnightEulaTransform : IBlackKnightEulaTransform
{
    public const string InputSha256 = "78739a21c7537c3894b46ffe14f04b9a418477a12e00967f82b89310b3707e00";
    public const string OutputSha256 = "d150fcebe8560bdfd5389b4eec9fa7f82b134f8714ef42591ad6daeb4afac85b";
    private const int ExportFileOffset = 0x1f30;
    private static readonly byte[] ExpectedEntry = [0x83, 0xec, 0x0c, 0x8b, 0x15, 0x04];
    private static readonly byte[] AcceptedReturn = [0xb8, 0x01, 0x00, 0x00, 0x00, 0xc3];

    public string Transform(string discRoot, string outputDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(discRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        var source = Path.Combine(Path.GetFullPath(discRoot), "MW4X", "EBUEULA.DLL");
        if (!File.Exists(source)) throw new FileNotFoundException("Black Knight EULA module is missing from selected media.", source);
        if ((File.GetAttributes(source) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("Black Knight EULA module cannot be a reparse point.");

        var bytes = File.ReadAllBytes(source);
        var inputHash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        if (!inputHash.Equals(InputSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Unsupported Black Knight EBUEULA.DLL SHA-256: {inputHash}");
        if (!bytes.AsSpan(ExportFileOffset, ExpectedEntry.Length).SequenceEqual(ExpectedEntry))
            throw new InvalidDataException("Black Knight EULA export does not match the qualified revision.");

        AcceptedReturn.CopyTo(bytes, ExportFileOffset);
        var outputHash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        if (!outputHash.Equals(OutputSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Black Knight EULA transform produced unexpected SHA-256: {outputHash}");

        var outputRoot = Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(outputRoot);
        var output = Path.Combine(outputRoot, "EBUEULA.DLL");
        File.WriteAllBytes(output, bytes);
        return output;
    }
}
