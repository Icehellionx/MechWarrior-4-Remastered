using System.Diagnostics;

namespace MW4Remastered.Core.Launch;

public interface IProcessStarter
{
    int Start(ProcessStartInfo startInfo);
}

public interface IGameProcessState
{
    bool IsRunning(string executablePath);
}

public sealed class SystemGameProcessState : IGameProcessState
{
    public bool IsRunning(string executablePath)
    {
        var fullPath = Path.GetFullPath(executablePath);
        var processName = Path.GetFileNameWithoutExtension(fullPath);
        foreach (var process in Process.GetProcessesByName(processName))
        {
            using (process)
            {
                try
                {
                    if (!process.HasExited && string.Equals(
                            process.MainModule?.FileName,
                            fullPath,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                catch (Exception error) when (error is InvalidOperationException or System.ComponentModel.Win32Exception)
                {
                    // A process that exits during inspection or belongs to a
                    // different security boundary cannot prove this exact game
                    // tree is already running.
                }
            }
        }
        return false;
    }
}

public sealed class SystemProcessStarter : IProcessStarter
{
    public int Start(ProcessStartInfo startInfo)
    {
        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Windows did not start {startInfo.FileName}.");
        return process.Id;
    }
}

public sealed class LaunchOrchestrator
{
    private readonly IProcessStarter processStarter;
    private readonly ILegacyGameRegistration gameRegistration;
    private readonly IGameProcessState gameProcessState;
    private readonly ILegacyGameConfiguration gameConfiguration;
    private readonly IGameConfigurationGuard gameConfigurationGuard;

    public LaunchOrchestrator(
        IProcessStarter processStarter,
        ILegacyGameRegistration gameRegistration,
        IGameProcessState? gameProcessState = null,
        ILegacyGameConfiguration? gameConfiguration = null,
        IGameConfigurationGuard? gameConfigurationGuard = null)
    {
        this.processStarter = processStarter ?? throw new ArgumentNullException(nameof(processStarter));
        this.gameRegistration = gameRegistration ?? throw new ArgumentNullException(nameof(gameRegistration));
        this.gameProcessState = gameProcessState ?? new SystemGameProcessState();
        this.gameConfiguration = gameConfiguration ?? new LegacyGameConfiguration();
        this.gameConfigurationGuard = gameConfigurationGuard ?? new LegacyGameConfigurationGuard(this.gameConfiguration);
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
        if (gameProcessState.IsRunning(executable))
        {
            throw new InvalidOperationException($"{status.Product.DisplayName} is already running.");
        }
        // Setup owns registry mutation. Normal launch only validates the record,
        // but configuration remains user-owned and MW4 can erase its graphics
        // page while booting when the obsolete autoconfigurator is bypassed.
        gameRegistration.ValidateOwned(status);
        gameConfiguration.Ensure(status);
        // Black Knight is installed below the shared Vengeance tree but resolves
        // its shell resources and optionsx.ini from that parent at runtime.
        var workingDirectory = status.Product.Id == "black-knight"
            ? Path.GetFullPath(status.InstallPath!)
            : Path.GetDirectoryName(executable)!;
        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
        };
        if (status.Product.Id is "vengeance" or "black-knight" or "mercenaries")
        {
            AddModernWindowsArguments(startInfo, status.Product.Id);
        }
        var processId = processStarter.Start(startInfo);
        gameConfigurationGuard.Protect(status, processId);
    }

    private static void AddModernWindowsArguments(ProcessStartInfo startInfo, string productId)
    {
        if (productId == "black-knight")
        {
            // Black Knight uses the expansion-specific AutoConfig bypass. dgVoodoo
            // owns modern fullscreen presentation beside MW4X.exe, so the launcher
            // must not force the old native windowed fallback.
            startInfo.ArgumentList.Add("-noautoconfigx");
            startInfo.ArgumentList.Add("/gosNoJoystick");
            return;
        }

        // Let dgVoodoo virtualize the game's fullscreen request through D3D11.
        startInfo.ArgumentList.Add("-32");
        startInfo.ArgumentList.Add("-noautoconfig");
        startInfo.ArgumentList.Add("-f");
        startInfo.ArgumentList.Add($"{LegacyGameConfiguration.DefaultWidth}x{LegacyGameConfiguration.DefaultHeight}");
        startInfo.ArgumentList.Add("-gl");
        startInfo.ArgumentList.Add("-GameTime.MaxVariableFps");
        startInfo.ArgumentList.Add("60");
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
