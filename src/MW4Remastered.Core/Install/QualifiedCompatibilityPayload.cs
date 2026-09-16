using System.Security.Cryptography;

namespace MW4Remastered.Core.Install;

public sealed record QualifiedCompatibilityFile(
    string SourceRelativePath,
    string DestinationRelativePath,
    string Sha256);

public sealed record QualifiedCompatibilitySupportFile(
    string SourceRelativePath,
    string Sha256);

public sealed class QualifiedCompatibilityPayload
{
    private readonly string root;
    private readonly IReadOnlyList<QualifiedCompatibilityFile> files;
    private readonly IReadOnlyList<QualifiedCompatibilitySupportFile> supportFiles;
    private readonly bool requireExactInventory;

    public QualifiedCompatibilityPayload(
        string root,
        IEnumerable<QualifiedCompatibilityFile> files,
        IEnumerable<QualifiedCompatibilitySupportFile>? supportFiles = null,
        bool requireExactInventory = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        ArgumentNullException.ThrowIfNull(files);
        this.root = Path.GetFullPath(root);
        this.files = files.ToArray();
        this.supportFiles = supportFiles?.ToArray() ?? Array.Empty<QualifiedCompatibilitySupportFile>();
        this.requireExactInventory = requireExactInventory;
        if (this.files.Count == 0) throw new ArgumentException("Compatibility payload cannot be empty.", nameof(files));
        if (this.files.Select(file => file.DestinationRelativePath).Distinct(StringComparer.OrdinalIgnoreCase).Count() != this.files.Count)
        {
            throw new ArgumentException("Compatibility payload destinations must be unique.", nameof(files));
        }

        foreach (var file in this.files)
        {
            ValidateRelativePath(file.SourceRelativePath, nameof(file.SourceRelativePath));
            ValidateRelativePath(file.DestinationRelativePath, nameof(file.DestinationRelativePath));
            ValidateSha256(file.SourceRelativePath, file.Sha256, nameof(files));
        }
        foreach (var file in this.supportFiles)
        {
            ValidateRelativePath(file.SourceRelativePath, nameof(file.SourceRelativePath));
            ValidateSha256(file.SourceRelativePath, file.Sha256, nameof(supportFiles));
        }
        var sourcePaths = this.files.Select(file => file.SourceRelativePath)
            .Concat(this.supportFiles.Select(file => file.SourceRelativePath));
        if (sourcePaths.Distinct(StringComparer.OrdinalIgnoreCase).Count() != this.files.Count + this.supportFiles.Count)
        {
            throw new ArgumentException("Compatibility payload source paths must be unique.", nameof(files));
        }
    }

    public IReadOnlyList<InstallFile> ValidateAndCreateInstallFiles()
    {
        if (!Directory.Exists(root)) throw new DirectoryNotFoundException($"Compatibility payload directory does not exist: {root}");
        if ((File.GetAttributes(root) & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidDataException("Compatibility payload directory cannot be a reparse point.");
        }

        if (requireExactInventory) ValidateExactInventory();

        var result = new List<InstallFile>(files.Count);
        foreach (var file in files)
        {
            ValidateSourceFile(file.SourceRelativePath, file.Sha256);
            result.Add(new InstallFile(root, file.SourceRelativePath, file.DestinationRelativePath));
        }
        foreach (var file in supportFiles) ValidateSourceFile(file.SourceRelativePath, file.Sha256);
        return result;
    }

    private void ValidateExactInventory()
    {
        var expected = files.Select(file => file.SourceRelativePath.Replace('\\', '/'))
            .Concat(supportFiles.Select(file => file.SourceRelativePath.Replace('\\', '/')))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var expectedDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in expected)
        {
            var segments = path.Split('/');
            for (var index = 1; index < segments.Length; index++)
                expectedDirectories.Add(string.Join('/', segments.Take(index)));
        }
        var actual = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var directory = pending.Pop();
            foreach (var child in Directory.EnumerateFileSystemEntries(directory))
            {
                var attributes = File.GetAttributes(child);
                if ((attributes & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException($"Compatibility payload inventory cannot contain a reparse point: {child}");
                if ((attributes & FileAttributes.Directory) != 0)
                {
                    var relativeDirectory = Path.GetRelativePath(root, child).Replace('\\', '/');
                    if (!expectedDirectories.Contains(relativeDirectory))
                        throw new InvalidDataException($"Compatibility payload inventory contains an unexpected directory: {relativeDirectory}");
                    pending.Push(child);
                    continue;
                }
                actual.Add(Path.GetRelativePath(root, child).Replace('\\', '/'));
            }
        }
        if (!actual.SetEquals(expected))
        {
            var unexpected = actual.Except(expected, StringComparer.OrdinalIgnoreCase).OrderBy(path => path).ToArray();
            var missing = expected.Except(actual, StringComparer.OrdinalIgnoreCase).OrderBy(path => path).ToArray();
            throw new InvalidDataException($"Compatibility payload inventory mismatch. Missing: [{string.Join(", ", missing)}]; unexpected: [{string.Join(", ", unexpected)}].");
        }
    }

    private void ValidateSourceFile(string relativePath, string expectedHash)
    {
        var source = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var relative = Path.GetRelativePath(root, source);
        if (relative == ".." || relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) || Path.IsPathRooted(relative))
            throw new InvalidDataException($"Compatibility payload source escapes its root: {relativePath}");
        if (!File.Exists(source)) throw new FileNotFoundException("Compatibility payload file does not exist.", source);
        if ((File.GetAttributes(source) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException($"Compatibility payload file cannot be a reparse point: {relativePath}");

        using var stream = File.OpenRead(source);
        var actualHash = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
        if (!string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Compatibility payload SHA-256 mismatch for {relativePath}: {actualHash}");
    }

    private static void ValidateSha256(string path, string sha256, string parameter)
    {
        if (sha256.Length != 64 || !sha256.All(Uri.IsHexDigit))
            throw new ArgumentException($"Compatibility payload hash must be SHA-256: {path}", parameter);
    }

    private static void ValidateRelativePath(string path, string parameter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path, parameter);
        var normalized = path.Replace('\\', '/');
        if (Path.IsPathRooted(path) || normalized.Split('/').Any(segment => segment is "" or "." or ".."))
        {
            throw new ArgumentException($"Compatibility payload path must be a contained relative path: {path}", parameter);
        }
    }
}
