using System.Security.Cryptography;

namespace MW4Remastered.Core.Launch;

public interface IJoystickAdapterVerifier
{
    void EnsureAvailable(string executablePath);
}

/// <summary>Requires the reviewed x86 DirectInput adapter beside the selected game.</summary>
public sealed class PinnedJoystickAdapterVerifier : IJoystickAdapterVerifier
{
    public const string AdapterSha256 = MW4Remastered.Core.Install.LegacyInputCompatibility.AdapterSha256;

    public void EnsureAvailable(string executablePath)
    {
        var adapterPath = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(executablePath))!, "dinput.dll");
        if (!File.Exists(adapterPath))
            throw new InvalidOperationException("Joystick mode needs the verified DirectInput adapter beside this game's executable. Run Repair to restore it.");
        using var stream = File.OpenRead(adapterPath);
        var actualHash = Convert.ToHexString(SHA256.HashData(stream));
        if (!actualHash.Equals(AdapterSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Joystick mode found an unrecognized dinput.dll beside the game. The game was not started.");
    }
}
