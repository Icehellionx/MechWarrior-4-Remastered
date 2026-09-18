using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using MW4Remastered.Core.Install;

namespace MW4Remastered.Core.Launch;

public interface ILegacyGameConfiguration
{
    GameResolution Resolution { get; }
    void Ensure(ProductStatus status);
}

public readonly record struct GameResolution(int Width, int Height)
{
    public override string ToString() => $"{Width}x{Height}";
}

public interface IGameResolutionProvider
{
    GameResolution GetResolution();
}

public sealed class PrimaryDisplayResolutionProvider : IGameResolutionProvider
{
    private const int SmCxScreen = 0;
    private const int SmCyScreen = 1;

    public GameResolution GetResolution()
    {
        if (!OperatingSystem.IsWindows()) return new GameResolution(1024, 768);

        var desktopWidth = GetSystemMetrics(SmCxScreen);
        var desktopHeight = GetSystemMetrics(SmCyScreen);
        if (desktopWidth < 800 || desktopHeight < 600) return new GameResolution(1024, 768);

        // MW4's UI and artwork are authored for 4:3. Use the full display height
        // and the largest matching width, so a 2560x1440 monitor renders at
        // 1920x1440 and dgVoodoo pillarboxes it without stretching anything.
        var height = desktopHeight - desktopHeight % 4;
        var width = Math.Min(desktopWidth, height * 4 / 3);
        width -= width % 4;
        height = width * 3 / 4;
        return width >= 800 && height >= 600
            ? new GameResolution(width, height)
            : new GameResolution(1024, 768);
    }

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);
}

public sealed class LegacyGameConfiguration : ILegacyGameConfiguration
{
    public GameResolution Resolution { get; }

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
        Resolution = (resolutionProvider ?? new PrimaryDisplayResolutionProvider()).GetResolution();
        if (Resolution.Width < 800 || Resolution.Height < 600 || Resolution.Width * 3 != Resolution.Height * 4)
            throw new InvalidOperationException($"Invalid 4:3 game resolution: {Resolution}.");
    }

    public void Ensure(ProductStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);
        if (status.Product.Id is not ("vengeance" or "black-knight" or "mercenaries")) return;
        if (status.State != ProductInstallState.Ready || string.IsNullOrWhiteSpace(status.InstallPath))
            throw new InvalidOperationException($"{status.Product.DisplayName} does not have a verified configuration root.");

        var fileName = status.Product.Id == "black-knight" ? "optionsx.ini" : "options.ini";
        foreach (var root in ConfigurationRoots(status)) EnsureFile(root, fileName);
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

    private void EnsureFile(string root, string fileName)
    {
        if (!Directory.Exists(root)) throw new DirectoryNotFoundException($"Verified game root is missing: {root}");
        if ((File.GetAttributes(root) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("Game configuration root cannot be a reparse point.");

        var path = Path.Combine(root, fileName);
        var existing = File.Exists(path) ? ReadRegularFile(root, path) : string.Empty;
        var updated = HasGraphicsPage(existing)
            ? EnsureRequiredResolution(existing)
            : existing + (existing.Length == 0 || existing.EndsWith('\n') ? string.Empty : Environment.NewLine)
                + (existing.Length == 0 ? string.Empty : Environment.NewLine)
                + string.Format(System.Globalization.CultureInfo.InvariantCulture, GraphicsPageTemplate,
                    Resolution.Width, Resolution.Height) + Environment.NewLine;
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

    private string EnsureRequiredResolution(string value)
    {
        var lines = value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n').ToList();
        var inGraphics = false;
        var foundWidth = false;
        var foundHeight = false;
        var foundDepth = false;
        var sectionEnd = lines.Count;

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
            if (key.Equals("screenwidth", StringComparison.OrdinalIgnoreCase))
            {
                lines[index] = $"ScreenWidth={Resolution.Width}";
                foundWidth = true;
            }
            else if (key.Equals("screenheight", StringComparison.OrdinalIgnoreCase))
            {
                lines[index] = $"ScreenHeight={Resolution.Height}";
                foundHeight = true;
            }
            else if (key.Equals("bitdepth", StringComparison.OrdinalIgnoreCase))
            {
                lines[index] = "bitdepth=32";
                foundDepth = true;
            }
        }

        var missing = new List<string>(3);
        if (!foundWidth) missing.Add($"ScreenWidth={Resolution.Width}");
        if (!foundHeight) missing.Add($"ScreenHeight={Resolution.Height}");
        if (!foundDepth) missing.Add("bitdepth=32");
        if (missing.Count > 0) lines.InsertRange(sectionEnd, missing);
        return string.Join(Environment.NewLine, lines);
    }
}

public interface IGameConfigurationGuard
{
    void Protect(ProductStatus status, int processId);
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

    public void Protect(ProductStatus status, int processId)
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
                    configuration.Ensure(status);
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
