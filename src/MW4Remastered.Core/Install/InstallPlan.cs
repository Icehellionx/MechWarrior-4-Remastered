namespace MW4Remastered.Core.Install;

public sealed record InstallFile(
    string SourceRoot,
    string SourceRelativePath,
    string DestinationRelativePath);

public sealed class InstallPlan
{
    public InstallPlan(string productId, IEnumerable<InstallFile> files, IEnumerable<string>? components = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productId);
        ArgumentNullException.ThrowIfNull(files);
        ProductId = productId;
        Files = files.ToArray();
        if (Files.Count == 0) throw new ArgumentException("An install plan must contain at least one file.", nameof(files));

        Components = (components ?? new[] { productId })
            .Select(component => component?.Trim())
            .Where(component => !string.IsNullOrWhiteSpace(component))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (Components.Count == 0) throw new ArgumentException("An install plan must own at least one component.", nameof(components));
        if (!Components.Contains(productId, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The physical product must also be an owned component.", nameof(components));
        }
    }

    public string ProductId { get; }

    public IReadOnlyList<InstallFile> Files { get; }

    public IReadOnlyList<string> Components { get; }
}
