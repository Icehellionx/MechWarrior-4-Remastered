using DiscUtils.Iso9660;
using System.IO.Compression;
using MW4Remastered.Core.Media;

internal static class CueMode1MediaSmoke
{
    public static void Run(List<string> failures)
    {
        var root = Path.Combine(Path.GetTempPath(), "mw4-cue-media-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var pack = BuildImage("inner-sphere-mech-pak", includeForbidden: true);
            var packCue = WriteCueBin(root, "Inner Sphere", pack);
            var backend = new TrackingBackend(Path.Combine(root, "mounted"));
            var factory = new MediaSourceSessionFactory(new OwnedIsoMediaSessionFactory(backend), new IsoArchiveExtractor());
            var scratchBefore = Scratch();
            using (var session = factory.Open(packCue))
            {
                var projected = session.Items.Single().RootPath;
                Check(session.Kind == MediaSourceKind.CueBin && backend.MountCount == 0,
                    "CUE Mech Pak projects without mounting or materializing a full ISO", failures);
                Check(new MediaInspectionService().InspectDirectory(projected).Layout?.Id == "inner-sphere-mech-pak",
                    "CUE Mech Pak remains structurally recognizable", failures);
                Check(!File.Exists(Path.Combine(projected, "SETUP.EXE")) &&
                    !File.Exists(Path.Combine(projected, "SECDRV.SYS")),
                    "CUE Mech Pak excludes setup and DRM files", failures);
            }
            Check(Scratch().SetEquals(scratchBefore), "CUE pack scratch is removed after use", failures);

            var game = BuildImage("black-knight-disc-1", includeForbidden: false);
            var gameCue = WriteCueBin(root, "Black Knight", game);
            Directory.CreateDirectory(backend.RootPath);
            foreach (var path in MediaCatalog.Layouts.Single(item => item.Id == "black-knight-disc-1").RequiredPaths)
                Write(backend.RootPath, path, "fixture");
            using (var session = factory.Open(gameCue))
            {
                Check(session.Kind == MediaSourceKind.CueBin && backend.MountCount == 1,
                    "game CUE converts its raw data track for an owned ISO mount", failures);
                Check(new MediaInspectionService().InspectDirectory(session.Items.Single().RootPath).Layout?.Id == "black-knight-disc-1",
                    "game CUE enters existing layout recognition", failures);
                Check(File.ReadAllBytes(backend.LastImagePath!).AsSpan().SequenceEqual(game),
                    "raw MODE1/2352 conversion reproduces every logical ISO byte", failures);
                using (var convertedStream = File.OpenRead(backend.LastImagePath!))
                using (var converted = new CDReader(convertedStream, joliet: true, hideVersions: true))
                {
                    Check(converted.FileExists("MW4X\\MW4X.EXE") && converted.FileExists("RESOURCE\\COREX.MW4"),
                        "converted game ISO remains a readable game-disc filesystem", failures);
                }
            }
            Check(backend.DismountCount == 1 && Scratch().SetEquals(scratchBefore),
                "CUE game mount and conversion scratch are owned and cleaned", failures);

            var missingCue = WriteCue(root, "Missing", "Missing.bin");
            var missing = false;
            try { using var unused = factory.Open(missingCue); }
            catch (FileNotFoundException error)
            {
                missing = error.Message.Contains("matching CUE and BIN", StringComparison.Ordinal);
            }
            Check(missing && Scratch().SetEquals(scratchBefore),
                "CUE without its sibling BIN gives an actionable error and leaves no scratch", failures);

            var escapeCue = WriteCue(root, "Escape", "../outside.bin");
            var escaped = false;
            try { using var unused = factory.Open(escapeCue); }
            catch (InvalidDataException) { escaped = true; }
            Check(escaped && Scratch().SetEquals(scratchBefore),
                "CUE cannot reference a BIN outside its own directory", failures);

            var multiTrackCue = WriteCue(root, "Multi Track", "Multi Track.bin");
            File.AppendAllText(multiTrackCue, "  TRACK 02 AUDIO\n    INDEX 01 01:00:00\n");
            var multiTrackRejected = false;
            try { using var unused = factory.Open(multiTrackCue); }
            catch (InvalidDataException) { multiTrackRejected = true; }
            Check(multiTrackRejected && Scratch().SetEquals(scratchBefore),
                "mixed-mode or multi-track CUEs are rejected before a BIN is opened", failures);

            var corrupt = Path.ChangeExtension(gameCue, ".bin");
            using (var file = new FileStream(corrupt, FileMode.Open, FileAccess.Write))
            {
                file.Position = 15;
                file.WriteByte(2);
            }
            var rejected = false;
            try { using var unused = factory.Open(gameCue); }
            catch (InvalidDataException) { rejected = true; }
            Check(rejected && Scratch().SetEquals(scratchBefore),
                "non-MODE1 sectors are rejected and conversion scratch is cleaned", failures);

            var realPack = Environment.GetEnvironmentVariable("MW4_CUE_MEDIA_SMOKE_ZIP");
            if (!string.IsNullOrWhiteSpace(realPack))
            {
                using var archive = ZipFile.OpenRead(realPack);
                var image = archive.Entries.Single(item =>
                    item.FullName.EndsWith(".iso", StringComparison.OrdinalIgnoreCase));
                if (image.Length <= 0 || image.Length % 2048 != 0)
                    throw new InvalidDataException("Real Mech Pak ISO is not sector aligned.");
                var cue = WriteCue(root, "Real Inner Sphere", "Real Inner Sphere.bin");
                using (var source = image.Open())
                using (var bin = File.Create(Path.ChangeExtension(cue, ".bin")))
                {
                    var raw = new byte[2352];
                    raw.AsSpan(1, 10).Fill(0xff);
                    raw[15] = 1;
                    for (long offset = 0; offset < image.Length; offset += 2048)
                    {
                        source.ReadExactly(raw.AsSpan(16, 2048));
                        bin.Write(raw);
                    }
                }
                var inspection = new MediaSourceInspector().Inspect(cue);
                Check(inspection.Kind == MediaSourceKind.CueBin &&
                    inspection.Items.Single().Recognition.Layout?.Id == "inner-sphere-mech-pak",
                    "real supplied Inner Sphere ISO round-trips through raw CUE/BIN intake", failures);
                Check(Scratch().SetEquals(scratchBefore),
                    "real CUE/BIN projection leaves no media scratch", failures);
            }

            var externalCue = Environment.GetEnvironmentVariable("MW4_CUE_MEDIA_SMOKE_CUE");
            if (!string.IsNullOrWhiteSpace(externalCue))
            {
                var inspection = new MediaSourceInspector().Inspect(externalCue);
                Check(inspection.Kind == MediaSourceKind.CueBin &&
                    inspection.Items.Single().Recognition.Layout?.Id == "inner-sphere-mech-pak",
                    "external Inner Sphere CUE/BIN is recognized before packaged installation", failures);
                Check(Scratch().SetEquals(scratchBefore),
                    "external CUE/BIN inspection cleans media scratch", failures);
            }
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static byte[] BuildImage(string layoutId, bool includeForbidden)
    {
        var builder = new CDBuilder { UseJoliet = true, VolumeIdentifier = "MW4TEST" };
        foreach (var path in MediaCatalog.Layouts.Single(item => item.Id == layoutId).RequiredPaths)
            builder.AddFile(path.Replace('/', '\\'), new MemoryStream("fixture"u8.ToArray()));
        if (includeForbidden)
        {
            builder.AddFile("SETUP.EXE", new MemoryStream("legacy setup"u8.ToArray()));
            builder.AddFile("SECDRV.SYS", new MemoryStream("legacy DRM"u8.ToArray()));
        }
        using var image = new MemoryStream();
        builder.Build(image);
        return image.ToArray();
    }

    private static string WriteCueBin(string root, string name, byte[] iso)
    {
        if (iso.Length % 2048 != 0) throw new InvalidDataException("Synthetic ISO is not sector aligned.");
        var cue = WriteCue(root, name, name + ".bin");
        using var bin = File.Create(Path.ChangeExtension(cue, ".bin"));
        var sector = new byte[2352];
        for (var offset = 0; offset < iso.Length; offset += 2048)
        {
            sector.AsSpan().Clear();
            sector.AsSpan(1, 10).Fill(0xff);
            sector[15] = 1;
            iso.AsSpan(offset, 2048).CopyTo(sector.AsSpan(16, 2048));
            bin.Write(sector);
        }
        return cue;
    }

    private static string WriteCue(string root, string name, string binName)
    {
        var cue = Path.Combine(root, name + ".cue");
        File.WriteAllText(cue, $"FILE \"{binName}\" BINARY\n  TRACK 01 MODE1/2352\n    INDEX 01 00:00:00\n");
        return cue;
    }

    private static string Write(string root, string relativePath, string content)
    {
        var file = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, content);
        return file;
    }

    private static HashSet<string> Scratch() =>
        Directory.EnumerateDirectories(Path.GetTempPath(), "mw4-media-session-*")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static void Check(bool condition, string message, List<string> failures)
    {
        if (!condition) failures.Add("FAIL: " + message);
    }

    private sealed class TrackingBackend(string rootPath) : IDiskImageBackend
    {
        public string RootPath { get; } = rootPath;
        public string? LastImagePath { get; private set; }
        public int MountCount { get; private set; }
        public int DismountCount { get; private set; }
        public bool IsAttached(string imagePath) => false;
        public string Mount(string imagePath)
        {
            LastImagePath = imagePath;
            MountCount++;
            return RootPath;
        }
        public void Dismount(string imagePath) => DismountCount++;
    }
}
