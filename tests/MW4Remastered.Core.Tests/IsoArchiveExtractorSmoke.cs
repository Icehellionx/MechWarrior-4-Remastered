using System.IO.Compression;
using MW4Remastered.Core.Media;

internal static class IsoArchiveExtractorSmoke
{
    public static void Run(List<string> failures)
    {
        var root = Path.Combine(Path.GetTempPath(), "mw4-iso-archive-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(root);
            var archive = Path.Combine(root, "media.zip");
            using (var stream = File.Create(archive))
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                Write(zip, "Media/Disc 1.iso", "disc one");
                Write(zip, "Media/Disc 2.ISO", "disc two");
                Write(zip, "Media/Serial.txt", "must stay excluded");
            }

            var destination = Path.Combine(root, "extracted");
            var result = new IsoArchiveExtractor().Extract(archive, destination);
            Check(result.IsoRelativePaths.Count == 2, "ISO archive extractor returns both ISO entries", failures);
            Check(result.ExcludedEntries.SequenceEqual(new[] { "Media/Serial.txt" }), "ISO archive extractor reports non-ISO entries without extracting them", failures);
            Check(File.Exists(Path.Combine(destination, "Media", "Disc 1.iso")) && !File.Exists(Path.Combine(destination, "Media", "Serial.txt")), "ISO archive extractor commits only ISO files", failures);

            var unsafeArchive = Path.Combine(root, "unsafe.zip");
            using (var stream = File.Create(unsafeArchive))
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                Write(zip, "../escape.iso", "escape");
            }
            var rejected = false;
            try
            {
                new IsoArchiveExtractor().Extract(unsafeArchive, Path.Combine(root, "unsafe-output"));
            }
            catch (InvalidDataException)
            {
                rejected = true;
            }
            Check(rejected && !Directory.Exists(Path.Combine(root, "unsafe-output")), "ISO archive extractor rejects traversal before extraction", failures);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
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
}
