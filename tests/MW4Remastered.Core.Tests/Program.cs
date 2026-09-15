using MW4Remastered.Core;
using MW4Remastered.Core.Media;

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
    Check(statuses["vengeance"].IsInstalled, "Vengeance executable is detected");
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
