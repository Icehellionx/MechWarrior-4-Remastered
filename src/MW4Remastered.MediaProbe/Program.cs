using MW4Remastered.Core.Media;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: MW4Remastered.MediaProbe <iso-or-extracted-media-root>");
    return 2;
}

try
{
    var input = Path.GetFullPath(args[0]);
    if (File.Exists(input) && string.Equals(Path.GetExtension(input), ".iso", StringComparison.OrdinalIgnoreCase))
    {
        using var session = new OwnedIsoMediaSessionFactory(new PowerShellDiskImageBackend()).Open(input);
        return Report(new MediaInspectionService().InspectDirectory(session.RootPath));
    }
    if (File.Exists(input) && string.Equals(Path.GetExtension(input), ".zip", StringComparison.OrdinalIgnoreCase))
    {
        return InspectArchive(input);
    }
    return Report(new MediaInspectionService().InspectDirectory(input));
}
catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException or InvalidOperationException)
{
    Console.Error.WriteLine($"Media inspection failed: {error.Message}");
    return 3;
}

static int Report(MediaRecognitionResult result)
{
    Console.WriteLine($"Status: {result.Status}");
    Console.WriteLine($"Layout: {result.Layout?.Id ?? "none"}");
    Console.WriteLine(result.Message);
    foreach (var excluded in result.ExcludedPaths) Console.WriteLine($"Excluded: {excluded}");
    return result.Status is MediaRecognitionStatus.Recognized or MediaRecognitionStatus.RecognizedWithExcludedContent ? 0 : 1;
}

static int InspectArchive(string archivePath)
{
    var scratch = Path.Combine(Path.GetTempPath(), "mw4-media-probe-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(scratch);
    try
    {
        var extraction = new IsoArchiveExtractor().Extract(archivePath, Path.Combine(scratch, "media"));
        foreach (var excluded in extraction.ExcludedEntries) Console.WriteLine($"Archive excluded: {excluded}");
        var exitCode = 0;
        var sessions = new OwnedIsoMediaSessionFactory(new PowerShellDiskImageBackend());
        foreach (var relativePath in extraction.IsoRelativePaths)
        {
            Console.WriteLine($"Archive ISO: {relativePath}");
            var image = Path.Combine(extraction.DestinationRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            using var session = sessions.Open(image);
            exitCode = Math.Max(exitCode, Report(new MediaInspectionService().InspectDirectory(session.RootPath)));
        }
        return exitCode;
    }
    finally
    {
        if (Directory.Exists(scratch)) Directory.Delete(scratch, true);
    }
}
