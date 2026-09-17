using MW4Remastered.Core;
using MW4Remastered.Core.Install;
using MW4Remastered.Core.Launch;
using MW4Remastered.Core.Media;
using System.Security.Cryptography;
using System.IO.Compression;
using System.Text.Json;

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

var mercenariesSecondLayerVector = Convert.FromHexString("000102030405060708090A0B0C0D0E0F");
SafeDisc15020SecondLayer.DecodePage(
    mercenariesSecondLayerVector,
    SafeDisc15020SecondLayerProfile.Mercenaries);
Check(
    Convert.ToHexString(mercenariesSecondLayerVector).Equals(
        "A6D0D913842538B95A1A9171B462FEDF",
        StringComparison.Ordinal),
    "Mercenaries SafeDisc 1.50.20 second layer matches its independent modifier-chain vector");

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
        rejected = exception.Message.Contains("Unsupported SafeDisc input MW4.EXE SHA-256", StringComparison.Ordinal);
    }
    Check(rejected, "Vengeance transform rejects media that does not match the qualified retail revision");
    Check(!Directory.Exists(Path.Combine(transformRejectionRoot, "scratch")),
        "Vengeance transform does not write output after input validation fails");
}
finally
{
    if (Directory.Exists(transformRejectionRoot)) Directory.Delete(transformRejectionRoot, true);
}

var patch3TransformRejectionRoot = Path.Combine(Path.GetTempPath(), "mw4-remastered-patch3-transform-rejection-" + Guid.NewGuid().ToString("N"));
try
{
    Directory.CreateDirectory(patch3TransformRejectionRoot);
    File.WriteAllText(Path.Combine(patch3TransformRejectionRoot, "MW4.EXE"), "unsupported Patch 3 loader");
    File.WriteAllText(Path.Combine(patch3TransformRejectionRoot, "MW4.ICD"), "unsupported Patch 3 image");
    File.WriteAllText(Path.Combine(patch3TransformRejectionRoot, "DPLAYERX.DLL"), "unsupported player");
    var rejected = false;
    try
    {
        new VengeancePatch3ExecutableTransform().Transform(
            patch3TransformRejectionRoot,
            Path.Combine(patch3TransformRejectionRoot, "scratch"));
    }
    catch (InvalidDataException exception)
    {
        rejected = exception.Message.Contains("Unsupported SafeDisc input MW4.EXE SHA-256", StringComparison.Ordinal);
    }
    Check(rejected, "Vengeance Patch 3 transform rejects inputs outside the qualified revision");
    Check(!Directory.Exists(Path.Combine(patch3TransformRejectionRoot, "scratch")),
        "Vengeance Patch 3 transform writes nothing after input validation fails");
}
finally
{
    if (Directory.Exists(patch3TransformRejectionRoot)) Directory.Delete(patch3TransformRejectionRoot, true);
}

var mercenariesTransformRejectionRoot = Path.Combine(Path.GetTempPath(), "mw4-remastered-mercenaries-transform-rejection-" + Guid.NewGuid().ToString("N"));
try
{
    Directory.CreateDirectory(mercenariesTransformRejectionRoot);
    File.WriteAllText(Path.Combine(mercenariesTransformRejectionRoot, "MW4MERCS.EXE"), "unsupported loader");
    File.WriteAllText(Path.Combine(mercenariesTransformRejectionRoot, "MW4MERCS.ICD"), "unsupported image");
    File.WriteAllText(Path.Combine(mercenariesTransformRejectionRoot, "DPLAYERX.DLL"), "unsupported player");
    var rejected = false;
    try
    {
        new MercenariesRetailExecutableTransform().Transform(
            mercenariesTransformRejectionRoot,
            Path.Combine(mercenariesTransformRejectionRoot, "scratch"));
    }
    catch (InvalidDataException exception)
    {
        rejected = exception.Message.Contains("Unsupported SafeDisc input MW4MERCS.EXE SHA-256", StringComparison.Ordinal);
    }
    Check(rejected, "Mercenaries transform rejects media that does not match the qualified retail revision");
    Check(!Directory.Exists(Path.Combine(mercenariesTransformRejectionRoot, "scratch")),
        "Mercenaries transform does not write output after input validation fails");
}
finally
{
    if (Directory.Exists(mercenariesTransformRejectionRoot)) Directory.Delete(mercenariesTransformRejectionRoot, true);
}

