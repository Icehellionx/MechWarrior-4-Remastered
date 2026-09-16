using MW4Remastered.Core;
using MW4Remastered.Core.Install;
using MW4Remastered.Core.Launch;
using MW4Remastered.Core.Media;
using System.Security.Cryptography;

var failures = new List<string>();
Check(ProductCatalog.All.Count == 5, "catalog contains exactly three games and two packs");
Check(ProductCatalog.All.Count(item => item.Kind == ProductKind.Game) == 3, "catalog contains three games");
Check(ProductCatalog.All.Count(item => item.Kind == ProductKind.OptionalPack) == 2, "catalog contains two optional packs");
Check(MediaCatalog.Layouts.Count == 7, "media catalog contains seven disc/pack layouts");

var root = Path.Combine(Path.GetTempPath(), "mw4-remastered-core-test-" + Guid.NewGuid().ToString("N"));
try
{
    Directory.CreateDirectory(Path.Combine(root, "vengeance"));
    File.WriteAllText(Path.Combine(root, "vengeance", "MW4.exe"), "synthetic fixture");
    var statuses = new InstallStatusReader(root).Read().ToDictionary(item => item.Product.Id);
    Check(!statuses["vengeance"].IsInstalled && statuses["vengeance"].State == ProductInstallState.NeedsRepair, "unmanifested Vengeance executable requires repair");
    Check(!statuses["black-knight"].IsInstalled, "missing Black Knight executable stays absent");
    Check(!statuses["clan"].IsInstalled, "Clan pack is not claimed without verified payload evidence");
    Check(!statuses["inner-sphere"].IsInstalled, "Inner Sphere pack is not claimed without verified payload evidence");
}
finally
{
    if (Directory.Exists(root)) Directory.Delete(root, true);
}

var recognizer = new MediaRecognizer();
foreach (var layout in MediaCatalog.Layouts)
{
    var result = recognizer.Recognize(layout.RequiredPaths.Select(path => path.ToLowerInvariant()));
    Check(result.Status == MediaRecognitionStatus.Recognized, $"{layout.Id} is recognized case-insensitively");
    Check(result.Layout?.Id == layout.Id, $"{layout.Id} resolves to itself");
}

var incomplete = recognizer.Recognize(new[] { "MW4.EXE", "RESOURCE/CORE.MW4" });
Check(incomplete.Status == MediaRecognitionStatus.Unknown, "partial Vengeance media is rejected");

var unsafeInventory = recognizer.Recognize(new[] { "MW4.EXE", "../outside.dll" });
Check(unsafeInventory.Status == MediaRecognitionStatus.UnsafeInventory, "parent traversal is rejected before recognition");

var modifiedInnerSphere = MediaCatalog.Layouts.Single(layout => layout.Id == "inner-sphere-mech-pak").RequiredPaths
    .Concat(new[] { "Razor1911/mw4.exe", "SECDRV.SYS", "CDSET/SCSHD.EXE" });
var modifiedResult = recognizer.Recognize(modifiedInnerSphere);
Check(modifiedResult.Status == MediaRecognitionStatus.RecognizedWithExcludedContent, "supported media with prohibited extras is isolated");
Check(modifiedResult.ExcludedPaths.Count == 3, "all prohibited paths are reported");

var absolute = recognizer.Recognize(new[] { "C:/Windows/System32/file.dll" });
Check(absolute.Status == MediaRecognitionStatus.UnsafeInventory, "drive-qualified paths are rejected");

var mediaRoot = Path.Combine(Path.GetTempPath(), "mw4-remastered-media-test-" + Guid.NewGuid().ToString("N"));
try
{
    foreach (var requiredPath in MediaCatalog.Layouts.Single(layout => layout.Id == "vengeance-disc-1").RequiredPaths)
    {
        var file = Path.Combine(mediaRoot, requiredPath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, "synthetic fixture");
    }

    var inspected = new MediaInspectionService().InspectDirectory(mediaRoot);
    Check(inspected.Layout?.Id == "vengeance-disc-1", "directory inventory feeds the shared recognizer");
}
finally
{
    if (Directory.Exists(mediaRoot)) Directory.Delete(mediaRoot, true);
}

