using Microsoft.Win32;

namespace MW4Remastered.Core.Launch;

public interface ILegacyGameRegistration
{
    void Ensure(ProductStatus status);
    void RemoveOwned(ProductStatus status);
}

public sealed class LegacyGameRegistration : ILegacyGameRegistration
{
    private const string VirtualStorePrefix =
        @"Software\Classes\VirtualStore\MACHINE\SOFTWARE\WOW6432Node\Microsoft\Microsoft Games";

    public void Ensure(ProductStatus status)
    {
        var registration = Describe(status);
        if (registration is null) return;
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Legacy MechWarrior 4 registration is supported only on Windows.");

        // The retail games query a virtualized 32-bit HKLM path. VirtualStore's
        // physical HKCU fallback includes WOW6432Node; selecting Registry32 on
        // HKCU alone does not add that segment.
        var view = registration.Use32BitView ? RegistryView.Registry32 : RegistryView.Default;
        using var currentUser = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, view);
        using var key = currentUser.CreateSubKey(registration.KeyPath, writable: true) ??
            throw new UnauthorizedAccessException("Windows did not permit the per-user MechWarrior 4 compatibility registration.");
        var existingPath = key.GetValue("CDPath") as string;
        if (!string.IsNullOrWhiteSpace(existingPath) &&
            !PathsEqual(existingPath, registration.InstallPath))
        {
            throw new InvalidOperationException(
                $"A different {status.Product.DisplayName} installation already owns the per-user compatibility registration: {existingPath}");
        }

        key.SetValue("CDPath", registration.InstallPath, RegistryValueKind.String);
        key.SetValue("EXE Path", registration.ExecutablePath, RegistryValueKind.String);
        key.SetValue("Version", registration.Version, RegistryValueKind.DWord);
        // Zero asks the original game to present its EULA. Preserve a value the
        // game has already written so accepting it once is not undone on every
        // subsequent launch.
        if (key.GetValue("FIRSTRUN") is null)
        {
            key.SetValue("FIRSTRUN", 0, RegistryValueKind.DWord);
        }
    }

    public void RemoveOwned(ProductStatus status)
    {
        var registration = Describe(status);
        if (registration is null || !OperatingSystem.IsWindows()) return;

        var view = registration.Use32BitView ? RegistryView.Registry32 : RegistryView.Default;
        using var currentUser = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, view);
        using (var key = currentUser.OpenSubKey(registration.KeyPath, writable: false))
        {
            if (key is null || !PathsEqual(key.GetValue("CDPath") as string, registration.InstallPath)) return;
        }
        currentUser.DeleteSubKeyTree(registration.KeyPath, throwOnMissingSubKey: false);
    }

    internal static LegacyRegistrationDescription? Describe(ProductStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);
        if (status.Product.Id is not ("vengeance" or "black-knight" or "mercenaries")) return null;
        if (string.IsNullOrWhiteSpace(status.InstallPath) || string.IsNullOrWhiteSpace(status.LaunchPath))
            throw new InvalidOperationException($"{status.Product.DisplayName} does not have a verified registration path.");

        var (keyPath, use32BitView) = status.Product.Id switch
        {
            "vengeance" => ($@"{VirtualStorePrefix}\MechWarrior Vengeance", false),
            "black-knight" => (@"Software\Microsoft\Microsoft Games\MechWarrior Black Knight", true),
            _ => (@"Software\Microsoft\Microsoft Games\MechWarrior Mercenaries", true),
        };
        return new LegacyRegistrationDescription(
            keyPath,
            use32BitView,
            Path.GetFullPath(status.InstallPath),
            Path.GetFullPath(status.LaunchPath),
            4);
    }

    private static bool PathsEqual(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right)) return false;
        try
        {
            return string.Equals(
                Path.TrimEndingDirectorySeparator(Path.GetFullPath(left)),
                Path.TrimEndingDirectorySeparator(Path.GetFullPath(right)),
                StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception error) when (error is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }
}

internal sealed record LegacyRegistrationDescription(
    string KeyPath,
    bool Use32BitView,
    string InstallPath,
    string ExecutablePath,
    int Version);
