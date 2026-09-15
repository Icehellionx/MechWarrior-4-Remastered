using MW4Remastered.Core;

namespace MW4Remastered.Launcher;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        var root = AppContext.BaseDirectory;
        Application.Run(new MainForm(new InstallStatusReader(root)));
    }
}
