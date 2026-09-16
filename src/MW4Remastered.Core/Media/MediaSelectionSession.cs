namespace MW4Remastered.Core.Media;

public sealed record ReopenedMediaLayout(
    MediaLayoutDefinition Layout,
    string SourcePath,
    string? ArchiveRelativePath,
    string RootPath);

public interface IMediaSelectionSession : IDisposable
{
    IReadOnlyDictionary<string, ReopenedMediaLayout> Layouts { get; }
    string GetRoot(string layoutId);
}

public sealed class MediaSelectionSessionFactory
{
    private readonly MediaSourceSessionFactory sourceSessions;
    private readonly MediaInspectionService inspection;

    public MediaSelectionSessionFactory(MediaSourceSessionFactory sourceSessions, MediaInspectionService inspection)
    {
        this.sourceSessions = sourceSessions ?? throw new ArgumentNullException(nameof(sourceSessions));
        this.inspection = inspection ?? throw new ArgumentNullException(nameof(inspection));
    }

    public IMediaSelectionSession Open(MediaSelectionSnapshot selection, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(selection);
        cancellationToken.ThrowIfCancellationRequested();
        if (selection.Layouts.Count == 0) throw new InvalidOperationException("No recognized media has been selected.");

        var owned = new List<IMediaSourceSession>();
        var reopened = new Dictionary<string, ReopenedMediaLayout>(StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (var sourceGroup in selection.Layouts.GroupBy(item => item.SourcePath, StringComparer.OrdinalIgnoreCase))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var sourceSession = sourceSessions.Open(sourceGroup.Key, cancellationToken);
                owned.Add(sourceSession);
                cancellationToken.ThrowIfCancellationRequested();
                var available = InspectOpenItems(sourceSession, cancellationToken);
                foreach (var expected in sourceGroup)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var matchingItem = available.SingleOrDefault(item =>
                        string.Equals(item.ArchiveRelativePath, expected.ArchiveRelativePath, StringComparison.OrdinalIgnoreCase));
                    if (matchingItem is null || !string.Equals(matchingItem.Recognition.Layout?.Id, expected.Layout.Id, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidDataException($"Selected media changed or no longer matches {expected.Layout.DisplayName}: {expected.SourcePath}");
                    }
                    reopened.Add(expected.Layout.Id, new ReopenedMediaLayout(
                        expected.Layout,
                        expected.SourcePath,
                        expected.ArchiveRelativePath,
                        matchingItem.RootPath));
                }
            }
            return new MediaSelectionSession(reopened, owned);
        }
        catch
        {
            DisposeAll(owned);
            throw;
        }
    }

    private IReadOnlyList<ReopenedItem> InspectOpenItems(IMediaSourceSession sourceSession, CancellationToken cancellationToken)
    {
        var items = new List<ReopenedItem>(sourceSession.Items.Count);
        foreach (var item in sourceSession.Items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            items.Add(new ReopenedItem(
                item.ArchiveRelativePath,
                item.RootPath,
                inspection.InspectDirectory(item.RootPath)));
        }
        return items;
    }

    private static void DisposeAll(IEnumerable<IMediaSourceSession> sessions)
    {
        Exception? firstError = null;
        foreach (var session in sessions.Reverse())
        {
            try
            {
                session.Dispose();
            }
            catch (Exception error) when (error is IOException or InvalidOperationException or UnauthorizedAccessException)
            {
                firstError ??= error;
            }
        }
        if (firstError is not null) throw firstError;
    }

    private sealed record ReopenedItem(
        string? ArchiveRelativePath,
        string RootPath,
        MediaRecognitionResult Recognition);

    private sealed class MediaSelectionSession(
        IReadOnlyDictionary<string, ReopenedMediaLayout> layouts,
        IReadOnlyList<IMediaSourceSession> ownedSessions) : IMediaSelectionSession
    {
        private bool disposed;

        public IReadOnlyDictionary<string, ReopenedMediaLayout> Layouts { get; } = layouts;

        public string GetRoot(string layoutId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(layoutId);
            if (!Layouts.TryGetValue(layoutId, out var media))
            {
                throw new KeyNotFoundException($"Selected media does not include required layout: {layoutId}");
            }
            return media.RootPath;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            DisposeAll(ownedSessions);
        }
    }
}
