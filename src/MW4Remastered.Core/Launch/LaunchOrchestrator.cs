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
    private readonly ILegacyGameRegistration gameRegistration;

    public LaunchOrchestrator(IProcessStarter processStarter, ILegacyGameRegistration gameRegistration)
    {
        this.processStarter = processStarter ?? throw new ArgumentNullException(nameof(processStarter));
        this.gameRegistration = gameRegistration ?? throw new ArgumentNullException(nameof(gameRegistration));
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
        gameRegistration.Ensure(status);
        var workingDirectory = Path.GetDirectoryName(executable)!;
        var startInfo = new ProcessStartInfo
        {
            FileName = status.CompatibilityLaunchPath ?? executable,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
        };
        if (status.CompatibilityLaunchPath is not null)
        {
            var compatibilityLauncher = Path.GetFullPath(status.CompatibilityLaunchPath);
            if (!File.Exists(compatibilityLauncher))
            {
                throw new FileNotFoundException("Verified compatibility launcher is no longer present.", compatibilityLauncher);
            }
            if (!string.Equals(Path.GetDirectoryName(compatibilityLauncher), workingDirectory, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("Verified compatibility launcher is not adjacent to the game executable.");
            }
            startInfo.FileName = compatibilityLauncher;
            startInfo.ArgumentList.Add(Path.GetFileName(executable));
        }
        else if (status.Product.Id is "vengeance" or "mercenaries")
        {
            AddModernWindowsArguments(startInfo);
        }
        processStarter.Start(startInfo);
    }

    private static void AddModernWindowsArguments(ProcessStartInfo startInfo)
    {
        // Keep the legacy renderer in its qualified 32-bit OpenGL/windowed path,
        // skip startup movies, and avoid current DirectInput enumeration.
        startInfo.ArgumentList.Add("-32");
        startInfo.ArgumentList.Add("-window");
        startInfo.ArgumentList.Add("-f");
        startInfo.ArgumentList.Add("1024x768");
        startInfo.ArgumentList.Add("-gl");
        startInfo.ArgumentList.Add("-GameTime.MaxVariableFps");
        startInfo.ArgumentList.Add("60");
        startInfo.ArgumentList.Add("/gosnovideo");
        startInfo.ArgumentList.Add("/gosNoJoystick");
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

public sealed class ApplicationUninstallOrchestrator
{
    private const string UninstallerFileName = "unins000.exe";
    private readonly string applicationRoot;
    private readonly IProcessStarter processStarter;

    public ApplicationUninstallOrchestrator(string applicationRoot, IProcessStarter processStarter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationRoot);
        this.applicationRoot = Path.GetFullPath(applicationRoot);
        this.processStarter = processStarter ?? throw new ArgumentNullException(nameof(processStarter));
    }

    public bool IsAvailable => File.Exists(GetUninstallerPath());

    public void Start()
    {
        var uninstaller = GetUninstallerPath();
        if (!File.Exists(uninstaller))
        {
            throw new FileNotFoundException("The standard application uninstaller is not present. Use Windows Installed Apps to repair or remove the package.", uninstaller);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = uninstaller,
            WorkingDirectory = applicationRoot,
            UseShellExecute = false,
        };
        // The launcher already obtained explicit confirmation. Inno's silent mode
        // skips a second prompt but retains its progress window while it removes
        // the launcher, manuals, shortcuts, and registration from temporary storage.
        startInfo.ArgumentList.Add("/SILENT");
        startInfo.ArgumentList.Add("/NORESTART");
        processStarter.Start(startInfo);
    }

    private string GetUninstallerPath() => Path.Combine(applicationRoot, UninstallerFileName);
}

public sealed class InstalledLauncherOrchestrator
{
    private const string LauncherFileName = "MW4RemasteredLauncher.exe";
    private readonly string applicationRoot;
    private readonly IProcessStarter processStarter;

    public InstalledLauncherOrchestrator(string applicationRoot, IProcessStarter processStarter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationRoot);
        this.applicationRoot = Path.GetFullPath(applicationRoot);
        this.processStarter = processStarter ?? throw new ArgumentNullException(nameof(processStarter));
    }

    public bool IsAvailable => File.Exists(GetLauncherPath());

    public void Start()
    {
        var launcher = GetLauncherPath();
        if (!File.Exists(launcher)) throw new FileNotFoundException("The installed launcher is not present.", launcher);
        processStarter.Start(new ProcessStartInfo
        {
            FileName = launcher,
            WorkingDirectory = applicationRoot,
            UseShellExecute = false,
        });
    }

    private string GetLauncherPath() => Path.Combine(applicationRoot, LauncherFileName);
}
