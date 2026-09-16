using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace MW4Remastered.CompatLauncher;

internal static class Program
{
    private const uint CreateSuspended = 0x00000004;
    private const uint MemCommit = 0x00001000;
    private const uint MemReserve = 0x00002000;
    private const uint MemRelease = 0x00008000;
    private const uint PageReadWrite = 0x04;
    private const uint Infinite = 0xFFFFFFFF;

    private static readonly HashSet<string> AllowedExecutables = new(StringComparer.OrdinalIgnoreCase)
    {
        "MW4.exe",
        "MW4X.exe",
        "MW4Mercs.exe",
    };

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            return Launch(args);
        }
        catch (Exception exception)
        {
            try
            {
                var logDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MechWarrior 4 Remastered",
                    "Logs");
                Directory.CreateDirectory(logDirectory);
                var logPath = Path.Combine(logDirectory, "compat-launch-error.log");
                File.WriteAllText(logPath, $"{DateTimeOffset.UtcNow:O}{Environment.NewLine}{exception}");
            }
            catch
            {
                // Failure reporting must not obscure the original launch failure.
            }

            return 1;
        }
    }

    private static int Launch(string[] args)
    {
        if (args.Length == 0 || !AllowedExecutables.Contains(args[0]) || Path.GetFileName(args[0]) != args[0])
        {
            throw new ArgumentException("The first argument must name a supported adjacent MW4 executable.");
        }

        var baseDirectory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(AppContext.BaseDirectory));
        var executablePath = Path.Combine(baseDirectory, args[0]);
        var libraryPath = Path.Combine(baseDirectory, "version.dll");
        RequireAdjacentRegularFile(executablePath, baseDirectory);
        RequireAdjacentRegularFile(libraryPath, baseDirectory);

        var commandLine = new StringBuilder(Quote(executablePath));
        foreach (var argument in args.Skip(1))
        {
            commandLine.Append(' ').Append(Quote(argument));
        }

        var startupInfo = new StartupInfo { Size = Marshal.SizeOf<StartupInfo>() };
        if (!CreateProcessW(
                executablePath,
                commandLine,
                IntPtr.Zero,
                IntPtr.Zero,
                false,
                CreateSuspended,
                IntPtr.Zero,
                baseDirectory,
                ref startupInfo,
                out var processInformation))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not create the protected game process.");
        }

        var resumed = false;
        try
        {
            InjectAdjacentLibrary(processInformation.Process, libraryPath);
            if (ResumeThread(processInformation.Thread) == uint.MaxValue)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not resume the protected game process.");
            }

            resumed = true;
            return 0;
        }
        finally
        {
            if (!resumed)
            {
                TerminateProcess(processInformation.Process, 1);
            }

            CloseHandle(processInformation.Thread);
            CloseHandle(processInformation.Process);
        }
    }

    private static void InjectAdjacentLibrary(IntPtr process, string libraryPath)
    {
        var encodedPath = Encoding.Unicode.GetBytes(libraryPath + '\0');
        var remoteBuffer = VirtualAllocEx(
            process,
            IntPtr.Zero,
            (nuint)encodedPath.Length,
            MemCommit | MemReserve,
            PageReadWrite);
        if (remoteBuffer == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not allocate compatibility-loader memory.");
        }

        try
        {
            if (!WriteProcessMemory(process, remoteBuffer, encodedPath, (nuint)encodedPath.Length, out var written) ||
                written != (nuint)encodedPath.Length)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not write the compatibility-loader path.");
            }

            var kernel32 = GetModuleHandleW("kernel32.dll");
            var loadLibrary = kernel32 == IntPtr.Zero ? IntPtr.Zero : GetProcAddress(kernel32, "LoadLibraryW");
            if (loadLibrary == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not resolve LoadLibraryW.");
            }

            var remoteThread = CreateRemoteThread(
                process,
                IntPtr.Zero,
                0,
                loadLibrary,
                remoteBuffer,
                0,
                out _);
            if (remoteThread == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not start the compatibility loader.");
            }

            try
            {
                if (WaitForSingleObject(remoteThread, Infinite) == uint.MaxValue)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Waiting for the compatibility loader failed.");
                }

                if (!GetExitCodeThread(remoteThread, out var moduleHandle) || moduleHandle == 0)
                {
                    throw new InvalidOperationException("The compatibility DLL did not load into the game process.");
                }
            }
            finally
            {
                CloseHandle(remoteThread);
            }
        }
        finally
        {
            VirtualFreeEx(process, remoteBuffer, 0, MemRelease);
        }
    }

    private static void RequireAdjacentRegularFile(string path, string baseDirectory)
    {
        var fullPath = Path.GetFullPath(path);
        if (!StringComparer.OrdinalIgnoreCase.Equals(Path.GetDirectoryName(fullPath), baseDirectory))
        {
            throw new InvalidOperationException("Compatibility inputs must be adjacent to the launcher.");
        }

        var attributes = File.GetAttributes(fullPath);
        if ((attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0)
        {
            throw new InvalidOperationException($"Compatibility input is not a regular file: {fullPath}");
        }
    }

    private static string Quote(string value)
    {
        if (!value.Any(character => char.IsWhiteSpace(character) || character == '"'))
        {
            return value;
        }

        var result = new StringBuilder("\"");
        var slashCount = 0;
        foreach (var character in value)
        {
            if (character == '\\')
            {
                slashCount++;
                continue;
            }

            if (character == '"')
            {
                result.Append('\\', slashCount * 2 + 1).Append('"');
                slashCount = 0;
                continue;
            }

            result.Append('\\', slashCount).Append(character);
            slashCount = 0;
        }

        return result.Append('\\', slashCount * 2).Append('"').ToString();
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct StartupInfo
    {
        public int Size;
        public string? Reserved;
        public string? Desktop;
        public string? Title;
        public int X;
        public int Y;
        public int XSize;
        public int YSize;
        public int XCountChars;
        public int YCountChars;
        public int FillAttribute;
        public int Flags;
        public short ShowWindow;
        public short Reserved2;
        public IntPtr Reserved2Pointer;
        public IntPtr StandardInput;
        public IntPtr StandardOutput;
        public IntPtr StandardError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessInformation
    {
        public IntPtr Process;
        public IntPtr Thread;
        public uint ProcessId;
        public uint ThreadId;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CreateProcessW(
        string applicationName,
        StringBuilder commandLine,
        IntPtr processAttributes,
        IntPtr threadAttributes,
        [MarshalAs(UnmanagedType.Bool)] bool inheritHandles,
        uint creationFlags,
        IntPtr environment,
        string currentDirectory,
        ref StartupInfo startupInfo,
        out ProcessInformation processInformation);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr VirtualAllocEx(IntPtr process, IntPtr address, nuint size, uint allocationType, uint protection);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool VirtualFreeEx(IntPtr process, IntPtr address, nuint size, uint freeType);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool WriteProcessMemory(IntPtr process, IntPtr address, byte[] buffer, nuint size, out nuint written);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr CreateRemoteThread(
        IntPtr process,
        IntPtr threadAttributes,
        nuint stackSize,
        IntPtr startAddress,
        IntPtr parameter,
        uint creationFlags,
        out uint threadId);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr GetModuleHandleW(string moduleName);

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
    private static extern IntPtr GetProcAddress(IntPtr module, string procedureName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetExitCodeThread(IntPtr thread, out uint exitCode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint ResumeThread(IntPtr thread);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool TerminateProcess(IntPtr process, uint exitCode);

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr handle);
}
