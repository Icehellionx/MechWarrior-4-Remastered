using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace MW4Remastered.RtpPatchHost;

internal static class Program
{
    private const string EngineSha256 = "0ec6e25234ad74489eb1890d4de57bb6140bb8196bdc4a5dcac90dd9d16eb2dd";
    private static readonly IReadOnlySet<string> QualifiedPatchSha256 = new HashSet<string>(StringComparer.Ordinal)
    {
        "caf43123e64be0a03083d4ae728b1314808569e8a7319fb35d140e20a51831c2", // Vengeance Patch 3
        "5f3b6a383b7c1821e9f682ea3d6675818c23bf0138289e89a951256ecf2ab6ff", // Black Knight PR1
        "d3ebf1c2dc098a8e55d7cbeb112426fb413dfd23595a1ed078cd35fe7a551a65", // Mercenaries PR1
    };

    private static readonly byte[] ContinueBytes = "continue\0"u8.ToArray();
    private static readonly GCHandle ContinueHandle = GCHandle.Alloc(ContinueBytes, GCHandleType.Pinned);

    private static int Main(string[] args)
    {
        try
        {
            if (args.Length != 3)
            {
                throw new ArgumentException("Usage: MW4RemasteredRtpPatchHost <PATCHW32.DLL> <scratch-tree> <qualified-update.RTP>");
            }

            var enginePath = RequireRegularFile(args[0], EngineSha256, "official RTPatch engine");
            var scratchTree = RequireScratchTree(args[1]);
            var patchPath = RequireQualifiedPatch(args[2]);

            var library = NativeLibrary.Load(enginePath);
            try
            {
                var export = NativeLibrary.GetExport(library, "RTPatchApply32@12");
                var apply = Marshal.GetDelegateForFunctionPointer<ApplyDelegate>(export);
                CallbackDelegate callback = ReportProgress;
                var result = apply($"\"{scratchTree}\" \"{patchPath}\"", callback, true);
                GC.KeepAlive(callback);
                if (result != 0)
                {
                    throw new InvalidOperationException($"The official RTPatch engine returned error {result}.");
                }
            }
            finally
            {
                NativeLibrary.Free(library);
            }

            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static nint ReportProgress(uint messageId, nint value)
    {
        return ContinueHandle.AddrOfPinnedObject();
    }

    private static string RequireRegularFile(string candidate, string expectedSha256, string description)
    {
        var path = Path.GetFullPath(candidate);
        var attributes = File.GetAttributes(path);
        if ((attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0)
        {
            throw new InvalidDataException($"The {description} is not a regular file.");
        }

        using var stream = File.OpenRead(path);
        var actual = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
        if (!actual.Equals(expectedSha256, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"The {description} SHA-256 is not the qualified revision: {actual}");
        }

        return path;
    }

    private static string RequireQualifiedPatch(string candidate)
    {
        var path = Path.GetFullPath(candidate);
        var attributes = File.GetAttributes(path);
        if ((attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0)
        {
            throw new InvalidDataException("The official update payload is not a regular file.");
        }

        using var stream = File.OpenRead(path);
        var actual = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
        if (!QualifiedPatchSha256.Contains(actual))
        {
            throw new InvalidDataException($"The official update payload SHA-256 is not a qualified revision: {actual}");
        }
        return path;
    }

    private static string RequireScratchTree(string candidate)
    {
        var path = Path.TrimEndingDirectorySeparator(Path.GetFullPath(candidate));
        var root = Path.TrimEndingDirectorySeparator(Path.GetPathRoot(path)!);
        if (path.Equals(root, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("The patch target cannot be a filesystem root.");
        }

        var attributes = File.GetAttributes(path);
        if ((attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != FileAttributes.Directory)
        {
            throw new InvalidDataException("The patch target must be a regular directory.");
        }

        for (var current = new DirectoryInfo(path); current is not null; current = current.Parent)
        {
            if ((current.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new InvalidDataException("The patch target cannot traverse a reparse point.");
            }
        }

        return path;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    private delegate ushort ApplyDelegate(
        [MarshalAs(UnmanagedType.LPStr)] string commandLine,
        CallbackDelegate callback,
        [MarshalAs(UnmanagedType.Bool)] bool wait);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate nint CallbackDelegate(uint messageId, nint value);
}
