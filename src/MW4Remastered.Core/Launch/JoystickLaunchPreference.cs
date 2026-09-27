namespace MW4Remastered.Core.Launch;

/// <summary>Per-user choice to let the original game enumerate joysticks at launch.</summary>
public sealed class JoystickLaunchPreference
{
    private readonly string preferencePath;

    public JoystickLaunchPreference(string? preferencePath = null)
    {
        this.preferencePath = preferencePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MechWarrior 4 Remastered", "joystick-enabled.txt");
    }

    public bool Read() => File.Exists(preferencePath) &&
        string.Equals(File.ReadAllText(preferencePath).Trim(), "1", StringComparison.Ordinal);

    public void Write(bool enabled)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(preferencePath)!);
        var temporaryPath = preferencePath + ".tmp";
        try
        {
            File.WriteAllText(temporaryPath, enabled ? "1\n" : "0\n");
            File.Move(temporaryPath, preferencePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }
}
