using System.IO.Compression;
using System.Security.Cryptography;
using MW4Remastered.Core.Install;

namespace MW4Remastered.Core.Media;

public sealed record MercenariesPr1ExtractionResult(
    string DestinationRoot,
    IReadOnlyList<string> ExcludedEntries);

public sealed class MercenariesPr1ArchiveExtractor
{
    public const string InstallerSha256 = "0c3d0094448e6fe5d2a30fb9ebb24001e8e8c03b39aff9232856bd30000efb30";
    public const string EngineSha256 = "0ec6e25234ad74489eb1890d4de57bb6140bb8196bdc4a5dcac90dd9d16eb2dd";
    public const string PayloadSha256 = "d3ebf1c2dc098a8e55d7cbeb112426fb413dfd23595a1ed078cd35fe7a551a65";

    private const long InstallerLength = 5_376_000;
    private static readonly QualifiedEntry[] RetainedEntries =
    [
        new("Patchw32.dll", 185_344, EngineSha256),
        new("English/MW4MERCS.RTP", 5_119_691, PayloadSha256),
    ];

    public bool TryExtract(
        string archivePath,
        string destinationRoot,
        out MercenariesPr1ExtractionResult? result,
        CancellationToken cancellationToken = default)
    {
        result = null;
        var archive = Path.GetFullPath(archivePath);
        if (!File.Exists(archive) || !Path.GetExtension(archive).Equals(".zip", StringComparison.OrdinalIgnoreCase)) return false;

        using var outer = ZipFile.OpenRead(archive);
        var regular = new List<(ZipArchiveEntry Entry, string Path)>();
        var claimed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in outer.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var portable = entry.FullName.Replace('\\', '/');
            var isDirectory = portable.EndsWith('/');
            if (!MediaRecognizer.TryNormalizeRelativePath(portable.TrimEnd('/'), out var normalized) || normalized.Length == 0)
                throw new InvalidDataException($"Unsafe update ZIP entry: {entry.FullName}");
            if (IsUnixLink(entry)) throw new InvalidDataException($"ZIP link entries are not supported: {entry.FullName}");
            if (isDirectory) continue;
            if (!claimed.Add(normalized)) throw new InvalidDataException($"Duplicate update ZIP entry: {normalized}");
            regular.Add((entry, normalized));
        }
        var candidates = regular.Where(item =>
            Path.GetFileName(item.Path).Equals("mercpr1.exe", StringComparison.OrdinalIgnoreCase) &&
            item.Entry.Length == InstallerLength).ToArray();
        if (candidates.Length == 0) return false;
        if (candidates.Length != 1) throw new InvalidDataException("Mercenaries update archive contains multiple mercpr1.exe candidates.");

        var destination = Path.GetFullPath(destinationRoot);
        if (Directory.Exists(destination) || File.Exists(destination)) throw new IOException($"Update destination already exists: {destination}");
        var parent = Directory.GetParent(destination)?.FullName ?? throw new InvalidDataException("Update destination must have a parent directory.");
        Directory.CreateDirectory(parent);
        var staging = Path.Combine(parent, $".{Path.GetFileName(destination)}.pr1-staging-{Guid.NewGuid():N}");
        Directory.CreateDirectory(staging);
        try
        {
            var installer = Path.Combine(staging, "mercpr1.exe");
            Copy(candidates[0].Entry, installer, cancellationToken);
            RequireHash(installer, InstallerLength, InstallerSha256, "Mercenaries PR1 installer");

            var output = Path.Combine(staging, "payload");
            Directory.CreateDirectory(output);
            using (var nested = ZipFile.OpenRead(installer))
            {
                var byPath = new Dictionary<string, ZipArchiveEntry>(StringComparer.OrdinalIgnoreCase);
                foreach (var entry in nested.Entries)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var portable = entry.FullName.Replace('\\', '/');
                    if (portable.EndsWith('/')) continue;
                    if (!MediaRecognizer.TryNormalizeRelativePath(portable, out var normalized) || normalized.Length == 0)
                        throw new InvalidDataException($"Unsafe Mercenaries PR1 entry: {entry.FullName}");
                    if (!byPath.TryAdd(normalized, entry)) throw new InvalidDataException($"Duplicate Mercenaries PR1 entry: {normalized}");
                }

                foreach (var expected in RetainedEntries)
                {
                    if (!byPath.TryGetValue(expected.Path, out var entry) || entry.Length != expected.Length)
                        throw new InvalidDataException($"Mercenaries PR1 is missing qualified entry: {expected.Path}");
                    var target = StagedInstallTransaction.ResolveContainedPath(output, expected.Path);
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    Copy(entry, target, cancellationToken);
                    RequireHash(target, expected.Length, expected.Sha256, expected.Path);
                }
            }

            File.Delete(installer);
            Directory.Move(output, destination);
            Directory.Delete(staging);
            var excluded = regular.Where(item => !ReferenceEquals(item.Entry, candidates[0].Entry))
                .Select(item => item.Path)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            result = new MercenariesPr1ExtractionResult(destination, excluded);
            return true;
        }
        finally
        {
            if (Directory.Exists(staging)) Directory.Delete(staging, recursive: true);
        }
    }

    private static void Copy(ZipArchiveEntry entry, string destination, CancellationToken cancellationToken)
    {
        using var source = entry.Open();
        using var target = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        var buffer = new byte[1024 * 1024];
        int read;
        while ((read = source.Read(buffer)) > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            target.Write(buffer, 0, read);
        }
    }

    private static void RequireHash(string path, long expectedLength, string expectedSha256, string description)
    {
        var info = new FileInfo(path);
        using var stream = File.OpenRead(path);
        var hash = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
        if (info.Length != expectedLength || !hash.Equals(expectedSha256, StringComparison.Ordinal))
            throw new InvalidDataException($"Unsupported {description} revision.");
    }

    private static bool IsUnixLink(ZipArchiveEntry entry)
    {
        const int UnixFileTypeMask = 0xF000;
        const int UnixSymbolicLink = 0xA000;
        return ((entry.ExternalAttributes >> 16) & UnixFileTypeMask) == UnixSymbolicLink;
    }

    private sealed record QualifiedEntry(string Path, long Length, string Sha256);
}