var transactionRoot = Path.Combine(Path.GetTempPath(), "mw4-remastered-transaction-test-" + Guid.NewGuid().ToString("N"));
try
{
    var disc1 = Path.Combine(transactionRoot, "disc1");
    var disc2 = Path.Combine(transactionRoot, "disc2");
    Directory.CreateDirectory(disc1);
    Directory.CreateDirectory(disc2);
    File.WriteAllText(Path.Combine(disc1, "MW4.EXE"), "synthetic executable");
    File.SetAttributes(Path.Combine(disc1, "MW4.EXE"), FileAttributes.ReadOnly);
    Directory.CreateDirectory(Path.Combine(disc2, "RESOURCE", "MAPS"));
    File.WriteAllText(Path.Combine(disc2, "RESOURCE", "MAPS", "ALPINE01.MW4"), "synthetic map");

    var destination = Path.Combine(transactionRoot, "installed", "vengeance");
    var plan = new InstallPlan("vengeance", new[]
    {
        new InstallFile(disc1, "MW4.EXE", "MW4.EXE"),
        new InstallFile(disc2, "RESOURCE/MAPS/ALPINE01.MW4", "RESOURCE/MAPS/ALPINE01.MW4"),
    });
    var manifest = new StagedInstallTransaction().Execute(plan, destination);
    Check(File.Exists(Path.Combine(destination, "MW4.EXE")), "transaction commits the first source file");
    Check((File.GetAttributes(Path.Combine(destination, "MW4.EXE")) & FileAttributes.ReadOnly) == 0, "transaction makes installed media files writable");
    Check(File.Exists(Path.Combine(destination, "RESOURCE", "MAPS", "ALPINE01.MW4")), "transaction commits the second source file");
    Check(File.Exists(Path.Combine(destination, InstallManifest.RelativePath.Replace('/', Path.DirectorySeparatorChar))), "transaction persists its ownership manifest");
    Check(manifest.Files.Count == 2 && manifest.Files.All(file => file.Sha256.Length == 64), "manifest hashes every installed file");
    Check(new InstallManifestVerifier().Verify(destination).IsValid, "manifest verifier accepts the committed tree");
    var installedStatuses = new InstallStatusReader(Path.Combine(transactionRoot, "installed")).Read().ToDictionary(item => item.Product.Id);
    Check(installedStatuses["vengeance"].State == ProductInstallState.Ready && installedStatuses["vengeance"].LaunchPath is not null, "status reader requires a verified ownership manifest before enabling launch");
    var processStarter = new RecordingProcessStarter();
    new LaunchOrchestrator(processStarter).Launch(installedStatuses["vengeance"]);
    Check(processStarter.LastStart?.FileName == installedStatuses["vengeance"].LaunchPath && processStarter.LastStart?.WorkingDirectory == destination, "launch orchestration uses the verified executable and its working directory");

    File.AppendAllText(Path.Combine(destination, "MW4.EXE"), "tampered");
    var tampered = new InstallManifestVerifier().Verify(destination);
    Check(!tampered.IsValid && tampered.Issues.Any(issue => issue.Contains("mismatch", StringComparison.OrdinalIgnoreCase)), "manifest verifier detects file tampering");
    File.WriteAllText(Path.Combine(destination, "UNTRACKED.TXT"), "unexpected");
    var unexpected = new InstallManifestVerifier().Verify(destination);
    Check(!unexpected.IsValid && unexpected.Issues.Any(issue => issue.StartsWith("Unexpected installed file:", StringComparison.Ordinal)), "manifest verifier detects unowned files");

    var failedDestination = Path.Combine(transactionRoot, "installed", "failed");
    var failed = false;
    try
    {
        new StagedInstallTransaction().Execute(new InstallPlan("vengeance", new[]
        {
            new InstallFile(disc1, "MW4.EXE", "MW4.EXE"),
            new InstallFile(disc2, "MISSING.MW4", "RESOURCE/MISSING.MW4"),
        }), failedDestination);
    }
    catch (FileNotFoundException)
    {
        failed = true;
    }
    Check(failed && !Directory.Exists(failedDestination), "failed transaction rolls back before commit");
    Check(!Directory.EnumerateDirectories(Path.Combine(transactionRoot, "installed"), ".failed.staging-*").Any(), "failed transaction removes its staging directory");

    var traversalRejected = false;
    try
    {
        new StagedInstallTransaction().Execute(new InstallPlan("vengeance", new[]
        {
            new InstallFile(disc1, "MW4.EXE", "../escape.exe"),
        }), Path.Combine(transactionRoot, "installed", "traversal"));
    }
    catch (InvalidDataException)
    {
        traversalRejected = true;
    }
    Check(traversalRejected, "transaction rejects destination traversal");

    var duplicateRejected = false;
    try
    {
        new StagedInstallTransaction().Execute(new InstallPlan("vengeance", new[]
        {
            new InstallFile(disc1, "MW4.EXE", "same.bin"),
            new InstallFile(disc2, "RESOURCE/MAPS/ALPINE01.MW4", "SAME.BIN"),
        }), Path.Combine(transactionRoot, "installed", "duplicate"));
    }
    catch (InvalidDataException)
    {
        duplicateRejected = true;
    }
    Check(duplicateRejected, "transaction rejects case-insensitive duplicate destinations");
}
finally
{
    if (Directory.Exists(transactionRoot))
    {
        foreach (var file in Directory.EnumerateFiles(transactionRoot, "*", SearchOption.AllDirectories)) File.SetAttributes(file, FileAttributes.Normal);
        Directory.Delete(transactionRoot, true);
    }
}

