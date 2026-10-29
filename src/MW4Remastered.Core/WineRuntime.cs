using System.Runtime.InteropServices;

namespace MW4Remastered.Core;

internal static class WineRuntime
{
    public static bool IsWine => OperatingSystem.IsWindows() &&
        GetProcAddress(GetModuleHandle("ntdll.dll"), "wine_get_version") != IntPtr.Zero;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, EntryPoint = "GetModuleHandleW")]
    private static extern IntPtr GetModuleHandle(string moduleName);

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern IntPtr GetProcAddress(IntPtr module, string procedureName);
}
