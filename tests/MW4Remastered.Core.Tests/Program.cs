using MW4Remastered.Core;
using MW4Remastered.Core.Install;
using MW4Remastered.Core.Launch;
using MW4Remastered.Core.Media;
using System.Security.Cryptography;
using System.IO.Compression;

var failures = new List<string>();
var safeDiscBlock = Convert.FromHexString("234319fa48b20e26");
SafeDisc1BlockCipher.DecryptBlock(
    safeDiscBlock,
    new uint[] { 0x01234567, 0x89abcdef, 0xfedcba98, 0x76543210 });
Check(Convert.ToHexString(safeDiscBlock).Equals("4433221188776655", StringComparison.OrdinalIgnoreCase),
    "SafeDisc 1 block cipher matches the independent TEA-family test vector");

var safeDiscSecondLayerVector = Convert.FromHexString("000102030405060708090A0B0C0D0E0F");
SafeDisc15020SecondLayer.DecodePage(safeDiscSecondLayerVector);
Check(
    Convert.ToHexString(safeDiscSecondLayerVector).Equals(
        "21260BFDA9304082FAB93A0742C1B366",
        StringComparison.Ordinal),
    "SafeDisc 1.50.20 second layer matches the independent modifier-chain vector");

var safeDiscSectionVector = Enumerable.Range(0, 4110)
    .Select(index => unchecked((byte)((index * 37) + 11)))
    .ToArray();
SafeDisc15020ImageCipher.DecodeSection(
    safeDiscSectionVector,
    4107,
    new uint[] { 0x01234567, 0x89abcdef, 0xfedcba98, 0x76543210 });
Check(
    Convert.ToHexString(SHA256.HashData(safeDiscSectionVector)).Equals(
        "1F475696D4D9319353FBA6FBC6A27EEECCE50B2BA0FAA63BFB5CDA66BA1F1581",
        StringComparison.Ordinal),
    "SafeDisc 1.50.20 section decoding matches the independent page and overlap-tail vector");

var transformRejectionRoot = Path.Combine(Path.GetTempPath(), "mw4-remastered-transform-rejection-" + Guid.NewGuid().ToString("N"));
try
{
    Directory.CreateDirectory(transformRejectionRoot);
    File.WriteAllText(Path.Combine(transformRejectionRoot, "MW4.EXE"), "unsupported loader");
    File.WriteAllText(Path.Combine(transformRejectionRoot, "MW4.ICD"), "unsupported image");
    File.WriteAllText(Path.Combine(transformRejectionRoot, "DPLAYERX.DLL"), "unsupported player");
    var rejected = false;
    try
    {
        new VengeanceRetailExecutableTransform().Transform(
            transformRejectionRoot,
            Path.Combine(transformRejectionRoot, "scratch"));
    }
    catch (InvalidDataException exception)
    {
        rejected = exception.Message.Contains("Unsupported Vengeance MW4.EXE SHA-256", StringComparison.Ordinal);
    }
    Check(rejected, "Vengeance transform rejects media that does not match the qualified retail revision");
    Check(!Directory.Exists(Path.Combine(transformRejectionRoot, "scratch")),
        "Vengeance transform does not write output after input validation fails");
}
finally
{
    if (Directory.Exists(transformRejectionRoot)) Directory.Delete(transformRejectionRoot, true);
}

Check(ProductCatalog.All.Count == 5, "catalog contains exactly three games and two packs");
Check(ProductCatalog.All.Count(item => item.Kind == ProductKind.Game) == 3, "catalog contains three games");
Check(ProductCatalog.All.Count(item => item.Kind == ProductKind.OptionalPack) == 2, "catalog contains two optional packs");
Check(MediaCatalog.Layouts.Count == 7, "media catalog contains seven disc/pack layouts");
Check(ProductDependencies.AreSatisfied("vengeance", Array.Empty<string>()), "Vengeance has no base-game dependency");
Check(!ProductDependencies.AreSatisfied("black-knight", Array.Empty<string>()) &&
    ProductDependencies.AreSatisfied("black-knight", new[] { "vengeance" }), "Black Knight requires Vengeance");
