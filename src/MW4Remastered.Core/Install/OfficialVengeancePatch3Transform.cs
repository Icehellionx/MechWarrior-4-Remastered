using System.Diagnostics;
using System.Security.Cryptography;
using MW4Remastered.Core.Media;

namespace MW4Remastered.Core.Install;

public sealed record PreparedVengeancePatch3Payload(string PayloadRoot, string TransformId);

public sealed class OfficialVengeancePatch3Transform
{
    public const string TransformId = "vengeance-official-patch3-01.30.04.1908-v1";

    private const string EngineRelativePath = "GOODIES/PATCH3/MW4P3/PATCHW32.DLL";
    private const string PatchRelativePath = "GOODIES/PATCH3/MW4P3/ENGLISH/MW4.RTP";

    private static readonly QualifiedPath[] RetailInputs =
    [
        new("AutoConfig.exe", 430080, "48bf300a5df2d2189d91ed52e3994eb50ff081bbed76b60fb6cd6633d346d6a7"),
        new("DPlayerX.dll", 138752, "eb751d527e8c9b893ae0f5dd6ade3e15bbce468c5b0331afc87d0a23ba621ff7"),
        new("MissionLang.dll", 192512, "aed0c8e03e8d76fed8f9489ce9c1567c21524595e36c4333fce4f09ffdcdfae0"),
        new("MW4.exe", 344863, "b129d9689e6578a3384e68b180eb2aa59a435988d13f5b89e5fe6f8b87b3ec0b"),
        new("MW4.ICD", 3498100, "2e4ca6c9bef73e26a30875ef61d6803930427d7c1856e796dc01439a9a7f5eb3"),
        new("RESOURCE/CORE.MW4", 981197, "411883d06236ff3fc1f0acba9ae52d411bb84949af9f09273a8a2c2115497abd"),
        new("RESOURCE/MISSIONS/att.txt", 614, "7d11b097f0254030e7998dcd74762ef328d149a6c725246d91734fe3cb0b5405"),
        new("RESOURCE/MISSIONS/KOTH.TXT", 696, "92f523941a44148e18ac9c53aff196595f9873755ce5d4f7f1f3960dab5a8e2a"),
        new("RESOURCE/props.mw4", 121909696, "595c5e66e0e6be9af87930d80e5ab68f69ead8eeac7ad5d4f06ffecb2a13f499"),
        new("RESOURCE/textures.mw4", 134035825, "55e320ec49b6a1f62a928c115fcb8282c6b7bb384d3a19bfd1165f9d52d2e056"),
        new("ScriptStrings.dll", 135168, "d086d7924ec81a3b06047827acb923633566086efc4cc9ecbd60ab94bddd9b0e"),
    ];

