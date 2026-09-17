using System.Diagnostics;
using System.Security.Cryptography;

namespace MW4Remastered.Core.Install;

public sealed record PreparedBlackKnightPr1Payload(
    string PayloadRoot,
    string TransformId,
    IReadOnlyList<InstallFile> InstallFiles);

public interface IBlackKnightPr1Transform
{
    PreparedBlackKnightPr1Payload Transform(
        InstallPlan aggregateRetailPlan,
        string officialVengeancePatch3Root,
        string patchMediaRoot,
        string patchHostPath,
        string captureDllPath,
        string scratchDirectory,
        CancellationToken cancellationToken = default);
}

public sealed class OfficialBlackKnightPr1Transform : IBlackKnightPr1Transform
{
    public const string TransformId = "black-knight-official-pr1-45.30.04.1908-static-v3";

    private static readonly QualifiedPath[] PatchedOutputs =
    [
        new("autoconfigx.exe", 438272, "60ef0143ed45ae73340fbb16f3d2e110770beda855e1260fde162a010d86b6cb"),
        new("MissionLangx.dll", 331776, "b8c6d7efd93265bb1e642819f085e21440aab375a05398fbbf847a7dcf280260"),
        new("MW4 Dedicated Server.bat", 25, "ff81e867d3fb7dc9fec4b65213a8ae3ba9800f5d0c2265ba508e5bef7f21aaa0"),
        new("MW4X/artpclnt.dll", 106496, "30ea1455f4ade24f4b6426781632b29c23c8006bc478da3a0d3c1fd6381925ee"),
        new("MW4X/cabarc.exe", 114688, "3ed33e71641645367442e65dca6dab0d326b22b48ef9a4c2a2488e67383aa9a6"),
        new("MW4X/Cdac14ba.dll", 112128, "9d8379cd2cf899e8873d40932c66bc683676d9bdad6f0cdec7873859c95b13c1"),
        new("MW4X/MW4x.exe", BlackKnightPr1ExecutableTransform.InputLength, BlackKnightPr1ExecutableTransform.InputSha256),
        new("NFXEditor.exe", 471040, "197b69f3d0f119ca131c7191073a7dd945963647fed02e863a4269ddd7c96523"),
        new("RESOURCE/corex.mw4", 1444440, "c1b444cb137820c7f08a66ee70f778da3fc985fda5551e22a22e89aeb6fe02a5"),
        new("RESOURCE/propsx.mw4", 33126658, "527a136b01b06105d0aa8db3eb6aae1a215cd1619dcd345ac018a7c290fed3e0"),
        new("RESOURCE/texturesx.mw4", 40373012, "1e7e8c89618e0de19f87598cbb459d811ddc0ae8943d6d2807835bc772049863"),
        new("ScriptStringsx.dll", 163840, "59ac2fda1f5234977bc95d91c622ec8faf3210de937278fb4311610c246bfd28"),
    ];

    private static readonly QualifiedPath[] OfficialVengeancePatch3Inputs =
    [
        new("artpclnt.dll", 106496, "30ea1455f4ade24f4b6426781632b29c23c8006bc478da3a0d3c1fd6381925ee"),
        new("AutoConfig.exe", 438272, "85c62ecdf39b327c4b2166dd0e8f4033b852743e1a1e8637c5f2b9b31fd63757"),
        new("cabarc.exe", 114688, "3ed33e71641645367442e65dca6dab0d326b22b48ef9a4c2a2488e67383aa9a6"),
        new("Cdac14ba.dll", 112128, "9d8379cd2cf899e8873d40932c66bc683676d9bdad6f0cdec7873859c95b13c1"),
        new("CONTENT/GameTypes.h", 990, "56675ef8d39fe6ba151dc5c9d615664f990f1351429f537bff6a884467969b01"),
        new("DPlayerX.dll", 138752, "eb751d527e8c9b893ae0f5dd6ade3e15bbce468c5b0331afc87d0a23ba621ff7"),
        new("MissionLang.dll", 196608, "06fdc773b3077cbb7ca44e90fd06e0d5b512200dd925d5c427a04f43701010eb"),
        new("MW4.exe", 344863, "578800c3f3d8d74c366a7229240c34850a85379573059fc8b7e7dc5603563e80"),
        new("MW4.ICD", 3698804, "9e13cfda761d655222da523f4c603ea91f46c1669341fe82d8b6630ccbe3ecf6"),
        new("NFOEditor.exe", 471040, "cfbc2087595904aef932f23699fc64ab7b5a8f8b6b383b6cd4c0151692d210c1"),
        new("RESOURCE/core.mw4", 1252036, "f59dfecb9aa4f76648b9c7f1e0c1c7e58ba5bcd0022c113dc87c20f6d33aa73c"),
        new("RESOURCE/MISSIONS/att.txt", 611, "ce04f0c9fe3799f12190d7134815a6b08cef5961984d8ec81e26e2f60f3af3fa"),
        new("RESOURCE/MISSIONS/koth.txt", 483, "e6c8dc7be11471b663c3c050b3e8d8ae126325f1b66c4d99b1ba8e24d8e48d75"),
        new("RESOURCE/props.mw4", 128704483, "21d4f234f8c19ee9b101330c0682223a4023fb649a19946600ea4dbed5a60b19"),
        new("RESOURCE/textures.mw4", 158439264, "11a0aa030bc40aa18531b6e5a814f5b0279c18561b4d1b3e36343ffad64856aa"),
        new("ScriptStrings.dll", 147456, "3b7abc2bb57130e554fa88a4f8f5057c8f252089fd802ca5df73fa848acbc505"),
    ];

