using System.Diagnostics;
using System.Text;

namespace MW4Remastered.Core.Media;

public sealed class PowerShellDiskImageBackend : IDiskImageBackend
{
    private static readonly TimeSpan CommandTimeout = TimeSpan.FromSeconds(30);
    private readonly string powerShellPath;

    public PowerShellDiskImageBackend()
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("ISO mounting is supported only on Windows.");
        powerShellPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            "WindowsPowerShell", "v1.0", "powershell.exe");
        if (!File.Exists(powerShellPath)) throw new FileNotFoundException("Windows PowerShell was not found.", powerShellPath);
    }

    public bool IsAttached(string imagePath)
    {
        var output = Run($"$image=Get-DiskImage -ImagePath {Literal(imagePath)} -ErrorAction Stop; if ($image.Attached) {{ 'attached' }} else {{ 'detached' }}");
        return output.Trim() switch
        {
            "attached" => true,
            "detached" => false,
            _ => throw new InvalidDataException($"Unexpected disk-image query output: {output.Trim()}"),
        };
    }

    public string Mount(string imagePath)
    {
        var output = Run(
            $"$image=Mount-DiskImage -ImagePath {Literal(imagePath)} -Access ReadOnly -PassThru -ErrorAction Stop; " +
            "$volume=$image | Get-Volume -ErrorAction Stop; " +
            "if (-not $volume.DriveLetter) { throw 'Mounted image has no drive letter.' }; " +
            "$volume.DriveLetter + ':\\'");
        return output.Trim();
    }

    public void Dismount(string imagePath)
    {
        Run(
            $"$image=Get-DiskImage -ImagePath {Literal(imagePath)} -ErrorAction Stop; " +
            $"if ($image.Attached) {{ Dismount-DiskImage -ImagePath {Literal(imagePath)} -ErrorAction Stop | Out-Null }}");
    }

    private string Run(string script)
    {
        var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes("$ErrorActionPreference='Stop'; " + script));
        var startInfo = new ProcessStartInfo
        {
            FileName = powerShellPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add("-NoLogo");
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-NonInteractive");
        startInfo.ArgumentList.Add("-EncodedCommand");
        startInfo.ArgumentList.Add(encoded);

        using var process = Process.Start(startInfo) ?? throw new IOException("Failed to start Windows PowerShell.");
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(CommandTimeout))
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit();
            throw new IOException($"Disk-image command exceeded the {CommandTimeout.TotalSeconds:0}-second timeout.");
        }
        var output = outputTask.GetAwaiter().GetResult();
        var error = errorTask.GetAwaiter().GetResult();
        if (process.ExitCode != 0) throw new IOException($"Disk-image command failed with code {process.ExitCode}: {error.Trim()}");
        return output;
    }

    private static string Literal(string value) => "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";
}
