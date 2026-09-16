namespace MW4Remastered.Core.Install;

public sealed class VengeancePatch3RetailInputBuilder
{
    private static readonly string[] PlanDestinations =
    [
        "AutoConfig.exe",
        "MissionLang.dll",
        "RESOURCE/CORE.MW4",
        "RESOURCE/MISSIONS/att.txt",
        "RESOURCE/MISSIONS/KOTH.TXT",
        "RESOURCE/props.mw4",
        "RESOURCE/textures.mw4",
        "ScriptStrings.dll",
    ];

    private static readonly string[] DiscOneFiles = ["DPLAYERX.DLL", "MW4.EXE", "MW4.ICD"];

    public string Build(
        InstallPlan retailPlan,
        string discOneRoot,
        string destinationRoot,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(retailPlan);
        if (!retailPlan.ProductId.Equals("vengeance", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Patch 3 retail inputs require a Vengeance install plan.");
        }

        var destination = Path.GetFullPath(destinationRoot);
        if (Directory.Exists(destination) || File.Exists(destination))
        {
            throw new IOException("Patch 3 retail-input destination already exists.");
        }
        Directory.CreateDirectory(destination);
        try
        {
            foreach (var wanted in PlanDestinations)
            {
                var matches = retailPlan.Files.Where(file =>
                    file.DestinationRelativePath.Equals(wanted, StringComparison.OrdinalIgnoreCase)).ToArray();
                if (matches.Length != 1)
                {
                    throw new InvalidDataException($"Vengeance plan must supply exactly one Patch 3 input for {wanted}.");
                }
                Copy(matches[0].SourceRoot, matches[0].SourceRelativePath, destination, wanted, cancellationToken);
            }

            foreach (var relativePath in DiscOneFiles)
            {
                Copy(discOneRoot, relativePath, destination, relativePath, cancellationToken);
            }
            return destination;
        }
        catch
        {
            RemoveTree(destination);
            throw;
        }
    }

    private static void Copy(
        string sourceRoot,
        string sourceRelativePath,
        string destinationRoot,
        string destinationRelativePath,
        CancellationToken cancellationToken)
    {
        var source = ContainedPath(sourceRoot, sourceRelativePath);
        if (!File.Exists(source) || (File.GetAttributes(source) & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0)
        {
            throw new InvalidDataException($"Patch 3 input is not a regular file: {sourceRelativePath}");
        }

        var destination = ContainedPath(destinationRoot, destinationRelativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        using var input = File.OpenRead(source);
        using var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        var buffer = new byte[1024 * 1024];
        int read;
        while ((read = input.Read(buffer)) > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            output.Write(buffer, 0, read);
        }
    }

    private static string ContainedPath(string root, string relativePath)
    {
        var fullRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        var path = Path.GetFullPath(Path.Combine(fullRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var relative = Path.GetRelativePath(fullRoot, path);
        if (Path.IsPathRooted(relative) || relative.Equals("..", StringComparison.Ordinal) ||
            relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new InvalidDataException("Patch 3 input path escaped its source root.");
        }
        return path;
    }

    private static void RemoveTree(string path)
    {
        if (!Directory.Exists(path)) return;
        foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)) File.SetAttributes(file, FileAttributes.Normal);
        Directory.Delete(path, recursive: true);
    }
}
