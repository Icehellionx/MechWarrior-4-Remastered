using MW4Remastered.Core.Media;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: MW4Remastered.MediaProbe <iso-or-extracted-media-root>");
    return 2;
}

try
{
    var source = new MediaSourceInspector().Inspect(args[0]);
    foreach (var excluded in source.ExcludedArchiveEntries) Console.WriteLine($"Archive excluded: {excluded}");
    var exitCode = 0;
    foreach (var item in source.Items)
    {
        if (item.ArchiveRelativePath is not null) Console.WriteLine($"Archive ISO: {item.ArchiveRelativePath}");
        exitCode = Math.Max(exitCode, Report(item.Recognition));
    }
    return exitCode;
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
