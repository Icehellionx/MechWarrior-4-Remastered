using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using MW4Remastered.Core.Media;

namespace MW4Remastered.Core.Install;

// SetupIterateCabinet supplies each CAB name before writing; only our callback
// chooses a target. No setup UI, registry mutation, or third-party binary is used.
internal static class WindowsCabinetReader
{
    private const uint FileInCabinet = 0x11, NeedNewCabinet = 0x12, FileExtracted = 0x13;
    private const uint FileOpAbort = 0, FileOpDoIt = 1, FileOpSkip = 2, InvalidData = 13;
    private const long MaximumPayloadBytes = 8L * 1024 * 1024 * 1024;
    private const uint MaximumFileBytes = 512 * 1024 * 1024;

    internal static IReadOnlyDictionary<string, uint> ReadInventory(string cabinet, CancellationToken cancellationToken)
        => Iterate(cabinet, null, null, cancellationToken);

    internal static void Extract(string cabinet, string staging, IReadOnlyDictionary<string, uint> expected, CancellationToken cancellationToken)
        => Iterate(cabinet, staging, expected, cancellationToken);

    private static Dictionary<string, uint> Iterate(string cabinet, string? staging,
        IReadOnlyDictionary<string, uint>? expected, CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Cabinet extraction requires Windows.");
        var entries = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
        ExceptionDispatchInfo? failure = null;
        long totalBytes = 0;
        var timer = Stopwatch.StartNew();
        uint Callback(IntPtr context, uint notification, IntPtr parameter1, IntPtr parameter2)
        {
            if (failure is not null) return notification == FileInCabinet ? FileOpAbort : InvalidData;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                // SetupAPI calls us between files; cancellation/deadline checks
                // are cooperative and cannot interrupt one native decompression.
                if (timer.Elapsed > TimeSpan.FromMinutes(2)) throw new IOException("Cabinet extraction exceeded its cooperative 120-second budget.");
                if (notification == NeedNewCabinet)
                    throw new InvalidDataException("Multi-cabinet payloads are not supported.");
                if (notification == FileExtracted)
                {
                    var error = Marshal.ReadInt32(parameter1, 2 * IntPtr.Size);
                    if (error != 0) throw new InvalidDataException("Cabinet file extraction failed.", new Win32Exception(error));
                }
                if (notification != FileInCabinet) return 0;

                var info = Marshal.PtrToStructure<FileInCabinetInfo>(parameter1);
                var name = Marshal.PtrToStringUni(info.NameInCabinet) ?? throw new InvalidDataException("Cabinet entry has no name.");
                var portable = name.Replace('\\', '/');
                if (!MediaRecognizer.TryNormalizeRelativePath(portable, out var normalized) ||
                    normalized.Length == 0 || !normalized.Equals(portable, StringComparison.Ordinal) ||
                    normalized.Split('/').Any(IsWindowsAlias) ||
                    (info.DosAttributes & ((ushort)FileAttributes.Directory | (ushort)FileAttributes.ReparsePoint)) != 0)
                    throw new InvalidDataException($"Unsafe cabinet entry: {name}");
                if (!entries.TryAdd(normalized, info.FileSize)) throw new InvalidDataException($"Duplicate cabinet destination: {name}");
                totalBytes += info.FileSize;
                if (entries.Count > 8192 || info.FileSize > MaximumFileBytes || totalBytes > MaximumPayloadBytes)
                    throw new InvalidDataException("Cabinet payload exceeds the extraction limit.");
                if (staging is null) return FileOpSkip;
                if (expected is null || !expected.TryGetValue(normalized, out var size) || size != info.FileSize)
                    throw new InvalidDataException("Cabinet inventory changed after validation.");

                var target = Path.GetFullPath(Path.Combine(staging, normalized.Replace('/', Path.DirectorySeparatorChar)));
                if (!target.StartsWith(staging + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Cabinet target escaped staging.");
                if (target.Length >= 260)
                    throw new InvalidDataException("Cabinet target exceeds the Windows cabinet path limit. Choose a shorter extraction path.");
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                info.FullTargetName = target;
                Marshal.StructureToPtr(info, parameter1, false);
                return FileOpDoIt;
            }
            catch (Exception exception)
            {
                // Never unwind managed exceptions across an unmanaged callback.
                failure ??= ExceptionDispatchInfo.Capture(exception);
                return notification == FileInCabinet ? FileOpAbort : InvalidData;
            }
        }

        SetupFileCallback callback = Callback;
        var success = SetupIterateCabinet(cabinet, 0, callback, IntPtr.Zero);
        var lastError = Marshal.GetLastWin32Error();
        GC.KeepAlive(callback);
        failure?.Throw();
        cancellationToken.ThrowIfCancellationRequested();
        if (!success) throw new InvalidDataException("Windows cabinet extraction failed.", new Win32Exception(lastError));
        if (expected is not null && (entries.Count != expected.Count || entries.Keys.Any(path => !expected.ContainsKey(path))))
            throw new InvalidDataException("Cabinet inventory changed after validation.");
        if (staging is not null)
            foreach (var entry in entries)
                if (new FileInfo(Path.Combine(staging, entry.Key)).Length != entry.Value)
                    throw new InvalidDataException($"Cabinet output size mismatch: {entry.Key}");
        return entries;
    }

    internal static bool IsWindowsAlias(string segment)
    {
        if (segment.EndsWith(' ') || segment.EndsWith('.')) return true;
        var stem = segment.Split('.')[0].ToUpperInvariant();
        return stem is "CON" or "PRN" or "AUX" or "NUL" ||
            (stem.Length == 4 && (stem.StartsWith("COM", StringComparison.Ordinal) || stem.StartsWith("LPT", StringComparison.Ordinal)) &&
                (stem[3] is >= '1' and <= '9' or '¹' or '²' or '³'));
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate uint SetupFileCallback(IntPtr context, uint notification, IntPtr parameter1, IntPtr parameter2);

    [DllImport("setupapi.dll", EntryPoint = "SetupIterateCabinetW", CharSet = CharSet.Unicode, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetupIterateCabinet(string cabinet, uint reserved, SetupFileCallback callback, IntPtr context);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct FileInCabinetInfo
    {
        public IntPtr NameInCabinet;
        public uint FileSize, Win32Error;
        public ushort DosDate, DosTime, DosAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string FullTargetName;
    }
}
