using System.IO.Compression;
using MW4Remastered.Core.Media;

internal static class MediaSourceInspectorSmoke
{
    public static void Run(List<string> failures)
    {
        var root = Path.Combine(Path.GetTempPath(), "mw4-media-source-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var mountedRoot = Path.Combine(root, "mounted");
            foreach (var path in MediaCatalog.Layouts.Single(item => item.Id == "black-knight-disc-1").RequiredPaths)
            {
                Write(mountedRoot, path, "fixture");
            }
            var backend = new RootDiskImageBackend(mountedRoot);
            var inspector = new MediaSourceInspector(
                new MediaInspectionService(), new OwnedIsoMediaSessionFactory(backend), new IsoArchiveExtractor());

            var directory = inspector.Inspect(mountedRoot);
            Check(directory.Kind == MediaSourceKind.Directory && directory.Items.Single().Recognition.Layout?.Id == "black-knight-disc-1", "media source inspector recognizes directory inputs", failures);

            var image = Write(root, "disc.iso", "image fixture");
            var iso = inspector.Inspect(image);
            Check(iso.Kind == MediaSourceKind.Iso && iso.Items.Single().Recognition.Layout?.Id == "black-knight-disc-1" && backend.DismountCount == 1, "media source inspector owns ISO recognition lifetime", failures);

            var archive = Path.Combine(root, "media.zip");
            using (var stream = File.Create(archive))
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                Write(zip, "Disc One.iso", "one");
                Write(zip, "Disc Two.iso", "two");
                Write(zip, "Serial.txt", "excluded");
            }
            var scratchBefore = Directory.EnumerateDirectories(Path.GetTempPath(), "mw4-media-inspection-*").ToHashSet(StringComparer.OrdinalIgnoreCase);
            var zipped = inspector.Inspect(archive);
            var scratchAfter = Directory.EnumerateDirectories(Path.GetTempPath(), "mw4-media-inspection-*").ToHashSet(StringComparer.OrdinalIgnoreCase);
            Check(zipped.Kind == MediaSourceKind.Zip && zipped.Items.Count == 2 && zipped.ExcludedArchiveEntries.SequenceEqual(new[] { "Serial.txt" }), "media source inspector composes ZIP extraction and ISO recognition", failures);
            Check(scratchAfter.SetEquals(scratchBefore) && backend.DismountCount == 3, "media source inspector cleans ZIP scratch and every owned mount", failures);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

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

    private sealed class RootDiskImageBackend(string rootPath) : IDiskImageBackend
    {
        public int DismountCount { get; private set; }
        public bool IsAttached(string imagePath) => false;
        public string Mount(string imagePath) => rootPath;
        public void Dismount(string imagePath) => DismountCount++;
    }
}
