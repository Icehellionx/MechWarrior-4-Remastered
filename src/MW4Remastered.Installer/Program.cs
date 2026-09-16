using MW4Remastered.Core.Install;
using MW4Remastered.Core.Media;

namespace MW4Remastered.Installer;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        var mediaSessions = new MediaSourceSessionFactory();
        var mediaInspection = new MediaInspectionService();
        Application.Run(new InstallerForm(
            new MediaSourceInspector(mediaInspection, mediaSessions),
            new MediaSelectionSet(),
            new MediaSelectionSessionFactory(mediaSessions, mediaInspection),
            new InstallDestinationPlanner()));
    }
}
