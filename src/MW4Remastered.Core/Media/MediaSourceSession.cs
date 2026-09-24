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
    private readonly MercenariesPr1ArchiveExtractor mercenariesPr1;
    private readonly MechPakIsoProjection mechPakProjection;

    public MediaSourceSessionFactory()
        : this(new OwnedIsoMediaSessionFactory(new PowerShellDiskImageBackend()), new IsoArchiveExtractor(), new MercenariesPr1ArchiveExtractor(), new MechPakIsoProjection())
    {
    }

    public MediaSourceSessionFactory(
        OwnedIsoMediaSessionFactory isoSessions,
        IsoArchiveExtractor archives,
        MercenariesPr1ArchiveExtractor? mercenariesPr1 = null,
        MechPakIsoProjection? mechPakProjection = null)
    {
        this.isoSessions = isoSessions ?? throw new ArgumentNullException(nameof(isoSessions));
        this.archives = archives ?? throw new ArgumentNullException(nameof(archives));
        this.mercenariesPr1 = mercenariesPr1 ?? new MercenariesPr1ArchiveExtractor();
        this.mechPakProjection = mechPakProjection ?? new MechPakIsoProjection();
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
            var scratch = Path.Combine(Path.GetTempPath(), "mw4-media-session-" + Guid.NewGuid().ToString("N"));
            try
            {
                if (mechPakProjection.TryProjectImage(source, Path.Combine(scratch, "mech-pak"), cancellationToken))
                {
                    return new MediaSourceSession(
                        MediaSourceKind.Iso,
                        new[] { new OpenMediaItem(null, Path.Combine(scratch, "mech-pak")) },
                        Array.Empty<string>(),
                        Array.Empty<IDisposable>(),
                        scratch);
                }
            }
            catch
            {
                if (Directory.Exists(scratch)) Directory.Delete(scratch, recursive: true);
                throw;
            }
            if (Directory.Exists(scratch)) Directory.Delete(scratch, recursive: true);
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
        if (string.Equals(Path.GetExtension(source), ".cue", StringComparison.OrdinalIgnoreCase))
        {
            return OpenCueBin(source, cancellationToken);
        }
        throw new InvalidDataException("Media source must be a directory, ISO, CUE with its sibling BIN, or ZIP containing ISO files.");
    }

    private IMediaSourceSession OpenCueBin(string cuePath, CancellationToken cancellationToken)
    {
        var scratch = Path.Combine(Path.GetTempPath(), "mw4-media-session-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(scratch);
        OwnedIsoMediaSession? mounted = null;
        try
        {
            var projected = Path.Combine(scratch, "mech-pak");
            using (var candidate = new CueMode1DataStream(cuePath))
            {
                if (mechPakProjection.TryProject(candidate, projected, cancellationToken))
                {
                    return new MediaSourceSession(
                        MediaSourceKind.CueBin,
                        [new OpenMediaItem(null, projected)],
                        Array.Empty<string>(),
                        Array.Empty<IDisposable>(),
                        scratch);
                }
            }

            using var data = new CueMode1DataStream(cuePath);
            var scratchVolume = new DriveInfo(Path.GetPathRoot(scratch)!);
            if (scratchVolume.AvailableFreeSpace < checked(data.Length + 128L * 1024 * 1024))
                throw new IOException("Not enough temporary disk space to inspect this CUE/BIN game disc.");
            var image = Path.Combine(scratch, "disc.iso");
            using (var output = new FileStream(image, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                var buffer = new byte[1024 * 1024];
                int read;
                while ((read = data.Read(buffer)) > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    output.Write(buffer, 0, read);
                }
                if (output.Length != data.Length)
                    throw new InvalidDataException("CUE/BIN data length changed during ISO conversion.");
            }
            cancellationToken.ThrowIfCancellationRequested();
            mounted = isoSessions.Open(image, cancellationToken);
            var result = new MediaSourceSession(
                MediaSourceKind.CueBin,
                [new OpenMediaItem(null, mounted.RootPath)],
                Array.Empty<string>(),
                [mounted],
                scratch);
            mounted = null;
            return result;
        }
        catch
        {
            try
            {
                mounted?.Dispose();
            }
            finally
            {
                if (Directory.Exists(scratch)) Directory.Delete(scratch, recursive: true);
            }
            throw;
        }
    }

    private IMediaSourceSession OpenArchive(string archivePath, CancellationToken cancellationToken)
    {
        var scratch = Path.Combine(Path.GetTempPath(), "mw4-media-session-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(scratch);
        var owned = new List<IDisposable>();
        try
        {
            if (mercenariesPr1.TryExtract(
                    archivePath,
                    Path.Combine(scratch, "mercenaries-pr1"),
                    out var update,
                    cancellationToken))
            {
                return new MediaSourceSession(
                    MediaSourceKind.Zip,
                    new[] { new OpenMediaItem(null, update!.DestinationRoot) },
                    update.ExcludedEntries,
                    owned,
                    scratch);
            }

            if (mechPakProjection.TryProjectArchive(
                    archivePath,
                    Path.Combine(scratch, "mech-pak"),
                    out var projectedItems,
                    out var projectedExcluded,
                    cancellationToken))
            {
                return new MediaSourceSession(
                    MediaSourceKind.Zip,
                    projectedItems,
                    projectedExcluded,
                    owned,
                    scratch);
            }

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
