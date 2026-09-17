using System.Text.Json.Serialization;

namespace MW4Remastered.Core.Install;

public sealed record InstalledFile(
    string Path,
    long Length,
    string Sha256,
    string SourceMedia);

public sealed record InstallManifest(
    int SchemaVersion,
    string ProductId,
    IReadOnlyList<InstalledFile> Files,
    IReadOnlyList<string>? Components = null)
{
    [JsonIgnore]
    public const string RelativePath = ".mw4-remastered/install-manifest.json";

    public bool HasComponent(string componentId) =>
        EffectiveComponents.Contains(componentId, StringComparer.OrdinalIgnoreCase);

    [JsonIgnore]
    public IReadOnlyList<string> EffectiveComponents =>
        Components is { Count: > 0 } ? Components : new[] { ProductId };
}
