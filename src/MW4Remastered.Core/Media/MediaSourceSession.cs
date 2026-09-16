namespace MW4Remastered.Core.Media;

public sealed record OpenMediaItem(string? ArchiveRelativePath, string RootPath);

public interface IMediaSourceSession : IDisposable
{
    MediaSourceKind Kind { get; }
    IReadOnlyList<OpenMediaItem> Items { get; }
    IReadOnlyList<string> ExcludedArchiveEntries { get; }
}

public sealed class MediaSourceSessionFactory
{
    private readonly OwnedIsoMediaSessionFactory isoSessions;
    private readonly IsoArchiveExtractor archives;

    public MediaSourceSessionFactory()
        : this(new OwnedIsoMediaSessionFactory(new PowerShellDiskImageBackend()), new IsoArchiveExtractor())
    {
    }

    public MediaSourceSessionFactory(OwnedIsoMediaSessionFactory isoSessions, IsoArchiveExtractor archives)
    {
        this.isoSessions = isoSessions ?? throw new ArgumentNullException(nameof(isoSessions));
        this.archives = archives ?? throw new ArgumentNullException(nameof(archives));
    }

    public IMediaSourceSession Open(string sourcePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        var source = Path.GetFullPath(sourcePath);
        if (Directory.Exists(source))
        {
            return new MediaSourceSession(
                MediaSourceKind.Directory,
                new[] { new OpenMediaItem(null, source) },
                Array.Empty<string>(),
                Array.Empty<IDisposable>(),
                scratchRoot: null);
        }
        if (!File.Exists(source)) throw new FileNotFoundException("Media source does not exist.", source);

        if (string.Equals(Path.GetExtension(source), ".iso", StringComparison.OrdinalIgnoreCase))
        {
            var session = isoSessions.Open(source, cancellationToken);
            return new MediaSourceSession(
                MediaSourceKind.Iso,
                new[] { new OpenMediaItem(null, session.RootPath) },
                Array.Empty<string>(),
                new[] { session },
                scratchRoot: null);
        }
        if (string.Equals(Path.GetExtension(source), ".zip", StringComparison.OrdinalIgnoreCase))
        {
            return OpenArchive(source, cancellationToken);
        }
        throw new InvalidDataException("Media source must be a directory, ISO, or ZIP containing ISO files.");
    }

    private IMediaSourceSession OpenArchive(string archivePath, CancellationToken cancellationToken)
    {
        var scratch = Path.Combine(Path.GetTempPath(), "mw4-media-session-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(scratch);
        var owned = new List<IDisposable>();
        try
        {
            var extraction = archives.Extract(archivePath, Path.Combine(scratch, "media"), cancellationToken);
            var items = new List<OpenMediaItem>(extraction.IsoRelativePaths.Count);
            foreach (var relativePath in extraction.IsoRelativePaths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var image = Path.Combine(extraction.DestinationRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
                var session = isoSessions.Open(image, cancellationToken);
                owned.Add(session);
                cancellationToken.ThrowIfCancellationRequested();
                items.Add(new OpenMediaItem(relativePath, session.RootPath));
            }
            return new MediaSourceSession(MediaSourceKind.Zip, items, extraction.ExcludedEntries, owned, scratch);
        }
        catch
        {
            try
            {
                DisposeAll(owned);
            }
            finally
            {
                if (Directory.Exists(scratch)) Directory.Delete(scratch, true);
            }
            throw;
        }
    }

    private static void DisposeAll(IEnumerable<IDisposable> sessions)
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

    private sealed class MediaSourceSession(
        MediaSourceKind kind,
        IReadOnlyList<OpenMediaItem> items,
        IReadOnlyList<string> excludedArchiveEntries,
        IReadOnlyList<IDisposable> ownedSessions,
        string? scratchRoot) : IMediaSourceSession
    {
        private bool disposed;

        public MediaSourceKind Kind { get; } = kind;
        public IReadOnlyList<OpenMediaItem> Items { get; } = items;
        public IReadOnlyList<string> ExcludedArchiveEntries { get; } = excludedArchiveEntries;

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

            Exception? firstError = null;
            try
            {
                DisposeAll(ownedSessions);
            }
            catch (Exception error) when (error is IOException or InvalidOperationException or UnauthorizedAccessException)
            {
                firstError = error;
            }

            try
            {
                if (scratchRoot is not null && Directory.Exists(scratchRoot)) Directory.Delete(scratchRoot, true);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                firstError ??= error;
            }

            if (firstError is not null) throw firstError;
        }
    }
}
