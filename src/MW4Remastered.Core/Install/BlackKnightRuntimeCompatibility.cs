using System.Security.Cryptography;

namespace MW4Remastered.Core.Install;

public sealed record PreparedBlackKnightRuntimePayload(
    string PayloadRoot,
    IReadOnlyList<InstallFile> InstallFiles);

public interface IBlackKnightRuntimeCompatibility
{
    PreparedBlackKnightRuntimePayload Prepare(
        string runtimeDllPath,
        string scratchDirectory,
        CancellationToken cancellationToken = default);
}

public sealed class BlackKnightRuntimeCompatibility : IBlackKnightRuntimeCompatibility
{
    public const long RuntimeDllLength = 92_160;
    public const string RuntimeDllSha256 = "f534b642defe15ea234988ba0b3b8f1a467aa76ee478cd3e862e6265bbd8bb1c";
    internal const string Configuration = "{\"CDROMDriveLetter\":\"L\",\"CDROMVolumeName\":\"MECHWARR_X1\",\"HookOEP\":\"true\"}";

    public PreparedBlackKnightRuntimePayload Prepare(
        string runtimeDllPath,
        string scratchDirectory,
        CancellationToken cancellationToken = default)
    {
        var scratch = Path.GetFullPath(scratchDirectory);
        if (Directory.Exists(scratch) || File.Exists(scratch))
            throw new IOException("Black Knight runtime scratch path already exists.");

        var payload = Path.Combine(scratch, "payload");
        Directory.CreateDirectory(payload);
        try
        {
            AddToPayload(runtimeDllPath, payload, cancellationToken);
            return new PreparedBlackKnightRuntimePayload(payload, CreateInstallFiles(payload));
        }
        catch
        {
            RemoveTree(scratch);
            throw;
        }
    }

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

    internal static IReadOnlyList<InstallFile> CreateInstallFiles(string payloadRoot)
    {
        var root = Path.GetFullPath(payloadRoot);
        var dll = RequireRuntimeDll(Path.Combine(root, "MW4X", "version.dll"));
        var configuration = Path.Combine(root, "MW4X", "version.json");
        if (!File.Exists(configuration) || File.ReadAllText(configuration) != Configuration)
            throw new InvalidDataException("Black Knight runtime configuration is missing or altered.");
        return
        [
            new InstallFile(root, Path.GetRelativePath(root, dll), "MW4X/version.dll"),
            new InstallFile(root, Path.GetRelativePath(root, configuration), "MW4X/version.json"),
        ];
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

    private static void RemoveTree(string path)
    {
        if (!Directory.Exists(path)) return;
        foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            File.SetAttributes(file, FileAttributes.Normal);
        Directory.Delete(path, recursive: true);
    }
}
