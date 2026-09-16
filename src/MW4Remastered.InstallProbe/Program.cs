using MW4Remastered.Core.Install;

if (args.Length == 2 && string.Equals(args[0], "--verify", StringComparison.OrdinalIgnoreCase))
{
    var verification = new InstallManifestVerifier().Verify(args[1]);
    foreach (var issue in verification.Issues) Console.Error.WriteLine($"Verification: {issue}");
    Console.WriteLine(verification.IsValid ? "Ownership manifest verification passed." : "Ownership manifest verification failed.");
    return verification.IsValid ? 0 : 1;
}

if (args.Length == 2 && string.Equals(args[0], "--uninstall", StringComparison.OrdinalIgnoreCase))
{
    var removal = new OwnedInstallUninstaller().Remove(args[1]);
    foreach (var path in removal.PreservedPaths) Console.WriteLine($"Preserved: {path}");
    foreach (var issue in removal.Issues) Console.Error.WriteLine($"Removal: {issue}");
    Console.WriteLine(removal.Status == InstallRemovalStatus.Removed
        ? $"Removed {removal.RemovedFiles.Count} owned files."
        : "Owned-file removal was blocked before mutation.");
    return removal.Status == InstallRemovalStatus.Removed ? 0 : 1;
}

if (args.Length == 3 && string.Equals(args[0], "--extract-mercenaries-cabinet", StringComparison.OrdinalIgnoreCase))
{
    try
    {
        var files = new CabinetPayloadExtractor().ExtractGamePayload(args[1], args[2]);
        Console.WriteLine($"Extracted and verified {files.Count} Mercenaries cabinet files to {Path.GetFullPath(args[2])}");
        return 0;
    }
    catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException)
    {
        Console.Error.WriteLine($"Mercenaries cabinet extraction failed: {error.Message}");
        return 1;
    }
}

if (args.Length == 4 && string.Equals(args[0], "--black-knight", StringComparison.OrdinalIgnoreCase))
{
    try
    {
        return Install(new BlackKnightInstallRequest(args[1], args[2]), args[3], "Black Knight");
    }
    catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException)
    {
        Console.Error.WriteLine($"Black Knight staging failed: {error.Message}");
        return 1;
    }
}

if (args.Length == 5 && string.Equals(args[0], "--mercenaries", StringComparison.OrdinalIgnoreCase))
{
    try
    {
        return Install(new MercenariesInstallRequest(args[1], args[2], args[3]), args[4], "Mercenaries");
    }
    catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException)
    {
        Console.Error.WriteLine($"Mercenaries staging failed: {error.Message}");
        return 1;
    }
}

if (args.Length != 4)
{
    Console.Error.WriteLine("Usage:");
    Console.Error.WriteLine("  MW4Remastered.InstallProbe <vengeance-disc-1-root> <vengeance-disc-2-root> <compatibility-executable> <new-destination>");
    Console.Error.WriteLine("  MW4Remastered.InstallProbe --black-knight <disc-root> <compatibility-executable> <new-destination>");
    Console.Error.WriteLine("  MW4Remastered.InstallProbe --mercenaries <disc-1-root> <disc-2-root> <compatibility-executable> <new-destination>");
    Console.Error.WriteLine("  MW4Remastered.InstallProbe --extract-mercenaries-cabinet <MSGAME.CAB> <new-destination>");
    Console.Error.WriteLine("  MW4Remastered.InstallProbe --verify <install-root>");
    Console.Error.WriteLine("  MW4Remastered.InstallProbe --uninstall <install-root>");
    return 2;
}

try
{
    return Install(new VengeanceInstallRequest(args[0], args[1], args[2]), args[3], "Vengeance");
}
catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException)
{
    Console.Error.WriteLine($"Vengeance staging failed: {error.Message}");
    return 1;
}

static int Install(GameInstallRequest request, string destination, string productName)
{
    try
    {
        var result = new GameInstallationCoordinator().Install(request, destination, new ConsoleInstallProgress());
        Console.WriteLine($"Committed {result.Manifest.Files.Count} files to {result.DestinationPath}");
        Console.WriteLine($"Ownership manifest: {InstallManifest.RelativePath}");
        Console.WriteLine("Ownership manifest verification passed.");
        return 0;
    }
    catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException)
    {
        Console.Error.WriteLine($"{productName} staging failed: {error.Message}");
        return 1;
    }
}

sealed class ConsoleInstallProgress : IProgress<GameInstallationProgress>
{
    public void Report(GameInstallationProgress value) => Console.WriteLine($"{value.Stage}: {value.Message}");
}
