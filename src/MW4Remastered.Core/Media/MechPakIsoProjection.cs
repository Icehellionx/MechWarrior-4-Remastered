using System.IO.Compression;
using DiscUtils.Iso9660;
using MW4Remastered.Core.Install;

namespace MW4Remastered.Core.Media;

/// <summary>
/// Projects only the qualified files needed from retail Mech Pak images. The
/// legacy setup, autorun, serial, and DRM payloads are never materialized.
/// </summary>
public sealed class MechPakIsoProjection
{
    private const long MaximumImageBytes = 512L * 1024 * 1024;
    private const long MaximumProjectedBytes = 256L * 1024 * 1024;

    private static readonly string[] CommonPaths =
    [
        "GOODIES/PATCH3/MW4P3/PATCHW32.DLL",
        "GOODIES/PATCH3/MW4P3/ENGLISH/MW4.RTP",
        "GOODIES/PATCH3/MW4XP1/PATCHW32.DLL",
        "GOODIES/PATCH3/MW4XP1/ENGLISH/MW4X.RTP",
    ];

    private static readonly IReadOnlyDictionary<string, string[]> PackPaths =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["inner-sphere-mech-pak"] =
            [
                "MPISETUP.DLL",
                "RESOURCE/MAPS/COLSM01.MW4",
                "RESOURCE/MAPS/GAGE.MW4",
                "RESOURCE/MISSIONS/COLISEUM.MW4",
                "RESOURCE/MISSIONS/COLISEUM.NFO",
                "RESOURCE/MISSIONS/COLISEUM.NFX",
                "RESOURCE/MISSIONS/COLISEUM.TGA",
                "RESOURCE/MISSIONS/GAGETOWN.MW4",
                "RESOURCE/MISSIONS/GAGETOWN.NFO",
                "RESOURCE/MISSIONS/GAGETOWN.NFX",
                "RESOURCE/MISSIONS/GAGETOWN.TGA",
            ],
            ["clan-mech-pak"] =
            [
                "MPCSETUP.DLL",
                "RESOURCE/MAPS/FACT01.MW4",
                "RESOURCE/MAPS/NGOTH.MW4",
                "RESOURCE/MISSIONS/FACTORY.MW4",
                "RESOURCE/MISSIONS/FACTORY.NFO",
                "RESOURCE/MISSIONS/FACTORY.NFX",
                "RESOURCE/MISSIONS/FACTORY.TGA",
                "RESOURCE/MISSIONS/NEWGOT_1.MW4",
                "RESOURCE/MISSIONS/NEWGOT_1.NFO",
                "RESOURCE/MISSIONS/NEWGOT_1.NFX",
                "RESOURCE/MISSIONS/NEWGOT_1.TGA",
            ],
        };

    public bool TryProjectArchive(
        string archivePath,
        string destinationRoot,
        out IReadOnlyList<OpenMediaItem> items,
        out IReadOnlyList<string> excludedEntries,
        CancellationToken cancellationToken = default)
    {
        items = Array.Empty<OpenMediaItem>();
        excludedEntries = Array.Empty<string>();
        using var zip = ZipFile.OpenRead(archivePath);
        var isoEntries = zip.Entries
            .Where(entry => !entry.FullName.EndsWith('/') &&
                string.Equals(Path.GetExtension(entry.FullName), ".iso", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (isoEntries.Length != 1) return false;

        var entry = isoEntries[0];
        if (entry.Length <= 0 || entry.Length > MaximumImageBytes) return false;
        cancellationToken.ThrowIfCancellationRequested();
        using var image = new MemoryStream(checked((int)entry.Length));
        using (var source = entry.Open()) Copy(source, image, entry.Length, cancellationToken);
        image.Position = 0;
        if (!TryProject(image, destinationRoot, cancellationToken)) return false;

        var excluded = zip.Entries
            .Where(candidate => !candidate.FullName.EndsWith('/') && !ReferenceEquals(candidate, entry))
            .Select(candidate => candidate.FullName.Replace('\\', '/'))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        items = [new OpenMediaItem(entry.FullName.Replace('\\', '/'), destinationRoot)];
        excludedEntries = excluded;
        return true;
    }

    public bool TryProjectImage(
        string imagePath,
        string destinationRoot,
        CancellationToken cancellationToken = default)
    {
        var info = new FileInfo(imagePath);
        if (!info.Exists || info.Length <= 0 || info.Length > MaximumImageBytes) return false;
        using var image = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return TryProject(image, destinationRoot, cancellationToken);
    }

    internal bool TryProject(
        Stream image,
        string destinationRoot,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!image.CanRead || !image.CanSeek) throw new ArgumentException("Mech Pak image stream must be readable and seekable.", nameof(image));

        CDReader disc;
        try
        {
            disc = new CDReader(image, joliet: true, hideVersions: true);
        }
        catch (Exception error) when (error is IOException or InvalidDataException or NotSupportedException)
        {
            return false;
        }

        using (disc)
        {
            var pack = PackPaths.SingleOrDefault(candidate => candidate.Value
                .Take(3)
                .All(path => disc.FileExists(ToIsoPath(path))));
            if (string.IsNullOrEmpty(pack.Key)) return false;

            var destination = Path.GetFullPath(destinationRoot);
            if (Directory.Exists(destination) || File.Exists(destination))
                throw new IOException($"Mech Pak projection destination already exists: {destination}");
            var staging = destination + ".projection-staging-" + Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(staging);
            try
            {
                long projectedBytes = 0;
                foreach (var relativePath in pack.Value.Concat(CommonPaths).Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var isoPath = ToIsoPath(relativePath);
                    if (!disc.FileExists(isoPath)) continue;
                    var length = disc.GetFileLength(isoPath);
                    if (length < 0 || checked(projectedBytes + length) > MaximumProjectedBytes)
                        throw new InvalidDataException("Mech Pak projection exceeds its bounded payload size.");
                    projectedBytes += length;

                    var output = StagedInstallTransaction.ResolveContainedPath(staging, relativePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(output)!);
                    using var source = disc.OpenFile(isoPath, FileMode.Open, FileAccess.Read);
                    using var target = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                    Copy(source, target, length, cancellationToken);
                    File.SetAttributes(output, FileAttributes.Normal);
                }

                var required = MediaCatalog.Layouts.Single(layout => layout.Id == pack.Key).RequiredPaths;
                var actual = new DirectoryMediaInventory().Read(staging).ToHashSet(StringComparer.OrdinalIgnoreCase);
                if (required.Any(path => !actual.Contains(path))) return false;
                Directory.Move(staging, destination);
                return true;
            }
            finally
            {
                if (Directory.Exists(staging)) Directory.Delete(staging, recursive: true);
            }
        }
    }

    private static string ToIsoPath(string relativePath) => relativePath.Replace('/', '\\');

    private static void Copy(Stream source, Stream target, long expectedLength, CancellationToken cancellationToken)
    {
        var buffer = new byte[1024 * 1024];
        long written = 0;
        int read;
        while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            target.Write(buffer, 0, read);
            written += read;
            if (written > expectedLength) throw new InvalidDataException("Mech Pak image entry exceeded its declared length.");
        }
        if (written != expectedLength) throw new InvalidDataException("Mech Pak image entry did not match its declared length.");
    }
}
