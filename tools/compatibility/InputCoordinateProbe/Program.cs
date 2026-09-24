using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

internal static class Program
{
    private const uint MonitorDefaultToNearest = 2;
    private static readonly (string Process, string Game)[] Games =
    [
        ("MW4", "Vengeance"),
        ("MW4X", "Black Knight"),
        ("MW4Mercs", "Mercenaries"),
    ];

    private static int Main(string[] args)
    {
        if (!OperatingSystem.IsWindows()) return 2;
        var seconds = args.Length == 0 ? 15 : int.Parse(args[0]);
        if (seconds is < 1 or > 60) throw new ArgumentOutOfRangeException(nameof(args), "Capture duration must be 1–60 seconds.");

        var matches = Games.SelectMany(game => Process.GetProcessesByName(game.Process)
            .Where(process => !process.HasExited)
            .Select(process => (Process: process, game.Game))).ToArray();
        try
        {
            if (matches.Length != 1)
            {
                Console.Error.WriteLine($"Expected exactly one running MW4 game process; found {matches.Length}.");
                return 2;
            }

            var (process, gameName) = matches[0];
            var window = FindWindow(process.Id);
            if (window == IntPtr.Zero)
            {
                Console.Error.WriteLine("The running game has no visible presentation window.");
                return 2;
            }

            var monitorInfo = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
            var monitor = MonitorFromWindow(window, MonitorDefaultToNearest);
            if (monitor == IntPtr.Zero || !GetMonitorInfo(monitor, ref monitorInfo))
                throw new InvalidOperationException("Could not resolve the game's display monitor.");

            var snapshots = new List<Snapshot>(seconds * 10);
            var timer = Stopwatch.StartNew();
            while (timer.Elapsed < TimeSpan.FromSeconds(seconds) && IsWindow(window) && !process.HasExited)
            {
                var outer = new NativeRect();
                var client = new NativeRect();
                var origin = new NativePoint();
                var clip = new NativeRect();
                var cursor = new NativePoint();
                if (!GetWindowRect(window, ref outer) || !GetClientRect(window, ref client) ||
                    !ClientToScreen(window, ref origin) || !GetClipCursor(ref clip) || !GetCursorPos(ref cursor))
                    throw new InvalidOperationException("Could not read a complete game-window input snapshot.");

                var clientOnScreen = new NativeRect(origin.X, origin.Y,
                    origin.X + client.Right - client.Left, origin.Y + client.Bottom - client.Top);
                snapshots.Add(new Snapshot(timer.ElapsedMilliseconds,
                    GetAncestor(GetForegroundWindow(), 2) == window,
                    outer, clientOnScreen, clip, cursor,
                    GetDpiForWindow(window),
                    GetAwarenessFromDpiAwarenessContext(GetWindowDpiAwarenessContext(window))));
                Thread.Sleep(100);
            }

            var report = new Report(gameName, process.Id, monitorInfo.Monitor, snapshots);
            var output = Path.Combine(Path.GetTempPath(),
                $"MW4-InputCoordinateProbe-{process.Id}-{DateTimeOffset.UtcNow:yyyyMMddTHHmmssfffZ}.json");
            File.WriteAllText(output, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"Captured {snapshots.Count} read-only input snapshots for {gameName}. Report: {output}");
            return snapshots.Count > 0 ? 0 : 1;
        }
        finally
        {
            foreach (var match in matches) match.Process.Dispose();
        }
    }

    private static IntPtr FindWindow(int processId)
    {
        var result = IntPtr.Zero;
        long bestArea = 0;
        _ = EnumWindows((window, parameter) =>
        {
            _ = GetWindowThreadProcessId(window, out var owner);
            if (owner != processId || !IsWindowVisible(window)) return true;
            var rect = new NativeRect();
            if (!GetWindowRect(window, ref rect)) return true;
            var width = Math.Max(0, rect.Right - rect.Left);
            var height = Math.Max(0, rect.Bottom - rect.Top);
            var area = (long)width * height;
            if (width >= 640 && height >= 480 && area > bestArea)
            {
                result = window;
                bestArea = area;
            }
            return true;
        }, IntPtr.Zero);
        return result;
    }

    private sealed record Report(string Game, int ProcessId, NativeRect MonitorRect, IReadOnlyList<Snapshot> Snapshots);
    private sealed record Snapshot(long ElapsedMilliseconds, bool IsForeground, NativeRect WindowRect,
        NativeRect ClientScreenRect, NativeRect CursorClip, NativePoint CursorPosition, uint WindowDpi, int DpiAwareness);

    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct NativePoint(int X, int Y)
    {
        public NativePoint() : this(0, 0) { }
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct NativeRect(int Left, int Top, int Right, int Bottom)
    {
        public NativeRect() : this(0, 0, 0, 0) { }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MonitorInfo
    {
        public int Size;
        public NativeRect Monitor;
        public NativeRect WorkArea;
        public uint Flags;
    }

    private delegate bool EnumWindowsCallback(IntPtr window, IntPtr parameter);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsCallback callback, IntPtr parameter);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out int processId);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr window);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr window, ref NativeRect rect);
    [DllImport("user32.dll")] private static extern bool GetClientRect(IntPtr window, ref NativeRect rect);
    [DllImport("user32.dll")] private static extern bool ClientToScreen(IntPtr window, ref NativePoint point);
    [DllImport("user32.dll")] private static extern bool GetClipCursor(ref NativeRect rect);
    [DllImport("user32.dll")] private static extern bool GetCursorPos(ref NativePoint point);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern IntPtr GetAncestor(IntPtr window, uint flags);
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr window);
    [DllImport("user32.dll")] private static extern IntPtr GetWindowDpiAwarenessContext(IntPtr window);
    [DllImport("user32.dll")] private static extern int GetAwarenessFromDpiAwarenessContext(IntPtr context);
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
}
