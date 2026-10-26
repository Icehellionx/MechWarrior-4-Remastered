using System.Security.Cryptography;

namespace MW4Remastered.Core.Install;

// Retail movie initialization releases its Surface7 reference while retaining
// that pointer for teardown. Official Vengeance Patch 3 omits this early Release
// and retains the teardown Release. Reproduce only that lifetime correction.
public sealed class VengeanceMovieSurfaceTransform
{
    public const string InputSha256 = VengeanceRetailExecutableTransform.OutputSha256;
    public const string OutputSha256 = "cac62712e7c4f652d166e27ec92474a705a32ee7e837275ceb31085a8882e0cc";
    private const string RelativeExecutable = "MW4.exe";
    private static readonly byte[] EarlyRelease = Convert.FromHexString("50E808810100");
    private readonly string inputHash;
    private readonly string outputHash;
    private readonly int patchOffset;

    public VengeanceMovieSurfaceTransform() : this(InputSha256, OutputSha256, 0x247B2) { }

    internal VengeanceMovieSurfaceTransform(string inputHash, string outputHash, int patchOffset)
    {
        this.inputHash = inputHash;
        this.outputHash = outputHash;
        this.patchOffset = patchOffset;
    }

    public InstallPlan TransformPlan(InstallPlan plan, string scratchDirectory, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsStandaloneRetail(plan.ProductId, plan.Components)) return plan;
        var files = plan.Files.ToArray();
        var index = Array.FindIndex(files, file => IsExecutable(file.DestinationRelativePath));
        if (index < 0) throw new InvalidDataException("Vengeance movie correction requires its executable in the install plan.");
        var bytes = ReadSource(files[index].SourceRoot, files[index].SourceRelativePath);
        // Plan builders already qualify executable revisions. Leave other
        // revisions (including synthetic orchestration fixtures) unchanged.
        if (!Hash(bytes).Equals(inputHash, StringComparison.OrdinalIgnoreCase)) return plan;
        var output = Transform(bytes, scratchDirectory, cancellationToken);
        files[index] = new InstallFile(Path.GetDirectoryName(output)!, Path.GetFileName(output), RelativeExecutable);
        return new InstallPlan(plan.ProductId, files, plan.Components);
    }

    public IReadOnlyList<InstallFile> CreateUpgradeReplacement(
        string installRoot, InstallManifest manifest, string scratchDirectory, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsStandaloneRetail(manifest.ProductId, manifest.EffectiveComponents)) return [];
        var owned = manifest.Files.Where(file => IsExecutable(file.Path)).ToArray();
        if (owned.Length != 1)
            throw new InvalidDataException("Vengeance movie correction requires one manifest-owned executable.");
        var bytes = ReadSource(installRoot, RelativeExecutable);
        var hash = Hash(bytes);
        if (owned[0].Length != bytes.Length || !owned[0].Sha256.Equals(hash, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("The owned Vengeance executable changed before movie correction.");
        if (hash.Equals(outputHash, StringComparison.OrdinalIgnoreCase)) return [];
        if (!hash.Equals(inputHash, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Unsupported Vengeance executable SHA-256 for movie correction: {hash}");
        var output = Transform(bytes, scratchDirectory, cancellationToken);
        return [new InstallFile(Path.GetDirectoryName(output)!, Path.GetFileName(output), RelativeExecutable)];
    }

    private string Transform(byte[] source, string scratchDirectory, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scratchDirectory);
        if (!Hash(source).Equals(inputHash, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Unsupported retail Vengeance input for movie correction.");
        if (patchOffset < 0 || patchOffset > source.Length - EarlyRelease.Length ||
            !source.AsSpan(patchOffset, EarlyRelease.Length).SequenceEqual(EarlyRelease))
            throw new InvalidDataException("Vengeance movie surface Release instructions do not match.");
        var output = source.ToArray();
        // Removing both PUSH and its stdcall call preserves the stack. The
        // unused Release return is overwritten at the initializer's join.
        output.AsSpan(patchOffset, EarlyRelease.Length).Fill(0x90);
        if (!Hash(output).Equals(outputHash, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Vengeance movie correction output SHA-256 does not match.");
        cancellationToken.ThrowIfCancellationRequested();
        var scratch = Path.GetFullPath(scratchDirectory);
        Directory.CreateDirectory(scratch);
        if ((File.GetAttributes(scratch) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("Vengeance movie correction scratch cannot be a reparse point.");
        var path = StagedInstallTransaction.ResolveContainedPath(scratch, "MW4-movie-surface.exe");
        using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None)) stream.Write(output);
        return path;
    }

    private static byte[] ReadSource(string root, string relative)
    {
        var path = StagedInstallTransaction.ResolveContainedPath(root, relative);
        StagedInstallTransaction.RejectContainedFilePath(root, path);
        return File.ReadAllBytes(path);
    }

    private static bool IsExecutable(string relative) =>
        relative.Replace('\\', '/').Equals(RelativeExecutable, StringComparison.OrdinalIgnoreCase);

    private static bool IsStandaloneRetail(string productId, IEnumerable<string> components) =>
        productId.Equals("vengeance", StringComparison.OrdinalIgnoreCase) &&
        !components.Any(component => component.Equals("black-knight", StringComparison.OrdinalIgnoreCase) ||
                                     component.Equals("clan", StringComparison.OrdinalIgnoreCase) ||
                                     component.Equals("inner-sphere", StringComparison.OrdinalIgnoreCase));

    private static string Hash(ReadOnlySpan<byte> bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
}