    private static readonly QualifiedPath[] PatchedInventory =
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
        "AutoConfig.exe",
        "CONTENT/GameTypes.h",
        "MissionLang.dll",
        "NFOEditor.exe",
        "RESOURCE/core.mw4",
        "RESOURCE/MISSIONS/att.txt",
        "RESOURCE/MISSIONS/koth.txt",
        "RESOURCE/props.mw4",
        "RESOURCE/textures.mw4",
        "ScriptStrings.dll",
    ];

    private readonly MediaInspectionService inspection;
    private readonly VengeancePatch3ExecutableTransform executableTransform;

    public OfficialVengeancePatch3Transform()
        : this(new MediaInspectionService(), new VengeancePatch3ExecutableTransform())
    {
    }

    public OfficialVengeancePatch3Transform(
        MediaInspectionService inspection,
        VengeancePatch3ExecutableTransform executableTransform)
    {
        this.inspection = inspection ?? throw new ArgumentNullException(nameof(inspection));
        this.executableTransform = executableTransform ?? throw new ArgumentNullException(nameof(executableTransform));
    }

    public PreparedVengeancePatch3Payload Transform(
        string retailInputRoot,
        string mechPakMediaRoot,
        string patchHostPath,
        string scratchDirectory,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var sourceRoot = Path.GetFullPath(retailInputRoot);
        var mediaRoot = Path.GetFullPath(mechPakMediaRoot);
        var host = RequireRegularFile(Path.GetFullPath(patchHostPath), "Patch 3 host");
        RequireMechPakMedia(mediaRoot);

        var scratch = Path.GetFullPath(scratchDirectory);
        if (Directory.Exists(scratch) || File.Exists(scratch))
        {
            throw new IOException("Patch 3 scratch path already exists.");
        }

        var workRoot = Path.Combine(scratch, "work");
        var outputRoot = Path.Combine(scratch, "payload");
        Directory.CreateDirectory(workRoot);
        try
        {
            foreach (var input in RetailInputs)
            {
                var source = RequireQualifiedFile(sourceRoot, input, "retail Patch 3 input");
                CopyContainedFile(source, workRoot, input.RelativePath, cancellationToken);
            }

            var engine = RequireRegularFile(ContainedPath(mediaRoot, EngineRelativePath), "official Patch 3 engine");
            var patch = RequireRegularFile(ContainedPath(mediaRoot, PatchRelativePath), "official Patch 3 payload");
            RunHost(host, engine, workRoot, patch, cancellationToken);
            VerifyExactInventory(workRoot, PatchedInventory, "official Patch 3 output");

            Directory.CreateDirectory(outputRoot);
            foreach (var relativePath in RetainedPatchedFiles)
            {
                CopyContainedFile(ContainedPath(workRoot, relativePath), outputRoot, relativePath, cancellationToken);
            }

            _ = executableTransform.Transform(workRoot, outputRoot, cancellationToken);
            VerifyRetainedPayload(outputRoot);
            return new PreparedVengeancePatch3Payload(outputRoot, TransformId);
        }
        catch
        {
            RemoveTree(scratch);
            throw;
        }
    }

    public bool IsQualifiedPatchMedia(string mediaRoot)
    {
        try
        {
            var root = Path.GetFullPath(mediaRoot);
            RequireMechPakMedia(root);
            _ = RequireQualifiedFile(root, new QualifiedPath(
                EngineRelativePath,
                185344,
                "0ec6e25234ad74489eb1890d4de57bb6140bb8196bdc4a5dcac90dd9d16eb2dd"), "official Patch 3 engine");
            _ = RequireQualifiedFile(root, new QualifiedPath(
                PatchRelativePath,
                40042724,
                "caf43123e64be0a03083d4ae728b1314808569e8a7319fb35d140e20a51831c2"), "official Patch 3 payload");
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (InvalidDataException)
        {
            return false;
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

    private void RequireMechPakMedia(string mediaRoot)
    {
        var recognition = inspection.InspectDirectory(mediaRoot);
        if (recognition.Layout?.Product is not (MediaProduct.InnerSphereMechPak or MediaProduct.ClanMechPak) ||
            recognition.Status is not (MediaRecognitionStatus.Recognized or MediaRecognitionStatus.RecognizedWithExcludedContent))
        {
            throw new InvalidDataException("Official Patch 3 requires recognized Inner Sphere or Clan Mech Pak media.");
        }
    }

    private static void RunHost(string host, string engine, string workRoot, string patch, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = host,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = workRoot,
        };
        startInfo.ArgumentList.Add(engine);
        startInfo.ArgumentList.Add(workRoot);
        startInfo.ArgumentList.Add(patch);

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start the Patch 3 host.");
        var stdout = process.StandardOutput.BaseStream.CopyToAsync(Stream.Null, cancellationToken);
        var stderr = process.StandardError.ReadToEndAsync(cancellationToken);
        try
        {
            process.WaitForExitAsync(cancellationToken).GetAwaiter().GetResult();
            stdout.GetAwaiter().GetResult();
            var errorText = stderr.GetAwaiter().GetResult();
            if (process.ExitCode != 0)
            {
                var detail = errorText.Length > 1024 ? errorText[..1024] : errorText;
                throw new InvalidDataException($"Official Patch 3 failed with exit code {process.ExitCode}: {detail}");
            }
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            throw;
        }
    }

    private static void VerifyExactInventory(string root, IReadOnlyCollection<QualifiedPath> expected, string description)
    {
        var expectedByPath = expected.ToDictionary(item => item.RelativePath, StringComparer.OrdinalIgnoreCase);
        var actual = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/'))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var unexpected = actual.Where(path => !expectedByPath.ContainsKey(path)).ToArray();
        var missing = expectedByPath.Keys.Where(path => !actual.Contains(path, StringComparer.OrdinalIgnoreCase)).ToArray();
        if (unexpected.Length > 0 || missing.Length > 0)
        {
            throw new InvalidDataException($"{description} inventory mismatch; unexpected=[{string.Join(", ", unexpected)}], missing=[{string.Join(", ", missing)}]");
        }

        foreach (var item in expected)
        {
            _ = RequireQualifiedFile(root, item, description);
        }
    }

    private static void VerifyRetainedPayload(string outputRoot)
    {
        var retained = PatchedInventory.Where(item => RetainedPatchedFiles.Contains(item.RelativePath, StringComparer.OrdinalIgnoreCase)).ToList();
        retained.Add(new QualifiedPath("MW4.exe", 3698688, VengeancePatch3ExecutableTransform.OutputSha256));
        VerifyExactInventory(outputRoot, retained, "retained Patch 3 payload");
    }

    private static string RequireQualifiedFile(string root, QualifiedPath expected, string description)
    {
        var path = RequireRegularFile(ContainedPath(root, expected.RelativePath), description);
        var info = new FileInfo(path);
        if (info.Length != expected.Length || !ComputeSha256(path).Equals(expected.Sha256, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Unsupported {description} {expected.RelativePath}.");
        }
        return path;
    }

    private static string RequireRegularFile(string path, string description)
    {
        if (!File.Exists(path)) throw new FileNotFoundException($"Missing {description}.", path);
        var attributes = File.GetAttributes(path);
        if ((attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0)
        {
            throw new InvalidDataException($"The {description} must be a regular file.");
        }
        return path;
    }

    private static string ContainedPath(string root, string relativePath)
    {
        var fullRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        var candidate = Path.GetFullPath(Path.Combine(fullRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var relative = Path.GetRelativePath(fullRoot, candidate);
        if (Path.IsPathRooted(relative) || relative.Equals("..", StringComparison.Ordinal) ||
            relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new InvalidDataException("Patch 3 path escaped its owned root.");
        }
        return candidate;
    }

    private static void CopyContainedFile(string sourcePath, string destinationRoot, string relativePath, CancellationToken cancellationToken)
    {
        var destination = ContainedPath(destinationRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        using var source = File.OpenRead(sourcePath);
        using var target = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        var buffer = new byte[1024 * 1024];
        int read;
        while ((read = source.Read(buffer)) > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            target.Write(buffer, 0, read);
        }
    }

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static void RemoveTree(string path)
    {
        if (!Directory.Exists(path)) return;
        foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)) File.SetAttributes(file, FileAttributes.Normal);
        Directory.Delete(path, recursive: true);
    }

    private sealed record QualifiedPath(string RelativePath, long Length, string Sha256);
}
