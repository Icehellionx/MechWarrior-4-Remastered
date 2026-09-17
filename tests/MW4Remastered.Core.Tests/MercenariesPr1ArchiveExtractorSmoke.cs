using System.IO.Compression;
using MW4Remastered.Core.Media;

internal static class MercenariesPr1ArchiveExtractorSmoke
{
    public static void Run(List<string> failures)
    {
        var root = Path.Combine(Path.GetTempPath(), "mw4-merc-pr1-archive-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var unrelated = Path.Combine(root, "ordinary-media.zip");
            using (var zip = ZipFile.Open(unrelated, ZipArchiveMode.Create))
            {
                using var writer = new StreamWriter(zip.CreateEntry("game.iso").Open());
                writer.Write("not an update");
            }
            var unrelatedResult = new MercenariesPr1ArchiveExtractor().TryExtract(
                unrelated, Path.Combine(root, "unrelated-output"), out var ignored);
            Check(!unrelatedResult && ignored is null && !Directory.Exists(Path.Combine(root, "unrelated-output")),
                "Mercenaries PR1 intake leaves ordinary ISO archives to the normal media extractor", failures);

            var counterfeit = Path.Combine(root, "counterfeit-fix.zip");
            using (var zip = ZipFile.Open(counterfeit, ZipArchiveMode.Create))
            {
                var entry = zip.CreateEntry("MW4 - NoCD/mercpr1.exe", CompressionLevel.Fastest);
                using var stream = entry.Open();
                var block = new byte[1024 * 1024];
                var remaining = 5_376_000;
                while (remaining > 0)
                {
                    var count = Math.Min(block.Length, remaining);
                    stream.Write(block, 0, count);
                    remaining -= count;
                }
            }

            var rejected = false;
            try
            {
                new MercenariesPr1ArchiveExtractor().TryExtract(
                    counterfeit, Path.Combine(root, "counterfeit-output"), out _);
            }
            catch (InvalidDataException)
            {
                rejected = true;
            }
            Check(rejected && !Directory.Exists(Path.Combine(root, "counterfeit-output")),
                "Mercenaries PR1 intake rejects a same-size counterfeit and removes scratch", failures);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static void Check(bool condition, string message, List<string> failures)
    {
        if (!condition) failures.Add("FAIL: " + message);
    }
}