var mercenariesPr1TransformRejectionRoot = Path.Combine(Path.GetTempPath(), "mw4-remastered-mercenaries-pr1-transform-rejection-" + Guid.NewGuid().ToString("N"));
try
{
    Directory.CreateDirectory(mercenariesPr1TransformRejectionRoot);
    File.WriteAllText(Path.Combine(mercenariesPr1TransformRejectionRoot, "MW4Mercs.exe"), "unsupported PR1 loader");
    File.WriteAllText(Path.Combine(mercenariesPr1TransformRejectionRoot, "MW4MERCS.ICD"), "unsupported PR1 image");
    File.WriteAllText(Path.Combine(mercenariesPr1TransformRejectionRoot, "DPLAYERX.DLL"), "unsupported player");
    var rejected = false;
    try
    {
        new MercenariesPr1ExecutableTransform().Transform(
            mercenariesPr1TransformRejectionRoot,
            Path.Combine(mercenariesPr1TransformRejectionRoot, "scratch"));
    }
    catch (InvalidDataException exception)
    {
        rejected = exception.Message.Contains("Unsupported SafeDisc input MW4Mercs.exe SHA-256", StringComparison.Ordinal);
    }
    Check(rejected, "Mercenaries PR1 transform rejects inputs outside the qualified revision");
    Check(!Directory.Exists(Path.Combine(mercenariesPr1TransformRejectionRoot, "scratch")),
        "Mercenaries PR1 transform writes nothing after input validation fails");
}
finally
{
    if (Directory.Exists(mercenariesPr1TransformRejectionRoot)) Directory.Delete(mercenariesPr1TransformRejectionRoot, true);
}

var blackKnightPr1TransformRejectionRoot = Path.Combine(Path.GetTempPath(), "mw4-remastered-black-knight-runtime-rejection-" + Guid.NewGuid().ToString("N"));
try
{
    Directory.CreateDirectory(blackKnightPr1TransformRejectionRoot);
    var runtimePath = Path.Combine(blackKnightPr1TransformRejectionRoot, "version.dll");
    var outputPath = Path.Combine(blackKnightPr1TransformRejectionRoot, "output");
    File.WriteAllText(runtimePath, "unsupported Black Knight runtime");
    var rejected = false;
    try
    {
        Directory.CreateDirectory(outputPath);
        BlackKnightRuntimeCompatibility.AddToPayload(runtimePath, outputPath);
    }
    catch (InvalidDataException exception)
    {
        rejected = exception.Message.Contains("Unsupported Black Knight runtime DLL", StringComparison.Ordinal);
    }
    Check(rejected, "Black Knight runtime rejects inputs outside the qualified source build");
    Check(!File.Exists(Path.Combine(outputPath, "MW4X", "version.dll")), "Black Knight runtime writes no DLL after input validation fails");
}
finally
{
    if (Directory.Exists(blackKnightPr1TransformRejectionRoot)) Directory.Delete(blackKnightPr1TransformRejectionRoot, true);
}

Check(ProductCatalog.All.Count == 5, "catalog contains exactly three games and two packs");
Check(ProductCatalog.All.Count(item => item.Kind == ProductKind.Game) == 3, "catalog contains three games");
Check(ProductCatalog.All.Count(item => item.Kind == ProductKind.OptionalPack) == 2, "catalog contains two optional packs");
Check(MediaCatalog.Layouts.Count == 8, "media catalog contains seven disc/pack layouts plus the qualified Mercenaries PR1 source");
Check(ProductDependencies.AreSatisfied("vengeance", Array.Empty<string>()), "Vengeance has no base-game dependency");
Check(!ProductDependencies.AreSatisfied("black-knight", Array.Empty<string>()) &&
    ProductDependencies.AreSatisfied("black-knight", new[] { "vengeance" }), "Black Knight requires Vengeance");
Check(ProductDependencies.AreSatisfied("mercenaries", Array.Empty<string>()), "Mercenaries remains independently selectable");
Check(!ProductDependencies.AreSatisfied("inner-sphere", Array.Empty<string>()) &&
    !ProductDependencies.AreSatisfied("clan", Array.Empty<string>()), "both retail Mech Paks require the Vengeance base");

var registrationRoot = Path.Combine(Path.GetTempPath(), "mw4-remastered-registration-contract");
var vengeanceRegistration = LegacyGameRegistration.Describe(new ProductStatus(
    ProductCatalog.All.Single(item => item.Id == "vengeance"), ProductInstallState.Ready,
    Path.Combine(registrationRoot, "vengeance", "MW4.exe"), null, null,
    Path.Combine(registrationRoot, "vengeance"), "synthetic"));
Check(vengeanceRegistration is not null && vengeanceRegistration.Use32BitView &&
    vengeanceRegistration.KeyPath.EndsWith(
        @"Software\Microsoft\Microsoft Games\MechWarrior Vengeance",
        StringComparison.OrdinalIgnoreCase) && vengeanceRegistration.Version == 4 &&
    vengeanceRegistration.CdPath == Path.Combine(registrationRoot, "vengeance"),
    "Vengeance registration targets its direct 32-bit per-user settings record");
