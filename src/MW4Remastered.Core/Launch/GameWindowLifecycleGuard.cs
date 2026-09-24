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
        _ = ClipCursor(IntPtr.Zero);
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
            var presentationRect = WaitForDesktopPresentation(process, window, monitorInfo.Monitor, cancellationToken);
            if (presentationRect is null) return;

            var cursorOwned = false;
            try
            {
                while (!cancellationToken.IsCancellationRequested && !process.HasExited && IsWindow(window))
                {
                    if (IsIconic(window)) _ = ShowWindowAsync(window, SwShowNoActivate);

                    var currentRect = new NativeRect();
                    if (GetWindowRect(window, ref currentRect) && !currentRect.Equals(presentationRect.Value))
                    {
                        _ = SetWindowPos(
                            window,
                            IntPtr.Zero,
                            presentationRect.Value.Left,
                            presentationRect.Value.Top,
                            presentationRect.Value.Right - presentationRect.Value.Left,
                            presentationRect.Value.Bottom - presentationRect.Value.Top,
                            SwpNoZOrder | SwpNoActivate | SwpShowWindow);
                    }

                    var isActive = GetAncestor(GetForegroundWindow(), GaRoot) == window;
                    if (isActive)
                    {
                        var captureRect = monitorInfo.Monitor;
                        cursorOwned = ClipCursor(ref captureRect);
                    }
                    else if (cursorOwned)
                    {
                        _ = ClipCursor(IntPtr.Zero);
                        cursorOwned = false;
                    }

                    cancellationToken.WaitHandle.WaitOne(PollInterval);
                }
            }
            finally
            {
                if (cursorOwned) _ = ClipCursor(IntPtr.Zero);
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

    private static NativeRect? WaitForDesktopPresentation(
        Process process,
        IntPtr window,
        NativeRect monitorRect,
        CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + WindowTimeout;
        while (!cancellationToken.IsCancellationRequested && DateTime.UtcNow < deadline)
        {
            if (process.HasExited || !IsWindow(window)) return null;
            var windowRect = new NativeRect();
            // dgVoodoo can create a borderless window a few pixels larger than
            // the monitor. Pin the physical monitor bounds, not that initial
            // oversized rectangle, so ultrawide edges remain visible.
            if (GetWindowRect(window, ref windowRect) && Covers(windowRect, monitorRect)) return monitorRect;
            cancellationToken.WaitHandle.WaitOne(PollInterval);
        }

        return null;
    }

    private static bool Covers(NativeRect outer, NativeRect inner) =>
        outer.Left <= inner.Left && outer.Top <= inner.Top && outer.Right >= inner.Right && outer.Bottom >= inner.Bottom;

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

    [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr window);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsCallback callback, IntPtr parameter);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out int processId);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr window);
    [DllImport("user32.dll")] private static extern bool ShowWindowAsync(IntPtr window, int command);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr window, ref NativeRect rect);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr window, IntPtr insertAfter, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern IntPtr GetAncestor(IntPtr window, uint flags);
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
    [DllImport("user32.dll")] private static extern bool ClipCursor(ref NativeRect rect);
    [DllImport("user32.dll")] private static extern bool ClipCursor(IntPtr rect);
}
