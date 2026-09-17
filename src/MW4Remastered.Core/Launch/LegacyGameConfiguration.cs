using System.Text;
using MW4Remastered.Core.Install;

namespace MW4Remastered.Core.Launch;

public sealed class LegacyGameConfiguration
{
    internal const int DefaultWidth = 1920;
    internal const int DefaultHeight = 1080;

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
screenwidth=1920
screenheight=1080
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

        var root = Path.GetFullPath(status.InstallPath);
        if (!Directory.Exists(root)) throw new DirectoryNotFoundException($"Verified game root is missing: {root}");
        if ((File.GetAttributes(root) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("Game configuration root cannot be a reparse point.");

        var fileName = status.Product.Id == "black-knight" ? "optionsx.ini" : "options.ini";
        var path = Path.Combine(root, fileName);
        var existing = File.Exists(path) ? ReadRegularFile(root, path) : string.Empty;
        if (HasGraphicsPage(existing)) return;

        var separator = existing.Length == 0 || existing.EndsWith('\n') ? string.Empty : Environment.NewLine;
        var updated = existing + separator + (existing.Length == 0 ? string.Empty : Environment.NewLine) + GraphicsPage + Environment.NewLine;
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
}
