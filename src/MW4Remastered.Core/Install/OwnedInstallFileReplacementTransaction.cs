using System.Security.Cryptography;
using System.Text.Json;

namespace MW4Remastered.Core.Install;

public sealed class OwnedInstallFileReplacementTransaction
{
    private static readonly JsonSerializerOptions ManifestJson = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly InstallManifestVerifier verifier;

    public OwnedInstallFileReplacementTransaction()
        : this(new InstallManifestVerifier())
    {
    }

    public OwnedInstallFileReplacementTransaction(InstallManifestVerifier verifier)
    {
        this.verifier = verifier ?? throw new ArgumentNullException(nameof(verifier));
    }

    public InstallManifest Execute(
        string productId,
        string installRoot,
        IReadOnlyList<InstallFile> replacements,
        CancellationToken cancellationToken = default)
        => ExecuteCore(productId, installRoot, replacements, [], allowNewDestinations: false, cancellationToken);

    public InstallManifest Migrate(
        string productId,
        string installRoot,
        IReadOnlyList<InstallFile> files,
        IReadOnlyList<string> retiredOwnedPaths,
        CancellationToken cancellationToken = default)
        => ExecuteCore(productId, installRoot, files, retiredOwnedPaths, allowNewDestinations: true, cancellationToken);

    private InstallManifest ExecuteCore(
        string productId,
        string installRoot,
        IReadOnlyList<InstallFile> files,
        IReadOnlyList<string> retiredOwnedPaths,
        bool allowNewDestinations,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productId);
        ArgumentException.ThrowIfNullOrWhiteSpace(installRoot);
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(retiredOwnedPaths);
        if (files.Count == 0 && retiredOwnedPaths.Count == 0)
            throw new InvalidDataException("Owned-file migration contains no operations.");

        var root = Path.GetFullPath(installRoot);
        var existing = verifier.Verify(root, InstallVerificationScope.OwnedFiles);
        if (!existing.IsValid || existing.Manifest is null)
        {
            throw new InvalidDataException("The target installation is not a verified owned tree: " + string.Join("; ", existing.Issues));
        }
        if (!string.Equals(existing.Manifest.ProductId, productId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"Replacement target '{productId}' does not match installed product '{existing.Manifest.ProductId}'.");
        }

        var manifestPath = StagedInstallTransaction.ResolveContainedPath(root, InstallManifest.RelativePath);
        var metadataRoot = Path.GetDirectoryName(manifestPath)!;
        var transactionRoot = Path.Combine(metadataRoot, $"replacement-{Guid.NewGuid():N}");
        var newRoot = Path.Combine(transactionRoot, "new");
        var backupRoot = Path.Combine(transactionRoot, "backup");
        var nextManifestPath = Path.Combine(transactionRoot, "install-manifest.next.json");
        var backupManifestPath = Path.Combine(transactionRoot, "install-manifest.backup.json");
        var committed = new List<(string Destination, string? Backup)>();
        var retired = new List<(string Destination, string Backup)>();
        var manifestReplaced = false;
        var recoveryRequired = false;

