namespace MW4Remastered.Core.Media;

public sealed class DirectoryMediaInventory
{
    public IReadOnlyList<string> Read(string rootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        var root = Path.GetFullPath(rootPath);
        if (!Directory.Exists(root)) throw new DirectoryNotFoundException($"Media root does not exist: {root}");
        RejectReparsePoint(root);

        var files = new List<string>();
        var pending = new Stack<string>();
        pending.Push(root);

        while (pending.Count > 0)
        {
            var directory = pending.Pop();
            foreach (var entry in Directory.EnumerateFileSystemEntries(directory))
            {
                RejectReparsePoint(entry);
                var attributes = File.GetAttributes(entry);
                if ((attributes & FileAttributes.Directory) != 0)
                {
                    pending.Push(entry);
                    continue;
                }

                var relative = Path.GetRelativePath(root, entry).Replace('\\', '/');
                if (!MediaRecognizer.TryNormalizeRelativePath(relative, out var normalized) || normalized.Length == 0)
                {
                    throw new InvalidDataException($"Media entry could not be represented safely: {relative}");
                }
                files.Add(normalized);
            }
        }

        files.Sort(StringComparer.OrdinalIgnoreCase);
        return files;
    }

    private static void RejectReparsePoint(string path)
    {
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidDataException($"Media inventory does not follow reparse points: {path}");
        }
    }
}