    private static readonly string[] RetainedPatchedFiles =
    [
        "autoconfigx.exe", "MissionLangx.dll", "MW4 Dedicated Server.bat",
        "MW4X/artpclnt.dll", "MW4X/cabarc.exe", "MW4X/Cdac14ba.dll",
        "NFXEditor.exe", "RESOURCE/corex.mw4", "RESOURCE/propsx.mw4",
        "RESOURCE/texturesx.mw4", "ScriptStringsx.dll",
    ];

    private readonly BlackKnightPr1ImageCapture imageCapture;
    private readonly BlackKnightPr1ExecutableTransform executableTransform;

    public OfficialBlackKnightPr1Transform()
        : this(new BlackKnightPr1ImageCapture(), new BlackKnightPr1ExecutableTransform()) { }

    public OfficialBlackKnightPr1Transform(
        BlackKnightPr1ImageCapture imageCapture,
        BlackKnightPr1ExecutableTransform executableTransform)
    {
        this.imageCapture = imageCapture ?? throw new ArgumentNullException(nameof(imageCapture));
        this.executableTransform = executableTransform ?? throw new ArgumentNullException(nameof(executableTransform));
    }

    public bool IsQualifiedPatchMedia(string mediaRoot)
    {
        try
        {
            var root = Path.GetFullPath(mediaRoot);
            _ = RequireQualifiedFile(root, new QualifiedPath(
                "GOODIES/PATCH3/MW4XP1/PATCHW32.DLL", 185344,
                "0ec6e25234ad74489eb1890d4de57bb6140bb8196bdc4a5dcac90dd9d16eb2dd"), "Black Knight PR1 engine");
            _ = RequireQualifiedFile(root, new QualifiedPath(
                "GOODIES/PATCH3/MW4XP1/ENGLISH/MW4X.RTP", 43865979,
                "5f3b6a383b7c1821e9f682ea3d6675818c23bf0138289e89a951256ecf2ab6ff"), "Black Knight PR1 payload");
            return true;
        }
        catch (Exception error) when (error is IOException or InvalidDataException or ArgumentException or NotSupportedException)
        {
            return false;
        }
    }

