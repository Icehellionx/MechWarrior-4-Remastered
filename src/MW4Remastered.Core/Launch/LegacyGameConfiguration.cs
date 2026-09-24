using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using MW4Remastered.Core.Install;

namespace MW4Remastered.Core.Launch;

public interface ILegacyGameConfiguration
{
    GameResolution ResolveResolution();
    void Ensure(ProductStatus status, GameResolution resolution);
}

public readonly record struct GameResolution(int Width, int Height)
{
    public override string ToString() => $"{Width}x{Height}";

    public static GameResolution LargestFourByThree(int displayWidth, int displayHeight)
    {
        if (displayWidth < 800 || displayHeight < 600) return new GameResolution(1024, 768);

        if ((long)displayWidth * 3 >= (long)displayHeight * 4)
        {
            var height = displayHeight - displayHeight % 3;
            return new GameResolution(height / 3 * 4, height);
        }

        var width = displayWidth - displayWidth % 4;
        return new GameResolution(width, width / 4 * 3);
    }
}

public sealed record GameResolutionPreset(GameResolution Resolution, string Label)
{
    public static IReadOnlyList<GameResolutionPreset> Choices { get; } =
    [
        new(new(800, 600), "800 × 600 (classic)"),
        new(new(1024, 768), "1024 × 768 (classic)"),
        new(new(1600, 1200), "1600 × 1200 (1920 × 1200 screen equivalent)"),
    ];

    public static bool IsSelectable(GameResolution resolution) =>
        Choices.Any(choice => choice.Resolution == resolution);

    public static GameResolution BestFitting(GameResolution displayArea) =>
        Choices.LastOrDefault(choice => choice.Resolution.Width <= displayArea.Width &&
                                        choice.Resolution.Height <= displayArea.Height)?.Resolution ??
        new GameResolution(1024, 768);
}

public readonly record struct GameDisplayGeometry(GameResolution Monitor, GameResolution Gameplay)
{
    public int LeftPillarboxWidth => Math.Max(0, Monitor.Width - Gameplay.Width) / 2;
    public int RightPillarboxWidth => Math.Max(0, Monitor.Width - Gameplay.Width) - LeftPillarboxWidth;
    public int TopLetterboxHeight => Math.Max(0, Monitor.Height - Gameplay.Height) / 2;
    public int BottomLetterboxHeight => Math.Max(0, Monitor.Height - Gameplay.Height) - TopLetterboxHeight;

    public static GameDisplayGeometry ForMonitor(int width, int height) =>
        new(new GameResolution(width, height), GameResolution.LargestFourByThree(width, height));
}

public sealed record GameDisplayPreset(GameResolution Monitor, string Label)
{
    // Presentation examples only. MW4 keeps a separately validated 4:3
    // render mode while dgVoodoo fits the image to the Windows monitor.
    public static IReadOnlyList<GameDisplayPreset> UltrawideChoices { get; } =
    [
        new(new(2560, 1080), "2560 × 1080 (ultrawide, 1080p height)"),
        new(new(3440, 1440), "3440 × 1440 (ultrawide, 1440p height)"),
        new(new(5120, 2160), "5120 × 2160 (ultrawide, 4K height)"),
    ];
}

public interface IGameResolutionProvider
{
    GameResolution GetResolution();
}

public sealed class ActiveMonitorResolutionProvider : IGameResolutionProvider
{
    private const uint MonitorDefaultToPrimary = 1;
    private const int SmCxScreen = 0;
    private const int SmCyScreen = 1;

    public GameResolution GetResolution() => GameResolutionPreset.BestFitting(GetDisplayGeometry().Gameplay);

