namespace MW4Remastered.Core.Media;

public enum MediaSourceKind
{
    Directory,
    Iso,
    Zip,
}

public sealed record InspectedMediaItem(
    string? ArchiveRelativePath,
    MediaRecognitionResult Recognition);

public sealed record MediaSourceInspection(
    MediaSourceKind Kind,
    IReadOnlyList<InspectedMediaItem> Items,
    IReadOnlyList<string> ExcludedArchiveEntries);

public sealed class MediaSourceInspector
{
    private readonly MediaInspectionService inspection;
    private readonly OwnedIsoMediaSessionFactory isoSessions;
    private readonly IsoArchiveExtractor archives;

    public MediaSourceInspector()
        : this(
            new MediaInspectionService(),
            new OwnedIsoMediaSessionFactory(new PowerShellDiskImageBackend()),
            new IsoArchiveExtractor())
    {
    }

    public MediaSourceInspector(
        MediaInspectionService inspection,
        OwnedIsoMediaSessionFactory isoSessions,
        IsoArchiveExtractor archives)
    {
        this.inspection = inspection ?? throw new ArgumentNullException(nameof(inspection));
        this.isoSessions = isoSessions ?? throw new ArgumentNullException(nameof(isoSessions));
        this.archives = archives ?? throw new ArgumentNullException(nameof(archives));
    }

    public MediaSourceInspection Inspect(string sourcePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        var source = Path.GetFullPath(sourcePath);
        if (Directory.Exists(source))
        {
            return new MediaSourceInspection(
                MediaSourceKind.Directory,
                new[] { new InspectedMediaItem(null, inspection.InspectDirectory(source)) },
                Array.Empty<string>());
        }
        if (!File.Exists(source)) throw new FileNotFoundException("Media source does not exist.", source);

        if (string.Equals(Path.GetExtension(source), ".iso", StringComparison.OrdinalIgnoreCase))
        {
            using var session = isoSessions.Open(source);
            return new MediaSourceInspection(
                MediaSourceKind.Iso,
                new[] { new InspectedMediaItem(null, inspection.InspectDirectory(session.RootPath)) },
                Array.Empty<string>());
        }
        if (string.Equals(Path.GetExtension(source), ".zip", StringComparison.OrdinalIgnoreCase))
        {
            return InspectArchive(source);
        }
        throw new InvalidDataException("Media source must be a directory, ISO, or ZIP containing ISO files.");
    }

    private MediaSourceInspection InspectArchive(string archivePath)
    {
        var scratch = Path.Combine(Path.GetTempPath(), "mw4-media-inspection-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(scratch);
        try
        {
            var extraction = archives.Extract(archivePath, Path.Combine(scratch, "media"));
            var items = new List<InspectedMediaItem>();
            foreach (var relativePath in extraction.IsoRelativePaths)
            {
                var image = Path.Combine(extraction.DestinationRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
                using var session = isoSessions.Open(image);
                items.Add(new InspectedMediaItem(relativePath, inspection.InspectDirectory(session.RootPath)));
            }
            return new MediaSourceInspection(MediaSourceKind.Zip, items, extraction.ExcludedEntries);
        }
        finally
        {
            if (Directory.Exists(scratch)) Directory.Delete(scratch, true);
        }
    }
}
