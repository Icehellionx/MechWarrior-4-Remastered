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

if (args.Length != 4)
{
    Console.Error.WriteLine("Usage:");
    Console.Error.WriteLine("  MW4Remastered.InstallProbe <vengeance-disc-1-root> <vengeance-disc-2-root> <compatibility-executable> <new-destination>");
    Console.Error.WriteLine("  MW4Remastered.InstallProbe --verify <install-root>");
    Console.Error.WriteLine("  MW4Remastered.InstallProbe --uninstall <install-root>");
    return 2;
}

try
{
    var plan = new VengeanceInstallPlanBuilder().Build(args[0], args[1], args[2]);
    Console.WriteLine($"Qualified {plan.Files.Count} source files; beginning staged transaction.");
    var manifest = new StagedInstallTransaction().Execute(plan, args[3]);
    Console.WriteLine($"Committed {manifest.Files.Count} files to {Path.GetFullPath(args[3])}");
    Console.WriteLine($"Ownership manifest: {InstallManifest.RelativePath}");
    var verification = new InstallManifestVerifier().Verify(args[3]);
    if (!verification.IsValid)
    {
        foreach (var issue in verification.Issues) Console.Error.WriteLine($"Verification: {issue}");
        return 1;
    }
    Console.WriteLine("Ownership manifest verification passed.");
    return 0;
}
catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException)
{
    Console.Error.WriteLine($"Vengeance staging failed: {error.Message}");
    return 1;
}
