using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms;

internal static class Program
{
    private const int SwRestore = 9;
    private const int SwShowNoActivate = 4;
    private const uint WmClose = 0x0010;
    private const uint BmClick = 0x00F5;
    private const uint GaRoot = 2;
    private const uint InputKeyboard = 1;
    private const uint InputMouse = 0;
    private const ushort VkMenu = 0x12;
    private const ushort VkTab = 0x09;
    private const uint KeyUp = 0x0002;
    private const uint MouseLeftDown = 0x0002;
    private const uint MouseLeftUp = 0x0004;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpShowWindow = 0x0040;
    private const int GwlStyle = -16;
    private const int GwlExStyle = -20;
    private const long WsCaption = 0x00C00000L;
    private const long WsThickFrame = 0x00040000L;
    private const long WsExTopMost = 0x00000008L;
    private const int EnumCurrentSettings = -1;

    [STAThread]
    private static int Main(string[] args)
    {
        var options = Options.Parse(args);
        Directory.CreateDirectory(options.OutputDirectory);

        var originalCursor = new PointNative();
        _ = GetCursorPos(ref originalCursor);
        var witness = new WitnessWindow();
        WindowLifecycleGuard? guard = null;
        Process? process = null;
        Process? launcherProcess = null;

        try
        {
            var initialMode = ReadDisplayMode();
            if (options.Launcher is null)
            {
                var startInfo = new ProcessStartInfo(options.Executable)
                {
                    Arguments = options.Arguments,
                    WorkingDirectory = options.WorkingDirectory,
                    UseShellExecute = false,
                };
                process = Process.Start(startInfo) ?? throw new InvalidOperationException("The game process did not start.");
            }
            else
            {
                launcherProcess = Process.Start(new ProcessStartInfo(options.Launcher)
                {
                    WorkingDirectory = Path.GetDirectoryName(options.Launcher)!,
                    UseShellExecute = false,
                }) ?? throw new InvalidOperationException("The installed launcher did not start.");
                var launcherWindow = WaitForGameWindow(launcherProcess, options.WindowTimeout);
                var launchButton = WaitForDescendant(launcherWindow, options.ButtonText!, options.WindowTimeout);
                _ = PostMessage(launchButton, BmClick, IntPtr.Zero, IntPtr.Zero);
                process = WaitForProcess(options.Executable, options.WindowTimeout);
            }
            var gameWindow = WaitForGameWindow(process, options.WindowTimeout);
            Thread.Sleep(options.SettleTime);

            var screen = Screen.FromHandle(gameWindow).Bounds;
            if (options.Guard)
            {
                var presentationRect = new RectNative();
                if (!GetWindowRect(gameWindow, ref presentationRect))
                {
                    throw new InvalidOperationException("Could not read the initial presentation rectangle.");
                }
                guard = new WindowLifecycleGuard(gameWindow, ToRect(screen), presentationRect);
                guard.Start();
            }
            var baseline = Snapshot("active-before", gameWindow, initialMode, screen);

            witness.Start(screen);
            // The centered witness intentionally takes focus when shown. The game
            // remains visible around it, so a click in the exposed top-left area
            // models the user's click-back path without forcing foreground state.
            ClickAt(new PointNative(screen.Left + 8, screen.Top + 8));
            var initialActivationSucceeded = TryWaitForForeground(gameWindow, options.FocusTimeout);
            Thread.Sleep(500);
            var activeAfterClick = Snapshot("active-after-click", gameWindow, initialMode, screen);
            CaptureWindow(gameWindow, Path.Combine(options.OutputDirectory, $"{Sanitize(options.Name)}-active.png"));
            CaptureInteractionFrames(options, gameWindow, screen);

            SendAltTab();
            var altTabAwaySucceeded = TryWaitForForegroundChange(gameWindow, options.FocusTimeout);
            Thread.Sleep(1500);
            var altTabbedAway = Snapshot("alt-tab-away", gameWindow, initialMode, screen);
            var awayForeground = GetForegroundWindow();

            var altTabReturnCount = AltTabUntil(gameWindow, 12);
            var altTabReturnSucceeded = altTabReturnCount > 0;
            var altTabbedBack = Snapshot("alt-tab-back", gameWindow, initialMode, screen);

            SendAltTab();
            var witnessActivationSucceeded = TryWaitForForegroundChange(gameWindow, options.FocusTimeout);
            Thread.Sleep(1500);
            var beforeClickReturn = Snapshot("before-click-return", gameWindow, initialMode, screen);
            ClickAt(new PointNative(screen.Left + 8, screen.Top + 8));
            var clickReturnSucceeded = TryWaitForForeground(gameWindow, options.FocusTimeout);
            Thread.Sleep(500);
            var clickedBack = Snapshot("click-back", gameWindow, initialMode, screen);

            var virtualScreen = ToRect(SystemInformation.VirtualScreen);
            var checks = new Dictionary<string, bool>
            {
                ["borderless"] = (activeAfterClick.Style & (WsCaption | WsThickFrame)) == 0,
                ["notTopMost"] = (activeAfterClick.ExStyle & WsExTopMost) == 0,
                ["desktopSized"] = Covers(activeAfterClick.WindowRect, ToRect(screen)),
                ["displayModeUnchanged"] = new[] { baseline, activeAfterClick, altTabbedAway, altTabbedBack, beforeClickReturn, clickedBack }.All(item => item.DisplayMode.Equals(initialMode)),
                ["notMinimizedAfterAltTabAway"] = !altTabbedAway.IsMinimized && altTabbedAway.IsVisible,
                ["initialActivationSucceeded"] = initialActivationSucceeded,
                ["altTabAwaySucceeded"] = altTabAwaySucceeded,
                ["altTabReturnedToGame"] = altTabReturnSucceeded && altTabbedBack.ForegroundRootWindow == gameWindow && !altTabbedBack.IsMinimized,
                ["presentationRectPreservedAfterAltTab"] = altTabbedBack.WindowRect.Equals(activeAfterClick.WindowRect),
                ["witnessActivationSucceeded"] = witnessActivationSucceeded,
                ["notMinimizedBeforeClickReturn"] = !beforeClickReturn.IsMinimized && beforeClickReturn.IsVisible,
                ["clickReturnedToGame"] = clickReturnSucceeded && clickedBack.ForegroundRootWindow == gameWindow && !clickedBack.IsMinimized,
                ["presentationRectPreservedAfterClick"] = clickedBack.WindowRect.Equals(activeAfterClick.WindowRect),
                ["focusActuallyLeftGame"] = awayForeground != IntPtr.Zero && awayForeground != gameWindow,
                ["cursorCapturedWhenActive"] = !activeAfterClick.CursorClip.Equals(virtualScreen) || activeAfterClick.GuiCaptureWindow == gameWindow,
                ["cursorReleasedWhileInactive"] = altTabbedAway.CursorClip.Equals(virtualScreen) && altTabbedAway.GuiCaptureWindow == IntPtr.Zero,
                ["cursorRecapturedAfterClickReturn"] = !clickedBack.CursorClip.Equals(virtualScreen) || clickedBack.GuiCaptureWindow == gameWindow,
            };

            var report = new Report(
                options.Name,
                options.Executable,
                process.Id,
                gameWindow,
                witness.Handle,
                screen,
                SystemInformation.VirtualScreen,
                initialMode,
                options.Guard,
                guard?.RestoreCount ?? 0,
                guard?.RepositionCount ?? 0,
                guard?.CaptureSuccessCount ?? 0,
                guard?.CaptureFailureCount ?? 0,
                altTabReturnCount,
                checks,
                new[] { baseline, activeAfterClick, altTabbedAway, altTabbedBack, beforeClickReturn, clickedBack });

            var reportPath = Path.Combine(options.OutputDirectory, $"{Sanitize(options.Name)}.json");
            File.WriteAllText(reportPath, JsonSerializer.Serialize(report, JsonOptions));
            Console.WriteLine(JsonSerializer.Serialize(report, JsonOptions));
            return checks.Values.All(value => value) ? 0 : 2;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
        finally
        {
            guard?.Dispose();
            witness.Dispose();
            if (process is { HasExited: false })
            {
                var window = FindGameWindow(process.Id);
                if (window != IntPtr.Zero)
                {
                    _ = PostMessage(window, WmClose, IntPtr.Zero, IntPtr.Zero);
                }

                if (!process.WaitForExit(5000))
                {
                    process.Kill(entireProcessTree: true);
                    process.WaitForExit(5000);
                }
            }

            if (launcherProcess is { HasExited: false })
            {
                var launcherWindow = FindGameWindow(launcherProcess.Id);
                if (launcherWindow != IntPtr.Zero) _ = PostMessage(launcherWindow, WmClose, IntPtr.Zero, IntPtr.Zero);
                if (!launcherProcess.WaitForExit(5000))
                {
                    launcherProcess.Kill(entireProcessTree: true);
                    launcherProcess.WaitForExit(5000);
                }
            }

            _ = SetCursorPos(originalCursor.X, originalCursor.Y);
        }
    }

    private static WindowSnapshot Snapshot(string phase, IntPtr gameWindow, DisplayMode expectedMode, Rectangle screen)
    {
        var rect = new RectNative();
        if (!GetWindowRect(gameWindow, ref rect))
        {
            throw new InvalidOperationException($"GetWindowRect failed during {phase}.");
        }

        var clip = new RectNative();
        if (!GetClipCursor(ref clip))
        {
            throw new InvalidOperationException($"GetClipCursor failed during {phase}.");
        }

        var threadId = GetWindowThreadProcessId(gameWindow, out _);
        var gui = new GuiThreadInfo { Size = Marshal.SizeOf<GuiThreadInfo>() };
        _ = GetGUIThreadInfo(threadId, ref gui);

        return new WindowSnapshot(
            phase,
            GetForegroundWindow(),
            GetAncestor(GetForegroundWindow(), GaRoot),
            IsWindowVisible(gameWindow),
            IsIconic(gameWindow),
            IsHungAppWindow(gameWindow),
            GetWindowLongPtr(gameWindow, GwlStyle).ToInt64(),
            GetWindowLongPtr(gameWindow, GwlExStyle).ToInt64(),
            rect,
            ToRect(screen),
            clip,
            gui.ActiveWindow,
            gui.FocusWindow,
            gui.CaptureWindow,
            ReadDisplayMode(),
            expectedMode);
    }

    private static IntPtr WaitForGameWindow(Process process, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (process.HasExited)
            {
                throw new InvalidOperationException($"The game exited with code {process.ExitCode} before creating a window.");
            }

            var window = FindGameWindow(process.Id);
            if (window != IntPtr.Zero)
            {
                return window;
            }

            Thread.Sleep(200);
            process.Refresh();
        }

        throw new TimeoutException($"No visible top-level game window appeared within {timeout.TotalSeconds:F0} seconds.");
    }

