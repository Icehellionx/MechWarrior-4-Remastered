using MW4Remastered.Core;
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
        Application.Run(new MainForm(
            new InstallStatusReader(root),
            new LaunchOrchestrator(processStarter),
            new DocumentOpener(processStarter)));
    }
}
