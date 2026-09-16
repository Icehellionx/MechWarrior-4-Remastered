using Microsoft.Win32;

namespace MW4Remastered.Core.Launch;

public interface ILegacyGameRegistration
{
    void Ensure(ProductStatus status);
    void ValidateOwned(ProductStatus status);
    void RemoveOwned(ProductStatus status);
}

public sealed class LegacyGameRegistration : ILegacyGameRegistration
{
    public void Ensure(ProductStatus status)
    {
        var registration = Describe(status);
        if (registration is null) return;
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Legacy MechWarrior 4 registration is supported only on Windows.");

        var view = registration.Use32BitView ? RegistryView.Registry32 : RegistryView.Default;
        using var currentUser = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, view);
        using var key = currentUser.CreateSubKey(registration.KeyPath, writable: true) ??
            throw new UnauthorizedAccessException("Windows did not permit the per-user MechWarrior 4 compatibility registration.");
        var existingExecutable = key.GetValue("EXE Path") as string;
        if (!string.IsNullOrWhiteSpace(existingExecutable) &&
            !PathsEqual(existingExecutable, registration.ExecutablePath))
        {
            throw new InvalidOperationException(
                $"A different {status.Product.DisplayName} installation already owns the per-user compatibility registration: {existingExecutable}");
        }

        key.SetValue("CDPath", registration.CdPath, RegistryValueKind.String);
        key.SetValue("EXE Path", registration.ExecutablePath, RegistryValueKind.String);
        key.SetValue("Version", registration.Version, RegistryValueKind.DWord);
        // Setup obtains explicit acceptance before this registration is created.
        // Recording it here keeps the original first-run dialog out of gameplay.
        key.SetValue("FIRSTRUN", 1, RegistryValueKind.DWord);
    }

    public void ValidateOwned(ProductStatus status)
    {
        var registration = Describe(status);
        if (registration is null) return;
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Legacy MechWarrior 4 registration is supported only on Windows.");

        var view = registration.Use32BitView ? RegistryView.Registry32 : RegistryView.Default;
        using var currentUser = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, view);
        using var key = currentUser.OpenSubKey(registration.KeyPath, writable: false);
        if (key is null ||
            !PathsEqual(key.GetValue("CDPath") as string, registration.CdPath) ||
            !PathsEqual(key.GetValue("EXE Path") as string, registration.ExecutablePath) ||
            Convert.ToInt32(key.GetValue("Version", 0)) != registration.Version)
        {
            throw new InvalidOperationException(
                $"{status.Product.DisplayName} setup registration is missing or belongs to another installation. Run Setup again to repair it.");
        }
    }

    public void RemoveOwned(ProductStatus status)
    {
        var registration = Describe(status);
        if (registration is null || !OperatingSystem.IsWindows()) return;

        var view = registration.Use32BitView ? RegistryView.Registry32 : RegistryView.Default;
        using var currentUser = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, view);
        using (var key = currentUser.OpenSubKey(registration.KeyPath, writable: true))
        {
            if (key is null || !PathsEqual(key.GetValue("EXE Path") as string, registration.ExecutablePath)) return;
            key.DeleteValue("CDPath", throwOnMissingValue: false);
            key.DeleteValue("EXE Path", throwOnMissingValue: false);
            key.DeleteValue("Version", throwOnMissingValue: false);
        }
    }

    internal static LegacyRegistrationDescription? Describe(ProductStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);
        if (status.Product.Id is not ("vengeance" or "black-knight" or "mercenaries")) return null;
        if (string.IsNullOrWhiteSpace(status.InstallPath) || string.IsNullOrWhiteSpace(status.LaunchPath))
            throw new InvalidOperationException($"{status.Product.DisplayName} does not have a verified registration path.");

        var (keyPath, use32BitView) = status.Product.Id switch
        {
            "vengeance" => (@"Software\Microsoft\Microsoft Games\MechWarrior Vengeance", true),
            "black-knight" => (@"Software\Microsoft\Microsoft Games\MechWarrior Black Knight", true),
            _ => (@"Software\Microsoft\Microsoft Games\MechWarrior Mercenaries", true),
        };
        var installPath = Path.GetFullPath(status.InstallPath);
        return new LegacyRegistrationDescription(
            keyPath,
            use32BitView,
            status.Product.Id == "black-knight" ? @"L:\" : installPath,
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
    string CdPath,
    string ExecutablePath,
    int Version);
