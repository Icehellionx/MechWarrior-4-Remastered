using System.Runtime.InteropServices;
using MW4Remastered.Core.Launch;

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        if (!OperatingSystem.IsWindows()) return 0;
        var originalClip = new NativeRect();
        if (!GetClipCursor(ref originalClip)) throw new InvalidOperationException("Could not save the existing cursor clip.");
        var reportPath = Path.Combine(Path.GetTempPath(), "MW4-CursorClipGuardSmoke-result.txt");
        try
        {
            RunCase(framed: false);
            RunCase(framed: true);
            File.WriteAllText(reportPath,
                $"PASS {DateTimeOffset.UtcNow:O}: borderless and framed clients fit; wrapper clip survives.{Environment.NewLine}");
            return 0;
        }
        catch (Exception error)
        {
            File.WriteAllText(reportPath, $"FAIL {DateTimeOffset.UtcNow:O}: {error}{Environment.NewLine}");
            Console.Error.WriteLine(error);
            return 1;
        }
        finally
        {
            _ = ClipCursor(ref originalClip);
        }
    }

    private static void RunCase(bool framed)
    {
        var monitor = Screen.PrimaryScreen!.Bounds;
        using var window = new Form
        {
            Text = framed ? "MW4 framed-client smoke" : "MW4 borderless-client smoke",
            FormBorderStyle = framed ? FormBorderStyle.FixedSingle : FormBorderStyle.None,
            StartPosition = FormStartPosition.Manual,
            BackColor = Color.DarkSlateGray,
            MaximumSize = new Size(10000, 10000),
        };
        if (framed)
        {
            window.ClientSize = monitor.Size;
            window.Location = monitor.Location;
        }
        else
        {
            window.Bounds = new Rectangle(monitor.Left, monitor.Top,
                monitor.Width + 6, monitor.Height + 29);
        }

        using var guard = new SystemGameWindowLifecycleGuard();
        Exception? failure = null;
        window.Shown += async (_, _) =>
        {
            try
            {
                window.Activate();
                FocusWindow(window.Handle);
                var wrapperClip = new NativeRect(
                    monitor.Left + monitor.Width / 4, monitor.Top + monitor.Height / 4,
                    monitor.Right - monitor.Width / 4, monitor.Bottom - monitor.Height / 4);
                if (!ClipCursor(ref wrapperClip)) throw new InvalidOperationException("Could not set simulated wrapper clip.");

                guard.Protect(Environment.ProcessId);
                for (var attempt = 0; attempt < 100 && GetForegroundWindow() != window.Handle; attempt++)
                    await Task.Delay(100);
                if (GetForegroundWindow() != window.Handle)
                    throw new InvalidOperationException("The synthetic game did not become foreground.");
                await Task.Delay(500);

                var observedClip = new NativeRect();
                if (!GetClipCursor(ref observedClip) || !observedClip.Equals(wrapperClip))
                    throw new InvalidOperationException("The guard overrode the simulated wrapper clip.");
                var outer = new NativeRect();
                var client = new NativeRect();
                var origin = new NativePoint();
                if (!GetWindowRect(window.Handle, ref outer) || !GetClientRect(window.Handle, ref client) ||
                    !ClientToScreen(window.Handle, ref origin) ||
                    origin.X != monitor.Left || origin.Y != monitor.Top ||
                    client.Right - client.Left != monitor.Width ||
                    client.Bottom - client.Top != monitor.Height)
                    throw new InvalidOperationException($"{window.Text}: client was not fitted to the monitor. Outer={outer}; client origin={origin}, size={client.Right - client.Left}x{client.Bottom - client.Top}, monitor={monitor}.");
                if (framed && outer.Right - outer.Left <= monitor.Width)
                    throw new InvalidOperationException("The framed outer rectangle was incorrectly shrunk to monitor size.");
                if (!framed && (outer.Left != monitor.Left || outer.Top != monitor.Top ||
                                outer.Right != monitor.Right || outer.Bottom != monitor.Bottom))
                    throw new InvalidOperationException("The oversized borderless outer rectangle was not fitted.");

                guard.Dispose();
                await Task.Delay(100);
                if (!GetClipCursor(ref observedClip) || !observedClip.Equals(wrapperClip))
                    throw new InvalidOperationException("Disposing the guard cleared the simulated wrapper clip.");
            }
            catch (Exception error)
            {
                failure = error;
            }
            finally
            {
                _ = ClipCursor(IntPtr.Zero);
                window.Close();
            }
        };
        Application.Run(window);
        if (failure is not null) throw failure;
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

    [DllImport("user32.dll")] private static extern bool GetClipCursor(ref NativeRect rect);
    [DllImport("user32.dll")] private static extern bool ClipCursor(ref NativeRect rect);
    [DllImport("user32.dll")] private static extern bool ClipCursor(IntPtr rect);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr window, ref NativeRect rect);
    [DllImport("user32.dll")] private static extern bool GetClientRect(IntPtr window, ref NativeRect rect);
    [DllImport("user32.dll")] private static extern bool ClientToScreen(IntPtr window, ref NativePoint point);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    [DllImport("user32.dll")] private static extern bool AttachThreadInput(uint attach, uint attachTo, bool value);
    [DllImport("user32.dll")] private static extern bool BringWindowToTop(IntPtr window);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr window);

    private static void FocusWindow(IntPtr window)
    {
        var previous = GetForegroundWindow();
        var previousThread = previous == IntPtr.Zero ? 0 : GetWindowThreadProcessId(previous, out _);
        var targetThread = GetWindowThreadProcessId(window, out _);
        var attached = previousThread != 0 && previousThread != targetThread &&
            AttachThreadInput(previousThread, targetThread, true);
        try
        {
            _ = BringWindowToTop(window);
            _ = SetForegroundWindow(window);
        }
        finally
        {
            if (attached) _ = AttachThreadInput(previousThread, targetThread, false);
        }
    }
}
