using MW4Remastered.Core.Install;
using MW4Remastered.Core.Launch;
using MW4Remastered.Core.Media;

namespace MW4Remastered.Installer;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        var initialMediaPaths = ParseMediaPaths(args);
        var mediaSessions = new MediaSourceSessionFactory();
        var mediaInspection = new MediaInspectionService();
        var processStarter = new SystemProcessStarter();
        Application.Run(new InstallerForm(
            new MediaSourceInspector(mediaInspection, mediaSessions),
            new MediaSelectionSet(),
            new MediaSelectionSessionFactory(mediaSessions, mediaInspection),
            new InstallDestinationPlanner(),
            new InstalledLauncherOrchestrator(AppContext.BaseDirectory, processStarter),
            initialMediaPaths));
    }

    private static IReadOnlyList<string> ParseMediaPaths(IReadOnlyList<string> args)
    {
        var paths = new List<string>();
        for (var index = 0; index < args.Count - 1; index++)
        {
            if (!args[index].Equals("--media", StringComparison.OrdinalIgnoreCase)) continue;
            var path = args[++index];
            if (!string.IsNullOrWhiteSpace(path)) paths.Add(path);
        }
        return paths;
    }
}
