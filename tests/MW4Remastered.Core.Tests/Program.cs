using MW4Remastered.Core;

var failures = new List<string>();
Check(ProductCatalog.All.Count == 5, "catalog contains exactly three games and two packs");
Check(ProductCatalog.All.Count(item => item.Kind == ProductKind.Game) == 3, "catalog contains three games");
Check(ProductCatalog.All.Count(item => item.Kind == ProductKind.OptionalPack) == 2, "catalog contains two optional packs");

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