Check(ProductDependencies.AreSatisfied("mercenaries", Array.Empty<string>()), "Mercenaries remains independently selectable");
Check(!ProductDependencies.AreSatisfied("inner-sphere", Array.Empty<string>()) &&
    !ProductDependencies.AreSatisfied("clan", Array.Empty<string>()), "both retail Mech Paks require the Vengeance base");

var root = Path.Combine(Path.GetTempPath(), "mw4-remastered-core-test-" + Guid.NewGuid().ToString("N"));
try
{
    Directory.CreateDirectory(Path.Combine(root, "vengeance"));
    File.WriteAllText(Path.Combine(root, "vengeance", "MW4.exe"), "synthetic fixture");
    Directory.CreateDirectory(Path.Combine(root, "mercenaries"));
    var statuses = new InstallStatusReader(root).Read().ToDictionary(item => item.Product.Id);
    Check(!statuses["vengeance"].IsInstalled && statuses["vengeance"].State == ProductInstallState.NeedsRepair, "unmanifested Vengeance executable requires repair");
    Check(!statuses["black-knight"].IsInstalled, "missing Black Knight executable stays absent");
    Check(statuses["mercenaries"].State == ProductInstallState.NeedsRepair && statuses["mercenaries"].InstallPath is not null,
        "an existing incomplete game directory remains removable as repair-required");
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
    Check(installedStatuses["vengeance"].InstallPath == destination, "status reader exposes the verified product root for ownership-safe removal");
    var processStarter = new RecordingProcessStarter();
    new LaunchOrchestrator(processStarter).Launch(installedStatuses["vengeance"]);
    Check(processStarter.LastStart?.FileName == installedStatuses["vengeance"].LaunchPath && processStarter.LastStart?.WorkingDirectory == destination, "launch orchestration uses the verified executable and its working directory");

    var applicationRoot = Path.Combine(transactionRoot, "application-shell");
    Directory.CreateDirectory(applicationRoot);
    var applicationUninstaller = new ApplicationUninstallOrchestrator(applicationRoot, processStarter);
    Check(!applicationUninstaller.IsAvailable, "application uninstall action remains unavailable without the package-owned uninstaller");
    var applicationUninstallerPath = Path.Combine(applicationRoot, "unins000.exe");
    File.WriteAllText(applicationUninstallerPath, "synthetic standard uninstaller");
    Check(applicationUninstaller.IsAvailable, "application uninstall action recognizes the adjacent standard uninstaller");
    applicationUninstaller.Start();
    Check(processStarter.LastStart?.FileName == applicationUninstallerPath &&
          processStarter.LastStart.WorkingDirectory == applicationRoot &&
          !processStarter.LastStart.UseShellExecute,
        "application uninstall action launches only the adjacent standard uninstaller without shell indirection");

    var installedLauncher = new InstalledLauncherOrchestrator(applicationRoot, processStarter);
    Check(!installedLauncher.IsAvailable, "post-install launcher action remains unavailable without the adjacent packaged launcher");
    var installedLauncherPath = Path.Combine(applicationRoot, "MW4RemasteredLauncher.exe");
    File.WriteAllText(installedLauncherPath, "synthetic launcher");
    Check(installedLauncher.IsAvailable, "post-install launcher action recognizes the adjacent packaged launcher");
    installedLauncher.Start();
    Check(processStarter.LastStart?.FileName == installedLauncherPath &&
          processStarter.LastStart.WorkingDirectory == applicationRoot &&
          !processStarter.LastStart.UseShellExecute,
        "post-install action launches only the adjacent packaged launcher without shell indirection");

    var compatibilityLauncher = Path.Combine(destination, "MW4RemasteredCompatLauncher.exe");
    File.WriteAllText(compatibilityLauncher, "synthetic helper");
    var statusWithUnownedHelper = new InstallStatusReader(Path.Combine(transactionRoot, "installed")).Read().Single(item => item.Product.Id == "vengeance");
    new LaunchOrchestrator(processStarter).Launch(statusWithUnownedHelper);
    Check(processStarter.LastStart?.FileName == statusWithUnownedHelper.LaunchPath,
        "launch orchestration ignores an adjacent helper that is not owned by the verified manifest");
    File.Delete(compatibilityLauncher);

    WriteFixture(rootPath: disc1, relativePath: "MW4RemasteredCompatLauncher.exe", contents: "owned helper");
    var compatibilityDestination = Path.Combine(transactionRoot, "compatible-installed", "vengeance");
    new StagedInstallTransaction().Execute(new InstallPlan("vengeance", new[]
    {
        new InstallFile(disc1, "MW4.EXE", "MW4.exe"),
        new InstallFile(disc1, "MW4RemasteredCompatLauncher.exe", "MW4RemasteredCompatLauncher.exe"),
    }), compatibilityDestination);
    var compatibleStatus = new InstallStatusReader(Path.Combine(transactionRoot, "compatible-installed")).Read().Single(item => item.Product.Id == "vengeance");
    new LaunchOrchestrator(processStarter).Launch(compatibleStatus);
    var compatibleStart = processStarter.LastStart;
    Check(compatibleStart is not null && compatibleStart.FileName == compatibleStatus.CompatibilityLaunchPath &&
          compatibleStart.ArgumentList.Count == 1 &&
          compatibleStart.ArgumentList[0].Equals("MW4.exe", StringComparison.OrdinalIgnoreCase),
        "launch orchestration routes a manifest-owned compatibility installation through its verified adjacent helper");

    WriteFixture(destination, "Saves/pilot.sav", "user-owned save");
    Check(new InstallManifestVerifier().Verify(destination, InstallVerificationScope.OwnedFiles).IsValid, "owned-file verification permits unowned user data");
    Check(!new InstallManifestVerifier().Verify(destination, InstallVerificationScope.ExactTree).IsValid, "exact-tree verification still reports unowned files for staging and release gates");
    var statusWithSave = new InstallStatusReader(Path.Combine(transactionRoot, "installed")).Read().Single(item => item.Product.Id == "vengeance");
    Check(statusWithSave.State == ProductInstallState.Ready, "user-owned save data does not disable a verified game");

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

    using var cancelledSource = new CancellationTokenSource();
    cancelledSource.Cancel();
    var cancelledDestination = Path.Combine(transactionRoot, "installed", "cancelled");
    var transactionCancellationObserved = false;
    try
    {
        new StagedInstallTransaction().Execute(new InstallPlan("vengeance", new[]
        {
            new InstallFile(disc1, "MW4.EXE", "MW4.EXE"),
        }), cancelledDestination, cancelledSource.Token);
    }
    catch (OperationCanceledException)
    {
        transactionCancellationObserved = true;
    }
    Check(transactionCancellationObserved && !Directory.Exists(cancelledDestination), "cancelled transaction leaves no destination");

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
    Check(!destinations.Contains("SECDRV.SYS") && !destinations.Contains("SETUP.EXE") &&
        !destinations.Contains("MW4.ICD") && !destinations.Contains("DPlayerX.dll"),
        "Vengeance plan excludes setup and SafeDisc components");
}
finally
{
    if (Directory.Exists(planRoot)) Directory.Delete(planRoot, true);
}

