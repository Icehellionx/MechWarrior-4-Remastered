using MW4Remastered.Core.Media;

namespace MW4Remastered.Core.Install;

public sealed record MechPakResourceOverlayPlan(
    string PackProductId,
    string TargetProductId,
    IReadOnlyList<InstallFile> Files);

public sealed class MechPakResourceOverlayPlanBuilder
{
    private static readonly IReadOnlyDictionary<string, PackDefinition> Packs =
        new Dictionary<string, PackDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["inner-sphere-mech-pak"] = new PackDefinition("inner-sphere", new[]
            {
                "RESOURCE/MAPS/COLSM01.MW4",
                "RESOURCE/MAPS/GAGE.MW4",
                "RESOURCE/MISSIONS/COLISEUM.MW4",
                "RESOURCE/MISSIONS/COLISEUM.NFO",
                "RESOURCE/MISSIONS/COLISEUM.NFX",
                "RESOURCE/MISSIONS/COLISEUM.TGA",
                "RESOURCE/MISSIONS/GAGETOWN.MW4",
                "RESOURCE/MISSIONS/GAGETOWN.NFO",
                "RESOURCE/MISSIONS/GAGETOWN.NFX",
                "RESOURCE/MISSIONS/GAGETOWN.TGA",
            }),
            ["clan-mech-pak"] = new PackDefinition("clan", new[]
            {
                "RESOURCE/MAPS/FACT01.MW4",
                "RESOURCE/MAPS/NGOTH.MW4",
                "RESOURCE/MISSIONS/FACTORY.MW4",
                "RESOURCE/MISSIONS/FACTORY.NFO",
                "RESOURCE/MISSIONS/FACTORY.NFX",
                "RESOURCE/MISSIONS/FACTORY.TGA",
                "RESOURCE/MISSIONS/NEWGOT_1.MW4",
                "RESOURCE/MISSIONS/NEWGOT_1.NFO",
                "RESOURCE/MISSIONS/NEWGOT_1.NFX",
                "RESOURCE/MISSIONS/NEWGOT_1.TGA",
            }),
        };

    private static readonly IReadOnlySet<string> SupportedTargets =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "vengeance", "black-knight" };

    private readonly MediaInspectionService inspection;
    private readonly DirectoryMediaInventory inventory;

    public MechPakResourceOverlayPlanBuilder()
        : this(new MediaInspectionService(), new DirectoryMediaInventory())
    {
    }

    public MechPakResourceOverlayPlanBuilder(MediaInspectionService inspection, DirectoryMediaInventory inventory)
    {
        this.inspection = inspection ?? throw new ArgumentNullException(nameof(inspection));
        this.inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
    }

    public MechPakResourceOverlayPlan Build(string mediaRoot, string targetProductId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mediaRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetProductId);
        if (!SupportedTargets.Contains(targetProductId))
        {
            throw new InvalidDataException("Retail Mech Pak resources are qualified only for Vengeance and Black Knight targets.");
        }

        var root = Path.GetFullPath(mediaRoot);
        var recognition = inspection.InspectDirectory(root);
        if (recognition.Status is not (MediaRecognitionStatus.Recognized or MediaRecognitionStatus.RecognizedWithExcludedContent)
            || recognition.Layout is null
            || !Packs.TryGetValue(recognition.Layout.Id, out var pack))
        {
            throw new InvalidDataException($"Expected recognized Mech Pak media, but recognition returned {recognition.Status} ({recognition.Layout?.Id ?? "unknown"}).");
        }

        var available = inventory.Read(root).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = pack.ResourcePaths.Where(path => !available.Contains(path)).ToArray();
        if (missing.Length > 0)
        {
            throw new InvalidDataException("Mech Pak media is missing allowlisted resources: " + string.Join(", ", missing));
        }

        var files = pack.ResourcePaths
            .Select(path => new InstallFile(root, path, path))
            .ToArray();
        return new MechPakResourceOverlayPlan(pack.ProductId, targetProductId, files);
    }

    private sealed record PackDefinition(string ProductId, IReadOnlyList<string> ResourcePaths);
}
