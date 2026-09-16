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
