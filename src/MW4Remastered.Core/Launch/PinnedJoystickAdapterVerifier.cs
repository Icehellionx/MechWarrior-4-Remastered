using System.Security.Cryptography;

namespace MW4Remastered.Core.Launch;

public interface IJoystickAdapterVerifier
{
    void EnsureAvailable(string executablePath);
}

/// <summary>Requires the reviewed x86 DirectInput adapter beside the selected game.</summary>
public sealed class PinnedJoystickAdapterVerifier : IJoystickAdapterVerifier
{
    public const string AdapterSha256 = "f96bb1101e23a6477043a3859cbf875a990eaa62ee6849f87e967b3dd963f781";

    public void EnsureAvailable(string executablePath)
    {
        var adapterPath = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(executablePath))!, "dinput.dll");
        if (!File.Exists(adapterPath))
            throw new InvalidOperationException("Joystick mode needs the verified DirectInput adapter beside the game. This install does not have it yet.");
        using var stream = File.OpenRead(adapterPath);
        var actualHash = Convert.ToHexString(SHA256.HashData(stream));
        if (!actualHash.Equals(AdapterSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Joystick mode found an unrecognized dinput.dll beside the game. The game was not started.");
    }
}
