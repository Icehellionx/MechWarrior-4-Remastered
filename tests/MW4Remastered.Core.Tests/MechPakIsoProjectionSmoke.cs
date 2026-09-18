using System.IO.Compression;
using DiscUtils.Iso9660;
using MW4Remastered.Core.Media;

internal static class MechPakIsoProjectionSmoke
{
    public static void Run(List<string> failures)
    {
        var root = Path.Combine(Path.GetTempPath(), "mw4-pack-projection-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(root);
            using var image = BuildImage();
            var archive = Path.Combine(root, "pack.zip");
            using (var zip = ZipFile.Open(archive, ZipArchiveMode.Create))
            {
                var entry = zip.CreateEntry("media/pack.iso", CompressionLevel.Optimal);
                using (var output = entry.Open()) image.CopyTo(output);
                using var serial = new StreamWriter(zip.CreateEntry("Serial.txt").Open());
                serial.Write("must not be extracted");
            }

            var destination = Path.Combine(root, "projection");
            var projected = new MechPakIsoProjection().TryProjectArchive(
                archive, destination, out var items, out var excluded);
            Check(projected && items.Count == 1, "Mech Pak ZIP is projected without mounting its ISO", failures);
            Check(excluded.SequenceEqual(new[] { "Serial.txt" }), "outer non-media files remain excluded", failures);
            Check(File.Exists(Path.Combine(destination, "MPISETUP.DLL")) &&
                File.Exists(Path.Combine(destination, "GOODIES", "PATCH3", "MW4P3", "ENGLISH", "MW4.RTP")),
                "projection retains required pack and official patch inputs", failures);
            Check(!File.Exists(Path.Combine(destination, "SETUP.EXE")) &&
                !File.Exists(Path.Combine(destination, "SECDRV.SYS")),
                "projection never materializes legacy setup or DRM files", failures);
            var recognition = new MediaInspectionService().InspectDirectory(destination);
            Check(recognition.Layout?.Id == "inner-sphere-mech-pak" && recognition.Status == MediaRecognitionStatus.Recognized,
                "projected payload remains recognizable as pack media", failures);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static MemoryStream BuildImage()
    {
        var builder = new CDBuilder { UseJoliet = true, VolumeIdentifier = "MW4PACK" };
        Add(builder, "MPISETUP.DLL", "marker");
        Add(builder, "RESOURCE/MAPS/COLSM01.MW4", "map-one");
        Add(builder, "RESOURCE/MAPS/GAGE.MW4", "map-two");
        Add(builder, "GOODIES/PATCH3/MW4P3/ENGLISH/MW4.RTP", "patch");
        Add(builder, "GOODIES/PATCH3/MW4P3/PATCHW32.DLL", "engine");
        Add(builder, "SETUP.EXE", "legacy setup must stay inside the image");
        Add(builder, "SECDRV.SYS", "legacy DRM must stay inside the image");
        var stream = new MemoryStream();
        builder.Build(stream);
        stream.Position = 0;
        return stream;
    }

    private static void Add(CDBuilder builder, string path, string contents) =>
        builder.AddFile(path.Replace('/', '\\'), new MemoryStream(System.Text.Encoding.UTF8.GetBytes(contents)));

    private static void Check(bool condition, string message, List<string> failures)
    {
        if (!condition) failures.Add("FAIL: " + message);
    }
}
