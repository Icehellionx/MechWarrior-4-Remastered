using System.IO.Compression;
using MW4Remastered.Core.Media;

internal static class MediaSourceSessionSmoke
{
    public static void Run(List<string> failures)
    {
        var root = Path.Combine(Path.GetTempPath(), "mw4-media-session-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(root);
            var mountedRoot = Path.Combine(root, "mounted");
            Directory.CreateDirectory(mountedRoot);
            var backend = new TrackingDiskImageBackend(mountedRoot);
            var factory = new MediaSourceSessionFactory(new OwnedIsoMediaSessionFactory(backend), new IsoArchiveExtractor());

            using (var directory = factory.Open(mountedRoot))
            {
                Check(directory.Kind == MediaSourceKind.Directory && directory.Items.Single().RootPath == Path.GetFullPath(mountedRoot),
                    "media session exposes directory inputs without mounting", failures);
            }
            Check(backend.MountCount == 0, "directory media session owns no image", failures);

            var image = Write(root, "disc.iso", "fixture");
            using (var iso = factory.Open(image))
            {
                Check(iso.Kind == MediaSourceKind.Iso && backend.MountCount == 1 && backend.DismountCount == 0,
                    "ISO remains mounted for the media session lifetime", failures);
            }
            Check(backend.DismountCount == 1, "ISO dismounts when its media session ends", failures);

            var archive = Path.Combine(root, "collection.zip");
            using (var stream = File.Create(archive))
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                Write(zip, "One.iso", "one");
                Write(zip, "Two.iso", "two");
                Write(zip, "Serial.txt", "excluded");
            }
            var scratchBefore = SessionScratch();
            string[] openRoots;
            using (var zipped = factory.Open(archive))
            {
                openRoots = zipped.Items.Select(item => item.RootPath).ToArray();
                Check(zipped.Kind == MediaSourceKind.Zip && zipped.Items.Count == 2 && zipped.ExcludedArchiveEntries.SequenceEqual(new[] { "Serial.txt" }),
                    "ZIP session exposes every ISO and reports non-ISO entries", failures);
                Check(SessionScratch().Except(scratchBefore, StringComparer.OrdinalIgnoreCase).Count() == 1,
                    "ZIP extraction stays owned while the media session is open", failures);
                Check(openRoots.All(Directory.Exists), "ZIP-mounted roots stay usable for the session lifetime", failures);
            }
            Check(backend.DismountCount == 3, "ZIP media session dismounts all owned images", failures);
            Check(SessionScratch().SetEquals(scratchBefore), "ZIP media session deletes owned extraction scratch", failures);

            var failingBackend = new TrackingDiskImageBackend(mountedRoot, failMountNumber: 2);
            var failingFactory = new MediaSourceSessionFactory(new OwnedIsoMediaSessionFactory(failingBackend), new IsoArchiveExtractor());
            var failureScratchBefore = SessionScratch();
            var rejected = false;
            try
            {
                using var unused = failingFactory.Open(archive);
            }
            catch (InvalidOperationException)
            {
                rejected = true;
            }
            Check(rejected && failingBackend.DismountCount == 2, "partial ZIP mount failure cleans the attempted mount and earlier owned images", failures);
            Check(SessionScratch().SetEquals(failureScratchBefore), "partial ZIP mount failure removes extraction scratch", failures);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    private static HashSet<string> SessionScratch() =>
        Directory.EnumerateDirectories(Path.GetTempPath(), "mw4-media-session-*").ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static string Write(string root, string relativePath, string contents)
    {
        var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, contents);
        return path;
    }

    private static void Write(ZipArchive zip, string path, string contents)
    {
        using var writer = new StreamWriter(zip.CreateEntry(path, CompressionLevel.NoCompression).Open());
        writer.Write(contents);
    }

    private static void Check(bool condition, string message, List<string> failures)
    {
        if (!condition) failures.Add("FAIL: " + message);
    }

    private sealed class TrackingDiskImageBackend(string rootPath, int? failMountNumber = null) : IDiskImageBackend
    {
        public int MountCount { get; private set; }
        public int DismountCount { get; private set; }
        public bool IsAttached(string imagePath) => false;

        public string Mount(string imagePath)
        {
            MountCount++;
            if (MountCount == failMountNumber) throw new InvalidOperationException("Injected mount failure.");
            return rootPath;
        }

        public void Dismount(string imagePath) => DismountCount++;
    }
}
