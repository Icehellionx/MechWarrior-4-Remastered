using MW4Remastered.Core.Media;

namespace MW4Remastered.Installer;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new InstallerForm(new MediaSourceInspector(), new MediaSelectionSet()));
    }
}