var blackKnightPlanRoot = Path.Combine(Path.GetTempPath(), "mw4-remastered-bk-plan-test-" + Guid.NewGuid().ToString("N"));
try
{
    var disc = Path.Combine(blackKnightPlanRoot, "disc");
    CreateLayoutFixture("black-knight-disc-1", disc);
    WriteFixture(disc, "AUTOCO_1.EXE", "autoconfig");
    WriteFixture(disc, "SCRIPT_1.DLL", "scripts");
    WriteFixture(disc, "FONTS/MECH.FNT", "font");
    WriteFixture(disc, "MW4X/LANGUAGE.DLL", "language");
    WriteFixture(disc, "MW4X/DRVMGT.DLL", "disc management");
    WriteFixture(disc, "MW4X/DSETUP.DLL", "directx setup");
    WriteFixture(disc, "MW4X/SECDRV.SYS", "safedisc driver");
    WriteFixture(disc, "SETUP.EXE", "legacy setup");
    var compatibilityRoot = Path.Combine(blackKnightPlanRoot, "compatibility");
    WriteFixture(compatibilityRoot, "MW4RemasteredCompatLauncher.exe", "synthetic helper");
    WriteFixture(compatibilityRoot, "version.dll", "synthetic loader");
    WriteFixture(compatibilityRoot, "LICENSE.txt", "synthetic license");
    WriteFixture(compatibilityRoot, "source.zip", "synthetic corresponding source");
    var compatibility = new QualifiedCompatibilityPayload(
        compatibilityRoot,
        new[]
        {
            QualifiedFile(compatibilityRoot, "MW4RemasteredCompatLauncher.exe", "MW4RemasteredCompatLauncher.exe"),
            QualifiedFile(compatibilityRoot, "version.dll", "version.dll"),
            QualifiedFile(compatibilityRoot, "LICENSE.txt", "Licenses/SafeDiscLoader2-GPL-3.0.txt"),
        },
        new[]
        {
            QualifiedSupportFile(compatibilityRoot, "source.zip"),
        },
        requireExactInventory: true);
    var builder = new BlackKnightInstallPlanBuilder(compatibility, new MediaInspectionService(), new DirectoryMediaInventory());
    var plan = builder.Build(disc);
    var destinations = plan.Files.Select(file => file.DestinationRelativePath).ToHashSet(StringComparer.OrdinalIgnoreCase);
    Check(plan.ProductId == "black-knight" && destinations.Contains("MW4X.exe"), "Black Knight plan installs the untouched executable from media");
    Check(plan.Files.Single(file => file.DestinationRelativePath.Equals("MW4X.exe", StringComparison.OrdinalIgnoreCase)).SourceRelativePath.Equals("MW4X/MW4X.EXE", StringComparison.OrdinalIgnoreCase), "Black Knight executable comes from recognized media, not a user-supplied replacement");
    Check(destinations.Contains("MW4RemasteredCompatLauncher.exe") && destinations.Contains("version.dll") && destinations.Contains("Licenses/SafeDiscLoader2-GPL-3.0.txt"), "Black Knight plan owns the exact internal compatibility bundle");
    Check(!destinations.Contains("source.zip"), "Black Knight plan validates but does not install the corresponding-source archive");
    Check(destinations.Contains("AutoConfig.exe") && destinations.Contains("ScriptStrings.dll"), "Black Knight plan expands installed root names");
    Check(destinations.Contains("FONTS/MECH.FNT") && destinations.Contains("LANGUAGE.DLL") && destinations.Contains("DRVMGT.DLL"), "Black Knight plan copies game data and flattens required runtime files");
    Check(!destinations.Contains("DSETUP.DLL") && !destinations.Contains("SECDRV.SYS") && !destinations.Contains("SETUP.EXE"), "Black Knight plan excludes setup and the obsolete SafeDisc driver");

    WriteFixture(compatibilityRoot, "unexpected.bin", "must not enter the bundle");
    var compatibilityExtraRejected = false;
    try
    {
        builder.Build(disc);
    }
    catch (InvalidDataException)
    {
        compatibilityExtraRejected = true;
    }
    Check(compatibilityExtraRejected, "Black Knight planning rejects unexpected compatibility-bundle files");
    File.Delete(Path.Combine(compatibilityRoot, "unexpected.bin"));

    Directory.CreateDirectory(Path.Combine(compatibilityRoot, "unexpected-directory"));
    var compatibilityDirectoryRejected = false;
    try
    {
        builder.Build(disc);
    }
    catch (InvalidDataException)
    {
        compatibilityDirectoryRejected = true;
    }
    Check(compatibilityDirectoryRejected, "Black Knight planning rejects unexpected compatibility-bundle directories");
    Directory.Delete(Path.Combine(compatibilityRoot, "unexpected-directory"));

    File.AppendAllText(Path.Combine(compatibilityRoot, "version.dll"), "tampered");
    var compatibilityTamperRejected = false;
    try
    {
        builder.Build(disc);
    }
    catch (InvalidDataException)
    {
        compatibilityTamperRejected = true;
    }
    Check(compatibilityTamperRejected, "Black Knight planning rejects a modified internal compatibility payload");
}
finally
{
    if (Directory.Exists(blackKnightPlanRoot)) Directory.Delete(blackKnightPlanRoot, true);
}