    private static IntPtr FindGameWindow(int processId)
    {
        var match = IntPtr.Zero;
        _ = EnumWindows((window, parameter) =>
        {
            _ = GetWindowThreadProcessId(window, out var ownerProcessId);
            if (ownerProcessId == processId && IsWindowVisible(window) && GetWindowTextLength(window) > 0)
            {
                match = window;
                return false;
            }

            return true;
        }, IntPtr.Zero);
        return match;
    }

    private static Process WaitForProcess(string executable, TimeSpan timeout)
    {
        var expected = Path.GetFullPath(executable);
        var processName = Path.GetFileNameWithoutExtension(expected);
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            foreach (var candidate in Process.GetProcessesByName(processName))
            {
                try
                {
                    if (!candidate.HasExited && string.Equals(candidate.MainModule?.FileName, expected, StringComparison.OrdinalIgnoreCase))
                    {
                        return candidate;
                    }
                }
                catch (Exception error) when (error is InvalidOperationException or System.ComponentModel.Win32Exception)
                {
                    // The process can exit while its executable path is inspected.
                }
                candidate.Dispose();
            }
            Thread.Sleep(200);
        }
        throw new TimeoutException($"The launcher did not start {expected} within {timeout.TotalSeconds:F0} seconds.");
    }

    private static IntPtr WaitForDescendant(IntPtr parent, string text, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var match = IntPtr.Zero;
            _ = EnumChildWindows(parent, (window, parameter) =>
            {
                var classBuffer = new System.Text.StringBuilder(256);
                _ = GetClassName(window, classBuffer, classBuffer.Capacity);
                if (!classBuffer.ToString().Contains("BUTTON", StringComparison.OrdinalIgnoreCase) || !IsWindowEnabled(window)) return true;
                var length = GetWindowTextLength(window);
                if (length <= 0) return true;
                var buffer = new System.Text.StringBuilder(length + 1);
                _ = GetWindowText(window, buffer, buffer.Capacity);
                var actualText = buffer.ToString();
                if (!text.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .All(token => actualText.Contains(token, StringComparison.OrdinalIgnoreCase))) return true;
                match = window;
                return false;
            }, IntPtr.Zero);
            if (match != IntPtr.Zero) return match;
            Thread.Sleep(200);
        }
        throw new TimeoutException($"No launcher control containing '{text}' appeared.");
    }

    private static void FocusWindow(IntPtr window)
    {
        _ = ShowWindowAsync(window, SwRestore);
        var foreground = GetForegroundWindow();
        var foregroundThread = foreground == IntPtr.Zero ? 0 : GetWindowThreadProcessId(foreground, out _);
        var targetThread = GetWindowThreadProcessId(window, out _);
        var attached = foregroundThread != 0 && foregroundThread != targetThread && AttachThreadInput(foregroundThread, targetThread, true);
        try
        {
            _ = BringWindowToTop(window);
            _ = SetForegroundWindow(window);
            _ = SetFocus(window);
        }
        finally
        {
            if (attached)
            {
                _ = AttachThreadInput(foregroundThread, targetThread, false);
            }
        }
    }

    private static bool TryWaitForForeground(IntPtr expected, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (GetAncestor(GetForegroundWindow(), GaRoot) == expected)
            {
                return true;
            }

            Thread.Sleep(100);
        }

        return false;
    }

    private static bool TryWaitForForegroundChange(IntPtr previous, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var foreground = GetAncestor(GetForegroundWindow(), GaRoot);
            if (foreground != IntPtr.Zero && foreground != previous)
            {
                return true;
            }

            Thread.Sleep(100);
        }

        return false;
    }

    private static int AltTabUntil(IntPtr expected, int maximumAttempts)
    {
        for (var attempt = 1; attempt <= maximumAttempts; attempt++)
        {
            SendAltTab();
            Thread.Sleep(750);
            if (GetAncestor(GetForegroundWindow(), GaRoot) == expected)
            {
                return attempt;
            }
        }

        return 0;
    }

    private static void SendAltTab()
    {
        var inputs = new[]
        {
            KeyboardInput(VkMenu, 0),
            KeyboardInput(VkTab, 0),
            KeyboardInput(VkTab, KeyUp),
            KeyboardInput(VkMenu, KeyUp),
        };
        if (SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>()) != inputs.Length)
        {
            throw new InvalidOperationException("SendInput did not deliver Alt-Tab.");
        }
    }

    private static void ClickAt(PointNative point)
    {
        if (!SetCursorPos(point.X, point.Y))
        {
            throw new InvalidOperationException("SetCursorPos failed.");
        }

        var inputs = new[]
        {
            MouseInput(MouseLeftDown),
            MouseInput(MouseLeftUp),
        };
        if (SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>()) != inputs.Length)
        {
            throw new InvalidOperationException("SendInput did not deliver the mouse click.");
        }
    }

    private static Input KeyboardInput(ushort key, uint flags) => new()
    {
        Type = InputKeyboard,
        Union = new InputUnion { Keyboard = new KeyboardInputNative { VirtualKey = key, Flags = flags } },
    };

    private static Input MouseInput(uint flags) => new()
    {
        Type = InputMouse,
        Union = new InputUnion { Mouse = new MouseInputNative { Flags = flags } },
    };

    private static DisplayMode ReadDisplayMode()
    {
        var mode = new DevMode { DeviceName = new string('\0', 32), FormName = new string('\0', 32), Size = (short)Marshal.SizeOf<DevMode>() };
        if (!EnumDisplaySettings(null, EnumCurrentSettings, ref mode))
        {
            throw new InvalidOperationException("EnumDisplaySettings failed.");
        }

        return new DisplayMode(mode.PelsWidth, mode.PelsHeight, mode.BitsPerPel, mode.DisplayFrequency);
    }

    private static RectNative ToRect(Rectangle rectangle) => new(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom);

    private static bool Covers(RectNative outer, RectNative inner) =>
        outer.Left <= inner.Left && outer.Top <= inner.Top && outer.Right >= inner.Right && outer.Bottom >= inner.Bottom;

    private static string Sanitize(string value) => string.Concat(value.Select(character => Path.GetInvalidFileNameChars().Contains(character) ? '_' : character));

    private static void CaptureWindow(IntPtr window, string path)
    {
        var rect = new RectNative();
        if (!GetWindowRect(window, ref rect))
        {
            throw new InvalidOperationException("Could not read the game window for capture.");
        }
        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;
        if (width <= 0 || height <= 0) throw new InvalidOperationException("The game window has invalid capture bounds.");

        using var bitmap = new Bitmap(width, height);
        using var graphics = Graphics.FromImage(bitmap);
        var deviceContext = graphics.GetHdc();
        try
        {
            if (!PrintWindow(window, deviceContext, 2))
            {
                throw new InvalidOperationException("PrintWindow could not capture the exact game window.");
            }
        }
        finally
        {
            graphics.ReleaseHdc(deviceContext);
        }
        bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
    }

    private static void CaptureInteractionFrames(Options options, IntPtr gameWindow, Rectangle screen)
    {
        if (string.IsNullOrWhiteSpace(options.CaptureClicks)) return;

        var stepNumber = 0;
        foreach (var step in options.CaptureClicks.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var fields = step.Split(',', StringSplitOptions.TrimEntries);
            if (fields.Length != 3 || !int.TryParse(fields[0], out var x) || !int.TryParse(fields[1], out var y) || !int.TryParse(fields[2], out var settleSeconds))
            {
                throw new ArgumentException($"Invalid capture click '{step}'; expected x,y,settleSeconds.");
            }

            if (x >= 0 && y >= 0)
            {
                ClickAt(new PointNative(screen.Left + x, screen.Top + y));
            }
            Thread.Sleep(TimeSpan.FromSeconds(settleSeconds));
            stepNumber++;
            CaptureWindow(gameWindow, Path.Combine(options.OutputDirectory, $"{Sanitize(options.Name)}-interaction-{stepNumber}.png"));
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new IntPtrJsonConverter() },
    };

    private sealed class IntPtrJsonConverter : JsonConverter<IntPtr>
    {
        public override IntPtr Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => new(reader.GetInt64());
        public override void Write(Utf8JsonWriter writer, IntPtr value, JsonSerializerOptions options) => writer.WriteNumberValue(value.ToInt64());
    }

    private sealed class WitnessWindow : IDisposable
    {
        private readonly ManualResetEventSlim ready = new();
        private Thread? thread;
        private Form? form;

        public IntPtr Handle { get; private set; }

        public void Start(Rectangle screen)
        {
            thread = new Thread(() =>
            {
                Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
                form = new Form
                {
                    Text = "MW4 window lifecycle witness",
                    StartPosition = FormStartPosition.Manual,
                    Bounds = new Rectangle(screen.Left + ((screen.Width - 520) / 2), screen.Top + ((screen.Height - 220) / 2), 520, 220),
                    TopMost = false,
                };
                form.Shown += (_, _) =>
                {
                    Handle = form.Handle;
                    ready.Set();
                };
                Application.Run(form);
            }) { IsBackground = true, Name = "MW4 lifecycle witness" };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            if (!ready.Wait(TimeSpan.FromSeconds(5)))
            {
                throw new TimeoutException("The witness window did not start.");
            }
        }

        public void Dispose()
        {
            if (form is not null && !form.IsDisposed)
            {
                form.BeginInvoke(form.Close);
            }

            thread?.Join(TimeSpan.FromSeconds(5));
            ready.Dispose();
        }
    }

    private sealed class WindowLifecycleGuard : IDisposable
    {
        private readonly IntPtr gameWindow;
        private RectNative captureRect;
        private readonly RectNative presentationRect;
        private readonly CancellationTokenSource cancellation = new();
        private Thread? thread;
        private bool cursorWasCaptured;

        public WindowLifecycleGuard(IntPtr gameWindow, RectNative captureRect, RectNative presentationRect)
        {
            this.gameWindow = gameWindow;
            this.captureRect = captureRect;
            this.presentationRect = presentationRect;
        }

        public int RestoreCount { get; private set; }
        public int RepositionCount { get; private set; }
        public int CaptureSuccessCount { get; private set; }
        public int CaptureFailureCount { get; private set; }

        public void Start()
        {
            thread = new Thread(Run) { IsBackground = true, Name = "MW4 window lifecycle guard" };
            thread.Start();
        }

        private void Run()
        {
            while (!cancellation.IsCancellationRequested && IsWindow(gameWindow))
            {
                if (IsIconic(gameWindow))
                {
                    if (ShowWindowAsync(gameWindow, SwShowNoActivate))
                    {
                        RestoreCount++;
                    }
                }

                var currentRect = new RectNative();
                if (GetWindowRect(gameWindow, ref currentRect) && !currentRect.Equals(presentationRect))
                {
                    if (SetWindowPos(
                        gameWindow,
                        IntPtr.Zero,
                        presentationRect.Left,
                        presentationRect.Top,
                        presentationRect.Right - presentationRect.Left,
                        presentationRect.Bottom - presentationRect.Top,
                        SwpNoZOrder | SwpNoActivate | SwpShowWindow))
                    {
                        RepositionCount++;
                    }
                }

                var isActive = GetAncestor(GetForegroundWindow(), GaRoot) == gameWindow;
                if (isActive)
                {
                    if (ClipCursor(ref captureRect))
                    {
                        CaptureSuccessCount++;
                        cursorWasCaptured = true;
                    }
                    else
                    {
                        CaptureFailureCount++;
                    }
                }
                else if (cursorWasCaptured)
                {
                    _ = ClipCursor(IntPtr.Zero);
                    cursorWasCaptured = false;
                }

                Thread.Sleep(25);
            }

            if (cursorWasCaptured)
            {
                _ = ClipCursor(IntPtr.Zero);
                cursorWasCaptured = false;
            }
        }

        public void Dispose()
        {
            cancellation.Cancel();
            thread?.Join(TimeSpan.FromSeconds(2));
            cancellation.Dispose();
        }
    }

    private sealed record Options(string Name, string Executable, string WorkingDirectory, string Arguments, string OutputDirectory, TimeSpan WindowTimeout, TimeSpan SettleTime, TimeSpan FocusTimeout, bool Guard, string? Launcher, string? ButtonText, string? CaptureClicks)
    {
        public static Options Parse(string[] args)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < args.Length; index += 2)
            {
                if (index + 1 >= args.Length || !args[index].StartsWith("--", StringComparison.Ordinal))
                {
                    throw new ArgumentException("Arguments must be supplied as --name value pairs.");
                }

                values[args[index][2..]] = args[index + 1];
            }

            string Required(string key) => values.TryGetValue(key, out var value) ? value : throw new ArgumentException($"Missing --{key}.");
            var executable = Path.GetFullPath(Required("exe"));
            return new Options(
                Required("name"),
                executable,
                Path.GetFullPath(values.GetValueOrDefault("working-dir", Path.GetDirectoryName(executable)!)),
                values.GetValueOrDefault("arguments", string.Empty),
                Path.GetFullPath(values.GetValueOrDefault("output", ".")),
                TimeSpan.FromSeconds(int.Parse(values.GetValueOrDefault("window-timeout", "30"))),
                TimeSpan.FromSeconds(int.Parse(values.GetValueOrDefault("settle", "8"))),
                TimeSpan.FromSeconds(int.Parse(values.GetValueOrDefault("focus-timeout", "8"))),
                bool.Parse(values.GetValueOrDefault("guard", "false")),
                values.TryGetValue("launcher", out var launcher) ? Path.GetFullPath(launcher) : null,
                values.GetValueOrDefault("button-text"),
                values.GetValueOrDefault("capture-clicks"));
        }
    }

    private sealed record Report(string Name, string Executable, int ProcessId, IntPtr GameWindow, IntPtr WitnessWindow, Rectangle Screen, Rectangle VirtualScreen, DisplayMode InitialDisplayMode, bool GuardEnabled, int GuardRestoreCount, int GuardRepositionCount, int GuardCaptureSuccessCount, int GuardCaptureFailureCount, int AltTabReturnCount, IReadOnlyDictionary<string, bool> Checks, IReadOnlyList<WindowSnapshot> Snapshots);
    private sealed record WindowSnapshot(string Phase, IntPtr ForegroundWindow, IntPtr ForegroundRootWindow, bool IsVisible, bool IsMinimized, bool IsHung, long Style, long ExStyle, RectNative WindowRect, RectNative ScreenRect, RectNative CursorClip, IntPtr GuiActiveWindow, IntPtr GuiFocusWindow, IntPtr GuiCaptureWindow, DisplayMode DisplayMode, DisplayMode ExpectedDisplayMode);
    private sealed record DisplayMode(int Width, int Height, int BitsPerPixel, int Frequency);

    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct PointNative(int X, int Y)
    {
        public PointNative() : this(0, 0) { }
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct RectNative(int Left, int Top, int Right, int Bottom)
    {
        public RectNative() : this(0, 0, 0, 0) { }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DevMode
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string DeviceName;
        public short SpecVersion;
        public short DriverVersion;
        public short Size;
        public short DriverExtra;
        public int Fields;
        public int PositionX;
        public int PositionY;
        public int DisplayOrientation;
        public int DisplayFixedOutput;
        public short Color;
        public short Duplex;
        public short YResolution;
        public short TTOption;
        public short Collate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string FormName;
        public short LogPixels;
        public int BitsPerPel;
        public int PelsWidth;
        public int PelsHeight;
        public int DisplayFlags;
        public int DisplayFrequency;
        public int ICMMethod;
        public int ICMIntent;
        public int MediaType;
        public int DitherType;
        public int Reserved1;
        public int Reserved2;
        public int PanningWidth;
        public int PanningHeight;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct GuiThreadInfo
    {
        public int Size;
        public int Flags;
        public IntPtr ActiveWindow;
        public IntPtr FocusWindow;
        public IntPtr CaptureWindow;
        public IntPtr MenuOwnerWindow;
        public IntPtr MoveSizeWindow;
        public IntPtr CaretWindow;
        public RectNative CaretRect;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint Type;
        public InputUnion Union;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MouseInputNative Mouse;
        [FieldOffset(0)] public KeyboardInputNative Keyboard;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInputNative
    {
        public int X;
        public int Y;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInputNative
    {
        public ushort VirtualKey;
        public ushort ScanCode;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    private delegate bool EnumWindowsCallback(IntPtr window, IntPtr parameter);

    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsCallback callback, IntPtr parameter);
    [DllImport("user32.dll")] private static extern bool EnumChildWindows(IntPtr parent, EnumWindowsCallback callback, IntPtr parameter);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out int processId);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr window);
    [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr window);
    [DllImport("user32.dll")] private static extern bool IsHungAppWindow(IntPtr window);
    [DllImport("user32.dll")] private static extern int GetWindowTextLength(IntPtr window);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr window, System.Text.StringBuilder text, int maximumCount);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr window, System.Text.StringBuilder className, int maximumCount);
    [DllImport("user32.dll")] private static extern bool IsWindowEnabled(IntPtr window);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr window, ref RectNative rect);
    [DllImport("user32.dll")] private static extern bool PrintWindow(IntPtr window, IntPtr deviceContext, uint flags);
    [DllImport("user32.dll")] private static extern bool GetClipCursor(ref RectNative rect);
    [DllImport("user32.dll")] private static extern bool ClipCursor(ref RectNative rect);
    [DllImport("user32.dll")] private static extern bool ClipCursor(IntPtr rect);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern IntPtr GetAncestor(IntPtr window, uint flags);
    [DllImport("user32.dll")] private static extern IntPtr GetWindowLongPtr(IntPtr window, int index);
    [DllImport("user32.dll")] private static extern bool GetGUIThreadInfo(uint threadId, ref GuiThreadInfo info);
    [DllImport("user32.dll")] private static extern bool ShowWindowAsync(IntPtr window, int command);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr window, IntPtr insertAfter, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll")] private static extern bool BringWindowToTop(IntPtr window);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] private static extern IntPtr SetFocus(IntPtr window);
    [DllImport("user32.dll")] private static extern bool AttachThreadInput(uint attach, uint attachTo, bool value);
    [DllImport("user32.dll")] private static extern uint SendInput(uint count, Input[] inputs, int size);
    [DllImport("user32.dll")] private static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] private static extern bool GetCursorPos(ref PointNative point);
    [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool EnumDisplaySettings(string? deviceName, int modeNumber, ref DevMode mode);
}
