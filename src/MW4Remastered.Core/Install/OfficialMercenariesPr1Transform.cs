using System.Diagnostics;
using System.Security.Cryptography;

namespace MW4Remastered.Core.Install;

public sealed record PreparedMercenariesPr1Payload(string PayloadRoot, string TransformId);

public sealed class OfficialMercenariesPr1Transform
{
    public const string TransformId = "mercenaries-official-pr1-50.07.01.2105-v1";

    private static readonly QualifiedPath[] PatchedOutputs =
    [
        new("Content/ABLScripts/StockScripts/objectives.abl", 1932, "1fb57e558b62828399d7204bc258f72c76b342617a24d99bb642aa05186d223d"),
        new("Content/ABLScripts/StockScripts/Stock_MissionPlay.abl", 4081, "c025ffa411d0c55e39c49fb551e833b83cc212fbe7029bddf97eb26184a01629"),
        new("EULA.RTF", 52711, "91226db5185574517ee0ed72971551c6cb086ba658dce865c0f2657344524a24"),
        new("MissionLang_MERCS.dll", 253952, "280d422c13d428bc4b4b17d7809ace628b10927047c2f5e37aadeb337f0db1b3"),
        new("MW4Mercs.exe", 344863, MercenariesPr1ExecutableTransform.LoaderSha256),
        new("MW4MERCS.ICD", 3915897, MercenariesPr1ExecutableTransform.EncryptedImageSha256),
        new("NFMEditor.exe", 471040, "173db0b5f7c7000febf86e7c21519c24f8e11cdab41730d049a1fd22aaf3aa21"),
        new("RESOURCE/core.mw4", 1694684, "ad045a0a2026408ecaf22ea739a53812803a91464d647f87a8d3e90b92ba1421"),
        new("RESOURCE/MISSIONS/paradise.mw4", 298072, "2a16f957749f020e7564aeb48db748bf626f3c0d5275c8b3cc3fc881a74cd177"),
        new("RESOURCE/props.mw4", 178114343, "9facd9753aa5336d320a5ec280bc063ed38cd2be7e0315fa147972f8cd91fb6c"),
        new("ScriptStrings_MERCS.dll", 307200, "588114d0ce5a8956785df13093276bafb617ad8ac1d2a271c3f1362dd9dd2620"),
    ];

    private static readonly string[] RetainedPatchedFiles =
    [
        "Content/ABLScripts/StockScripts/objectives.abl",
        "Content/ABLScripts/StockScripts/Stock_MissionPlay.abl",
        "EULA.RTF",
        "MissionLang_MERCS.dll",
        "NFMEditor.exe",
        "RESOURCE/core.mw4",
        "RESOURCE/MISSIONS/paradise.mw4",
        "RESOURCE/props.mw4",
        "ScriptStrings_MERCS.dll",
    ];

    private readonly MercenariesPr1ExecutableTransform executableTransform;

    public OfficialMercenariesPr1Transform()
        : this(new MercenariesPr1ExecutableTransform())
    {
    }

    public OfficialMercenariesPr1Transform(MercenariesPr1ExecutableTransform executableTransform)
    {
        this.executableTransform = executableTransform ?? throw new ArgumentNullException(nameof(executableTransform));
    }

