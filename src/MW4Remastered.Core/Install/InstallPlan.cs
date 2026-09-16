namespace MW4Remastered.Core.Install;

public sealed record InstallFile(
    string SourceRoot,
    string SourceRelativePath,
    string DestinationRelativePath);

public sealed class InstallPlan
{
    public InstallPlan(string productId, IEnumerable<InstallFile> files)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productId);
        ArgumentNullException.ThrowIfNull(files);
        ProductId = productId;
        Files = files.ToArray();
        if (Files.Count == 0) throw new ArgumentException("An install plan must contain at least one file.", nameof(files));
    }

    public string ProductId { get; }

    public IReadOnlyList<InstallFile> Files { get; }
}
