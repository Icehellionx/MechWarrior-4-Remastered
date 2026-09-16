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
    private readonly MediaSourceSessionFactory sessions;

    public MediaSourceInspector()
        : this(new MediaInspectionService(), new MediaSourceSessionFactory())
    {
    }

    public MediaSourceInspector(
        MediaInspectionService inspection,
        OwnedIsoMediaSessionFactory isoSessions,
        IsoArchiveExtractor archives)
        : this(inspection, new MediaSourceSessionFactory(isoSessions, archives))
    {
    }

    public MediaSourceInspector(MediaInspectionService inspection, MediaSourceSessionFactory sessions)
    {
        this.inspection = inspection ?? throw new ArgumentNullException(nameof(inspection));
        this.sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
    }

    public MediaSourceInspection Inspect(string sourcePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        using var session = sessions.Open(sourcePath);
        var items = session.Items
            .Select(item => new InspectedMediaItem(item.ArchiveRelativePath, inspection.InspectDirectory(item.RootPath)))
            .ToArray();
        return new MediaSourceInspection(session.Kind, items, session.ExcludedArchiveEntries);
    }
}
