using MW4Remastered.Core.Media;

namespace MW4Remastered.Core.Install;

public sealed class CabinetPayloadExtractor
{
    public IReadOnlyList<string> ExtractGamePayload(string cabinetPath, string destinationRoot, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var cabinet = Path.GetFullPath(cabinetPath);
        if (!File.Exists(cabinet)) throw new FileNotFoundException("Mercenaries cabinet does not exist.", cabinet);
        if ((File.GetAttributes(cabinet) & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidDataException("Mercenaries cabinet cannot be a reparse point.");
        }
        // Keep the validated source immutable through both native passes.
        using var cabinetLease = new FileStream(cabinet, FileMode.Open, FileAccess.Read, FileShare.Read);

        var destination = Path.GetFullPath(destinationRoot);
        if (Directory.Exists(destination) || File.Exists(destination))
        {
            throw new IOException($"Cabinet extraction destination already exists: {destination}");
        }
        var parent = Directory.GetParent(destination)?.FullName ?? throw new InvalidDataException("Cabinet extraction destination must have a parent directory.");
        Directory.CreateDirectory(parent);

        // Windows tar/libarchive 3.8.9 cannot decompress this valid LZX cabinet
        // (libarchive #3542/#3544). Use the OS cabinet reader, not a PATH tool.
        var archiveEntries = WindowsCabinetReader.ReadInventory(cabinet, cancellationToken);
        var expected = ValidateGameEntries(archiveEntries.Keys);
        var staging = Path.Combine(parent, $".cab-staging-{Guid.NewGuid():N}");
        Directory.CreateDirectory(staging);

        try
        {
            WindowsCabinetReader.Extract(cabinet, staging, archiveEntries, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            var extractedGameRoot = Path.Combine(staging, "GAME");
            if (!Directory.Exists(extractedGameRoot)) throw new InvalidDataException("Cabinet did not produce the expected GAME payload root.");

            var actual = new DirectoryMediaInventory().Read(extractedGameRoot).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (!actual.SetEquals(expected))
            {
                var missing = expected.Except(actual, StringComparer.OrdinalIgnoreCase).OrderBy(path => path).Take(5);
                var extra = actual.Except(expected, StringComparer.OrdinalIgnoreCase).OrderBy(path => path).Take(5);
                throw new InvalidDataException($"Cabinet output inventory mismatch. Missing: {string.Join(", ", missing)}. Extra: {string.Join(", ", extra)}.");
            }

            Directory.Move(extractedGameRoot, destination);
            return expected.OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray();
        }
        finally
        {
            if (Directory.Exists(staging)) Directory.Delete(staging, true);
        }
    }

    private static HashSet<string> ValidateGameEntries(IEnumerable<string> entries)
    {
        var accepted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries)
        {
            var portable = entry.Replace('\\', '/');
            if (!portable.StartsWith("GAME/", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"Cabinet entry is outside the GAME payload: {entry}");
            }

            var relative = portable["GAME/".Length..];
            if (!MediaRecognizer.TryNormalizeRelativePath(relative, out var normalized) || normalized.Length == 0)
            {
                throw new InvalidDataException($"Unsafe cabinet entry: {entry}");
            }
            if (!accepted.Add(normalized)) throw new InvalidDataException($"Duplicate cabinet destination: {normalized}");
        }

        if (accepted.Count == 0) throw new InvalidDataException("Cabinet contains no GAME payload files.");
        return accepted;
    }

}
