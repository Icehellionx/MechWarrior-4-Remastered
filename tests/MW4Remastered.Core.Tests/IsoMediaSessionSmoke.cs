using MW4Remastered.Core.Media;

internal static class IsoMediaSessionSmoke
{
    public static void Run(List<string> failures)
    {
        var root = Path.Combine(Path.GetTempPath(), "mw4-iso-session-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(root);
            var image = Path.Combine(root, "media.iso");
            File.WriteAllText(image, "synthetic image fixture");
            var mountedRoot = Path.Combine(root, "mounted");
            Directory.CreateDirectory(mountedRoot);

            var backend = new RecordingDiskImageBackend(mountedRoot);
            var session = new OwnedIsoMediaSessionFactory(backend).Open(image);
            Check(session.RootPath == Path.GetFullPath(mountedRoot) && backend.MountCount == 1, "ISO session exposes only its owned mounted root", failures);
            session.Dispose();
            session.Dispose();
            Check(backend.DismountCount == 1, "ISO session dismounts its image exactly once", failures);

            var attached = new RecordingDiskImageBackend(mountedRoot) { Attached = true };
            var attachedRejected = false;
            try
            {
                new OwnedIsoMediaSessionFactory(attached).Open(image);
            }
            catch (InvalidOperationException)
            {
                attachedRejected = true;
            }
            Check(attachedRejected && attached.MountCount == 0 && attached.DismountCount == 0, "ISO session refuses pre-attached images without taking cleanup ownership", failures);

            var missingRoot = new RecordingDiskImageBackend(Path.Combine(root, "missing"));
            var missingRejected = false;
            try
            {
                new OwnedIsoMediaSessionFactory(missingRoot).Open(image);
            }
            catch (DirectoryNotFoundException)
            {
                missingRejected = true;
            }
            Check(missingRejected && missingRoot.DismountCount == 1, "ISO session dismounts after mounted-root validation fails", failures);

            var wrongExtension = Path.Combine(root, "media.img");
            File.WriteAllText(wrongExtension, "fixture");
            var extensionRejected = false;
            try
            {
                new OwnedIsoMediaSessionFactory(backend).Open(wrongExtension);
            }
            catch (InvalidDataException)
            {
                extensionRejected = true;
            }
            Check(extensionRejected, "ISO session rejects non-ISO paths before backend access", failures);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    private static void Check(bool condition, string message, List<string> failures)
    {
        if (!condition) failures.Add("FAIL: " + message);
    }

    private sealed class RecordingDiskImageBackend(string rootPath) : IDiskImageBackend
    {
        public bool Attached { get; set; }
        public int MountCount { get; private set; }
        public int DismountCount { get; private set; }

        public bool IsAttached(string imagePath) => Attached;

        public string Mount(string imagePath)
        {
            MountCount++;
            Attached = true;
            return rootPath;
        }

        public void Dismount(string imagePath)
        {
            DismountCount++;
            Attached = false;
        }
    }
}
