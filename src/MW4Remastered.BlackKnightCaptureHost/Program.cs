using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace MW4Remastered.BlackKnightCaptureHost;

internal static class Program
{
    private const uint CreateSuspended = 0x00000004;
    private const uint CreateNoWindow = 0x08000000;
    private const uint MemCommit = 0x00001000;
    private const uint MemReserve = 0x00002000;
    private const uint MemRelease = 0x00008000;
    private const uint PageReadWrite = 0x04;
    private const uint WaitObject0 = 0;
    private const uint WaitTimeout = 258;
    private const uint CaptureTimeoutMilliseconds = 60_000;
    private const long ExpectedMappedImageLength = 0x4A8000;
    private const string OutputEnvironmentVariable = "MW4_PR1_IMAGE_OUTPUT";

    private static int Main(string[] args)
    {
        try
        {
            if (args.Length != 0) throw new ArgumentException("The Black Knight capture host accepts no arguments.");
            Capture();
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static void Capture()
    {
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(AppContext.BaseDirectory));
        var executable = RequireAdjacentRegularFile(root, "MW4x.exe");
        var library = RequireAdjacentRegularFile(root, "version.dll");
        _ = RequireAdjacentRegularFile(root, "version.json");
        var output = Path.Combine(root, "MW4x.mapped.bin");
        if (File.Exists(output) || Directory.Exists(output)) throw new IOException("Black Knight mapped-image output already exists.");

        var previousOutput = Environment.GetEnvironmentVariable(OutputEnvironmentVariable);
        Environment.SetEnvironmentVariable(OutputEnvironmentVariable, output);
        var startup = new StartupInfo { Size = Marshal.SizeOf<StartupInfo>() };
        var commandLine = new StringBuilder(Quote(executable));
        if (!CreateProcessW(executable, commandLine, IntPtr.Zero, IntPtr.Zero, false,
                CreateSuspended | CreateNoWindow, IntPtr.Zero, root, ref startup, out var process))
        {
            Environment.SetEnvironmentVariable(OutputEnvironmentVariable, previousOutput);
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not create the protected Black Knight process.");
        }

        var completed = false;
        try
        {
            InjectAdjacentLibrary(process.Process, library);
            if (ResumeThread(process.Thread) == uint.MaxValue)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not resume the protected Black Knight process.");

            var wait = WaitForSingleObject(process.Process, CaptureTimeoutMilliseconds);
            if (wait == WaitTimeout) throw new TimeoutException("Black Knight PR1 image capture timed out.");
            if (wait != WaitObject0) throw new Win32Exception(Marshal.GetLastWin32Error(), "Waiting for Black Knight PR1 image capture failed.");
            if (!GetExitCodeProcess(process.Process, out var exitCode) || exitCode != 0)
                throw new InvalidDataException($"Black Knight PR1 image capture failed with exit code {exitCode}.");

            var outputInfo = new FileInfo(output);
            if (!outputInfo.Exists || outputInfo.Length != ExpectedMappedImageLength ||
                (outputInfo.Attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0)
                throw new InvalidDataException("Black Knight PR1 image capture produced an invalid output.");
            completed = true;
        }
        finally
        {
            Environment.SetEnvironmentVariable(OutputEnvironmentVariable, previousOutput);
            if (!completed) TerminateProcess(process.Process, 1);
            CloseHandle(process.Thread);
            CloseHandle(process.Process);
            if (!completed && File.Exists(output)) File.Delete(output);
        }
    }

    private static void InjectAdjacentLibrary(IntPtr process, string libraryPath)
    {
        var encodedPath = Encoding.Unicode.GetBytes(libraryPath + '\0');
        var remoteBuffer = VirtualAllocEx(process, IntPtr.Zero, (nuint)encodedPath.Length,
            MemCommit | MemReserve, PageReadWrite);
        if (remoteBuffer == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not allocate image-capture loader memory.");
        try
        {
            if (!WriteProcessMemory(process, remoteBuffer, encodedPath, (nuint)encodedPath.Length, out var written) ||
                written != (nuint)encodedPath.Length)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not write the image-capture loader path.");
            var kernel32 = GetModuleHandleW("kernel32.dll");
            var loadLibrary = kernel32 == IntPtr.Zero ? IntPtr.Zero : GetProcAddress(kernel32, "LoadLibraryW");
            if (loadLibrary == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not resolve LoadLibraryW.");
            var remoteThread = CreateRemoteThread(process, IntPtr.Zero, 0, loadLibrary, remoteBuffer, 0, out _);
            if (remoteThread == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not start the image-capture loader.");
            var remoteCompleted = false;
            try
            {
                var wait = WaitForSingleObject(remoteThread, CaptureTimeoutMilliseconds);
                remoteCompleted = wait == WaitObject0;
                if (!remoteCompleted || !GetExitCodeThread(remoteThread, out var module) || module == 0)
                    throw new InvalidOperationException("The image-capture DLL did not load into Black Knight.");
            }
            finally { CloseHandle(remoteThread); }
            if (remoteCompleted) VirtualFreeEx(process, remoteBuffer, 0, MemRelease);
        }
        catch
        {
            // If the remote thread did not finish, its path argument must stay valid
            // until the outer boundary terminates the exact child process.
            throw;
        }
    }

    private static string RequireAdjacentRegularFile(string root, string name)
    {
        var path = Path.GetFullPath(Path.Combine(root, name));
        if (!StringComparer.OrdinalIgnoreCase.Equals(Path.GetDirectoryName(path), root) || !File.Exists(path))
            throw new FileNotFoundException($"Missing adjacent Black Knight capture input {name}.", path);
        if ((File.GetAttributes(path) & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0)
            throw new InvalidDataException($"Black Knight capture input {name} must be a regular file.");
        return path;
    }

    private static string Quote(string value) => "\"" + value.Replace("\"", "\\\"") + "\"";

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct StartupInfo
    {
        public int Size; public string? Reserved; public string? Desktop; public string? Title;
        public int X; public int Y; public int XSize; public int YSize; public int XCountChars; public int YCountChars;
        public int FillAttribute; public int Flags; public short ShowWindow; public short Reserved2;
        public IntPtr Reserved2Pointer; public IntPtr StandardInput; public IntPtr StandardOutput; public IntPtr StandardError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessInformation { public IntPtr Process; public IntPtr Thread; public uint ProcessId; public uint ThreadId; }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CreateProcessW(string applicationName, StringBuilder commandLine, IntPtr processAttributes,
        IntPtr threadAttributes, [MarshalAs(UnmanagedType.Bool)] bool inheritHandles, uint creationFlags,
        IntPtr environment, string currentDirectory, ref StartupInfo startupInfo, out ProcessInformation processInformation);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr VirtualAllocEx(IntPtr process, IntPtr address, nuint size, uint allocationType, uint protection);
    [DllImport("kernel32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool VirtualFreeEx(IntPtr process, IntPtr address, nuint size, uint freeType);
    [DllImport("kernel32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool WriteProcessMemory(IntPtr process, IntPtr address, byte[] buffer, nuint size, out nuint written);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr CreateRemoteThread(IntPtr process, IntPtr threadAttributes, nuint stackSize, IntPtr startAddress, IntPtr parameter, uint creationFlags, out uint threadId);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern IntPtr GetModuleHandleW(string moduleName);
    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)] private static extern IntPtr GetProcAddress(IntPtr module, string procedureName);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);
    [DllImport("kernel32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetExitCodeThread(IntPtr thread, out uint exitCode);
    [DllImport("kernel32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetExitCodeProcess(IntPtr process, out uint exitCode);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern uint ResumeThread(IntPtr thread);
    [DllImport("kernel32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool TerminateProcess(IntPtr process, uint exitCode);
    [DllImport("kernel32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool CloseHandle(IntPtr handle);
}