var planRoot = Path.Combine(Path.GetTempPath(), "mw4-remastered-plan-test-" + Guid.NewGuid().ToString("N"));
try
{
    var disc1 = Path.Combine(planRoot, "disc1");
    var disc2 = Path.Combine(planRoot, "disc2");
    CreateLayoutFixture("vengeance-disc-1", disc1);
    CreateLayoutFixture("vengeance-disc-2", disc2);
    WriteFixture(disc1, "AUTOCO_1.EXE", "autoconfig");
    WriteFixture(disc1, "SCRIPT_1.DLL", "scripts");
    WriteFixture(disc1, "CONTENT/SHELLS_1/FILES/STUTTE_1.WAV", "audio");
    WriteFixture(disc1, "CONTENT/TEXTURES/CUSTOM_1/CSTMDCAL.TXT", "decals");
    WriteFixture(disc1, "SECDRV.SYS", "must not copy");
    WriteFixture(disc1, "SETUP.EXE", "must not copy");
    var replacement = Path.Combine(planRoot, "replacement", "MW4.exe");
    WriteFixture(Path.GetDirectoryName(replacement)!, Path.GetFileName(replacement), "synthetic patched executable");
    var replacementHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(replacement))).ToLowerInvariant();

    var builder = new VengeanceInstallPlanBuilder(replacementHash, new MediaInspectionService(), new DirectoryMediaInventory());
    var plan = builder.Build(disc1, disc2, replacement);
    var destinations = plan.Files.Select(file => file.DestinationRelativePath).ToHashSet(StringComparer.OrdinalIgnoreCase);
    Check(destinations.Contains("MW4.exe"), "Vengeance plan supplies the qualified compatibility executable");
    Check(destinations.Contains("AutoConfig.exe") && destinations.Contains("ScriptStrings.dll"), "Vengeance plan expands patch-relevant 8.3 root names");
    Check(destinations.Contains("Content/ShellScripts/FILES/STUTTE_1.WAV"), "Vengeance plan restores the ShellScripts directory name");
    Check(destinations.Contains("Content/Textures/customdecals/CSTMDCAL.TXT"), "Vengeance plan restores the custom decals directory name");
    Check(!destinations.Contains("SECDRV.SYS") && !destinations.Contains("SETUP.EXE") && !destinations.Contains("MW4.ICD"), "Vengeance plan excludes setup and SafeDisc components");
}
finally
{
    if (Directory.Exists(planRoot)) Directory.Delete(planRoot, true);
}

