using MW4Remastered.Core.Install;
using MW4Remastered.Core.Media;

internal static class MechPakResourceOverlayPlanSmoke
{
    public static void Run(List<string> failures)
    {
        var root = Path.Combine(Path.GetTempPath(), "mw4-pack-overlay-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var innerSphere = Path.Combine(root, "inner-sphere");
            CreatePackFixture(innerSphere, new[]
            {
                "RESOURCE/MAPS/COLSM01.MW4", "RESOURCE/MAPS/GAGE.MW4",
                "RESOURCE/MISSIONS/COLISEUM.MW4", "RESOURCE/MISSIONS/COLISEUM.NFO",
                "RESOURCE/MISSIONS/COLISEUM.NFX", "RESOURCE/MISSIONS/COLISEUM.TGA",
                "RESOURCE/MISSIONS/GAGETOWN.MW4", "RESOURCE/MISSIONS/GAGETOWN.NFO",
                "RESOURCE/MISSIONS/GAGETOWN.NFX", "RESOURCE/MISSIONS/GAGETOWN.TGA",
            }, "inner-sphere-mech-pak");
            Write(innerSphere, "Razor1911/mw4.exe", "excluded crack");
            Write(innerSphere, "CDSET/SCSHD.EXE", "excluded DRM");
            Write(innerSphere, "GOODIES/PATCH3/MW4P3/ENGLISH/MW4.RTP", "separate patch contract");

            var unqualifiedRejected = false;
            try
            {
                new MechPakResourceOverlayPlanBuilder().Build(innerSphere, "vengeance");
            }
            catch (InvalidDataException)
            {
                unqualifiedRejected = true;
            }
            Check(unqualifiedRejected, "pack overlay rejects structurally valid but unknown resource hashes", failures);

            var builder = new MechPakResourceOverlayPlanBuilder(
                new MediaInspectionService(),
                new DirectoryMediaInventory(),
                requireQualifiedHashes: false);
            var plan = builder.Build(innerSphere, "vengeance");
            Check(plan.PackProductId == "inner-sphere" && plan.TargetProductId == "vengeance", "pack overlay identifies pack and target", failures);
            Check(plan.Files.Count == 10 && plan.Files.All(file => file.DestinationRelativePath.StartsWith("RESOURCE/", StringComparison.OrdinalIgnoreCase)),
                "pack overlay contains exactly the ten allowlisted resources", failures);
            Check(plan.Files.All(file => !file.SourceRelativePath.Contains("Razor", StringComparison.OrdinalIgnoreCase)
                && !file.SourceRelativePath.Contains("SCSHD", StringComparison.OrdinalIgnoreCase)
                && !file.SourceRelativePath.EndsWith(".RTP", StringComparison.OrdinalIgnoreCase)),
                "pack overlay excludes cracks DRM and the separately owned patch transform", failures);

            var blackKnight = builder.Build(innerSphere, "black-knight");
            Check(blackKnight.TargetProductId == "black-knight", "pack overlay supports the documented Black Knight target", failures);

            var mercenariesRejected = false;
            try
            {
                builder.Build(innerSphere, "mercenaries");
            }
            catch (InvalidDataException)
            {
                mercenariesRejected = true;
            }
            Check(mercenariesRejected, "pack overlay refuses unqualified Mercenaries collisions", failures);

            File.Delete(Path.Combine(innerSphere, "RESOURCE", "MAPS", "GAGE.MW4"));
            var missingRejected = false;
            try
            {
                builder.Build(innerSphere, "vengeance");
            }
            catch (InvalidDataException)
            {
                missingRejected = true;
            }
            Check(missingRejected, "pack overlay rejects a partial payload", failures);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    private static void CreatePackFixture(string root, IEnumerable<string> resources, string layoutId)
    {
        foreach (var path in resources) Write(root, path, "resource fixture");
        foreach (var path in MediaCatalog.Layouts.Single(item => item.Id == layoutId).RequiredPaths) Write(root, path, "recognition fixture");
    }

    private static void Write(string root, string relativePath, string contents)
    {
        var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, contents);
    }

    private static void Check(bool condition, string message, List<string> failures)
    {
        if (!condition) failures.Add("FAIL: " + message);
    }
}