var cabinetRoot = Path.Combine(Path.GetTempPath(), "mw4-remastered-cabinet-test-" + Guid.NewGuid().ToString("N"));
try
{
    Directory.CreateDirectory(cabinetRoot);
    var source = Path.Combine(cabinetRoot, "source");
    WriteFixture(source, "GAME/RESOURCE/core.mw4", "core");
    WriteFixture(source, "GAME/RESOURCE/MAPS/test.mw4", "map");
    var archive = Path.Combine(cabinetRoot, "fixture.cab");
    ZipFile.CreateFromDirectory(source, archive, CompressionLevel.NoCompression, includeBaseDirectory: false);
    var destination = Path.Combine(cabinetRoot, "extracted");
    var extracted = new CabinetPayloadExtractor().ExtractGamePayload(archive, destination);
    Check(extracted.Count == 2 && File.Exists(Path.Combine(destination, "RESOURCE", "core.mw4")), "cabinet extractor commits only the validated GAME payload");

    var unsafeSource = Path.Combine(cabinetRoot, "unsafe-source");
    WriteFixture(unsafeSource, "SETUP.EXE", "outside game payload");
    var unsafeArchive = Path.Combine(cabinetRoot, "unsafe.cab");
    ZipFile.CreateFromDirectory(unsafeSource, unsafeArchive, CompressionLevel.NoCompression, includeBaseDirectory: false);
    var unsafeRejected = false;
    try
    {
        new CabinetPayloadExtractor().ExtractGamePayload(unsafeArchive, Path.Combine(cabinetRoot, "unsafe-output"));
    }
    catch (InvalidDataException)
    {
        unsafeRejected = true;
    }
    Check(unsafeRejected && !Directory.Exists(Path.Combine(cabinetRoot, "unsafe-output")), "cabinet extractor rejects entries outside GAME before extraction");

    var traversalArchive = Path.Combine(cabinetRoot, "traversal.cab");
    using (var archiveStream = File.Create(traversalArchive))
    using (var zip = new ZipArchive(archiveStream, ZipArchiveMode.Create))
    {
        using var writer = new StreamWriter(zip.CreateEntry("GAME/../escape.bin").Open());
        writer.Write("escape");
    }
    var traversalRejected = false;
    try
    {
        new CabinetPayloadExtractor().ExtractGamePayload(traversalArchive, Path.Combine(cabinetRoot, "traversal-output"));
    }
    catch (InvalidDataException)
    {
        traversalRejected = true;
    }
    Check(traversalRejected && !Directory.Exists(Path.Combine(cabinetRoot, "traversal-output")), "cabinet extractor rejects traversal before extraction");
}
finally
{
    if (Directory.Exists(cabinetRoot)) Directory.Delete(cabinetRoot, true);
}

