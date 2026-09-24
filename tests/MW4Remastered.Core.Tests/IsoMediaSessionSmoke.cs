using MW4Remastered.Core.Media;

internal static class IsoMediaSessionSmoke
{
    public static void Run(List<string> failures)
    {
        Check(PowerShellDiskImageBackend.ParseAttachmentState(" attached \r\n") &&
              !PowerShellDiskImageBackend.ParseAttachmentState("detached\r\n"),
            "disk-image query parses only explicit attachment states", failures);
        var blankStateRejected = false;
        try { _ = PowerShellDiskImageBackend.ParseAttachmentState(" \r\n"); }
        catch (InvalidDataException error)
        {
            blankStateRejected = error.Message.Contains("no disk-image attachment state", StringComparison.Ordinal) &&
                error.Message.Contains("did not mount", StringComparison.Ordinal);
        }
        Check(blankStateRejected, "empty disk-image query output reports a safe, actionable failure", failures);
        var ambiguousStateRejected = false;
        try { _ = PowerShellDiskImageBackend.ParseAttachmentState("attached\r\ndetached\r\n"); }
        catch (InvalidDataException) { ambiguousStateRejected = true; }
        Check(ambiguousStateRejected, "disk-image query rejects ambiguous attachment output", failures);
        var privatePath = @"C:\Users\Example\Private\disc.iso";
        var serializedAccessError = $"#< CLIXML <S S=\"Error\">Get-DiskImage : Access denied {privatePath} 0x80041003</S>";
        var classified = PowerShellDiskImageBackend.CommandFailure(1, serializedAccessError);
        Check(classified is UnauthorizedAccessException &&
              classified.Message.Contains("approve its UAC prompt", StringComparison.Ordinal) &&
              !classified.Message.Contains(privatePath, StringComparison.Ordinal) &&
              !classified.Message.Contains("CLIXML", StringComparison.Ordinal),
            "disk-image privilege failures are actionable and never echo serialized private paths", failures);
        var otherFailure = PowerShellDiskImageBackend.CommandFailure(5, $"Unexpected path {privatePath}");
        Check(otherFailure is IOException && !otherFailure.Message.Contains(privatePath, StringComparison.Ordinal),
            "other disk-image failures do not export raw PowerShell stderr", failures);

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
            catch (InvalidOperationException error)
            {
                attachedRejected = error.Message.Contains("Eject that virtual disc", StringComparison.Ordinal);
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

            using var cancellation = new CancellationTokenSource();
            var cancellingBackend = new CancellingDiskImageBackend(mountedRoot, cancellation);
            var cancellationObserved = false;
            try
            {
                new OwnedIsoMediaSessionFactory(cancellingBackend).Open(image, cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                cancellationObserved = true;
            }
            Check(cancellationObserved && cancellingBackend.DismountCount == 1,
                "ISO session dismounts an image when cancellation arrives during mount", failures);
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

    private sealed class CancellingDiskImageBackend(string rootPath, CancellationTokenSource cancellation) : IDiskImageBackend
    {
        public int DismountCount { get; private set; }
        public bool IsAttached(string imagePath) => false;

        public string Mount(string imagePath)
        {
            cancellation.Cancel();
            return rootPath;
        }

        public void Dismount(string imagePath) => DismountCount++;
    }
}
