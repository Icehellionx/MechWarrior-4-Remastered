using System.Security.Cryptography;
using System.Text.Json;
using MW4Remastered.Core.Media;

namespace MW4Remastered.Core.Install;

public enum InstallRemovalStatus
{
    Removed,
    Blocked,
}

public sealed record InstallRemovalResult(
    InstallRemovalStatus Status,
    IReadOnlyList<string> RemovedFiles,
    IReadOnlyList<string> PreservedPaths,
    IReadOnlyList<string> Issues);

public sealed class OwnedInstallUninstaller
{
    private static readonly JsonSerializerOptions ManifestJson = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly DirectoryMediaInventory inventory;

    public OwnedInstallUninstaller()
        : this(new DirectoryMediaInventory())
    {
    }

    public OwnedInstallUninstaller(DirectoryMediaInventory inventory)
    {
        this.inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
    }

    public InstallRemovalResult Remove(string installRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(installRoot);
        var root = Path.GetFullPath(installRoot);
        var fileSystemRoot = Path.GetPathRoot(root);
        if (fileSystemRoot is null || string.Equals(Path.TrimEndingDirectorySeparator(root), Path.TrimEndingDirectorySeparator(fileSystemRoot), StringComparison.OrdinalIgnoreCase))
        {
            return new InstallRemovalResult(InstallRemovalStatus.Blocked, Array.Empty<string>(), Array.Empty<string>(), new[] { "A filesystem root can never be an install root." });
        }
        var issues = new List<string>();
        var preserved = new List<string>();

        IReadOnlyList<string> actualPaths;
        try
        {
            actualPaths = inventory.Read(root);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            return Blocked($"Installed tree could not be inventoried safely: {error.Message}");
        }

        var manifestPath = StagedInstallTransaction.ResolveContainedPath(root, InstallManifest.RelativePath);
        var manifest = ReadManifest(manifestPath, issues);
        if (manifest is null) return new InstallRemovalResult(InstallRemovalStatus.Blocked, Array.Empty<string>(), preserved, issues);
        if (manifest.SchemaVersion != 1) issues.Add($"Unsupported ownership manifest schema: {manifest.SchemaVersion}");
        if (string.IsNullOrWhiteSpace(manifest.ProductId)) issues.Add("Ownership manifest has no product id.");
        if (manifest.Files is null || manifest.Files.Count == 0) issues.Add("Ownership manifest contains no installed files.");

        var owned = new Dictionary<string, InstalledFile>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in manifest.Files ?? Array.Empty<InstalledFile>())
        {
            string normalized;
            try
            {
                normalized = StagedInstallTransaction.NormalizeRelativePath(file.Path).Replace('\\', '/');
                _ = StagedInstallTransaction.ResolveContainedPath(root, normalized);
            }
            catch (Exception error) when (error is ArgumentException or InvalidDataException)
            {
                issues.Add($"Unsafe manifest path '{file.Path}': {error.Message}");
                continue;
            }

            if (string.Equals(normalized, InstallManifest.RelativePath, StringComparison.OrdinalIgnoreCase))
            {
                issues.Add("Ownership manifest cannot list itself as an installed file.");
            }
            else if (!owned.TryAdd(normalized, file))
            {
                issues.Add($"Duplicate manifest path: {normalized}");
            }
        }

