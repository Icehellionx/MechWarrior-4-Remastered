using System.Security.Cryptography;
using System.Text.Json;
using MW4Remastered.Core.Media;

namespace MW4Remastered.Core.Install;

public sealed class StagedInstallTransaction
{
    private static readonly JsonSerializerOptions ManifestJson = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public InstallManifest Execute(InstallPlan plan, string destinationPath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        cancellationToken.ThrowIfCancellationRequested();

        var destination = Path.GetFullPath(destinationPath);
        if (File.Exists(destination))
        {
            throw new IOException($"Install destination is an existing file: {destination}");
        }
        var preserveExisting = Directory.Exists(destination);

        var parent = Directory.GetParent(destination)
            ?? throw new InvalidDataException($"Install destination has no parent: {destination}");
        Directory.CreateDirectory(parent.FullName);
        RejectDirectoryChain(parent.FullName, "destination parent");

        var staging = Path.Combine(parent.FullName, $".{Path.GetFileName(destination)}.staging-{Guid.NewGuid():N}");
        string? backup = null;
        var committed = false;
        try
        {
            Directory.CreateDirectory(staging);
            var installedFiles = new List<InstalledFile>(plan.Files.Count);
            var claimedDestinations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var operation in plan.Files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var sourceRoot = Path.GetFullPath(operation.SourceRoot);
                RejectReparsePoint(sourceRoot, "source root");
                var source = ResolveContainedPath(sourceRoot, operation.SourceRelativePath);
                if (!File.Exists(source)) throw new FileNotFoundException("Install source file does not exist.", source);
                RejectContainedFilePath(sourceRoot, source);

                var normalizedDestination = NormalizeRelativePath(operation.DestinationRelativePath);
                if (!claimedDestinations.Add(normalizedDestination))
                {
                    throw new InvalidDataException($"Install plan writes the same destination more than once: {normalizedDestination}");
                }

                var stagedFile = ResolveContainedPath(staging, normalizedDestination);
                Directory.CreateDirectory(Path.GetDirectoryName(stagedFile)!);
                CopyFile(source, stagedFile, cancellationToken);
                File.SetAttributes(stagedFile, FileAttributes.Normal);

                var info = new FileInfo(stagedFile);
                installedFiles.Add(new InstalledFile(
                    normalizedDestination.Replace('\\', '/'),
                    info.Length,
                    ComputeSha256(stagedFile),
                    Path.GetFileName(sourceRoot)));
            }

            installedFiles.Sort((left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.Path, right.Path));
            cancellationToken.ThrowIfCancellationRequested();
            var manifest = new InstallManifest(2, plan.ProductId, installedFiles, plan.Components);
            var manifestPath = ResolveContainedPath(staging, InstallManifest.RelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(manifestPath)!);
            File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, ManifestJson) + Environment.NewLine);

            cancellationToken.ThrowIfCancellationRequested();
            if (preserveExisting)
            {
                PreserveExistingFiles(destination, staging, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                backup = Path.Combine(parent.FullName, $".{Path.GetFileName(destination)}.previous-{Guid.NewGuid():N}");
                Directory.Move(destination, backup);
                try
                {
                    Directory.Move(staging, destination);
                }
                catch
                {
                    Directory.Move(backup, destination);
                    backup = null;
                    throw;
                }
            }
            else
            {
                Directory.Move(staging, destination);
            }
            committed = true;
            if (backup is not null)
            {
                try { Directory.Delete(backup, recursive: true); }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException)
                {
                    // The committed tree already contains verified copies of every
                    // preserved file. A stale sibling backup is safer than turning a
                    // successful atomic swap into a destructive rollback.
                }
            }
            return manifest;
        }
        finally
        {
            if (!committed && Directory.Exists(staging)) Directory.Delete(staging, recursive: true);
        }
    }

    private static void PreserveExistingFiles(string existingRoot, string stagingRoot, CancellationToken cancellationToken)
    {
        var paths = new DirectoryMediaInventory().Read(existingRoot);
        foreach (var relativePath in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.Equals(relativePath, InstallManifest.RelativePath, StringComparison.OrdinalIgnoreCase))
            {
                throw new IOException("An existing ownership manifest requires uninstall or repair before reinstalling this game.");
            }

            var source = ResolveContainedPath(existingRoot, relativePath);
            RejectContainedFilePath(existingRoot, source);
            var destination = ResolveContainedPath(stagingRoot, relativePath);
            if (File.Exists(destination) || Directory.Exists(destination))
            {
                throw new IOException($"Preserved user file collides with the fresh game payload: {relativePath}");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            CopyFile(source, destination, cancellationToken);
            File.SetAttributes(destination, File.GetAttributes(source));
            File.SetLastWriteTimeUtc(destination, File.GetLastWriteTimeUtc(source));
        }
    }

    private static void CopyFile(string source, string destination, CancellationToken cancellationToken)
    {
        using var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024, FileOptions.SequentialScan);
        using var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1024 * 1024, FileOptions.SequentialScan);
        var buffer = new byte[1024 * 1024];
        int bytesRead;
        while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            output.Write(buffer, 0, bytesRead);
        }
        cancellationToken.ThrowIfCancellationRequested();
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

    internal static string ResolveContainedPath(string root, string relativePath)
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

    internal static void RejectPathChain(string root, string file)
    {
        var current = new DirectoryInfo(Path.GetDirectoryName(file)!);
        var rootPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        while (current is not null)
        {
            RejectReparsePoint(current.FullName, "contained path");
            if (string.Equals(Path.TrimEndingDirectorySeparator(current.FullName), rootPath, StringComparison.OrdinalIgnoreCase)) return;
            current = current.Parent;
        }
        throw new InvalidDataException($"Source path is not contained by its declared root: {file}");
    }

    internal static void RejectContainedFilePath(string root, string file)
    {
        RejectPathChain(root, file);
        if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidDataException($"Operation does not follow reparse-point files: {file}");
        }
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