var qualifiedVengeanceRegistration = vengeanceRegistration ?? throw new InvalidOperationException("Vengeance registration description was not created.");
Check(LegacyGameRegistration.ValuesMatchOrWereConsumed(
        qualifiedVengeanceRegistration, qualifiedVengeanceRegistration.CdPath,
        qualifiedVengeanceRegistration.ExecutablePath, qualifiedVengeanceRegistration.Version),
    "launch validation accepts the exact setup-owned registration");
Check(LegacyGameRegistration.ValuesMatchOrWereConsumed(qualifiedVengeanceRegistration, null, null, null),
    "launch validation accepts the legacy game's all-consumed setup registration state");
Check(!LegacyGameRegistration.ValuesMatchOrWereConsumed(
        qualifiedVengeanceRegistration, qualifiedVengeanceRegistration.CdPath, null, qualifiedVengeanceRegistration.Version),
    "launch validation rejects a partially consumed setup registration");
Check(!LegacyGameRegistration.ValuesMatchOrWereConsumed(
        qualifiedVengeanceRegistration, qualifiedVengeanceRegistration.CdPath,
        Path.Combine(registrationRoot, "other", "MW4.exe"), qualifiedVengeanceRegistration.Version),
    "launch validation rejects a registration owned by another executable");
Check(!LegacyGameRegistration.ValuesMatchOrWereConsumed(
        qualifiedVengeanceRegistration, Path.Combine(registrationRoot, "other"),
        qualifiedVengeanceRegistration.ExecutablePath, qualifiedVengeanceRegistration.Version),
    "launch validation rejects a registration with another media path");
Check(!LegacyGameRegistration.ValuesMatchOrWereConsumed(
        qualifiedVengeanceRegistration, qualifiedVengeanceRegistration.CdPath,
        qualifiedVengeanceRegistration.ExecutablePath, qualifiedVengeanceRegistration.Version + 1),
    "launch validation rejects a registration with another version");