        var actual = actualPaths.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var (path, expected) in owned)
        {
            if (!actual.Contains(path)) continue;
            var fullPath = StagedInstallTransaction.ResolveContainedPath(root, path);
            try
            {
                StagedInstallTransaction.RejectContainedFilePath(root, fullPath);
                if (!Matches(fullPath, expected)) issues.Add($"Owned file was modified and will not be removed: {path}");
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                issues.Add($"Owned file could not be verified and will not be removed: {path} ({error.Message})");
            }
        }

        foreach (var path in actual)
        {
            if (!owned.ContainsKey(path) && !string.Equals(path, InstallManifest.RelativePath, StringComparison.OrdinalIgnoreCase))
            {
                preserved.Add(path);
            }
        }
        preserved.Sort(StringComparer.OrdinalIgnoreCase);

        if (issues.Count > 0)
        {
            issues.Sort(StringComparer.OrdinalIgnoreCase);
            return new InstallRemovalResult(InstallRemovalStatus.Blocked, Array.Empty<string>(), preserved, issues);
        }

        var removalRoot = Path.Combine(Path.GetDirectoryName(manifestPath)!, $"removal-{Guid.NewGuid():N}");
        var moved = new List<(string Original, string Temporary, string Relative)>();
        try
        {
            foreach (var (path, expected) in owned)
            {
                var original = StagedInstallTransaction.ResolveContainedPath(root, path);
                if (!File.Exists(original)) continue;
                StagedInstallTransaction.RejectContainedFilePath(root, original);
                if (!Matches(original, expected)) throw new IOException($"Owned file changed during removal preflight: {path}");
                var temporary = StagedInstallTransaction.ResolveContainedPath(removalRoot, path);
                Directory.CreateDirectory(Path.GetDirectoryName(temporary)!);
                File.Move(original, temporary);
                moved.Add((original, temporary, path));
            }

            var temporaryManifest = Path.Combine(removalRoot, "install-manifest.json");
            File.Move(manifestPath, temporaryManifest);
            moved.Add((manifestPath, temporaryManifest, InstallManifest.RelativePath));
        }
        catch
        {
            foreach (var item in moved.AsEnumerable().Reverse())
            {
                if (!File.Exists(item.Temporary)) continue;
                Directory.CreateDirectory(Path.GetDirectoryName(item.Original)!);
                File.Move(item.Temporary, item.Original);
            }
            if (Directory.Exists(removalRoot)) Directory.Delete(removalRoot, recursive: true);
            throw;
        }

        Directory.Delete(removalRoot, recursive: true);
        RemoveEmptyDirectories(root);
        var removed = moved.Select(item => item.Relative).OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray();
        return new InstallRemovalResult(InstallRemovalStatus.Removed, removed, preserved, Array.Empty<string>());

        InstallRemovalResult Blocked(string issue) =>
            new(InstallRemovalStatus.Blocked, Array.Empty<string>(), Array.Empty<string>(), new[] { issue });
    }

    private static InstallManifest? ReadManifest(string path, List<string> issues)
    {
        if (!File.Exists(path))
        {
            issues.Add($"Ownership manifest is missing: {InstallManifest.RelativePath}");
            return null;
        }
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
        {
            issues.Add("Ownership manifest cannot be a reparse point.");
            return null;
        }
        try
        {
            var manifest = JsonSerializer.Deserialize<InstallManifest>(File.ReadAllText(path), ManifestJson);
            if (manifest is null) issues.Add("Ownership manifest is empty.");
            return manifest;
        }
        catch (JsonException error)
        {
            issues.Add($"Ownership manifest is invalid JSON: {error.Message}");
            return null;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            issues.Add($"Ownership manifest could not be read: {error.Message}");
            return null;
        }
    }

    private static bool Matches(string path, InstalledFile expected)
    {
        var info = new FileInfo(path);
        if (info.Length != expected.Length || string.IsNullOrWhiteSpace(expected.Sha256) || expected.Sha256.Length != 64) return false;
        using var stream = File.OpenRead(path);
        var actualHash = Convert.ToHexString(SHA256.HashData(stream));
        return string.Equals(actualHash, expected.Sha256, StringComparison.OrdinalIgnoreCase);
    }

    private static void RemoveEmptyDirectories(string root)
    {
        foreach (var directory in Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories)
                     .OrderByDescending(path => path.Length))
        {
            if (!Directory.EnumerateFileSystemEntries(directory).Any()) Directory.Delete(directory);
        }
        if (!Directory.EnumerateFileSystemEntries(root).Any()) Directory.Delete(root);
    }
}