    public GameDisplayGeometry GetDisplayGeometry()
    {
        if (!OperatingSystem.IsWindows()) return GameDisplayGeometry.ForMonitor(1024, 768);

        // Launch is requested by the foreground launcher. Resolve its monitor at
        // that moment instead of assuming the primary display. The launcher is
        // PerMonitorV2-aware, so these monitor bounds are physical pixels.
        var monitor = MonitorFromWindow(GetForegroundWindow(), MonitorDefaultToPrimary);
        var monitorInfo = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (monitor != IntPtr.Zero && GetMonitorInfo(monitor, ref monitorInfo))
        {
            return GameDisplayGeometry.ForMonitor(
                monitorInfo.Monitor.Right - monitorInfo.Monitor.Left,
                monitorInfo.Monitor.Bottom - monitorInfo.Monitor.Top);
        }

        return GameDisplayGeometry.ForMonitor(
            GetSystemMetrics(SmCxScreen), GetSystemMetrics(SmCyScreen));
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct NativeRect(int Left, int Top, int Right, int Bottom);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MonitorInfo
    {
        public int Size;
        public NativeRect Monitor;
        public NativeRect WorkArea;
        public uint Flags;
    }

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
}

public sealed class ConfiguredGameResolutionProvider : IGameResolutionProvider
{
    private readonly string preferencePath;
    private readonly IGameResolutionProvider monitor;

    public ConfiguredGameResolutionProvider(string applicationRoot, IGameResolutionProvider? monitor = null)
    {
        preferencePath = Path.Combine(Path.GetFullPath(applicationRoot), "Compatibility", "dgVoodoo2", "gameplay-resolution.txt");
        this.monitor = monitor ?? new ActiveMonitorResolutionProvider();
    }

    public GameResolution GetResolution()
    {
        var saved = ReadSaved();
        return saved is { } selected && GameResolutionPreset.IsSelectable(selected)
            ? selected
            : GameResolutionPreset.BestFitting(monitor.GetResolution());
    }

