using System.Security.Cryptography;

namespace MW4Remastered.Core.Install;

public sealed record QualifiedCompatibilityFile(
    string SourceRelativePath,
    string DestinationRelativePath,
    string Sha256);

public sealed class QualifiedCompatibilityPayload
{
    private readonly string root;
    private readonly IReadOnlyList<QualifiedCompatibilityFile> files;

    public QualifiedCompatibilityPayload(string root, IEnumerable<QualifiedCompatibilityFile> files)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        ArgumentNullException.ThrowIfNull(files);
        this.root = Path.GetFullPath(root);
        this.files = files.ToArray();
        if (this.files.Count == 0) throw new ArgumentException("Compatibility payload cannot be empty.", nameof(files));
        if (this.files.Select(file => file.DestinationRelativePath).Distinct(StringComparer.OrdinalIgnoreCase).Count() != this.files.Count)
        {
            throw new ArgumentException("Compatibility payload destinations must be unique.", nameof(files));
        }

        foreach (var file in this.files)
        {
            ValidateRelativePath(file.SourceRelativePath, nameof(file.SourceRelativePath));
            ValidateRelativePath(file.DestinationRelativePath, nameof(file.DestinationRelativePath));
            if (file.Sha256.Length != 64 || !file.Sha256.All(Uri.IsHexDigit))
            {
                throw new ArgumentException($"Compatibility payload hash must be SHA-256: {file.SourceRelativePath}", nameof(files));
            }
        }
    }

    public IReadOnlyList<InstallFile> ValidateAndCreateInstallFiles()
    {
        if (!Directory.Exists(root)) throw new DirectoryNotFoundException($"Compatibility payload directory does not exist: {root}");
        if ((File.GetAttributes(root) & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidDataException("Compatibility payload directory cannot be a reparse point.");
        }

        var result = new List<InstallFile>(files.Count);
        foreach (var file in files)
        {
            var source = Path.GetFullPath(Path.Combine(root, file.SourceRelativePath.Replace('/', Path.DirectorySeparatorChar)));
            var relative = Path.GetRelativePath(root, source);
            if (relative == ".." || relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) || Path.IsPathRooted(relative))
            {
                throw new InvalidDataException($"Compatibility payload source escapes its root: {file.SourceRelativePath}");
            }
            if (!File.Exists(source)) throw new FileNotFoundException("Compatibility payload file does not exist.", source);
            if ((File.GetAttributes(source) & FileAttributes.ReparsePoint) != 0)
            {
                throw new InvalidDataException($"Compatibility payload file cannot be a reparse point: {file.SourceRelativePath}");
            }

            using var stream = File.OpenRead(source);
            var actualHash = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
            if (!string.Equals(actualHash, file.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"Compatibility payload SHA-256 mismatch for {file.SourceRelativePath}: {actualHash}");
            }
            result.Add(new InstallFile(root, file.SourceRelativePath, file.DestinationRelativePath));
        }
        return result;
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