    public PreparedMercenariesPr1Payload Transform(
        InstallPlan retailPlan,
        string discOneRoot,
        string updateRoot,
        string patchHostPath,
        string scratchDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(retailPlan);
        if (!retailPlan.ProductId.Equals("mercenaries", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Mercenaries PR1 requires a retail Mercenaries install plan.");

        var scratch = Path.GetFullPath(scratchDirectory);
        if (Directory.Exists(scratch) || File.Exists(scratch)) throw new IOException("Mercenaries PR1 scratch path already exists.");
        var workRoot = Path.Combine(scratch, "work");
        var outputRoot = Path.Combine(scratch, "payload");
        Directory.CreateDirectory(workRoot);
        try
        {
            foreach (var file in retailPlan.Files.Where(item =>
                         !item.DestinationRelativePath.Equals("MW4Mercs.exe", StringComparison.OrdinalIgnoreCase)))
            {
                Copy(file.SourceRoot, file.SourceRelativePath, workRoot, file.DestinationRelativePath, cancellationToken);
            }
            Copy(discOneRoot, "MW4MERCS.EXE", workRoot, "MW4Mercs.exe", cancellationToken);
            Copy(discOneRoot, "MW4MERCS.ICD", workRoot, "MW4MERCS.ICD", cancellationToken);
            Copy(discOneRoot, "DPLAYERX.DLL", workRoot, "DPLAYERX.DLL", cancellationToken);

            var update = Path.GetFullPath(updateRoot);
            var engine = RequireQualifiedFile(update, new QualifiedPath(
                "Patchw32.dll", 185344, "0ec6e25234ad74489eb1890d4de57bb6140bb8196bdc4a5dcac90dd9d16eb2dd"), "Mercenaries PR1 engine");
            var patch = RequireQualifiedFile(update, new QualifiedPath(
                "English/MW4MERCS.RTP", 5119691, "d3ebf1c2dc098a8e55d7cbeb112426fb413dfd23595a1ed078cd35fe7a551a65"), "Mercenaries PR1 payload");
            RunHost(RequireRegularFile(Path.GetFullPath(patchHostPath), "RTP patch host"), engine, workRoot, patch, cancellationToken);
            foreach (var expected in PatchedOutputs) _ = RequireQualifiedFile(workRoot, expected, "Mercenaries PR1 output");

            Directory.CreateDirectory(outputRoot);
            foreach (var relativePath in RetainedPatchedFiles)
                Copy(workRoot, relativePath, outputRoot, relativePath, cancellationToken);
            _ = executableTransform.Transform(workRoot, outputRoot, cancellationToken);
            VerifyRetainedPayload(outputRoot);
            return new PreparedMercenariesPr1Payload(outputRoot, TransformId);
        }
        catch
        {
            RemoveTree(scratch);
            throw;
        }
    }

    public static IReadOnlyList<InstallFile> CreateInstallFiles(string payloadRoot)
    {
        var root = Path.GetFullPath(payloadRoot);
        VerifyRetainedPayload(root);
        return Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/'))
            .Order(StringComparer.OrdinalIgnoreCase)
            .Select(path => new InstallFile(root, path, path))
            .ToArray();
    }

    private static void VerifyRetainedPayload(string root)
    {
        var expected = PatchedOutputs.Where(item => RetainedPatchedFiles.Contains(item.RelativePath, StringComparer.OrdinalIgnoreCase)).ToList();
        expected.Add(new QualifiedPath("MW4Mercs.exe", 3915776, MercenariesPr1ExecutableTransform.OutputSha256));
        var actual = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/')).ToArray();
        if (actual.Length != expected.Count || actual.Any(path => !expected.Any(item => item.RelativePath.Equals(path, StringComparison.OrdinalIgnoreCase))))
            throw new InvalidDataException("Retained Mercenaries PR1 payload inventory does not match its qualified allowlist.");
        foreach (var item in expected) _ = RequireQualifiedFile(root, item, "retained Mercenaries PR1 payload");
    }

    private static void RunHost(string host, string engine, string workRoot, string patch, CancellationToken cancellationToken)
    {
        var start = new ProcessStartInfo
        {
            FileName = host,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = workRoot,
        };
        start.ArgumentList.Add(engine);
        start.ArgumentList.Add(workRoot);
        start.ArgumentList.Add(patch);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start the RTP patch host.");
        var stdout = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = process.StandardError.ReadToEndAsync(cancellationToken);
        try
        {
            process.WaitForExitAsync(cancellationToken).GetAwaiter().GetResult();
            var output = stdout.GetAwaiter().GetResult();
            var error = stderr.GetAwaiter().GetResult();
            if (process.ExitCode != 0)
            {
                var detail = string.Join(Environment.NewLine, new[] { output, error }.Where(value => !string.IsNullOrWhiteSpace(value)));
                throw new InvalidDataException($"Official Mercenaries PR1 failed with exit code {process.ExitCode}: {(detail.Length > 4096 ? detail[..4096] : detail)}");
            }
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            throw;
        }
    }

    private static void Copy(string sourceRoot, string sourceRelativePath, string destinationRoot, string destinationRelativePath, CancellationToken cancellationToken)
    {
        var source = RequireRegularFile(ContainedPath(sourceRoot, sourceRelativePath), "Mercenaries PR1 input");
        var destination = ContainedPath(destinationRoot, destinationRelativePath);
        if (File.Exists(destination)) throw new InvalidDataException($"Duplicate Mercenaries PR1 input destination: {destinationRelativePath}");
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

    private static string RequireQualifiedFile(string root, QualifiedPath expected, string description)
    {
        var path = RequireRegularFile(ContainedPath(root, expected.RelativePath), description);
        using var stream = File.OpenRead(path);
        var hash = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
        if (stream.Length != expected.Length || !hash.Equals(expected.Sha256, StringComparison.Ordinal))
            throw new InvalidDataException(
                $"Unsupported {description} {expected.RelativePath}: length={stream.Length}, sha256={hash}.");
        return path;
    }

    private static string RequireRegularFile(string path, string description)
    {
        if (!File.Exists(path)) throw new FileNotFoundException($"Missing {description}.", path);
        if ((File.GetAttributes(path) & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0)
            throw new InvalidDataException($"The {description} must be a regular file.");
        return path;
    }

    private static string ContainedPath(string root, string relativePath)
    {
        var fullRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        var candidate = Path.GetFullPath(Path.Combine(fullRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var relative = Path.GetRelativePath(fullRoot, candidate);
        if (Path.IsPathRooted(relative) || relative.Equals("..", StringComparison.Ordinal) ||
            relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new InvalidDataException("Mercenaries PR1 path escaped its owned root.");
        return candidate;
    }

    private static void RemoveTree(string path)
    {
        if (!Directory.Exists(path)) return;
        foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)) File.SetAttributes(file, FileAttributes.Normal);
        Directory.Delete(path, recursive: true);
    }

    private sealed record QualifiedPath(string RelativePath, long Length, string Sha256);
}
