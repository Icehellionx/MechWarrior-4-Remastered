using MW4Remastered.Core;
using MW4Remastered.Core.Install;
using MW4Remastered.Core.Launch;

namespace MW4Remastered.Launcher;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        var root = AppContext.BaseDirectory;
        var processStarter = new SystemProcessStarter();
        var gameRegistration = new LegacyGameRegistration();
        var joystickAdapterVerifier = new PinnedJoystickAdapterVerifier();
        using var gameWindowLifecycleGuard = new SystemGameWindowLifecycleGuard();
        Application.Run(new MainForm(
            new InstallStatusReader(root),
            new LaunchOrchestrator(processStarter, gameRegistration,
                gameConfiguration: new LegacyGameConfiguration(new ActiveMonitorResolutionProvider()),
                gameWindowLifecycleGuard: gameWindowLifecycleGuard,
                joystickAdapterVerifier: joystickAdapterVerifier),
            new DocumentOpener(processStarter),
            new OwnedInstallUninstaller(),
            new ApplicationUninstallOrchestrator(root, processStarter),
            gameRegistration,
            gameWindowLifecycleGuard,
            new GraphicsSettingsService(root),
            new InstallationDiagnosticsService(),
            new JoystickLaunchPreference(),
            joystickAdapterVerifier));
    }
}