    public PreparedBlackKnightPr1Payload Transform(
        InstallPlan aggregateRetailPlan,
        string officialVengeancePatch3Root,
        string patchMediaRoot,
        string patchHostPath,
        string captureDllPath,
        string scratchDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(aggregateRetailPlan);
        if (!aggregateRetailPlan.ProductId.Equals("vengeance", StringComparison.OrdinalIgnoreCase) ||
            !aggregateRetailPlan.Components.Contains("black-knight", StringComparer.OrdinalIgnoreCase))
            throw new InvalidDataException("Black Knight PR1 requires a complete Vengeance-family plan containing Black Knight.");

        var scratch = Path.GetFullPath(scratchDirectory);
        if (Directory.Exists(scratch) || File.Exists(scratch)) throw new IOException("Black Knight PR1 scratch path already exists.");
        var workRoot = Path.Combine(scratch, "work");
        var outputRoot = Path.Combine(scratch, "payload");
        Directory.CreateDirectory(workRoot);
        try
        {
            foreach (var file in aggregateRetailPlan.Files)
                Copy(file.SourceRoot, file.SourceRelativePath, workRoot, file.DestinationRelativePath, cancellationToken);

            // Black Knight PR1 validates the official protected Patch 3 files.
            // The install plan intentionally contains our static Vengeance output,
            // so restore the exact official intermediates only inside this scratch
            // tree before applying Microsoft's next patch.
            OverlayOfficialVengeancePatch3(officialVengeancePatch3Root, workRoot, cancellationToken);

            var media = Path.GetFullPath(patchMediaRoot);
            var engine = RequireQualifiedFile(media, new QualifiedPath(
                "GOODIES/PATCH3/MW4XP1/PATCHW32.DLL", 185344,
                "0ec6e25234ad74489eb1890d4de57bb6140bb8196bdc4a5dcac90dd9d16eb2dd"), "Black Knight PR1 engine");
            var patch = RequireQualifiedFile(media, new QualifiedPath(
                "GOODIES/PATCH3/MW4XP1/ENGLISH/MW4X.RTP", 43865979,
                "5f3b6a383b7c1821e9f682ea3d6675818c23bf0138289e89a951256ecf2ab6ff"), "Black Knight PR1 payload");
            RunProcess(RequireRegularFile(patchHostPath, "RTP patch host"), workRoot,
                [engine, workRoot, patch], "Official Black Knight PR1", cancellationToken);
            foreach (var expected in PatchedOutputs) _ = RequireQualifiedFile(workRoot, expected, "Black Knight PR1 output");

            var protectedExecutable = ContainedPath(workRoot, "MW4X/MW4x.exe");
            var mappedImage = imageCapture.Capture(
                protectedExecutable,
                captureDllPath,
                ContainedPath(scratch, "capture"),
                cancellationToken);

            Directory.CreateDirectory(outputRoot);
            foreach (var relativePath in RetainedPatchedFiles)
                Copy(workRoot, relativePath, outputRoot, relativePath, cancellationToken);
            _ = executableTransform.Transform(
                protectedExecutable,
                mappedImage,
                ContainedPath(outputRoot, "MW4X/MW4x.exe"),
                cancellationToken);
            VerifyRetainedPayload(outputRoot);
            return new PreparedBlackKnightPr1Payload(outputRoot, TransformId, CreateInstallFiles(outputRoot));
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
        expected.Add(new QualifiedPath("MW4X/MW4x.exe", BlackKnightPr1ExecutableTransform.OutputLength,
            BlackKnightPr1ExecutableTransform.OutputSha256));
        var actual = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/')).ToArray();
        if (actual.Length != expected.Count || actual.Any(path => !expected.Any(item => item.RelativePath.Equals(path, StringComparison.OrdinalIgnoreCase))))
            throw new InvalidDataException("Retained Black Knight PR1 payload inventory does not match its qualified allowlist.");
        foreach (var item in expected) _ = RequireQualifiedFile(root, item, "retained Black Knight PR1 payload");
    }

    private static void RunProcess(string executable, string workingDirectory, IReadOnlyList<string> arguments,
        string description, CancellationToken cancellationToken)
    {
        var start = new ProcessStartInfo
        {
            FileName = executable, WorkingDirectory = workingDirectory, UseShellExecute = false,
            CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true,
        };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException($"Could not start {description}.");
        var stdout = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = process.StandardError.ReadToEndAsync(cancellationToken);
        try
        {
            process.WaitForExitAsync(cancellationToken).GetAwaiter().GetResult();
            var detail = string.Join(Environment.NewLine, new[] { stdout.GetAwaiter().GetResult(), stderr.GetAwaiter().GetResult() }
                .Where(value => !string.IsNullOrWhiteSpace(value)));
            if (process.ExitCode != 0)
                throw new InvalidDataException($"{description} failed with exit code {process.ExitCode}: {(detail.Length > 4096 ? detail[..4096] : detail)}");
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            throw;
        }
    }

    private static void OverlayOfficialVengeancePatch3(string sourceRoot, string destinationRoot, CancellationToken cancellationToken)
    {
        var root = Path.GetFullPath(sourceRoot);
        foreach (var input in OfficialVengeancePatch3Inputs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var source = RequireQualifiedFile(root, input, "official Vengeance Patch 3 intermediate");
            var destination = ContainedPath(destinationRoot, input.RelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(source, destination, overwrite: true);
        }
    }

    private static void Copy(string sourceRoot, string sourceRelativePath, string destinationRoot, string destinationRelativePath, CancellationToken cancellationToken)
    {
        var source = RequireRegularFile(ContainedPath(sourceRoot, sourceRelativePath), "Black Knight PR1 input");
        var destination = ContainedPath(destinationRoot, destinationRelativePath);
        if (File.Exists(destination)) throw new InvalidDataException($"Duplicate Black Knight PR1 input destination: {destinationRelativePath}");
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

    private static string Hash(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static string RequireRegularFile(string path, string description)
    {
        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath)) throw new FileNotFoundException($"Missing {description}.", fullPath);
        if ((File.GetAttributes(fullPath) & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0)
            throw new InvalidDataException($"The {description} must be a regular file.");
        return fullPath;
    }

    private static string ContainedPath(string root, string relativePath)
    {
        var fullRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        var candidate = Path.GetFullPath(Path.Combine(fullRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var relative = Path.GetRelativePath(fullRoot, candidate);
        if (Path.IsPathRooted(relative) || relative.Equals("..", StringComparison.Ordinal) || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new InvalidDataException("Black Knight PR1 path escaped its owned root.");
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