var mercenariesPlanRoot = Path.Combine(Path.GetTempPath(), "mw4-remastered-mercs-plan-test-" + Guid.NewGuid().ToString("N"));
try
{
    var disc1 = Path.Combine(mercenariesPlanRoot, "disc1");
    var disc2 = Path.Combine(mercenariesPlanRoot, "disc2");
    var cabinet = Path.Combine(mercenariesPlanRoot, "cabinet");
    CreateLayoutFixture("mercenaries-disc-1", disc1);
    CreateLayoutFixture("mercenaries-disc-2", disc2);
    WriteFixture(disc1, "AUTOCO_1.EXE", "autoconfig");
    WriteFixture(disc1, "GUNTIC_1.DLL", "gun ticket");
    WriteFixture(disc1, "CDAC14BA.DLL", "c-dilla");
    WriteFixture(disc1, "MW4MERCS.ICD", "safedisc game image");
    WriteFixture(disc2, "CONTENT/MERCSS_1/FILES/NEXTMO_1.WAV", "audio");
    WriteFixture(disc2, "Crack/MW4Mercs.exe", "media crack");
    WriteFixture(cabinet, "RESOURCE/CORE.MW4", "core");
    WriteFixture(cabinet, "RESOURCE/PROPS.MW4", "props");
    WriteFixture(cabinet, "RESOURCE/TEXTURES.MW4", "textures");
    var replacement = Path.Combine(mercenariesPlanRoot, "replacement", "MW4Mercs.exe");
    WriteFixture(Path.GetDirectoryName(replacement)!, Path.GetFileName(replacement), "synthetic Mercenaries executable");
    var replacementHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(replacement))).ToLowerInvariant();

    var builder = new MercenariesInstallPlanBuilder(replacementHash, new MediaInspectionService(), new DirectoryMediaInventory());
    var plan = builder.Build(disc1, disc2, cabinet, replacement);
    var destinations = plan.Files.Select(file => file.DestinationRelativePath).ToHashSet(StringComparer.OrdinalIgnoreCase);
    Check(plan.ProductId == "mercenaries" && destinations.Contains("MW4Mercs.exe"), "Mercenaries plan supplies the qualified compatibility executable");
    Check(destinations.Contains("AutoConfig.exe") && destinations.Contains("GunTicket.dll"), "Mercenaries plan expands installed root names");
    Check(destinations.Contains("Content/MercsShellScripts/FILES/NEXTMO_1.WAV"), "Mercenaries plan restores the MercsShellScripts directory name");
    Check(destinations.Contains("RESOURCE/CORE.MW4") && !destinations.Contains("MW4MERCS.ICD") && !destinations.Contains("CDAC14BA.DLL"), "Mercenaries plan combines cabinet data while excluding disc protection");
}
finally
{
    if (Directory.Exists(mercenariesPlanRoot)) Directory.Delete(mercenariesPlanRoot, true);
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

InstallationCoordinatorSmoke.Run(failures);
InstallDestinationPlannerSmoke.Run(failures);
IsoMediaSessionSmoke.Run(failures);
IsoArchiveExtractorSmoke.Run(failures);
MediaSourceInspectorSmoke.Run(failures);
MediaSelectionSetSmoke.Run(failures);
MediaSourceSessionSmoke.Run(failures);
MediaSelectionSessionSmoke.Run(failures);
MechPakResourceOverlayPlanSmoke.Run(failures);
OwnedInstallOverlayTransactionSmoke.Run(failures);

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

QualifiedCompatibilityFile QualifiedFile(string rootPath, string sourceRelativePath, string destinationRelativePath)
{
    var path = Path.Combine(rootPath, sourceRelativePath.Replace('/', Path.DirectorySeparatorChar));
    var hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
    return new QualifiedCompatibilityFile(sourceRelativePath, destinationRelativePath, hash);
}

QualifiedCompatibilitySupportFile QualifiedSupportFile(string rootPath, string sourceRelativePath)
{
    var path = Path.Combine(rootPath, sourceRelativePath.Replace('/', Path.DirectorySeparatorChar));
    var hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
    return new QualifiedCompatibilitySupportFile(sourceRelativePath, hash);
}

sealed class RecordingProcessStarter : IProcessStarter
{
    public System.Diagnostics.ProcessStartInfo? LastStart { get; private set; }

    public void Start(System.Diagnostics.ProcessStartInfo startInfo)
    {
        LastStart = startInfo;
    }
}
