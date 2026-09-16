using System.IO.Compression;
using MW4Remastered.Core.Install;

namespace MW4Remastered.Core.Media;

public sealed record IsoArchiveExtractionResult(
    string DestinationRoot,
    IReadOnlyList<string> IsoRelativePaths,
    IReadOnlyList<string> ExcludedEntries);

public sealed class IsoArchiveExtractor
{
    private const int MaximumIsoCount = 4;
    private const long MaximumIsoBytes = 4L * 1024 * 1024 * 1024;
    private const long MaximumTotalBytes = 8L * 1024 * 1024 * 1024;

    public IsoArchiveExtractionResult Extract(string archivePath, string destinationRoot)
    {
        var archive = Path.GetFullPath(archivePath);
        if (!File.Exists(archive)) throw new FileNotFoundException("Media archive does not exist.", archive);
        if (!string.Equals(Path.GetExtension(archive), ".zip", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Only ZIP media archives are supported.");
        }
        if ((File.GetAttributes(archive) & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidDataException("Media archive cannot be a reparse point.");
        }

        var destination = Path.GetFullPath(destinationRoot);
        if (Directory.Exists(destination) || File.Exists(destination)) throw new IOException($"Archive destination already exists: {destination}");
        var parent = Directory.GetParent(destination)?.FullName ?? throw new InvalidDataException("Archive destination must have a parent directory.");
        Directory.CreateDirectory(parent);

        using var zip = ZipFile.OpenRead(archive);
        var isoEntries = new List<(ZipArchiveEntry Entry, string Path)>();
        var excluded = new List<string>();
        var claimed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        long totalBytes = 0;

        foreach (var entry in zip.Entries)
        {
            var portable = entry.FullName.Replace('\\', '/');
            var isDirectory = portable.EndsWith('/');
            if (!MediaRecognizer.TryNormalizeRelativePath(portable.TrimEnd('/'), out var normalized) || normalized.Length == 0)
            {
                throw new InvalidDataException($"Unsafe ZIP entry: {entry.FullName}");
            }
            if (IsUnixLink(entry)) throw new InvalidDataException($"ZIP link entries are not supported: {entry.FullName}");
            if (isDirectory) continue;
            if (!claimed.Add(normalized)) throw new InvalidDataException($"Duplicate ZIP destination: {normalized}");

            if (!string.Equals(Path.GetExtension(normalized), ".iso", StringComparison.OrdinalIgnoreCase))
            {
                excluded.Add(normalized);
                continue;
            }
            if (entry.Length <= 0 || entry.Length > MaximumIsoBytes)
            {
                throw new InvalidDataException($"ISO entry has an unsupported size: {normalized} ({entry.Length} bytes)");
            }
            totalBytes = checked(totalBytes + entry.Length);
            if (totalBytes > MaximumTotalBytes) throw new InvalidDataException("ZIP ISO payload exceeds the total extraction limit.");
            isoEntries.Add((entry, normalized));
        }

        if (isoEntries.Count == 0) throw new InvalidDataException("ZIP archive contains no ISO files.");
        if (isoEntries.Count > MaximumIsoCount) throw new InvalidDataException($"ZIP archive contains more than {MaximumIsoCount} ISO files.");

        var staging = Path.Combine(parent, $".{Path.GetFileName(destination)}.zip-staging-{Guid.NewGuid():N}");
        Directory.CreateDirectory(staging);
        try
        {
            foreach (var (entry, relativePath) in isoEntries)
            {
                var output = StagedInstallTransaction.ResolveContainedPath(staging, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(output)!);
                using var source = entry.Open();
                using var target = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                source.CopyTo(target);
                if (target.Length != entry.Length) throw new InvalidDataException($"ZIP entry length mismatch after extraction: {relativePath}");
                File.SetAttributes(output, FileAttributes.Normal);
            }

            var actual = new DirectoryMediaInventory().Read(staging);
            var expected = isoEntries.Select(item => item.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (!actual.ToHashSet(StringComparer.OrdinalIgnoreCase).SetEquals(expected))
            {
                throw new InvalidDataException("Extracted ZIP inventory does not match the validated ISO entry set.");
            }
            Directory.Move(staging, destination);
            excluded.Sort(StringComparer.OrdinalIgnoreCase);
            return new IsoArchiveExtractionResult(destination, expected.OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray(), excluded);
        }
        finally
        {
            if (Directory.Exists(staging)) Directory.Delete(staging, true);
        }
    }

    private static bool IsUnixLink(ZipArchiveEntry entry)
    {
        const int UnixFileTypeMask = 0xF000;
        const int UnixSymbolicLink = 0xA000;
        return ((entry.ExternalAttributes >> 16) & UnixFileTypeMask) == UnixSymbolicLink;
    }
}
