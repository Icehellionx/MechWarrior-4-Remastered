using System.Security.Cryptography;
using System.Text.Json;

namespace MW4Remastered.Core.Install;

public sealed class StagedInstallTransaction
{
    private static readonly JsonSerializerOptions ManifestJson = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public InstallManifest Execute(InstallPlan plan, string destinationPath)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        var destination = Path.GetFullPath(destinationPath);
        if (Directory.Exists(destination) || File.Exists(destination))
        {
            throw new IOException($"Install destination already exists: {destination}");
        }

        var parent = Directory.GetParent(destination)
            ?? throw new InvalidDataException($"Install destination has no parent: {destination}");
        Directory.CreateDirectory(parent.FullName);
        RejectDirectoryChain(parent.FullName, "destination parent");

        var staging = Path.Combine(parent.FullName, $".{Path.GetFileName(destination)}.staging-{Guid.NewGuid():N}");
        var committed = false;
        try
        {
            Directory.CreateDirectory(staging);
            var installedFiles = new List<InstalledFile>(plan.Files.Count);
            var claimedDestinations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var operation in plan.Files)
            {
                var sourceRoot = Path.GetFullPath(operation.SourceRoot);
                RejectReparsePoint(sourceRoot, "source root");
                var source = ResolveContainedPath(sourceRoot, operation.SourceRelativePath);
                RejectPathChain(sourceRoot, source);
                if (!File.Exists(source)) throw new FileNotFoundException("Install source file does not exist.", source);
                if ((File.GetAttributes(source) & FileAttributes.ReparsePoint) != 0)
                {
                    throw new InvalidDataException($"Install transaction does not copy reparse-point files: {source}");
                }

                var normalizedDestination = NormalizeRelativePath(operation.DestinationRelativePath);
                if (!claimedDestinations.Add(normalizedDestination))
                {
                    throw new InvalidDataException($"Install plan writes the same destination more than once: {normalizedDestination}");
                }

                var stagedFile = ResolveContainedPath(staging, normalizedDestination);
                Directory.CreateDirectory(Path.GetDirectoryName(stagedFile)!);
                File.Copy(source, stagedFile, overwrite: false);
                File.SetAttributes(stagedFile, FileAttributes.Normal);

                var info = new FileInfo(stagedFile);
                installedFiles.Add(new InstalledFile(
                    normalizedDestination.Replace('\\', '/'),
                    info.Length,
                    ComputeSha256(stagedFile),
                    Path.GetFileName(sourceRoot)));
            }

            installedFiles.Sort((left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.Path, right.Path));
            var manifest = new InstallManifest(1, plan.ProductId, installedFiles);
            var manifestPath = ResolveContainedPath(staging, InstallManifest.RelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(manifestPath)!);
            File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, ManifestJson) + Environment.NewLine);

            Directory.Move(staging, destination);
            committed = true;
            return manifest;
        }
        finally
        {
            if (!committed && Directory.Exists(staging)) Directory.Delete(staging, recursive: true);
        }
    }

    internal static string NormalizeRelativePath(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        if (Path.IsPathRooted(normalized)) throw new InvalidDataException($"Install path must be relative: {relativePath}");

        var segments = normalized.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0 || segments.Any(segment => segment is "." or ".." || segment.Contains(':')))
        {
            throw new InvalidDataException($"Install path is unsafe: {relativePath}");
        }
        return string.Join(Path.DirectorySeparatorChar, segments);
    }

    private static string ResolveContainedPath(string root, string relativePath)
    {
        var normalized = NormalizeRelativePath(relativePath);
        var fullRoot = Path.GetFullPath(root);
        var rootWithSeparator = Path.EndsInDirectorySeparator(fullRoot) ? fullRoot : fullRoot + Path.DirectorySeparatorChar;
        var candidate = Path.GetFullPath(Path.Combine(rootWithSeparator, normalized));
        if (!candidate.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"Install path escapes its root: {relativePath} (root: {rootWithSeparator}, resolved: {candidate})");
        }
        return candidate;
    }

    private static void RejectPathChain(string root, string file)
    {
        var current = new DirectoryInfo(Path.GetDirectoryName(file)!);
        var rootPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        while (current is not null)
        {
            RejectReparsePoint(current.FullName, "source path");
            if (string.Equals(Path.TrimEndingDirectorySeparator(current.FullName), rootPath, StringComparison.OrdinalIgnoreCase)) return;
            current = current.Parent;
        }
        throw new InvalidDataException($"Source path is not contained by its declared root: {file}");
    }

    private static void RejectReparsePoint(string path, string role)
    {
        if (!Directory.Exists(path)) throw new DirectoryNotFoundException($"{role} does not exist: {path}");
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidDataException($"Install transaction does not follow reparse points ({role}): {path}");
        }
    }

    private static void RejectDirectoryChain(string directory, string role)
    {
        for (var current = new DirectoryInfo(directory); current is not null; current = current.Parent)
        {
            RejectReparsePoint(current.FullName, role);
        }
    }

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }
}