var uninstallRoot = Path.Combine(Path.GetTempPath(), "mw4-remastered-uninstall-test-" + Guid.NewGuid().ToString("N"));
try
{
    var source = Path.Combine(uninstallRoot, "source");
    Directory.CreateDirectory(source);
    WriteFixture(source, "MW4.exe", "owned executable");
    WriteFixture(source, "Resource/core.mw4", "owned archive");

    var preservedInstall = Path.Combine(uninstallRoot, "preserved-install");
    new StagedInstallTransaction().Execute(new InstallPlan("vengeance", new[]
    {
        new InstallFile(source, "MW4.exe", "MW4.exe"),
        new InstallFile(source, "Resource/core.mw4", "Resource/core.mw4"),
    }), preservedInstall);
    WriteFixture(preservedInstall, "Saves/pilot.sav", "user save");
    var removed = new OwnedInstallUninstaller().Remove(preservedInstall);
    Check(removed.Status == InstallRemovalStatus.Removed, "uninstaller removes a verified owned install");
    Check(!File.Exists(Path.Combine(preservedInstall, "MW4.exe")), "uninstaller removes owned files");
    Check(File.Exists(Path.Combine(preservedInstall, "Saves", "pilot.sav")), "uninstaller preserves unowned saves");
    Check(!File.Exists(Path.Combine(preservedInstall, InstallManifest.RelativePath.Replace('/', Path.DirectorySeparatorChar))), "uninstaller removes its ownership manifest");

    var modifiedInstall = Path.Combine(uninstallRoot, "modified-install");
    new StagedInstallTransaction().Execute(new InstallPlan("vengeance", new[]
    {
        new InstallFile(source, "MW4.exe", "MW4.exe"),
        new InstallFile(source, "Resource/core.mw4", "Resource/core.mw4"),
    }), modifiedInstall);
    File.AppendAllText(Path.Combine(modifiedInstall, "MW4.exe"), "modified");
    var blockedRemoval = new OwnedInstallUninstaller().Remove(modifiedInstall);
    Check(blockedRemoval.Status == InstallRemovalStatus.Blocked, "uninstaller blocks on modified owned content");
    Check(File.Exists(Path.Combine(modifiedInstall, "MW4.exe")) && File.Exists(Path.Combine(modifiedInstall, "Resource", "core.mw4")), "blocked uninstall performs no partial deletion");

    var malformedInstall = Path.Combine(uninstallRoot, "malformed-install");
    new StagedInstallTransaction().Execute(new InstallPlan("vengeance", new[]
    {
        new InstallFile(source, "MW4.exe", "MW4.exe"),
    }), malformedInstall);
    File.WriteAllText(Path.Combine(malformedInstall, InstallManifest.RelativePath.Replace('/', Path.DirectorySeparatorChar)), "{ invalid json");
    var malformedRemoval = new OwnedInstallUninstaller().Remove(malformedInstall);
    Check(malformedRemoval.Status == InstallRemovalStatus.Blocked && File.Exists(Path.Combine(malformedInstall, "MW4.exe")), "malformed manifest blocks before deletion");

    var volumeRoot = Path.GetPathRoot(Path.GetFullPath(uninstallRoot))!;
    Check(new OwnedInstallUninstaller().Remove(volumeRoot).Status == InstallRemovalStatus.Blocked, "uninstaller refuses a filesystem root before inventory");
}
finally
{
    if (Directory.Exists(uninstallRoot)) Directory.Delete(uninstallRoot, true);
}

if (failures.Count > 0)
{
    Console.Error.WriteLine(string.Join(Environment.NewLine, failures));
    return 1;
}

Console.WriteLine("MW4Remastered.Core smoke tests passed.");
return 0;

void Check(bool condition, string message)
{
    if (!condition) failures.Add("FAIL: " + message);
}

void CreateLayoutFixture(string layoutId, string rootPath)
{
    foreach (var requiredPath in MediaCatalog.Layouts.Single(layout => layout.Id == layoutId).RequiredPaths)
    {
        WriteFixture(rootPath, requiredPath, "synthetic fixture");
    }
}

void WriteFixture(string rootPath, string relativePath, string contents)
{
    var file = Path.Combine(rootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
    Directory.CreateDirectory(Path.GetDirectoryName(file)!);
    File.WriteAllText(file, contents);
}

sealed class RecordingProcessStarter : IProcessStarter
{
    public System.Diagnostics.ProcessStartInfo? LastStart { get; private set; }

    public void Start(System.Diagnostics.ProcessStartInfo startInfo)
    {
        LastStart = startInfo;
    }
}
