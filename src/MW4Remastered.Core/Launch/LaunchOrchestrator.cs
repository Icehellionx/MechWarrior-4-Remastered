using System.Diagnostics;

namespace MW4Remastered.Core.Launch;

public interface IProcessStarter
{
    void Start(ProcessStartInfo startInfo);
}

public sealed class SystemProcessStarter : IProcessStarter
{
    public void Start(ProcessStartInfo startInfo)
    {
        Process.Start(startInfo);
    }
}

public sealed class LaunchOrchestrator
{
    private readonly IProcessStarter processStarter;

    public LaunchOrchestrator(IProcessStarter processStarter)
    {
        this.processStarter = processStarter ?? throw new ArgumentNullException(nameof(processStarter));
    }

    public void Launch(ProductStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);
        if (status.State != ProductInstallState.Ready || string.IsNullOrWhiteSpace(status.LaunchPath))
        {
            throw new InvalidOperationException($"{status.Product.DisplayName} is not a verified, launchable installation.");
        }

        var executable = Path.GetFullPath(status.LaunchPath);
        if (!File.Exists(executable)) throw new FileNotFoundException("Verified game executable is no longer present.", executable);
        processStarter.Start(new ProcessStartInfo
        {
            FileName = executable,
            WorkingDirectory = Path.GetDirectoryName(executable)!,
            UseShellExecute = false,
        });
    }
}

public sealed class DocumentOpener
{
    private readonly IProcessStarter processStarter;

    public DocumentOpener(IProcessStarter processStarter)
    {
        this.processStarter = processStarter ?? throw new ArgumentNullException(nameof(processStarter));
    }

    public void Open(string documentPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentPath);
        var document = Path.GetFullPath(documentPath);
        if (!File.Exists(document)) throw new FileNotFoundException("Document is no longer present.", document);
        processStarter.Start(new ProcessStartInfo
        {
            FileName = document,
            WorkingDirectory = Path.GetDirectoryName(document)!,
            UseShellExecute = true,
        });
    }
}