foreach (var (productId, executableName, productKey) in new[]
{
    ("black-knight", "MW4X.exe", "MechWarrior Black Knight"),
    ("mercenaries", "MW4Mercs.exe", "MechWarrior Mercenaries"),
})
{
    var installPath = Path.Combine(registrationRoot, productId);
    var registration = LegacyGameRegistration.Describe(new ProductStatus(
        ProductCatalog.All.Single(item => item.Id == productId), ProductInstallState.Ready,
        Path.Combine(installPath, executableName), null, null, installPath, "synthetic"));
    Check(registration is not null && registration.Use32BitView &&
        registration.KeyPath.Equals($@"Software\Microsoft\Microsoft Games\{productKey}", StringComparison.OrdinalIgnoreCase) &&
        registration.Version == 4 &&
        registration.CdPath == (productId == "black-knight" ? @"L:\" : installPath),
        $"{productKey} registration targets its direct 32-bit per-user product record");
}
Check(LegacyGameRegistration.Describe(new ProductStatus(
    ProductCatalog.All.Single(item => item.Id == "inner-sphere"), ProductInstallState.Missing,
    null, null, null, null, "synthetic")) is null,
    "optional packs never create legacy game registration records");

var root = Path.Combine(Path.GetTempPath(), "mw4-remastered-core-test-" + Guid.NewGuid().ToString("N"));
try
{
    Directory.CreateDirectory(Path.Combine(root, "vengeance"));
    File.WriteAllText(Path.Combine(root, "vengeance", "MW4.exe"), "synthetic fixture");
    Directory.CreateDirectory(Path.Combine(root, "mercenaries"));
    var manualRoot = Directory.CreateDirectory(Path.Combine(root, "Manuals")).FullName;
    foreach (var game in ProductCatalog.All.Where(item => item.Kind == ProductKind.Game))
    {
        File.WriteAllText(Path.Combine(manualRoot, game.ManualFileName!), "synthetic manual");
    }
    var statuses = new InstallStatusReader(root).Read().ToDictionary(item => item.Product.Id);
    Check(!statuses["vengeance"].IsInstalled && statuses["vengeance"].State == ProductInstallState.NeedsRepair, "unmanifested Vengeance executable requires repair");
    Check(!statuses["black-knight"].IsInstalled, "missing Black Knight executable stays absent");
    Check(statuses["mercenaries"].State == ProductInstallState.NeedsRepair && statuses["mercenaries"].InstallPath is not null,
        "an existing incomplete game directory remains removable as repair-required");
    Check(!statuses["clan"].IsInstalled, "Clan pack is not claimed without verified payload evidence");
    Check(!statuses["inner-sphere"].IsInstalled, "Inner Sphere pack is not claimed without verified payload evidence");
    Check(statuses.Values.Where(item => item.Product.Kind == ProductKind.Game).All(item => item.ManualPath is not null),
        "all three packaged manual paths are discovered independently of game installation health");
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
    Directory.CreateDirectory(Path.Combine(disc1, "MW4X"));
    File.WriteAllText(Path.Combine(disc1, "MW4X", "MW4X.EXE"), "synthetic expansion executable");
    File.SetAttributes(Path.Combine(disc1, "MW4.EXE"), FileAttributes.ReadOnly);
    Directory.CreateDirectory(Path.Combine(disc2, "RESOURCE", "MAPS"));
    File.WriteAllText(Path.Combine(disc2, "RESOURCE", "MAPS", "ALPINE01.MW4"), "synthetic map");

    var destination = Path.Combine(transactionRoot, "installed", "vengeance");
    var plan = new InstallPlan("vengeance", new[]
    {
        new InstallFile(disc1, "MW4.EXE", "MW4.EXE"),
        new InstallFile(disc1, "MW4X/MW4X.EXE", "MW4X/MW4X.EXE"),
        new InstallFile(disc2, "RESOURCE/MAPS/ALPINE01.MW4", "RESOURCE/MAPS/ALPINE01.MW4"),
    }, new[] { "vengeance", "black-knight" });
    var manifest = new StagedInstallTransaction().Execute(plan, destination);
    Check(File.Exists(Path.Combine(destination, "MW4.EXE")), "transaction commits the first source file");
    Check((File.GetAttributes(Path.Combine(destination, "MW4.EXE")) & FileAttributes.ReadOnly) == 0, "transaction makes installed media files writable");
    Check(File.Exists(Path.Combine(destination, "RESOURCE", "MAPS", "ALPINE01.MW4")), "transaction commits the second source file");
    Check(File.Exists(Path.Combine(destination, InstallManifest.RelativePath.Replace('/', Path.DirectorySeparatorChar))), "transaction persists its ownership manifest");
    Check(manifest.SchemaVersion == 2 && manifest.HasComponent("black-knight") &&
        manifest.Files.Count == 3 && manifest.Files.All(file => file.Sha256.Length == 64),
        "schema 2 manifest hashes every installed file and records shared-tree components");
    Check(new InstallManifestVerifier().Verify(destination).IsValid, "manifest verifier accepts the committed tree");
    var installedStatuses = new InstallStatusReader(Path.Combine(transactionRoot, "installed")).Read().ToDictionary(item => item.Product.Id);
    Check(installedStatuses["vengeance"].State == ProductInstallState.Ready && installedStatuses["vengeance"].LaunchPath is not null, "status reader requires a verified ownership manifest before enabling launch");
    Check(installedStatuses["black-knight"].State == ProductInstallState.Ready &&
        installedStatuses["black-knight"].InstallPath == destination &&
        string.Equals(installedStatuses["black-knight"].LaunchPath, Path.Combine(destination, "MW4X", "MW4X.EXE"), StringComparison.OrdinalIgnoreCase),
        $"status reader exposes Black Knight from the shared Vengeance-family manifest and MW4X path ({installedStatuses["black-knight"].State}; {installedStatuses["black-knight"].LaunchPath}; {installedStatuses["black-knight"].Detail})");
    Check(installedStatuses["vengeance"].InstallPath == destination, "status reader exposes the verified product root for ownership-safe removal");
    var configuration = new LegacyGameConfiguration();
    configuration.Ensure(installedStatuses["black-knight"]);
    Check(File.Exists(Path.Combine(destination, "MW4X", "optionsx.ini")),
        "shared-tree Black Knight configuration is seeded beside MW4X.exe rather than at the Vengeance root");
    File.WriteAllText(Path.Combine(destination, "options.ini"), "[joystick]" + Environment.NewLine + "BiThrottleCenter=0.300000" + Environment.NewLine);
    configuration.Ensure(installedStatuses["vengeance"]);
    var processStarter = new RecordingProcessStarter();
    var gameRegistration = new RecordingGameRegistration();
    new LaunchOrchestrator(processStarter, gameRegistration).Launch(installedStatuses["vengeance"]);
    Check(processStarter.LastStart?.FileName == installedStatuses["vengeance"].LaunchPath && processStarter.LastStart?.WorkingDirectory == destination, "launch orchestration uses the verified executable and its working directory");
    Check(gameRegistration.LastValidated == installedStatuses["vengeance"], "launch orchestration only validates setup-owned registration before starting Vengeance");
    var modernArguments = new[] { "-32", "-noautoconfig", "-f", "1024x768", "-gl", "-GameTime.MaxVariableFps", "60", "/gosNoJoystick" };
    var blackKnightArguments = new[] { "-noautoconfigx", "/gosNoJoystick" };
    Check(processStarter.LastStart?.ArgumentList.SequenceEqual(modernArguments) == true,
        "Vengeance requests a 1024x768 fullscreen surface for dgVoodoo presentation");
    var vengeanceOptions = File.ReadAllText(Path.Combine(destination, "options.ini"));
    Check(vengeanceOptions.Contains("[graphics options]", StringComparison.OrdinalIgnoreCase) &&
          vengeanceOptions.Contains("screenwidth=1024", StringComparison.OrdinalIgnoreCase) &&
          vengeanceOptions.Contains("BiThrottleCenter=0.300000", StringComparison.Ordinal),
        "configuration seeding preserves existing controls and adds the graphics page required by pilot scripts");
    configuration.Ensure(installedStatuses["vengeance"]);
    Check(File.ReadAllText(Path.Combine(destination, "options.ini")) == vengeanceOptions,
        "configuration seeding is byte-stable after the required graphics page exists");
    File.WriteAllText(Path.Combine(destination, "options.ini"), vengeanceOptions
        .Replace("screenwidth=1024", "ScreenWidth=800", StringComparison.OrdinalIgnoreCase)
        .Replace("screenheight=768", "ScreenHeight=600", StringComparison.OrdinalIgnoreCase)
        .Replace("bitdepth=32", "bitdepth=16", StringComparison.OrdinalIgnoreCase));
    configuration.Ensure(installedStatuses["vengeance"]);
    var repairedOptions = File.ReadAllText(Path.Combine(destination, "options.ini"));
    Check(repairedOptions.Contains("ScreenWidth=1024", StringComparison.OrdinalIgnoreCase) &&
          repairedOptions.Contains("ScreenHeight=768", StringComparison.OrdinalIgnoreCase) &&
          repairedOptions.Contains("bitdepth=32", StringComparison.OrdinalIgnoreCase) &&
          repairedOptions.Contains("BiThrottleCenter=0.300000", StringComparison.Ordinal),
        "configuration guard repairs the resolution page MW4 rewrites during startup without discarding controls");
    var mercenaryRoot = Directory.CreateDirectory(Path.Combine(transactionRoot, "mercenary-launch")).FullName;
    var mercenaryExecutable = Path.Combine(mercenaryRoot, "MW4Mercs.exe");
    File.WriteAllText(mercenaryExecutable, "synthetic executable");
    var mercenaryProduct = ProductCatalog.All.Single(item => item.Id == "mercenaries");
    var mercenaryStatus = new ProductStatus(
        mercenaryProduct, ProductInstallState.Ready, mercenaryExecutable, null, null, mercenaryRoot, "synthetic");
    configuration.Ensure(mercenaryStatus);
    new LaunchOrchestrator(processStarter, gameRegistration).Launch(mercenaryStatus);
    Check(processStarter.LastStart?.ArgumentList.SequenceEqual(modernArguments) == true,
        "Mercenaries launch seeds configuration and bypasses legacy joystick enumeration");
    Check(File.ReadAllText(Path.Combine(mercenaryRoot, "options.ini")).Contains("[graphics options]", StringComparison.OrdinalIgnoreCase),
        "Mercenaries receives the required graphics page before launch");

    var blackKnightRoot = Directory.CreateDirectory(Path.Combine(transactionRoot, "black-knight-launch")).FullName;
    var blackKnightLaunchExecutable = Path.Combine(blackKnightRoot, "MW4X.exe");
    File.WriteAllText(blackKnightLaunchExecutable, "synthetic executable");
    var blackKnightLaunchProduct = ProductCatalog.All.Single(item => item.Id == "black-knight");
    var blackKnightLaunchStatus = new ProductStatus(
        blackKnightLaunchProduct, ProductInstallState.Ready, blackKnightLaunchExecutable, null, null,
        blackKnightRoot, "synthetic");
    configuration.Ensure(blackKnightLaunchStatus);
    new LaunchOrchestrator(processStarter, gameRegistration).Launch(blackKnightLaunchStatus);
    Check(processStarter.LastStart?.FileName == blackKnightLaunchExecutable &&
        processStarter.LastStart.ArgumentList.SequenceEqual(blackKnightArguments),
        "Black Knight starts directly through dgVoodoo fullscreen presentation with no process-injection helper");
    Check(File.ReadAllText(Path.Combine(blackKnightRoot, "optionsx.ini")).Contains("[graphics options]", StringComparison.OrdinalIgnoreCase),
        "Black Knight receives its title-specific required optionsx graphics page");

    var alreadyRunningState = new RecordingGameProcessState { Running = true };
    var duplicateBlocked = false;
    try
    {
        new LaunchOrchestrator(processStarter, gameRegistration, alreadyRunningState).Launch(new ProductStatus(
            mercenaryProduct, ProductInstallState.Ready, mercenaryExecutable, null, null, mercenaryRoot, "synthetic"));
    }
    catch (InvalidOperationException exception)
    {
        duplicateBlocked = exception.Message.Contains("already running", StringComparison.OrdinalIgnoreCase);
    }
    Check(duplicateBlocked && alreadyRunningState.LastExecutablePath == mercenaryExecutable,
        "launch orchestration blocks a second instance of the same installed game executable");

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
    new LaunchOrchestrator(processStarter, gameRegistration).Launch(statusWithUnownedHelper);
    Check(processStarter.LastStart?.FileName == statusWithUnownedHelper.LaunchPath,
        "launch orchestration ignores an adjacent helper that is not owned by the verified manifest");
    File.Delete(compatibilityLauncher);

    var blackKnightExecutable = Path.Combine(destination, "MW4X", "MW4X.EXE");
    var blackKnightProduct = ProductCatalog.All.Single(item => item.Id == "black-knight");
    new LaunchOrchestrator(processStarter, gameRegistration).Launch(new ProductStatus(
        blackKnightProduct, ProductInstallState.Ready, blackKnightExecutable, null,
        null, destination, "synthetic"));
    Check(processStarter.LastStart?.FileName == blackKnightExecutable &&
        processStarter.LastStart.ArgumentList.SequenceEqual(blackKnightArguments),
        "Black Knight launch uses its title-specific autoconfig bypass without a process-injection helper");

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

    var reinstallDestination = Path.Combine(transactionRoot, "installed", "reinstall-with-user-data");
    WriteFixture(reinstallDestination, "Mechwarrior4.txt", "preserved runtime log");
    WriteFixture(reinstallDestination, "options.ini", "preserved configuration");
    WriteFixture(reinstallDestination, "Saves/pilot.sav", "preserved pilot");
    new StagedInstallTransaction().Execute(new InstallPlan("vengeance", new[]
    {
        new InstallFile(disc1, "MW4.EXE", "MW4.EXE"),
    }), reinstallDestination);
    Check(File.Exists(Path.Combine(reinstallDestination, "MW4.EXE")),
        "reinstall commits fresh owned payload into a directory containing preserved user data");
    Check(File.ReadAllText(Path.Combine(reinstallDestination, "Mechwarrior4.txt")) == "preserved runtime log" &&
          File.ReadAllText(Path.Combine(reinstallDestination, "options.ini")) == "preserved configuration" &&
          File.ReadAllText(Path.Combine(reinstallDestination, "Saves", "pilot.sav")) == "preserved pilot",
        "reinstall preserves existing logs, configuration, and saves byte-for-byte");
    Check(new InstallManifestVerifier().Verify(reinstallDestination, InstallVerificationScope.OwnedFiles).IsValid,
        "reinstalled owned payload verifies with preserved user data present");

    var collisionDestination = Path.Combine(transactionRoot, "installed", "reinstall-collision");
    WriteFixture(collisionDestination, "MW4.EXE", "user collision");
    var collisionPreserved = false;
    try
    {
        new StagedInstallTransaction().Execute(new InstallPlan("vengeance", new[]
        {
            new InstallFile(disc1, "MW4.EXE", "MW4.EXE"),
        }), collisionDestination);
    }
    catch (IOException error)
    {
        collisionPreserved = error.Message.Contains("collides", StringComparison.OrdinalIgnoreCase);
    }
    Check(collisionPreserved && File.ReadAllText(Path.Combine(collisionDestination, "MW4.EXE")) == "user collision",
        "reinstall refuses owned-path collisions without modifying existing content");
    Check(!Directory.EnumerateDirectories(Path.Combine(transactionRoot, "installed"), ".reinstall-collision.staging-*").Any() &&
          !Directory.EnumerateDirectories(Path.Combine(transactionRoot, "installed"), ".reinstall-collision.previous-*").Any(),
        "reinstall collision leaves no staging or previous-tree residue");

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
    WriteFixture(disc1, "DSETUP.DLL", "directx runtime query");
    WriteFixture(disc1, "CONTENT/SHELLS_1/FILES/STUTTE_1.WAV", "audio");
    WriteFixture(disc1, "CONTENT/TEXTURES/CUSTOM_1/CSTMDCAL.TXT", "decals");
    WriteFixture(disc1, "CONTENT/MOVIES/BURNLO_1.AVI", "menu loop");
    WriteFixture(disc1, "CONTENT/MOVIES/CLOSEA_1.MPG", "closing a");
    WriteFixture(disc1, "CONTENT/MOVIES/CLOSEB_1.MPG", "closing b");
    WriteFixture(disc1, "CONTENT/MOVIES/CLOSIN_1.MPG", "closing intro");
    WriteFixture(disc1, "RESOURCE/MISSIONS/CENTRA_1.TGA", "central park preview");
    WriteFixture(disc1, "RESOURCE/MISSIONS/EDITOR_1.MW4", "editor template");
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
    Check(destinations.Contains("DSetup.dll"), "Vengeance plan retains the DirectX version-query runtime imported by the game executable");
    Check(destinations.Contains("Content/ShellScripts/Files/StutterShark_music.wav"),
        "Vengeance plan restores the installed ShellScripts audio path");
    Check(destinations.Contains("Content/Textures/customdecals/CSTMDCAL.TXT"), "Vengeance plan restores the custom decals directory name");
    Check(destinations.Contains("Content/Movies/Burnloop_lr_15.avi") &&
        destinations.Contains("Content/Movies/Close a.mpg") &&
        destinations.Contains("Content/Movies/Close b.mpg") &&
        destinations.Contains("Content/Movies/Closinga1.mpg"),
        "Vengeance plan restores every renamed movie from the original setup table");
    Check(destinations.Contains("Resource/Missions/centralpark.tga") &&
        destinations.Contains("Resource/Missions/editortemplate.mw4"),
        "Vengeance plan restores renamed mission assets from the original setup table");
    var setupTableNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["CONTENT/MOVIES/BURNLO_1.AVI"] = "Content/Movies/Burnloop_lr_15.avi",
        ["CONTENT/MOVIES/CLOSEA_1.MPG"] = "Content/Movies/Close a.mpg",
        ["CONTENT/MOVIES/CLOSEB_1.MPG"] = "Content/Movies/Close b.mpg",
        ["CONTENT/MOVIES/CLOSIN_1.MPG"] = "Content/Movies/Closinga1.mpg",
        ["CONTENT/SHELLS_1/FILES/STUTTE_1.WAV"] = "Content/ShellScripts/Files/StutterShark_music.wav",
        ["CONTENT/TEXTURES/CUSTOM_1/CSTMDCAL.TXT"] = "Content/Textures/customdecals/CSTMDCAL.TXT",
        ["RESOURCE/MISSIONS/CENTRA_1.TGA"] = "Resource/Missions/centralpark.tga",
        ["RESOURCE/MISSIONS/EDITOR_1.MW4"] = "Resource/Missions/editortemplate.mw4",
        ["RESOURCE/MISSIONS/FROSTB_1.TGA"] = "Resource/Missions/frostbite.tga",
        ["RESOURCE/MISSIONS/GATORB_1.TGA"] = "Resource/Missions/gatorbait.tga",
        ["RESOURCE/MISSIONS/INNERC_1.TGA"] = "Resource/Missions/innercity.tga",
        ["RESOURCE/MISSIONS/PALACE_1.TGA"] = "Resource/Missions/PalaceGates.tga",
        ["RESOURCE/MISSIONS/TIMBER_1.TGA"] = "Resource/Missions/timberline.tga",
    };
    Check(setupTableNames.All(entry => VengeanceMediaPathMap.Map(entry.Key) == entry.Value),
        "Vengeance path map reproduces every renamed content/resource entry reviewed from the original setup table");
    var legacyManifest = new InstallManifest(2, "vengeance",
        setupTableNames.Keys.Select(path => new InstalledFile(path, 1, new string('0', 64), "disc1")).ToArray(),
        new[] { "vengeance" });
    var migration = VengeanceMediaPathMap.CreateOwnedMigration(disc1, legacyManifest);
    Check(migration.Files.Count == setupTableNames.Count &&
        migration.RetiredOwnedPaths.Count == setupTableNames.Count &&
        migration.Files.All(file => setupTableNames[file.SourceRelativePath] == file.DestinationRelativePath),
        "Vengeance upgrade plan renames every manifest-owned legacy path without broadening source ownership");
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
    WriteFixture(disc, "MISSIO_1.DLL", "mission language");
    WriteFixture(disc, "SCRIPT_1.DLL", "scripts");
    WriteFixture(disc, "SERVER_1.TXT", "server cycle");
    WriteFixture(disc, "FONTS/MECH.FNT", "font");
    WriteFixture(disc, "MW4X/LANGUAGE.DLL", "language");
    WriteFixture(disc, "MW4X/DRVMGT.DLL", "disc management");
    WriteFixture(disc, "MW4X/DSETUP.DLL", "directx setup");
    WriteFixture(disc, "MW4X/SECDRV.SYS", "safedisc driver");
    WriteFixture(disc, "CONTENT/GAMETY_1.H", "game types");
    WriteFixture(disc, "CONTENT/SHELLS_1/FILES/STUTTE_1.WAV", "audio");
    WriteFixture(disc, "RESOURCE/MISSIONS/VOLCAN_1.MW4", "mission");
    WriteFixture(disc, "RESOURCE/TEXTUR_1.MW4", "textures");
    WriteFixture(disc, "SETUP.EXE", "legacy setup");
    var preparedEula = Path.Combine(blackKnightPlanRoot, "prepared", "EBUEULA.DLL");
    WriteFixture(Path.GetDirectoryName(preparedEula)!, Path.GetFileName(preparedEula), "setup-accepted EULA module");
    var builder = new BlackKnightInstallPlanBuilder();
    var overlay = builder.BuildOverlay(disc, preparedEula);
    var destinations = overlay.Select(file => file.DestinationRelativePath).ToHashSet(StringComparer.OrdinalIgnoreCase);
    Check(destinations.Contains("MW4X/MW4X.exe"), "Black Knight overlay preserves the original MW4X subdirectory");
    Check(overlay.Single(file => file.DestinationRelativePath.Equals("MW4X/MW4X.exe", StringComparison.OrdinalIgnoreCase)).SourceRelativePath.Equals("MW4X/MW4X.EXE", StringComparison.OrdinalIgnoreCase), "Black Knight executable comes from recognized media");
    Check(!destinations.Contains("version.dll") && !destinations.Contains("version.json"), "Black Knight overlay does not ship the superseded retail loader experiment");
    Check(destinations.Contains("AutoConfigx.exe") && destinations.Contains("MissionLangx.dll") &&
        destinations.Contains("ScriptStringsx.dll") && destinations.Contains("servercyclex.txt"),
        "Black Knight plan restores expansion-specific root names");
    Check(destinations.Contains("FONTS/MECH.FNT") && destinations.Contains("MW4X/LANGUAGE.DLL") && destinations.Contains("MW4X/DRVMGT.DLL"), "Black Knight overlay keeps expansion runtime files in MW4X");
    Check(destinations.Contains("Content/GameTypesX.h") && destinations.Contains("Content/ShellScriptsX/Files/StutterShark_music.wav") &&
        destinations.Contains("Resource/Missions/volcan01_holdout.mw4") && destinations.Contains("Resource/texturesx.mw4"),
        "Black Knight plan restores long content and resource names from the original setup table");
    Check(destinations.Contains("MW4X/DSETUP.DLL"), "Black Knight overlay retains the DirectX runtime inside MW4X");
    Check(!destinations.Contains("MW4X/SECDRV.SYS") && !destinations.Contains("SETUP.EXE"), "Black Knight overlay excludes legacy setup and the obsolete SafeDisc driver");
    Check(destinations.Contains("MW4X/EBUEula.dll"), "Black Knight setup-time EULA state is installed beside the expansion executable");
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
    WriteFixture(disc1, "DSETUP.DLL", "directx runtime query");
    WriteFixture(disc1, "GUNTIC_1.DLL", "gun ticket");
    WriteFixture(disc1, "MISSIO_1.DLL", "mission language");
    WriteFixture(disc1, "SCRIPT_1.DLL", "script strings");
    WriteFixture(disc1, "RESOURCE/VARIAN_1/VARIAN_1.TXT", "variants");
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
    Check(destinations.Contains("AutoConfig_MERCS.exe") && destinations.Contains("MissionLang_MERCS.dll") &&
        destinations.Contains("ScriptStrings_MERCS.dll") && destinations.Contains("GunTicket.dll"),
        "Mercenaries plan restores setup-table root names");
    Check(destinations.Contains("DSetup.dll"), "Mercenaries plan retains the DirectX version-query runtime imported by the game executable");
    Check(destinations.Contains("Content/MercsShellScripts/Files/nextmove_music.wav"), "Mercenaries plan restores the installed shell-audio path");
    Check(destinations.Contains("Resource/VariantsMercs/VariantsMercs.txt"), "Mercenaries plan restores the installed variants path");
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
MercenariesPr1ArchiveExtractorSmoke.Run(failures);
MediaSourceInspectorSmoke.Run(failures);
MediaSelectionSetSmoke.Run(failures);
MediaSourceSessionSmoke.Run(failures);
MediaSelectionSessionSmoke.Run(failures);
MechPakResourceOverlayPlanSmoke.Run(failures);
OwnedInstallOverlayTransactionSmoke.Run(failures);
OwnedInstallFileReplacementTransactionSmoke.Run(failures);

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

    public int Start(System.Diagnostics.ProcessStartInfo startInfo)
    {
        LastStart = startInfo;
        return 4242;
    }
}

sealed class RecordingGameRegistration : ILegacyGameRegistration
{
    public ProductStatus? LastEnsured { get; private set; }
    public ProductStatus? LastValidated { get; private set; }
    public ProductStatus? LastRemoved { get; private set; }

    public void Ensure(ProductStatus status)
    {
        LastEnsured = status;
    }

    public void ValidateOwned(ProductStatus status)
    {
        LastValidated = status;
    }

    public void RemoveOwned(ProductStatus status)
    {
        LastRemoved = status;
    }
}

sealed class RecordingGameProcessState : IGameProcessState
{
    public bool Running { get; init; }
    public string? LastExecutablePath { get; private set; }

    public bool IsRunning(string executablePath)
    {
        LastExecutablePath = executablePath;
        return Running;
    }
}
