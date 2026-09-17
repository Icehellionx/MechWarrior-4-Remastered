using System.Text;
using System.Diagnostics;
using MW4Remastered.Core.Install;

namespace MW4Remastered.Core.Launch;

public interface ILegacyGameConfiguration
{
    void Ensure(ProductStatus status);
}

public sealed class LegacyGameConfiguration : ILegacyGameConfiguration
{
    internal const int DefaultWidth = 1024;
    internal const int DefaultHeight = 768;

    private const string GraphicsPage = """
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
screenwidth=1024
screenheight=768
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

    public void Ensure(ProductStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);
        if (status.Product.Id is not ("vengeance" or "black-knight" or "mercenaries")) return;
        if (status.State != ProductInstallState.Ready || string.IsNullOrWhiteSpace(status.InstallPath))
            throw new InvalidOperationException($"{status.Product.DisplayName} does not have a verified configuration root.");

        var configurationRoot = status.Product.Id == "black-knight"
            ? Path.GetDirectoryName(status.LaunchPath
                ?? throw new InvalidOperationException("Black Knight does not have a verified launch path."))
            : status.InstallPath;
        var root = Path.GetFullPath(configurationRoot
            ?? throw new InvalidOperationException($"{status.Product.DisplayName} does not have a configuration root."));
        if (!Directory.Exists(root)) throw new DirectoryNotFoundException($"Verified game root is missing: {root}");
        if ((File.GetAttributes(root) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("Game configuration root cannot be a reparse point.");

        var fileName = status.Product.Id == "black-knight" ? "optionsx.ini" : "options.ini";
        var path = Path.Combine(root, fileName);
        var existing = File.Exists(path) ? ReadRegularFile(root, path) : string.Empty;
        var updated = HasGraphicsPage(existing)
            ? EnsureRequiredResolution(existing)
            : existing + (existing.Length == 0 || existing.EndsWith('\n') ? string.Empty : Environment.NewLine)
                + (existing.Length == 0 ? string.Empty : Environment.NewLine) + GraphicsPage + Environment.NewLine;
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

    private static string EnsureRequiredResolution(string value)
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
                lines[index] = $"ScreenWidth={DefaultWidth}";
                foundWidth = true;
            }
            else if (key.Equals("screenheight", StringComparison.OrdinalIgnoreCase))
            {
                lines[index] = $"ScreenHeight={DefaultHeight}";
                foundHeight = true;
            }
            else if (key.Equals("bitdepth", StringComparison.OrdinalIgnoreCase))
            {
                lines[index] = "bitdepth=32";
                foundDepth = true;
            }
        }

        var missing = new List<string>(3);
        if (!foundWidth) missing.Add($"ScreenWidth={DefaultWidth}");
        if (!foundHeight) missing.Add($"ScreenHeight={DefaultHeight}");
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
