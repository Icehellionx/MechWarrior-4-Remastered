using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace MW4Remastered.Core.Install;

public sealed class BlackKnightPr1ImageCapture
{
    public const long CaptureDllLength = 91_648;
    public const string CaptureDllSha256 = "7fbf1fbd0b251986f0dcd2082f218650eddff40d661ede0deefd4b2ce6b5c6fc";
    public const int MappedImageLength = BlackKnightPr1ExecutableTransform.MappedImageLength;

    private const string OutputEnvironmentVariable = "MW4_PR1_IMAGE_OUTPUT";
    private const string Configuration = "{\"HookOEP\":\"true\"}";

    public string Capture(
        string protectedExecutablePath,
        string captureDllPath,
        string scratchDirectory,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var executable = RequireRegularFile(protectedExecutablePath, "Black Knight PR1 executable");
        var captureDll = RequireQualifiedCaptureDll(captureDllPath);
        var gameRoot = Path.TrimEndingDirectorySeparator(Path.GetDirectoryName(executable)!);
        var scratch = Path.GetFullPath(scratchDirectory);
        if (Directory.Exists(scratch) || File.Exists(scratch))
            throw new IOException("Black Knight image-capture scratch path already exists.");
        Directory.CreateDirectory(scratch);

        var adjacentDll = Path.Combine(gameRoot, "version.dll");
        var adjacentConfiguration = Path.Combine(gameRoot, "version.json");
        var mappedImage = Path.Combine(scratch, "MW4x.mapped.bin");
        if (File.Exists(adjacentDll) || Directory.Exists(adjacentDll) ||
            File.Exists(adjacentConfiguration) || Directory.Exists(adjacentConfiguration))
            throw new InvalidDataException("Black Knight PR1 capture requires unoccupied app-local compatibility paths.");

        Process? process = null;
        OwnedProcessJob? processJob = null;
        try
        {
            File.Copy(captureDll, adjacentDll);
            File.WriteAllText(adjacentConfiguration, Configuration);
            processJob = OwnedProcessJob.Create();
            var start = new ProcessStartInfo
            {
                FileName = executable,
                WorkingDirectory = gameRoot,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            start.Environment[OutputEnvironmentVariable] = mappedImage;
            process = Process.Start(start) ?? throw new InvalidOperationException("Could not start Black Knight PR1 image capture.");
            processJob.Assign(process);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(60));
            try
            {
                process.WaitForExitAsync(timeout.Token).GetAwaiter().GetResult();
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                if (!process.HasExited) process.Kill(entireProcessTree: true);
                throw new TimeoutException("Black Knight PR1 image capture timed out.");
            }
            if (process.ExitCode != 0)
                throw new InvalidDataException($"Black Knight PR1 image capture failed with exit code {process.ExitCode}.");

            var output = RequireRegularFile(mappedImage, "Black Knight PR1 mapped image");
            if (new FileInfo(output).Length != MappedImageLength)
                throw new InvalidDataException("Black Knight PR1 image capture produced an unexpected image size.");
            return output;
        }
        catch (OperationCanceledException)
        {
            if (process is { HasExited: false }) process.Kill(entireProcessTree: true);
            throw;
        }
        finally
        {
            // SafeDisc can outlive the protected launcher as a temporary child.
            // Closing the job terminates only the process family setup created.
            processJob?.Dispose();
            process?.Dispose();
            DeleteCaptureArtifact(adjacentConfiguration);
            DeleteCaptureArtifact(adjacentDll);
        }
    }

    private static void DeleteCaptureArtifact(string path)
    {
        // The protected launcher can exit just before its temporary child finishes
        // ExitProcess in the capture hook. Windows keeps the proxy DLL mapped for
        // that brief interval, so immediate deletion races child teardown. Never
        // carry it into the install plan: wait for this exact owned path and fail
        // closed if it cannot be removed.
        var deadline = Stopwatch.StartNew();
        while (File.Exists(path))
        {
            try
            {
                File.SetAttributes(path, FileAttributes.Normal);
                File.Delete(path);
                return;
            }
            catch (Exception error) when (
                error is IOException or UnauthorizedAccessException &&
                deadline.Elapsed < TimeSpan.FromSeconds(15))
            {
                Thread.Sleep(100);
            }
        }
    }

