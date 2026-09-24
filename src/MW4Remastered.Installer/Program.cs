using System.Security.Principal;
using MW4Remastered.Core.Launch;

namespace MW4Remastered.Installer;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        InstallWorkerArguments? worker = null;
        try
        {
            worker = InstallWorkerArguments.Parse(args);
            if (!IsAdministrator())
            {
                throw new UnauthorizedAccessException(
                    "The game-installation worker requires administrative privileges. Restart Setup normally and approve the Windows User Account Control prompt.");
            }
            return new InstallWorker().Run(worker);
        }
        catch (ExistingGameRegistrationException error)
        {
            InstallWorker.TryAppendLog(worker?.LogPath, $"FAILED: {error}");
            return 2;
        }
        catch (Exception error) when (error is ArgumentException or IOException or UnauthorizedAccessException or
                                           InvalidDataException or InvalidOperationException or TimeoutException or
                                           System.ComponentModel.Win32Exception)
        {
            InstallWorker.TryAppendLog(worker?.LogPath, $"FAILED: {error}");
            return 1;
        }
    }

    private static bool IsAdministrator()
    {
        if (!OperatingSystem.IsWindows()) return false;
        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }
}
