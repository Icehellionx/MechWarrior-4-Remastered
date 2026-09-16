using MW4Remastered.Core.Media;

internal static class MediaSelectionSessionSmoke
{
    public static void Run(List<string> failures)
    {
        var root = Path.Combine(Path.GetTempPath(), "mw4-selection-session-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var disc1 = Path.Combine(root, "disc1");
            var disc2 = Path.Combine(root, "disc2");
            CreateLayout("vengeance-disc-1", disc1);
            CreateLayout("vengeance-disc-2", disc2);

            var selection = new MediaSelectionSet();
            selection.Add(disc1, Inspection("vengeance-disc-1"));
            var snapshot = selection.Add(disc2, Inspection("vengeance-disc-2"));
            var sessionFactory = new MediaSelectionSessionFactory(
                new MediaSourceSessionFactory(
                    new OwnedIsoMediaSessionFactory(new NoMountBackend()),
                    new IsoArchiveExtractor()),
                new MediaInspectionService());

            using (var session = sessionFactory.Open(snapshot))
            {
                Check(session.Layouts.Count == 2, "selection session reopens every selected layout", failures);
                Check(session.GetRoot("vengeance-disc-1") == Path.GetFullPath(disc1), "selection session exposes the revalidated disc root", failures);
            }

            File.Delete(Path.Combine(disc2, "AUTORUN2.EXE"));
            var changedRejected = false;
            try
            {
                using var unused = sessionFactory.Open(snapshot);
            }
            catch (InvalidDataException)
            {
                changedRejected = true;
            }
            Check(changedRejected, "selection session rejects media changed after intake", failures);

            var mountedRoot = Path.Combine(root, "mounted");
            CreateLayout("black-knight-disc-1", mountedRoot);
            var image = Write(root, "black-knight.iso", "fixture");
            var missing = Path.Combine(root, "missing.iso");
            var backend = new TrackingBackend(mountedRoot);
            var ownedFactory = new MediaSelectionSessionFactory(
                new MediaSourceSessionFactory(new OwnedIsoMediaSessionFactory(backend), new IsoArchiveExtractor()),
                new MediaInspectionService());
            var partial = new MediaSelectionSnapshot(
                new[]
                {
                    Evidence("black-knight-disc-1", image),
                    Evidence("clan-mech-pak", missing),
                },
                Array.Empty<MediaCapabilityStatus>(),
                2,
                0);
            var missingRejected = false;
            try
            {
                using var unused = ownedFactory.Open(partial);
            }
            catch (FileNotFoundException)
            {
                missingRejected = true;
            }
            Check(missingRejected && backend.DismountCount == 1, "selection reopen failure closes sources opened earlier in the transaction", failures);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    private static MediaSourceInspection Inspection(string layoutId)
    {
        var layout = MediaCatalog.Layouts.Single(item => item.Id == layoutId);
        return new MediaSourceInspection(
            MediaSourceKind.Directory,
            new[] { new InspectedMediaItem(null, new MediaRecognitionResult(MediaRecognitionStatus.Recognized, layout, Array.Empty<string>(), "Synthetic.")) },
            Array.Empty<string>());
    }

    private static SelectedMediaLayout Evidence(string layoutId, string sourcePath)
    {
        var layout = MediaCatalog.Layouts.Single(item => item.Id == layoutId);
        return new SelectedMediaLayout(layout, Path.GetFullPath(sourcePath), null, 0);
    }

    private static void CreateLayout(string layoutId, string root)
    {
        foreach (var path in MediaCatalog.Layouts.Single(item => item.Id == layoutId).RequiredPaths)
        {
            Write(root, path, "fixture");
        }
    }

    private static string Write(string root, string relativePath, string contents)
    {
        var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, contents);
        return path;
    }

    private static void Check(bool condition, string message, List<string> failures)
    {
        if (!condition) failures.Add("FAIL: " + message);
    }

    private sealed class NoMountBackend : IDiskImageBackend
    {
        public bool IsAttached(string imagePath) => false;
        public string Mount(string imagePath) => throw new InvalidOperationException("Mount was not expected.");
        public void Dismount(string imagePath) { }
    }

    private sealed class TrackingBackend(string rootPath) : IDiskImageBackend
    {
        public int DismountCount { get; private set; }
        public bool IsAttached(string imagePath) => false;
        public string Mount(string imagePath) => rootPath;
        public void Dismount(string imagePath) => DismountCount++;
    }
}
