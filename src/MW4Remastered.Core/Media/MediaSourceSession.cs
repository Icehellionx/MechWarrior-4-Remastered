using System.Runtime.ExceptionServices;

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
        : this(new OwnedIsoMediaSessionFactory(static () => new PowerShellDiskImageBackend()), new IsoArchiveExtractor(), new MercenariesPr1ArchiveExtractor(), new MechPakIsoProjection())
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
            catch (Exception openError)
            {
                ReleaseAfterFailedOpen(openError, Array.Empty<IDisposable>(), scratch);
                throw;
            }
            DeleteScratch(scratch);
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
        catch (Exception openError)
        {
            ReleaseAfterFailedOpen(openError, mounted is null ? [] : [mounted], scratch);
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
        catch (Exception openError)
        {
            ReleaseAfterFailedOpen(openError, owned, scratch);
            throw;
        }
    }

    private static void ReleaseAfterFailedOpen(Exception openError, IReadOnlyList<IDisposable> sessions, string scratchRoot)
    {
        try
        {
            Release(sessions, scratchRoot);
        }
        catch (Exception cleanupError) when (IsMediaCleanupFailure(cleanupError))
        {
            throw new IOException("Media could not be opened and its temporary files or mounts could not all be released.",
                new AggregateException(openError, cleanupError));
        }
    }

    private static void Release(IReadOnlyList<IDisposable> sessions, string? scratchRoot)
    {
        var failures = new List<Exception>();
        for (var index = sessions.Count - 1; index >= 0; index--)
        {
            try
            {
                sessions[index].Dispose();
            }
            catch (Exception error) when (IsMediaCleanupFailure(error))
            {
                failures.Add(error);
            }
        }

        try
        {
            if (scratchRoot is not null) DeleteScratch(scratchRoot);
        }
        catch (Exception error) when (IsMediaCleanupFailure(error))
        {
            failures.Add(error);
        }

        if (failures.Count == 1) ExceptionDispatchInfo.Throw(failures[0]);
        if (failures.Count > 1)
            throw new IOException("Multiple errors occurred while releasing temporary media.", new AggregateException(failures));
    }

    internal static bool IsMediaCleanupFailure(Exception error) =>
        error is IOException or InvalidDataException or InvalidOperationException or UnauthorizedAccessException;

    private static void DeleteScratch(string scratchRoot)
    {
        const int sharingViolation = unchecked((int)0x80070020);
        const int lockViolation = unchecked((int)0x80070021);
        const int retryCount = 10;

        // Windows can report a disc detached before the backing ISO handle is released.
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                Directory.Delete(scratchRoot, recursive: true);
                return;
            }
            catch (DirectoryNotFoundException)
            {
                return;
            }
            catch (IOException error) when (attempt < retryCount && error.HResult is sharingViolation or lockViolation)
            {
                Thread.Sleep(200);
            }
        }
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

            Release(ownedSessions, scratchRoot);
        }
    }
}
