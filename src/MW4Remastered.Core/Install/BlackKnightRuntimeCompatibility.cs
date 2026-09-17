using System.Security.Cryptography;

namespace MW4Remastered.Core.Install;

public static class BlackKnightRuntimeCompatibility
{
    public const long RuntimeDllLength = 92_160;
    public const string RuntimeDllSha256 = "f534b642defe15ea234988ba0b3b8f1a467aa76ee478cd3e862e6265bbd8bb1c";
    internal const string Configuration = "{\"CDROMDriveLetter\":\"L\",\"CDROMVolumeName\":\"MECHWARR_X1\",\"HookOEP\":\"true\"}";

    internal static void AddToPayload(
        string runtimeDllPath,
        string payloadRoot,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var source = RequireRuntimeDll(runtimeDllPath);
        var gameRoot = Path.Combine(Path.GetFullPath(payloadRoot), "MW4X");
        Directory.CreateDirectory(gameRoot);
        File.Copy(source, Path.Combine(gameRoot, "version.dll"));
        File.WriteAllText(Path.Combine(gameRoot, "version.json"), Configuration);
    }

    private static string RequireRuntimeDll(string path)
    {
        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath)) throw new FileNotFoundException("Missing qualified Black Knight runtime DLL.", fullPath);
        if ((File.GetAttributes(fullPath) & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0)
            throw new InvalidDataException("Black Knight runtime DLL must be a regular file.");
        using var stream = File.OpenRead(fullPath);
        var hash = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
        if (stream.Length != RuntimeDllLength || !hash.Equals(RuntimeDllSha256, StringComparison.Ordinal))
            throw new InvalidDataException($"Unsupported Black Knight runtime DLL: length={stream.Length}, sha256={hash}.");
        return fullPath;
    }

}