    private static string RequireQualifiedCaptureDll(string path)
    {
        var fullPath = RequireRegularFile(path, "Black Knight PR1 capture DLL");
        using var stream = File.OpenRead(fullPath);
        var hash = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
        if (stream.Length != CaptureDllLength || !hash.Equals(CaptureDllSha256, StringComparison.Ordinal))
            throw new InvalidDataException($"Unsupported Black Knight PR1 capture DLL: length={stream.Length}, sha256={hash}.");
        return fullPath;
    }

    private sealed class OwnedProcessJob : IDisposable
    {
        private const uint JobObjectLimitKillOnJobClose = 0x00002000;
        private const int ExtendedLimitInformationClass = 9;
        private readonly SafeFileHandle handle;

        private OwnedProcessJob(SafeFileHandle handle) => this.handle = handle;

        public static OwnedProcessJob Create()
        {
            var handle = CreateJobObjectW(nint.Zero, null);
            if (handle.IsInvalid)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not create the Black Knight setup-capture process job.");

            var information = new JobObjectExtendedLimitInformation
            {
                BasicLimitInformation = new JobObjectBasicLimitInformation
                {
                    LimitFlags = JobObjectLimitKillOnJobClose,
                },
            };
            var size = Marshal.SizeOf<JobObjectExtendedLimitInformation>();
            var buffer = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(information, buffer, false);
                if (!SetInformationJobObject(handle, ExtendedLimitInformationClass, buffer, (uint)size))
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not configure the Black Knight setup-capture process job.");
            }
            catch
            {
                handle.Dispose();
                throw;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
            return new OwnedProcessJob(handle);
        }

        public void Assign(Process process)
        {
            if (!AssignProcessToJobObject(handle, process.SafeHandle))
            {
                var errorCode = Marshal.GetLastWin32Error();
                try
                {
                    if (!process.HasExited) process.Kill(entireProcessTree: true);
                }
                catch (InvalidOperationException)
                {
                }
                throw new Win32Exception(errorCode, "Could not contain the Black Knight setup-capture process.");
            }
        }

        public void Dispose() => handle.Dispose();

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern SafeFileHandle CreateJobObjectW(nint jobAttributes, string? name);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetInformationJobObject(
            SafeFileHandle job,
            int informationClass,
            nint information,
            uint informationLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AssignProcessToJobObject(SafeFileHandle job, SafeProcessHandle process);

        [StructLayout(LayoutKind.Sequential)]
        private struct JobObjectBasicLimitInformation
        {
            public long PerProcessUserTimeLimit;
            public long PerJobUserTimeLimit;
            public uint LimitFlags;
            public nuint MinimumWorkingSetSize;
            public nuint MaximumWorkingSetSize;
            public uint ActiveProcessLimit;
            public nuint Affinity;
            public uint PriorityClass;
            public uint SchedulingClass;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IoCounters
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JobObjectExtendedLimitInformation
        {
            public JobObjectBasicLimitInformation BasicLimitInformation;
            public IoCounters IoInfo;
            public nuint ProcessMemoryLimit;
            public nuint JobMemoryLimit;
            public nuint PeakProcessMemoryUsed;
            public nuint PeakJobMemoryUsed;
        }
    }

    private static string RequireRegularFile(string path, string description)
    {
        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath)) throw new FileNotFoundException($"Missing {description}.", fullPath);
        if ((File.GetAttributes(fullPath) & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0)
            throw new InvalidDataException($"The {description} must be a regular file.");
        return fullPath;
    }
}
