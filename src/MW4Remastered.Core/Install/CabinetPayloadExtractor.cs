using System.Diagnostics;
using MW4Remastered.Core.Media;

namespace MW4Remastered.Core.Install;

public sealed class CabinetPayloadExtractor
{
    private static readonly TimeSpan ArchiveToolTimeout = TimeSpan.FromMinutes(2);
    private readonly string archiveToolPath;

    public CabinetPayloadExtractor(string archiveToolPath = "tar.exe")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(archiveToolPath);
        this.archiveToolPath = archiveToolPath;
    }

    public IReadOnlyList<string> ExtractGamePayload(string cabinetPath, string destinationRoot, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var cabinet = Path.GetFullPath(cabinetPath);
        if (!File.Exists(cabinet)) throw new FileNotFoundException("Mercenaries cabinet does not exist.", cabinet);
        if ((File.GetAttributes(cabinet) & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidDataException("Mercenaries cabinet cannot be a reparse point.");
        }

        var destination = Path.GetFullPath(destinationRoot);
        if (Directory.Exists(destination) || File.Exists(destination))
        {
            throw new IOException($"Cabinet extraction destination already exists: {destination}");
        }
        var parent = Directory.GetParent(destination)?.FullName ?? throw new InvalidDataException("Cabinet extraction destination must have a parent directory.");
        Directory.CreateDirectory(parent);

        var archiveEntries = ListEntries(cabinet, cancellationToken);
        var expected = ValidateGameEntries(archiveEntries);
        var staging = Path.Combine(parent, $".{Path.GetFileName(destination)}.cab-staging-{Guid.NewGuid():N}");
        Directory.CreateDirectory(staging);

        try
        {
            RunArchiveTool(new[] { "-xf", cabinet, "-C", staging }, cancellationToken);
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

    private IReadOnlyList<string> ListEntries(string cabinet, CancellationToken cancellationToken)
    {
        var result = RunArchiveTool(new[] { "-tf", cabinet }, cancellationToken);
        return result.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
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

    private string RunArchiveTool(IEnumerable<string> arguments, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = archiveToolPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        foreach (var argument in arguments) startInfo.ArgumentList.Add(argument);

        using var process = Process.Start(startInfo) ?? throw new IOException($"Failed to start archive tool: {archiveToolPath}");
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        var deadline = DateTime.UtcNow + ArchiveToolTimeout;
        try
        {
            while (!process.WaitForExit(250))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (DateTime.UtcNow >= deadline)
                    throw new IOException($"Archive tool exceeded the {ArchiveToolTimeout.TotalSeconds:0}-second timeout.");
            }
        }
        catch
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit();
            }
            throw;
        }
        var output = outputTask.GetAwaiter().GetResult();
        var error = errorTask.GetAwaiter().GetResult();
        if (process.ExitCode != 0)
        {
            throw new InvalidDataException($"Archive tool exited with code {process.ExitCode}: {error.Trim()}");
        }
        return output;
    }
}
