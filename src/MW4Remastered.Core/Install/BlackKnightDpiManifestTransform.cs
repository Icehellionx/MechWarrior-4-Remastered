using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace MW4Remastered.Core.Install;

// The Black Knight executable does not honor an adjacent DPI-only manifest on
// the qualified Windows VM. Embed the same reviewed manifest into a copy of an
// exact, project-produced executable before that copy enters an owned plan.
public sealed class BlackKnightDpiManifestTransform
{
    public const string RelativeExecutablePath = "MW4X/MW4x.exe";
    private const string ManifestName = "MW4x.exe.manifest";
    private static readonly IReadOnlyDictionary<string, string> OutputHashes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [BlackKnightPr1ExecutableTransform.OutputSha256] = "61f4439637a3f37a2de25384dbf4122ac1c1115ed0029e105466069e178d7ea7",
            ["a13588cbb08c32f47d2b5c53c65189752e49cbec692ac3889b7df83ee9b47471"] = "42982c088459a0373a580b99299effa0389a0d5b197018539b6f6dc7198cfea9",
            ["18516b63e07a6347272f3aede0b053def2f7a1ed978642857378123c12ec6af4"] = "8aaa44cc79ac7e56973d6ee3047f1109fecb1c473e110df2557aa1d90ce4cbc8",
            ["8ff3eeb53730eb1d0d88eb0f678b93aab38ec49a0fa1d9a6e3f2cb3d0307777c"] = "ff97adcbbff26a3e9bd8a6d5aa18a3f31cbb590e92ed46710754fa8d05a94e59",
        };
    private static readonly IReadOnlyDictionary<int, string> OriginalByPackMask =
        new Dictionary<int, string>
        {
            [0] = BlackKnightPr1ExecutableTransform.OutputSha256,
            [1] = "a13588cbb08c32f47d2b5c53c65189752e49cbec692ac3889b7df83ee9b47471",
            [2] = "18516b63e07a6347272f3aede0b053def2f7a1ed978642857378123c12ec6af4",
            [3] = "8ff3eeb53730eb1d0d88eb0f678b93aab38ec49a0fa1d9a6e3f2cb3d0307777c",
        };

    public static bool IsEmbeddedForPackMask(int packMask, string hash) =>
        OriginalByPackMask.TryGetValue(packMask, out var original) &&
        hash.Equals(OutputHashes[original], StringComparison.OrdinalIgnoreCase);

    public InstallPlan TransformPlan(InstallPlan plan, string compatibilityRoot, string scratchDirectory)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var files = plan.Files.ToArray();
        var matches = Enumerable.Range(0, files.Length).Where(index =>
            Normalize(files[index].DestinationRelativePath).Equals(RelativeExecutablePath, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (matches.Length != 1) throw new InvalidDataException("Expected one Black Knight executable in the owned install plan.");
        var index = matches[0];
        var source = SourceFile(files[index]);
        var packMask = (plan.Components.Contains("clan", StringComparer.OrdinalIgnoreCase) ? 1 : 0) |
                       (plan.Components.Contains("inner-sphere", StringComparer.OrdinalIgnoreCase) ? 2 : 0);
        if (!OriginalByPackMask.TryGetValue(packMask, out var original) ||
            !Hash(source).Equals(original, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Black Knight executable does not match the selected Mech Paks.");
        var output = Transform(source, compatibilityRoot, scratchDirectory);
        files[index] = new InstallFile(Path.GetDirectoryName(output)!, Path.GetFileName(output), files[index].DestinationRelativePath);
        return new InstallPlan(plan.ProductId, files, plan.Components);
    }

    public IReadOnlyList<InstallFile> CreateUpgradeReplacement(
        string installRoot, InstallManifest manifest, string compatibilityRoot, string scratchDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(installRoot);
        ArgumentNullException.ThrowIfNull(manifest);
        if (!manifest.HasComponent("black-knight")) return [];
        if (!manifest.Files.Any(file => Normalize(file.Path).Equals(RelativeExecutablePath, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidDataException("The owned Black Knight executable is missing from its manifest.");

        var source = StagedInstallTransaction.ResolveContainedPath(installRoot, RelativeExecutablePath);
        StagedInstallTransaction.RejectContainedFilePath(installRoot, source);
        var hash = Hash(source);
        var packMask = (manifest.HasComponent("clan") ? 1 : 0) |
                       (manifest.HasComponent("inner-sphere") ? 2 : 0);
        if (!OriginalByPackMask.TryGetValue(packMask, out var original))
            throw new InvalidDataException("Unsupported Black Knight Mech Pak selection for DPI migration.");
        if (hash.Equals(OutputHashes[original], StringComparison.OrdinalIgnoreCase)) return [];
        if (!hash.Equals(original, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Black Knight executable does not match its owned Mech Pak selection: {hash}");
        var output = Transform(source, compatibilityRoot, scratchDirectory);
        return [new InstallFile(Path.GetDirectoryName(output)!, Path.GetFileName(output), RelativeExecutablePath)];
    }

    private static string Transform(string source, string compatibilityRoot, string scratchDirectory)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("PE manifest embedding requires Windows.");
        var sourceHash = Hash(source);
        if (!OutputHashes.TryGetValue(sourceHash, out var expected))
            throw new InvalidDataException($"Unsupported Black Knight executable SHA-256 for DPI manifest: {sourceHash}");
        var root = Path.GetFullPath(scratchDirectory);
        Directory.CreateDirectory(root);
        if ((File.GetAttributes(root) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("Black Knight DPI scratch directory cannot be a reparse point.");
        var manifest = StagedInstallTransaction.ResolveContainedPath(compatibilityRoot, ManifestName);
        StagedInstallTransaction.RejectContainedFilePath(compatibilityRoot, manifest);
        var manifestBytes = File.ReadAllBytes(manifest);
        if (!Convert.ToHexString(SHA256.HashData(manifestBytes)).Equals(
                LegacyPresentationCompatibility.BlackKnightManifestSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Unsupported Black Knight DPI manifest.");

        var output = Path.Combine(root, "MW4x-dpi.exe");
        if (File.Exists(output) || Directory.Exists(output))
            throw new IOException("Black Knight DPI transform output already exists.");
        File.Copy(source, output);
        var handle = BeginUpdateResource(output, false);
        if (handle == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastPInvokeError(), "Could not open Black Knight resource update.");
        var committed = false;
        try
        {
            if (!UpdateResource(handle, new IntPtr(24), new IntPtr(1), 0, manifestBytes, (uint)manifestBytes.Length))
                throw new Win32Exception(Marshal.GetLastPInvokeError(), "Could not embed Black Knight DPI manifest.");
            committed = true;
        }
        finally
        {
            var ended = EndUpdateResource(handle, !committed);
            if (!ended) throw new Win32Exception(Marshal.GetLastPInvokeError(), "Could not finish Black Knight resource update.");
        }
        var actual = Hash(output);
        if (!actual.Equals(expected, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Black Knight DPI executable hash mismatch: {actual}");
        return output;
    }

    private static string SourceFile(InstallFile file)
    {
        var source = StagedInstallTransaction.ResolveContainedPath(file.SourceRoot, file.SourceRelativePath);
        StagedInstallTransaction.RejectContainedFilePath(file.SourceRoot, source);
        if (!File.Exists(source)) throw new FileNotFoundException("Black Knight executable source is missing.", source);
        return source;
    }

    private static string Hash(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static string Normalize(string path) => path.Replace('\\', '/');

    [DllImport("kernel32.dll", EntryPoint = "BeginUpdateResourceW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr BeginUpdateResource(string fileName, bool deleteExistingResources);
    [DllImport("kernel32.dll", EntryPoint = "UpdateResourceW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool UpdateResource(IntPtr update, IntPtr type, IntPtr name, ushort language, byte[] data, uint size);
    [DllImport("kernel32.dll", EntryPoint = "EndUpdateResourceW", SetLastError = true)]
    private static extern bool EndUpdateResource(IntPtr update, bool discard);
}
