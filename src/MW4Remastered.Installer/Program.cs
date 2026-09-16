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
            return new InstallWorker().Run(worker);
        }
        catch (Exception error) when (error is ArgumentException or IOException or UnauthorizedAccessException or
                                           InvalidDataException or InvalidOperationException or TimeoutException or
                                           System.ComponentModel.Win32Exception)
        {
            InstallWorker.TryAppendLog(worker?.LogPath, $"FAILED: {error}");
            return 1;
        }
    }
}
