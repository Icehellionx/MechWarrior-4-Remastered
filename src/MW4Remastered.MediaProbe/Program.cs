using MW4Remastered.Core.Media;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: MW4Remastered.MediaProbe <mounted-or-extracted-media-root>");
    return 2;
}

try
{
    var result = new MediaInspectionService().InspectDirectory(args[0]);
    Console.WriteLine($"Status: {result.Status}");
    Console.WriteLine($"Layout: {result.Layout?.Id ?? "none"}");
    Console.WriteLine(result.Message);
    foreach (var excluded in result.ExcludedPaths) Console.WriteLine($"Excluded: {excluded}");
    return result.Status is MediaRecognitionStatus.Recognized or MediaRecognitionStatus.RecognizedWithExcludedContent ? 0 : 1;
}
catch (Exception error) when (error is IOException or UnauthorizedAccessException)
{
    Console.Error.WriteLine($"Media inspection failed: {error.Message}");
    return 3;
}
