using System.Security.Cryptography;

namespace MW4Remastered.Core.Install;

// Wine's built-in VERSION.dll shadows the setup proxy. Retarget just the
// protected launcher's import name in a disposable copy, outside the retained
// static image. No Wine registry or inherited loader preference is changed.
internal static class BlackKnightPr1CaptureImport
{
    internal const string LibraryName = "mw4cap_.dll";
    internal const string OutputSha256 = "6645736ca3e0c85abec2220d8eeae467b22223c4b375908c362e55389c087d8d";
    private const int ImportNameOffset = 0x397904;

    internal static void Create(string sourcePath, string destinationPath)
    {
        var image = File.ReadAllBytes(sourcePath);
        if (image.LongLength != BlackKnightPr1ExecutableTransform.InputLength ||
            !Convert.ToHexString(SHA256.HashData(image)).Equals(BlackKnightPr1ExecutableTransform.InputSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Unsupported Black Knight PR1 executable for setup capture import.");
        if (!image.AsSpan(ImportNameOffset, 12).SequenceEqual("VERSION.dll\0"u8))
            throw new InvalidDataException("Black Knight PR1 setup capture import does not match its qualified location.");
        "mw4cap_.dll"u8.CopyTo(image.AsSpan(ImportNameOffset, 11));
        if (!Convert.ToHexString(SHA256.HashData(image)).Equals(OutputSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Black Knight PR1 setup capture import output mismatch.");
        using var output = new FileStream(destinationPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        output.Write(image);
    }
}
