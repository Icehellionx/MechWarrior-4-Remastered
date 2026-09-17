using System.Security.Cryptography;
using System.Text.Json;
using MW4Remastered.Core.Media;

namespace MW4Remastered.Core.Install;

public sealed record InstallVerificationResult(
    bool IsValid,
    InstallManifest? Manifest,
    IReadOnlyList<string> Issues);

public enum InstallVerificationScope
{
    ExactTree,
    OwnedFiles,
}

public sealed class InstallManifestVerifier
{
    private static readonly JsonSerializerOptions ManifestJson = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly DirectoryMediaInventory inventory;

    public InstallManifestVerifier()
        : this(new DirectoryMediaInventory())
    {
    }

    public InstallManifestVerifier(DirectoryMediaInventory inventory)
    {
        this.inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
    }

    public InstallVerificationResult Verify(string installRoot, InstallVerificationScope scope = InstallVerificationScope.ExactTree)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(installRoot);
        var root = Path.GetFullPath(installRoot);
        var manifestPath = Path.Combine(root, InstallManifest.RelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(manifestPath))
        {
            return new InstallVerificationResult(false, null, new[] { $"Ownership manifest is missing: {InstallManifest.RelativePath}" });
        }

        InstallManifest? manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<InstallManifest>(File.ReadAllText(manifestPath), ManifestJson);
        }
        catch (JsonException error)
        {
            return new InstallVerificationResult(false, null, new[] { $"Ownership manifest is invalid JSON: {error.Message}" });
        }

        if (manifest is null)
        {
            return new InstallVerificationResult(false, null, new[] { "Ownership manifest is empty." });
        }

        var issues = new List<string>();
        if (manifest.SchemaVersion is not (1 or 2)) issues.Add($"Unsupported ownership manifest schema: {manifest.SchemaVersion}");
        if (string.IsNullOrWhiteSpace(manifest.ProductId)) issues.Add("Ownership manifest has no product id.");
        if (manifest.Files is null || manifest.Files.Count == 0) issues.Add("Ownership manifest contains no installed files.");
        if (manifest.SchemaVersion == 2)
        {
            if (manifest.Components is null || manifest.Components.Count == 0)
            {
                issues.Add("Ownership manifest contains no components.");
            }
            else
            {
                if (manifest.Components.Any(string.IsNullOrWhiteSpace)) issues.Add("Ownership manifest contains an empty component id.");
                if (manifest.Components.Distinct(StringComparer.OrdinalIgnoreCase).Count() != manifest.Components.Count)
                    issues.Add("Ownership manifest contains duplicate component ids.");
                if (!manifest.HasComponent(manifest.ProductId)) issues.Add("Ownership manifest does not include its physical product as a component.");
            }
        }

        var declared = new Dictionary<string, InstalledFile>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in manifest.Files ?? Array.Empty<InstalledFile>())
        {
            string normalized;
            try
            {
                normalized = StagedInstallTransaction.NormalizeRelativePath(file.Path).Replace('\\', '/');
            }
            catch (Exception error) when (error is ArgumentException or InvalidDataException)
            {
                issues.Add($"Unsafe manifest path '{file.Path}': {error.Message}");
                continue;
            }

            if (!declared.TryAdd(normalized, file))
            {
                issues.Add($"Duplicate manifest path: {normalized}");
            }
        }

        IReadOnlyList<string> actualPaths;
        try
        {
            actualPaths = inventory.Read(root);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            issues.Add($"Installed tree could not be inventoried safely: {error.Message}");
            return new InstallVerificationResult(false, manifest, issues);
        }

        var actual = actualPaths
            .Where(path => !string.Equals(path, InstallManifest.RelativePath, StringComparison.OrdinalIgnoreCase))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var path in declared.Keys.Except(actual, StringComparer.OrdinalIgnoreCase)) issues.Add($"Missing installed file: {path}");
        if (scope == InstallVerificationScope.ExactTree)
        {
            foreach (var path in actual.Except(declared.Keys, StringComparer.OrdinalIgnoreCase)) issues.Add($"Unexpected installed file: {path}");
        }

        foreach (var (path, expected) in declared)
        {
            if (!actual.Contains(path)) continue;
            var fullPath = Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar));
            var info = new FileInfo(fullPath);
            if (info.Length != expected.Length)
            {
                issues.Add($"Length mismatch: {path} (expected {expected.Length}, actual {info.Length})");
                continue;
            }
            var actualHash = ComputeSha256(fullPath);
            if (!string.Equals(actualHash, expected.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                issues.Add($"SHA-256 mismatch: {path}");
            }
        }

        issues.Sort(StringComparer.OrdinalIgnoreCase);
        return new InstallVerificationResult(issues.Count == 0, manifest, issues);
    }

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }
}
