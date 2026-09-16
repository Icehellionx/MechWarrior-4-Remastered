namespace MW4Remastered.Core.Media;

public interface IDiskImageBackend
{
    bool IsAttached(string imagePath);
    string Mount(string imagePath);
    void Dismount(string imagePath);
}

public sealed class OwnedIsoMediaSessionFactory
{
    private readonly IDiskImageBackend backend;

    public OwnedIsoMediaSessionFactory(IDiskImageBackend backend)
    {
        this.backend = backend ?? throw new ArgumentNullException(nameof(backend));
    }

    public OwnedIsoMediaSession Open(string imagePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(imagePath);
        var image = Path.GetFullPath(imagePath);
        if (!File.Exists(image)) throw new FileNotFoundException("Disc image does not exist.", image);
        if (!string.Equals(Path.GetExtension(image), ".iso", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Only ISO disc images are supported by this media session.");
        }
        if ((File.GetAttributes(image) & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidDataException("Disc image cannot be a reparse point.");
        }
        if (backend.IsAttached(image))
        {
            throw new InvalidOperationException("Refusing to reuse a disc image that this process did not mount.");
        }

        try
        {
            var root = Path.GetFullPath(backend.Mount(image));
            cancellationToken.ThrowIfCancellationRequested();
            if (!Directory.Exists(root)) throw new DirectoryNotFoundException($"Mounted disc root does not exist: {root}");
            if ((File.GetAttributes(root) & FileAttributes.ReparsePoint) != 0)
            {
                throw new InvalidDataException("Mounted disc root cannot be a reparse point.");
            }
            return new OwnedIsoMediaSession(image, root, backend);
        }
        catch (Exception mountError)
        {
            try
            {
                backend.Dismount(image);
            }
            catch (Exception cleanupError)
            {
                throw new AggregateException("Disc image mount failed and cleanup also failed.", mountError, cleanupError);
            }
            throw;
        }
    }
}

public sealed class OwnedIsoMediaSession : IDisposable
{
    private readonly IDiskImageBackend backend;
    private bool disposed;

    internal OwnedIsoMediaSession(string imagePath, string rootPath, IDiskImageBackend backend)
    {
        ImagePath = imagePath;
        RootPath = rootPath;
        this.backend = backend;
    }

    public string ImagePath { get; }
    public string RootPath { get; }

    public void Dispose()
    {
        if (disposed) return;
        backend.Dismount(ImagePath);
        disposed = true;
    }
}
