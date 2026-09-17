namespace MW4Remastered.Core.Media;

public sealed record SelectedMediaLayout(
    MediaLayoutDefinition Layout,
    string SourcePath,
    string? ArchiveRelativePath,
    int ExcludedContentCount);

public sealed record MediaCapabilityStatus(
    string ProductId,
    string DisplayName,
    ProductKind Kind,
    IReadOnlyList<string> RequiredLayoutIds,
    IReadOnlyList<string> PresentLayoutIds)
{
    public bool IsComplete => RequiredLayoutIds.Count == PresentLayoutIds.Count;
}

public sealed record MediaSelectionSnapshot(
    IReadOnlyList<SelectedMediaLayout> Layouts,
    IReadOnlyList<MediaCapabilityStatus> Capabilities,
    int SourceCount,
    int ExcludedContentCount);

public sealed class MediaSelectionSet
{
    private static readonly IReadOnlyDictionary<string, string[]> RequiredLayouts =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["vengeance"] = new[] { "vengeance-disc-1", "vengeance-disc-2" },
            ["black-knight"] = new[] { "black-knight-disc-1" },
            ["mercenaries"] = new[] { "mercenaries-disc-1", "mercenaries-disc-2" },
            ["inner-sphere"] = new[] { "inner-sphere-mech-pak" },
            ["clan"] = new[] { "clan-mech-pak" },
        };

    private readonly Dictionary<string, SelectedSource> sources = new(StringComparer.OrdinalIgnoreCase);
    private long nextSequence;

    public MediaSelectionSnapshot Current => BuildSnapshot();

    public MediaSelectionSnapshot Add(string sourcePath, MediaSourceInspection inspection)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentNullException.ThrowIfNull(inspection);

        var fullSourcePath = Path.GetFullPath(sourcePath);
        var accepted = ValidateInspection(fullSourcePath, inspection);

        // Mutation happens only after the complete source has passed validation.
        sources[fullSourcePath] = new SelectedSource(
            fullSourcePath,
            ++nextSequence,
            accepted,
            inspection.ExcludedArchiveEntries.Count + accepted.Sum(item => item.ExcludedContentCount));
        return BuildSnapshot();
    }

    private static IReadOnlyList<SelectedMediaLayout> ValidateInspection(
        string sourcePath,
        MediaSourceInspection inspection)
    {
        if (inspection.Items.Count == 0)
        {
            throw new InvalidDataException("The selected source does not contain any recognized media to inspect.");
        }

        var accepted = new List<SelectedMediaLayout>(inspection.Items.Count);
        var layoutIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in inspection.Items)
        {
            var recognition = item.Recognition;
            if (recognition.Status is not (MediaRecognitionStatus.Recognized or MediaRecognitionStatus.RecognizedWithExcludedContent)
                || recognition.Layout is null)
            {
                throw new InvalidDataException(recognition.Message);
            }
            if (!layoutIds.Add(recognition.Layout.Id))
            {
                throw new InvalidDataException($"The source contains more than one copy of {recognition.Layout.DisplayName}.");
            }

            accepted.Add(new SelectedMediaLayout(
                recognition.Layout,
                sourcePath,
                item.ArchiveRelativePath,
                recognition.ExcludedPaths.Count));
        }
        return accepted;
    }

    private MediaSelectionSnapshot BuildSnapshot()
    {
        var byLayout = new Dictionary<string, SelectedMediaLayout>(StringComparer.OrdinalIgnoreCase);
        foreach (var source in sources.Values.OrderBy(item => item.Sequence))
        {
            foreach (var layout in source.Layouts) byLayout[layout.Layout.Id] = layout;
        }

        var capabilities = ProductCatalog.All.Select(product =>
        {
            var required = RequiredLayouts[product.Id];
            var present = required.Where(byLayout.ContainsKey).ToArray();
            return new MediaCapabilityStatus(product.Id, product.DisplayName, product.Kind, required, present);
        }).ToArray();

        return new MediaSelectionSnapshot(
            byLayout.Values.OrderBy(item => item.Layout.Product).ThenBy(item => item.Layout.DiscNumber).ToArray(),
            capabilities,
            sources.Count,
            sources.Values.Sum(item => item.ExcludedContentCount));
    }

    private sealed record SelectedSource(
        string SourcePath,
        long Sequence,
        IReadOnlyList<SelectedMediaLayout> Layouts,
        int ExcludedContentCount);
}
