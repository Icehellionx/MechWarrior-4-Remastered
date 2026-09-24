using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MW4Remastered.Core.Launch;

public interface IGameWindowLifecycleGuard : IDisposable
{
    event EventHandler? ActiveSessionsChanged;
    bool HasActiveSessions { get; }
    void Protect(int processId);
}

public sealed class NoOpGameWindowLifecycleGuard : IGameWindowLifecycleGuard
{
    public event EventHandler? ActiveSessionsChanged
    {
        add { }
        remove { }
    }

    public bool HasActiveSessions => false;
    public void Protect(int processId) { }
    public void Dispose() { }
}

public sealed class SystemGameWindowLifecycleGuard : IGameWindowLifecycleGuard
{
    private delegate bool EnumWindowsCallback(IntPtr window, IntPtr parameter);

    private const int SwShowNoActivate = 4;
    private const uint GaRoot = 2;
    private const uint MonitorDefaultToNearest = 2;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpShowWindow = 0x0040;
    private const int SmXVirtualScreen = 76;
    private const int SmYVirtualScreen = 77;
    private const int SmCxVirtualScreen = 78;
    private const int SmCyVirtualScreen = 79;
    private static readonly TimeSpan WindowTimeout = TimeSpan.FromSeconds(45);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(25);

    private readonly ConcurrentDictionary<int, CancellationTokenSource> sessions = new();
    private bool disposed;

    public event EventHandler? ActiveSessionsChanged;
    public bool HasActiveSessions => !sessions.IsEmpty;

    public void Protect(int processId)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (processId <= 0) throw new ArgumentOutOfRangeException(nameof(processId));

        var cancellation = new CancellationTokenSource();
        if (!sessions.TryAdd(processId, cancellation))
        {
            cancellation.Dispose();
            throw new InvalidOperationException($"Process {processId} already has a window lifecycle guard.");
        }

        ActiveSessionsChanged?.Invoke(this, EventArgs.Empty);
        _ = Task.Run(() => Monitor(processId, cancellation.Token)).ContinueWith(
            _ => Complete(processId, cancellation),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        foreach (var cancellation in sessions.Values) cancellation.Cancel();
        // Each session releases only the clip it actually claimed. Clearing the
        // desktop-wide clip here could undo the game's or another app's capture.
    }

    private void Complete(int processId, CancellationTokenSource cancellation)
    {
        _ = sessions.TryRemove(processId, out _);
        cancellation.Dispose();
        ActiveSessionsChanged?.Invoke(this, EventArgs.Empty);
    }

    private static void Monitor(int processId, CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindows()) return;