    public GameResolution? ReadSaved()
    {
        if (!File.Exists(preferencePath)) return null;
        var parent = Path.GetDirectoryName(preferencePath)!;
        StagedInstallTransaction.RejectContainedFilePath(parent, preferencePath);
        var value = File.ReadAllText(preferencePath).Trim();
        var parts = value.Split('x');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var width) ||
            !int.TryParse(parts[1], out var height) || width < 800 || height < 600 ||
            width * 3 != height * 4)
            throw new InvalidDataException("The saved gameplay resolution is invalid.");
        return new GameResolution(width, height);
    }

    public void Save(GameResolution? resolution)
    {
        var parent = Path.GetDirectoryName(preferencePath)!;
        if (!Directory.Exists(parent) || (File.GetAttributes(parent) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("The compatibility settings directory is unavailable.");
        if (resolution is { } selected &&
            !GameResolutionPreset.IsSelectable(selected))
            throw new ArgumentOutOfRangeException(nameof(resolution));
        if (File.Exists(preferencePath)) StagedInstallTransaction.RejectContainedFilePath(parent, preferencePath);
        if (resolution is null)
        {
            if (File.Exists(preferencePath)) File.Delete(preferencePath);
            return;
        }
        var temporary = Path.Combine(parent, $".gameplay-resolution-{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllText(temporary, resolution.Value.ToString() + Environment.NewLine);
            File.Move(temporary, preferencePath, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}

public sealed class LegacyGameConfiguration : ILegacyGameConfiguration
{
    private readonly IGameResolutionProvider resolutionProvider;

    // These are the values emitted by MW4's Ultra High preset, with the
    // obsolete in-game antialiasing path left off because dgVoodoo supplies
    // the qualified 4x MSAA implementation for all three games.
    private static readonly (string Key, string Value)[] MaximumGraphicsDefaults =
    [
        ("DetailTexture", "true"),
        ("MultiTexture", "true"),
        ("LightMaps", "true"),
        ("Culturals", "true"),
        ("Footsteps", "true"),
        ("NoBlend", "false"),
        ("AntiAlias", "false"),
        ("VertexLighting", "true"),
        ("SimpleLighting", "false"),
        ("MipBias", "0"),
        ("ShadowMode", "2"),
        ("FancyWater", "true"),
        ("MovieTextures", "true"),
        ("MaxLights", "8"),
        ("Compositing", "3"),
        ("EffectLOD", "0.000000"),
        ("LOD", "0.000000"),
        ("LoadRadius", "4"),
        ("HideSky", "false"),
        ("UseExtendedVisibility", "1"),
    ];

    private const string GraphicsPageTemplate = """
[graphics options]
VideoDriverIndex=0
DetailTexture=true
MultiTexture=true
LightMaps=true
Culturals=true
Footsteps=true
MissionMusic=true
NoBlend=false
AntiAlias=false
VertexLighting=true
SimpleLighting=false
MipBias=0
ShadowMode=2
fontsmall=-2
fontmedium=-2
fontlarge=-2
fontlarge2=-2
fontlarge3=-2
screenwidth={0}
screenheight={1}
bitdepth=32
FancyWater=true
MovieTextures=true
MaxLights=8
Compositing=3
EffectLOD=0.000000
LOD=0.000000
LoadRadius=4
HideSky=false
UseExtendedVisibility=1

[sound options]
LowEndSound=false
Radius=1.000000
HardwareMixing=false

[special commands]
KillGame=false
AutoTorsoCenter=0
HudDamageMode=false
HudTargetDamageMode=false
""";

    public LegacyGameConfiguration(IGameResolutionProvider? resolutionProvider = null)
    {
        this.resolutionProvider = resolutionProvider ?? new ActiveMonitorResolutionProvider();
    }

    public GameResolution ResolveResolution()
    {
        var resolution = resolutionProvider.GetResolution();
        if (!GameResolutionPreset.IsSelectable(resolution))
            throw new InvalidOperationException($"Unsupported game render resolution: {resolution}.");
        return resolution;
    }

    public void Ensure(ProductStatus status, GameResolution resolution)
    {
        EnsureCore(status, resolution, applyMaximumGraphicsDefaults: false);
    }

    public void EnsureDefaults(ProductStatus status, GameResolution resolution)
    {
        EnsureCore(status, resolution, applyMaximumGraphicsDefaults: true);
    }

    private void EnsureCore(ProductStatus status, GameResolution resolution, bool applyMaximumGraphicsDefaults)
    {
        ArgumentNullException.ThrowIfNull(status);
        if (!GameResolutionPreset.IsSelectable(resolution))
            throw new InvalidOperationException($"Unsupported game render resolution: {resolution}.");
        if (status.Product.Id is not ("vengeance" or "black-knight" or "mercenaries")) return;
        if (status.State != ProductInstallState.Ready || string.IsNullOrWhiteSpace(status.InstallPath))
            throw new InvalidOperationException($"{status.Product.DisplayName} does not have a verified configuration root.");

        var fileName = status.Product.Id == "black-knight" ? "optionsx.ini" : "options.ini";
        foreach (var root in ConfigurationRoots(status))
            EnsureFile(root, fileName, resolution, applyMaximumGraphicsDefaults);
    }

    private static IEnumerable<string> ConfigurationRoots(ProductStatus status)
    {
        var installRoot = Path.GetFullPath(status.InstallPath!);
        if (status.Product.Id != "black-knight")
        {
            yield return installRoot;
            yield break;
        }

        // Black Knight's executable and renderer wrappers live in MW4X, but the
        // expansion switches its current directory to the shared Vengeance root
        // before PilotEntry.script reads optionsx.ini. Keep both locations valid:
        // the executable-local copy supports the initial bootstrap and the shared
        // copy supports shell/pilot creation.
        var executableRoot = Path.GetFullPath(Path.GetDirectoryName(status.LaunchPath
            ?? throw new InvalidOperationException("Black Knight does not have a verified launch path."))!);
        yield return executableRoot;
        if (!installRoot.Equals(executableRoot, StringComparison.OrdinalIgnoreCase)) yield return installRoot;
    }

    private void EnsureFile(
        string root,
        string fileName,
        GameResolution resolution,
        bool applyMaximumGraphicsDefaults)
    {
        if (!Directory.Exists(root)) throw new DirectoryNotFoundException($"Verified game root is missing: {root}");
        if ((File.GetAttributes(root) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("Game configuration root cannot be a reparse point.");

        var path = Path.Combine(root, fileName);
        var existing = File.Exists(path) ? ReadRegularFile(root, path) : string.Empty;
        var updated = HasGraphicsPage(existing)
            ? EnsureRequiredGraphics(existing, resolution, applyMaximumGraphicsDefaults)
            : existing + (existing.Length == 0 || existing.EndsWith('\n') ? string.Empty : Environment.NewLine)
                + (existing.Length == 0 ? string.Empty : Environment.NewLine)
                + string.Format(System.Globalization.CultureInfo.InvariantCulture, GraphicsPageTemplate,
                    resolution.Width, resolution.Height) + Environment.NewLine;
        updated = NormalizeWindowsLineEndings(updated);
        if (updated.Equals(existing, StringComparison.Ordinal)) return;
        var temporary = Path.Combine(root, $".{fileName}.mw4-remastered-{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllText(temporary, updated, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    private static string NormalizeWindowsLineEndings(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Replace("\n", "\r\n", StringComparison.Ordinal);

    private static string ReadRegularFile(string root, string path)
    {
        StagedInstallTransaction.RejectContainedFilePath(root, path);
        return File.ReadAllText(path);
    }

    private static bool HasGraphicsPage(string value)
    {
        using var reader = new StringReader(value);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (line.Trim().Equals("[graphics options]", StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private static string EnsureRequiredGraphics(
        string value,
        GameResolution resolution,
        bool applyMaximumGraphicsDefaults)
    {
        var lines = value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n').ToList();
        var inGraphics = false;
        var sectionEnd = lines.Count;
        var required = new Dictionary<string, (string Key, string Value)>(StringComparer.OrdinalIgnoreCase)
        {
            ["screenwidth"] = ("ScreenWidth", resolution.Width.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            ["screenheight"] = ("ScreenHeight", resolution.Height.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            ["bitdepth"] = ("bitdepth", "32"),
        };
        if (applyMaximumGraphicsDefaults)
        {
            foreach (var setting in MaximumGraphicsDefaults)
                required[setting.Key] = setting;
        }
        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < lines.Count; index++)
        {
            var trimmed = lines[index].Trim();
            if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
            {
                if (inGraphics)
                {
                    sectionEnd = index;
                    break;
                }
                inGraphics = trimmed.Equals("[graphics options]", StringComparison.OrdinalIgnoreCase);
                continue;
            }
            if (!inGraphics) continue;

            var separator = trimmed.IndexOf('=');
            if (separator <= 0) continue;
            var key = trimmed[..separator].Trim();
            if (required.TryGetValue(key, out var setting))
            {
                lines[index] = $"{setting.Key}={setting.Value}";
                found.Add(key);
            }
        }

        var missing = required
            .Where(setting => !found.Contains(setting.Key))
            .Select(setting => $"{setting.Value.Key}={setting.Value.Value}")
            .ToList();
        if (missing.Count > 0) lines.InsertRange(sectionEnd, missing);
        return string.Join(Environment.NewLine, lines);
    }
}

public interface IGameConfigurationGuard
{
    void Protect(ProductStatus status, int processId, GameResolution resolution);
}

public sealed class LegacyGameConfigurationGuard : IGameConfigurationGuard
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan ProtectionWindow = TimeSpan.FromSeconds(30);
    private readonly ILegacyGameConfiguration configuration;

    public LegacyGameConfigurationGuard(ILegacyGameConfiguration configuration)
    {
        this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public void Protect(ProductStatus status, int processId, GameResolution resolution)
    {
        ArgumentNullException.ThrowIfNull(status);
        if (processId <= 0) throw new ArgumentOutOfRangeException(nameof(processId));

        _ = Task.Run(async () =>
        {
            var elapsed = Stopwatch.StartNew();
            var expectedExecutable = Path.GetFullPath(status.LaunchPath
                ?? throw new InvalidOperationException("A configuration guard requires a verified launch path."));
            while (elapsed.Elapsed < ProtectionWindow && IsRunning(processId, expectedExecutable))
            {
                try
                {
                    configuration.Ensure(status, resolution);
                }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException)
                {
                    // MW4 replaces the INI during startup. A transient sharing
                    // violation is expected; retry while this exact process lives.
                }

                await Task.Delay(PollInterval).ConfigureAwait(false);
            }
        });
    }

    private static bool IsRunning(int processId, string expectedExecutable)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            if (process.HasExited) return false;
            var actualExecutable = process.MainModule?.FileName;
            return !string.IsNullOrWhiteSpace(actualExecutable) &&
                Path.GetFullPath(actualExecutable).Equals(expectedExecutable, StringComparison.OrdinalIgnoreCase);
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }
}
