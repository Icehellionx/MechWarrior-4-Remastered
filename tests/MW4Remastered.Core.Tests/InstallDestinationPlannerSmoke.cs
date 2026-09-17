using MW4Remastered.Core.Install;
using MW4Remastered.Core.Media;

internal static class InstallDestinationPlannerSmoke
{
    public static void Run(List<string> failures)
    {
        var selection = new MediaSelectionSet();
        AddLayout(selection, "vengeance-disc-1");
        AddLayout(selection, "vengeance-disc-2");
        AddLayout(selection, "black-knight-disc-1");

        var destination = Path.Combine(Path.GetTempPath(), "mw4-destination-plan", "Games");
        var plan = new InstallDestinationPlanner(new FixedCapacityReader(8L * 1024 * 1024 * 1024))
            .Plan(selection.Current, destination);

        Check(plan.Products.Count == 1, "destination planner creates one physical Vengeance-family operation", failures);
        Check(plan.Products.Single().ProductId == "vengeance" &&
            plan.Products.Single().DestinationPath.EndsWith("vengeance", StringComparison.Ordinal) &&
            plan.Products.Single().EffectiveComponents.Contains("black-knight"),
            "destination planner assigns Vengeance and Black Knight to the shared Vengeance tree", failures);
        Check(plan.HasEnoughSpace && plan.RequiredBytes > plan.Products.Sum(item => item.BudgetBytes),
            "destination planner includes safety reserve and reports adequate capacity", failures);

        var expansionOnly = new MediaSelectionSet();
        AddLayout(expansionOnly, "black-knight-disc-1");
        var blockedExpansion = new InstallDestinationPlanner(new FixedCapacityReader(long.MaxValue))
            .Plan(expansionOnly.Current, destination);
        Check(!blockedExpansion.Products.Any(item => item.ProductId == "black-knight"),
            "destination planner blocks Black Knight media without its Vengeance base", failures);
        Check(blockedExpansion.BlockedProducts.Single().ProductId == "black-knight" &&
            blockedExpansion.BlockedProducts.Single().MissingDependencyIds.SequenceEqual(new[] { "vengeance-media" }),
            "destination planner explains that an atomic rebuild needs Vengeance media", failures);
        var expansionForInstalledBase = new InstallDestinationPlanner(new FixedCapacityReader(long.MaxValue))
            .Plan(expansionOnly.Current, destination, new[] { "vengeance" });
        Check(expansionForInstalledBase.Products.Count == 0 && expansionForInstalledBase.BlockedProducts.Count == 1,
            "destination planner will not mutate an installed Vengeance tree with a partial Black Knight overlay", failures);

        var constrained = new InstallDestinationPlanner(new FixedCapacityReader(1)).Plan(selection.Current, destination);
        Check(!constrained.HasEnoughSpace, "destination planner reports insufficient capacity", failures);

        var relativeRejected = false;
        try
        {
            new InstallDestinationPlanner(new FixedCapacityReader(long.MaxValue)).Plan(selection.Current, "relative-path");
        }
        catch (InvalidDataException)
        {
            relativeRejected = true;
        }
        Check(relativeRejected, "destination planner rejects relative roots", failures);

        var filesystemRootRejected = false;
        try
        {
            var filesystemRoot = Path.GetPathRoot(Path.GetFullPath(destination))!;
            new InstallDestinationPlanner(new FixedCapacityReader(long.MaxValue)).Plan(selection.Current, filesystemRoot);
        }
        catch (InvalidDataException)
        {
            filesystemRootRejected = true;
        }
        Check(filesystemRootRejected, "destination planner rejects filesystem roots", failures);

        var existingFile = Path.GetTempFileName();
        try
        {
            var fileRejected = false;
            try
            {
                new InstallDestinationPlanner(new FixedCapacityReader(long.MaxValue)).Plan(selection.Current, existingFile);
            }
            catch (InvalidDataException)
            {
                fileRejected = true;
            }
            Check(fileRejected, "destination planner rejects an existing file as the root", failures);
        }
        finally
        {
            File.Delete(existingFile);
        }
    }

    private static void AddLayout(MediaSelectionSet selection, string layoutId)
    {
        var layout = MediaCatalog.Layouts.Single(item => item.Id == layoutId);
        var inspection = new MediaSourceInspection(
            MediaSourceKind.Iso,
            new[]
            {
                new InspectedMediaItem(null, new MediaRecognitionResult(
                    MediaRecognitionStatus.Recognized,
                    layout,
                    Array.Empty<string>(),
                    "Synthetic recognition.")),
            },
            Array.Empty<string>());
        selection.Add(Path.Combine(Path.GetTempPath(), layoutId + ".iso"), inspection);
    }

    private static void Check(bool condition, string message, List<string> failures)
    {
        if (!condition) failures.Add("FAIL: " + message);
    }

    private sealed class FixedCapacityReader(long bytes) : IStorageCapacityReader
    {
        public long GetAvailableBytes(string destinationPath) => bytes;
    }
}