        try
        {
            using var process = Process.GetProcessById(processId);
            var window = WaitForWindow(process, cancellationToken);
            if (window == IntPtr.Zero) return;

            var monitorInfo = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
            var monitor = MonitorFromWindow(window, MonitorDefaultToNearest);
            if (monitor == IntPtr.Zero || !GetMonitorInfo(monitor, ref monitorInfo)) return;
            if (!WaitForDesktopPresentation(process, window, monitorInfo.Monitor, cancellationToken)) return;

            var cursorOwned = false;
            var wasActive = false;
            var captureRect = monitorInfo.Monitor;
            var virtualRect = GetVirtualScreenRect();
            try
            {
                while (!cancellationToken.IsCancellationRequested && !process.HasExited && IsWindow(window))
                {
                    if (IsIconic(window)) _ = ShowWindowAsync(window, SwShowNoActivate);

                    if (TryGetWindowAndClientRect(window, out var currentRect, out var clientRect) &&
                        !clientRect.Equals(monitorInfo.Monitor))
                    {
                        // A framed dgVoodoo window can have a monitor-sized client
                        // with an outer rectangle several pixels larger. Resizing
                        // the outer rectangle to the monitor clips that client.
                        var target = FitOuterToClient(currentRect, clientRect, monitorInfo.Monitor);
                        _ = SetWindowPos(
                            window,
                            IntPtr.Zero,
                            target.Left,
                            target.Top,
                            target.Right - target.Left,
                            target.Bottom - target.Top,
                            SwpNoZOrder | SwpNoActivate | SwpShowWindow);
                    }

                    var isActive = GetAncestor(GetForegroundWindow(), GaRoot) == window;
                    if (isActive && !wasActive && !captureRect.Equals(virtualRect))
                    {
                        var existingClip = new NativeRect();
                        // dgVoodoo owns mouse capture when it has already set a
                        // tighter region. Claim only an unrestricted cursor and
                        // only once per activation; repeated ClipCursor calls
                        // can override the wrapper's game-coordinate mapping.
                        if (GetClipCursor(ref existingClip) && existingClip.Equals(virtualRect))
                            cursorOwned = ClipCursor(ref captureRect);
                    }
                    else if (!isActive && wasActive && cursorOwned)
                    {
                        ReleaseOwnedCursor(captureRect);
                        cursorOwned = false;
                    }
                    wasActive = isActive;

                    cancellationToken.WaitHandle.WaitOne(PollInterval);
                }
            }
            finally
            {
                if (cursorOwned) ReleaseOwnedCursor(captureRect);
            }
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            // The game can exit at any point while its process or window is read.
            // Losing the guard is safe; setup-owned files and process state are not changed.
        }
    }

    private static IntPtr WaitForWindow(Process process, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + WindowTimeout;
        while (!cancellationToken.IsCancellationRequested && DateTime.UtcNow < deadline)
        {
            if (process.HasExited) return IntPtr.Zero;
            var window = FindPresentationWindow(process.Id);
            if (window != IntPtr.Zero) return window;
            cancellationToken.WaitHandle.WaitOne(PollInterval);
        }

        return IntPtr.Zero;
    }

    private static IntPtr FindPresentationWindow(int processId)
    {
        var result = IntPtr.Zero;
        long resultArea = 0;
        _ = EnumWindows((window, parameter) =>
        {
            _ = GetWindowThreadProcessId(window, out var ownerProcessId);
            if (ownerProcessId != processId || !IsWindowVisible(window)) return true;

            var rect = new NativeRect();
            if (!GetWindowRect(window, ref rect)) return true;
            var width = Math.Max(0, rect.Right - rect.Left);
            var height = Math.Max(0, rect.Bottom - rect.Top);
            var area = (long)width * height;
            if (width >= 640 && height >= 480 && area > resultArea)
            {
                result = window;
                resultArea = area;
            }
            return true;
        }, IntPtr.Zero);
        return result;
    }

    private static bool WaitForDesktopPresentation(
        Process process,
        IntPtr window,
        NativeRect monitorRect,
        CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + WindowTimeout;
        while (!cancellationToken.IsCancellationRequested && DateTime.UtcNow < deadline)
        {
            if (process.HasExited || !IsWindow(window)) return false;
            var windowRect = new NativeRect();
            // Wait for dgVoodoo's desktop presentation rather than repositioning
            // an early 800x600 shell window. The outer frame may exceed the
            // monitor even when the actual client already fits exactly.
            if (GetWindowRect(window, ref windowRect) && Covers(windowRect, monitorRect)) return true;
            cancellationToken.WaitHandle.WaitOne(PollInterval);
        }

        return false;
    }

    private static bool Covers(NativeRect outer, NativeRect inner) =>
        outer.Left <= inner.Left && outer.Top <= inner.Top && outer.Right >= inner.Right && outer.Bottom >= inner.Bottom;

    private static bool TryGetWindowAndClientRect(IntPtr window, out NativeRect outer, out NativeRect clientOnScreen)
    {
        outer = new NativeRect();
        clientOnScreen = new NativeRect();
        var client = new NativeRect();
        var origin = new NativePoint();
        if (!GetWindowRect(window, ref outer) || !GetClientRect(window, ref client) ||
            !ClientToScreen(window, ref origin)) return false;
        clientOnScreen = new NativeRect(origin.X, origin.Y,
            origin.X + client.Right - client.Left, origin.Y + client.Bottom - client.Top);
        return true;
    }

    private static NativeRect FitOuterToClient(NativeRect outer, NativeRect client, NativeRect monitor) =>
        new(monitor.Left - (client.Left - outer.Left),
            monitor.Top - (client.Top - outer.Top),
            monitor.Right + (outer.Right - client.Right),
            monitor.Bottom + (outer.Bottom - client.Bottom));

    private static NativeRect GetVirtualScreenRect()
    {
        var left = GetSystemMetrics(SmXVirtualScreen);
        var top = GetSystemMetrics(SmYVirtualScreen);
        return new NativeRect(left, top,
            left + GetSystemMetrics(SmCxVirtualScreen),
            top + GetSystemMetrics(SmCyVirtualScreen));
    }

    private static void ReleaseOwnedCursor(NativeRect claimedRect)
    {
        var current = new NativeRect();
        if (GetClipCursor(ref current) && current.Equals(claimedRect))
            _ = ClipCursor(IntPtr.Zero);
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct NativeRect(int Left, int Top, int Right, int Bottom)
    {
        public NativeRect() : this(0, 0, 0, 0) { }
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct NativePoint(int X, int Y)
    {
        public NativePoint() : this(0, 0) { }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MonitorInfo
    {
        public int Size;
        public NativeRect Monitor;
        public NativeRect WorkArea;
        public uint Flags;
    }

    [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr window);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsCallback callback, IntPtr parameter);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out int processId);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr window);
    [DllImport("user32.dll")] private static extern bool ShowWindowAsync(IntPtr window, int command);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr window, ref NativeRect rect);
    [DllImport("user32.dll")] private static extern bool GetClientRect(IntPtr window, ref NativeRect rect);
    [DllImport("user32.dll")] private static extern bool ClientToScreen(IntPtr window, ref NativePoint point);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr window, IntPtr insertAfter, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern IntPtr GetAncestor(IntPtr window, uint flags);
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
    [DllImport("user32.dll")] private static extern bool ClipCursor(ref NativeRect rect);
    [DllImport("user32.dll")] private static extern bool ClipCursor(IntPtr rect);
    [DllImport("user32.dll")] private static extern bool GetClipCursor(ref NativeRect rect);
    [DllImport("user32.dll")] private static extern int GetSystemMetrics(int index);
}