        try
        {
            var prepared = Prepare(files, retiredOwnedPaths, allowNewDestinations, root, existing.Manifest, newRoot, cancellationToken);
            if (prepared.Files.Count == 0 && prepared.RetiredPaths.Count == 0) return existing.Manifest;

            Directory.CreateDirectory(backupRoot);
            foreach (var file in prepared.Files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var staged = StagedInstallTransaction.ResolveContainedPath(newRoot, file.Path);
                var destination = StagedInstallTransaction.ResolveContainedPath(root, file.Path);
                var backup = StagedInstallTransaction.ResolveContainedPath(backupRoot, file.Path);
                Directory.CreateDirectory(Path.GetDirectoryName(backup)!);
                StagedInstallTransaction.RejectPathChain(root, destination);
                if (file.ReplacesOwnedFile)
                {
                    File.Replace(staged, destination, backup);
                    committed.Add((destination, backup));
                }
                else
                {
                    if (File.Exists(destination))
                        throw new InvalidDataException($"Owned-file migration cannot overwrite an unowned destination: {file.Path}");
                    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                    File.Move(staged, destination);
                    committed.Add((destination, null));
                }
            }
            foreach (var relative in prepared.RetiredPaths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var destination = StagedInstallTransaction.ResolveContainedPath(root, relative);
                var backup = StagedInstallTransaction.ResolveContainedPath(backupRoot, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(backup)!);
                StagedInstallTransaction.RejectPathChain(root, destination);
                File.Move(destination, backup);
                retired.Add((destination, backup));
            }

            File.WriteAllText(nextManifestPath, JsonSerializer.Serialize(prepared.Manifest, ManifestJson) + Environment.NewLine);
            File.Replace(nextManifestPath, manifestPath, backupManifestPath);
            manifestReplaced = true;

            var verification = verifier.Verify(root, InstallVerificationScope.OwnedFiles);
            if (!verification.IsValid)
            {
                throw new InvalidDataException("Owned-file replacement committed an invalid manifest: " + string.Join("; ", verification.Issues));
            }

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
                foreach (var file in retired.AsEnumerable().Reverse())
                {
                    if (File.Exists(file.Backup)) File.Move(file.Backup, file.Destination);
                }
                foreach (var file in committed.AsEnumerable().Reverse())
                {
                    if (file.Backup is not null && File.Exists(file.Backup))
                        File.Replace(file.Backup, file.Destination, destinationBackupFileName: null);
                    else if (file.Backup is null && File.Exists(file.Destination))
                        File.Delete(file.Destination);
                }
            }
            catch (Exception rollbackError)
            {
                recoveryRequired = true;
                throw new AggregateException("Owned-file replacement failed and rollback also failed.", commitError, rollbackError);
            }
            throw;
        }
        finally
        {
            if (!recoveryRequired && Directory.Exists(transactionRoot)) Directory.Delete(transactionRoot, recursive: true);
        }
    }

    private static PreparedReplacement Prepare(
        IReadOnlyList<InstallFile> files,
        IReadOnlyList<string> retiredOwnedPaths,
        bool allowNewDestinations,
        string installRoot,
        InstallManifest existing,
        string newRoot,
        CancellationToken cancellationToken)
    {
        var owned = existing.Files.ToDictionary(file => Normalize(file.Path), StringComparer.OrdinalIgnoreCase);
        var claimed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var changed = new List<PreparedFile>();
        Directory.CreateDirectory(newRoot);

        foreach (var operation in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relative = Normalize(operation.DestinationRelativePath);
            if (!claimed.Add(relative)) throw new InvalidDataException($"Owned-file replacement duplicates destination: {relative}");
            var replacesOwnedFile = owned.TryGetValue(relative, out var current);
            if (!replacesOwnedFile && !allowNewDestinations)
            {
                throw new InvalidDataException($"Owned-file replacement cannot claim an unowned destination: {relative}");
            }

            var sourceRoot = Path.GetFullPath(operation.SourceRoot);
            var source = StagedInstallTransaction.ResolveContainedPath(sourceRoot, operation.SourceRelativePath);
            if (!File.Exists(source)) throw new FileNotFoundException("Replacement source file does not exist.", source);
            StagedInstallTransaction.RejectContainedFilePath(sourceRoot, source);
            var destination = StagedInstallTransaction.ResolveContainedPath(installRoot, relative);
            if (replacesOwnedFile)
            {
                if (!File.Exists(destination)) throw new FileNotFoundException("Owned replacement destination is missing.", destination);
                StagedInstallTransaction.RejectContainedFilePath(installRoot, destination);
            }
            else if (File.Exists(destination) || Directory.Exists(destination))
            {
                throw new InvalidDataException($"Owned-file migration cannot claim an existing unowned destination: {relative}");
            }

            var sourceInfo = new FileInfo(source);
            var sourceHash = ComputeSha256(source);
            if (replacesOwnedFile && sourceInfo.Length == current!.Length &&
                sourceHash.Equals(current.Sha256, StringComparison.OrdinalIgnoreCase)) continue;

            var staged = StagedInstallTransaction.ResolveContainedPath(newRoot, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(staged)!);
            File.Copy(source, staged, overwrite: false);
            File.SetAttributes(staged, FileAttributes.Normal);
            changed.Add(new PreparedFile(
                new InstalledFile(relative, sourceInfo.Length, sourceHash, Path.GetFileName(sourceRoot)),
                replacesOwnedFile));
        }

        var retired = new List<string>();
        foreach (var requestedPath in retiredOwnedPaths)
        {
            var relative = Normalize(requestedPath);
            if (!claimed.Add(relative))
                throw new InvalidDataException($"Owned-file migration duplicates destination or retirement: {relative}");
            if (!owned.ContainsKey(relative)) continue;
            var destination = StagedInstallTransaction.ResolveContainedPath(installRoot, relative);
            if (!File.Exists(destination)) throw new FileNotFoundException("Retired owned file is missing.", destination);
            StagedInstallTransaction.RejectContainedFilePath(installRoot, destination);
            retired.Add(relative);
        }

        var replacementByPath = changed.ToDictionary(file => file.File.Path, StringComparer.OrdinalIgnoreCase);
        var retiredSet = retired.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var nextFiles = existing.Files
            .Where(file => !retiredSet.Contains(Normalize(file.Path)))
            .Select(file => replacementByPath.TryGetValue(Normalize(file.Path), out var replacement) ? replacement.File : file)
            .Concat(changed.Where(file => !file.ReplacesOwnedFile).Select(file => file.File))
            .OrderBy(file => file.Path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return new PreparedReplacement(
            new InstallManifest(existing.SchemaVersion, existing.ProductId, nextFiles, existing.Components),
            changed,
            retired);
    }

    private static string Normalize(string path) =>
        StagedInstallTransaction.NormalizeRelativePath(path).Replace('\\', '/');

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private sealed record PreparedFile(InstalledFile File, bool ReplacesOwnedFile)
    {
        public string Path => File.Path;
    }

    private sealed record PreparedReplacement(
        InstallManifest Manifest,
        IReadOnlyList<PreparedFile> Files,
        IReadOnlyList<string> RetiredPaths);
}
