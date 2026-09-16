using System.Security.Cryptography;
using System.Text.Json;

namespace MW4Remastered.Core.Install;

public sealed class OwnedInstallOverlayTransaction
{
    private static readonly JsonSerializerOptions ManifestJson = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly InstallManifestVerifier verifier;

    public OwnedInstallOverlayTransaction()
        : this(new InstallManifestVerifier())
    {
    }

    public OwnedInstallOverlayTransaction(InstallManifestVerifier verifier)
    {
        this.verifier = verifier ?? throw new ArgumentNullException(nameof(verifier));
    }

    public InstallManifest Execute(MechPakResourceOverlayPlan plan, string installRoot)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentException.ThrowIfNullOrWhiteSpace(installRoot);
        var root = Path.GetFullPath(installRoot);

        var existing = verifier.Verify(root, InstallVerificationScope.OwnedFiles);
        if (!existing.IsValid || existing.Manifest is null)
        {
            throw new InvalidDataException("The target installation is not a verified owned tree: " + string.Join("; ", existing.Issues));
        }
        if (!string.Equals(existing.Manifest.ProductId, plan.TargetProductId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"Overlay target '{plan.TargetProductId}' does not match installed product '{existing.Manifest.ProductId}'.");
        }

        var manifestPath = StagedInstallTransaction.ResolveContainedPath(root, InstallManifest.RelativePath);
        var metadataRoot = Path.GetDirectoryName(manifestPath)!;
        var staging = Path.Combine(metadataRoot, $"overlay-{Guid.NewGuid():N}");
        var nextManifestPath = Path.Combine(metadataRoot, $"install-manifest.next-{Guid.NewGuid():N}.json");
        var backupManifestPath = Path.Combine(metadataRoot, $"install-manifest.backup-{Guid.NewGuid():N}.json");
        var moved = new List<(string Destination, string Staged)>();
        var manifestReplaced = false;
        var completed = false;
        var recoveryRequired = false;

        try
        {
            var prepared = Prepare(plan, root, existing.Manifest, staging);
            foreach (var file in prepared.NewFiles)
            {
                var staged = StagedInstallTransaction.ResolveContainedPath(staging, file.Path);
                var destination = StagedInstallTransaction.ResolveContainedPath(root, file.Path);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                StagedInstallTransaction.RejectPathChain(root, destination);
                File.Move(staged, destination);
                moved.Add((destination, staged));
            }

            File.WriteAllText(nextManifestPath, JsonSerializer.Serialize(prepared.Manifest, ManifestJson) + Environment.NewLine);
            File.Replace(nextManifestPath, manifestPath, backupManifestPath);
            manifestReplaced = true;

            var verification = verifier.Verify(root, InstallVerificationScope.OwnedFiles);
            if (!verification.IsValid)
            {
                throw new InvalidDataException("Overlay committed an invalid ownership manifest: " + string.Join("; ", verification.Issues));
            }

            File.Delete(backupManifestPath);
            completed = true;
            return prepared.Manifest;
        }
        catch (Exception commitError)
        {
            try
            {
                if (manifestReplaced && File.Exists(backupManifestPath))
                {
                    File.Replace(backupManifestPath, manifestPath, destinationBackupFileName: null);
                }
                foreach (var file in moved.AsEnumerable().Reverse())
                {
                    if (!File.Exists(file.Destination)) continue;
                    Directory.CreateDirectory(Path.GetDirectoryName(file.Staged)!);
                    File.Move(file.Destination, file.Staged);
                }
            }
            catch (Exception rollbackError)
            {
                recoveryRequired = true;
                throw new AggregateException("Install overlay failed and rollback also failed.", commitError, rollbackError);
            }
            throw;
        }
        finally
        {
            if (!recoveryRequired && File.Exists(nextManifestPath)) File.Delete(nextManifestPath);
            if (completed && File.Exists(backupManifestPath)) File.Delete(backupManifestPath);
            if (!recoveryRequired && Directory.Exists(staging)) Directory.Delete(staging, recursive: true);
        }
    }

    private static PreparedOverlay Prepare(
        MechPakResourceOverlayPlan plan,
        string installRoot,
        InstallManifest existing,
        string staging)
    {
        if (plan.Files.Count == 0) throw new InvalidDataException("Install overlay contains no files.");
        var claimed = existing.Files.Select(file => Normalize(file.Path)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var additions = new List<InstalledFile>(plan.Files.Count);
        Directory.CreateDirectory(staging);

        foreach (var operation in plan.Files)
        {
            var sourceRoot = Path.GetFullPath(operation.SourceRoot);
            var source = StagedInstallTransaction.ResolveContainedPath(sourceRoot, operation.SourceRelativePath);
            if (!File.Exists(source)) throw new FileNotFoundException("Overlay source file does not exist.", source);
            StagedInstallTransaction.RejectContainedFilePath(sourceRoot, source);

            var relative = Normalize(operation.DestinationRelativePath);
            if (!claimed.Add(relative)) throw new InvalidDataException($"Overlay destination is already owned or duplicated: {relative}");
            var destination = StagedInstallTransaction.ResolveContainedPath(installRoot, relative);
            if (File.Exists(destination) || Directory.Exists(destination))
            {
                throw new IOException($"Overlay destination already exists and will not be replaced: {relative}");
            }

            var staged = StagedInstallTransaction.ResolveContainedPath(staging, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(staged)!);
            File.Copy(source, staged, overwrite: false);
            File.SetAttributes(staged, FileAttributes.Normal);
            var info = new FileInfo(staged);
            additions.Add(new InstalledFile(relative, info.Length, ComputeSha256(staged), Path.GetFileName(sourceRoot)));
        }

        var allFiles = existing.Files.Concat(additions)
            .OrderBy(file => file.Path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return new PreparedOverlay(new InstallManifest(existing.SchemaVersion, existing.ProductId, allFiles), additions);
    }

    private static string Normalize(string path) =>
        StagedInstallTransaction.NormalizeRelativePath(path).Replace('\\', '/');

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private sealed record PreparedOverlay(InstallManifest Manifest, IReadOnlyList<InstalledFile> NewFiles);
}
