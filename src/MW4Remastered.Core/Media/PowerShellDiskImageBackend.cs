using System.Diagnostics;
using System.Text;

namespace MW4Remastered.Core.Media;

public sealed class PowerShellDiskImageBackend : IDiskImageBackend
{
    private static readonly TimeSpan CommandTimeout = TimeSpan.FromSeconds(45);
    private readonly string powerShellPath;

    public PowerShellDiskImageBackend() : this(WineRuntime.IsWine)
    {
    }

    internal PowerShellDiskImageBackend(bool runningUnderWine)
    {
        if (runningUnderWine)
            throw new InvalidOperationException(
                "Windows ISO mounting is unavailable under Wine. Extract each original game disc into a separate regular folder in Linux, then use Add folder to select those folders in Setup. Mounted disc roots and symbolic links are not supported. Keep the source folders available until installation finishes; Setup will not change or remove them.");
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("ISO mounting is supported only on Windows.");
        powerShellPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            "WindowsPowerShell", "v1.0", "powershell.exe");
        if (!File.Exists(powerShellPath)) throw new FileNotFoundException("Windows PowerShell was not found.", powerShellPath);
    }

    public bool IsAttached(string imagePath)
    {
        var output = Run($"$images=@(Get-DiskImage -ImagePath {Literal(imagePath)} -ErrorAction Stop); " +
            "if ($images.Count -ne 1) { throw 'Windows returned no unique disk-image record.' }; " +
            "if ($images[0].Attached) { 'attached' } else { 'detached' }");
        return ParseAttachmentState(output);
    }

    internal static bool ParseAttachmentState(string output)
    {
        return output.Trim() switch
        {
            "attached" => true,
            "detached" => false,
            "" => throw new InvalidDataException(
                "Windows returned no disk-image attachment state. Verify that the ISO is accessible and retry; Setup did not mount or change it."),
            _ => throw new InvalidDataException(
                $"Unexpected disk-image attachment state: {output.Trim()}"),
        };
    }

    public string Mount(string imagePath)
    {
        var output = Run($$"""
            $image = Mount-DiskImage -ImagePath {{Literal(imagePath)}} -Access ReadOnly -PassThru -ErrorAction Stop
            $wait = [Diagnostics.Stopwatch]::StartNew()
            do {
                foreach ($volume in ($image | Get-Volume -ErrorAction SilentlyContinue)) {
                    if ($volume.DriveLetter) {
                        $root = ([string]$volume.DriveLetter) + ':\'
                        if (Test-Path -LiteralPath $root -PathType Container) { $root; exit }
                    }
                }
                Start-Sleep -Milliseconds 250
            } while ($wait.Elapsed.TotalSeconds -lt 15)
            'not-ready'
            """).Trim();
        if (output == "not-ready")
            throw new IOException("Windows attached the ISO, but its disc drive did not become readable within 15 seconds. You can extract the disc and select it with Add folder.");
        if (output.Length != 3 || !char.IsAsciiLetter(output[0]) || output[1..] != @":\")
            throw new IOException("Windows did not return a usable drive letter for the mounted ISO.");
        return output;
    }

    public void Dismount(string imagePath)
    {
        var output = Run($$"""
            $path = {{Literal(imagePath)}}
            $wait = [Diagnostics.Stopwatch]::StartNew()
            do {
                $image = Get-DiskImage -ImagePath $path -ErrorAction Stop
                if (-not $image.Attached) { 'detached'; exit }
                Dismount-DiskImage -ImagePath $path -ErrorAction SilentlyContinue | Out-Null
                Start-Sleep -Milliseconds 250
            } while ($wait.Elapsed.TotalSeconds -lt 15)
            $image = Get-DiskImage -ImagePath $path -ErrorAction Stop
            if ($image.Attached) { 'attached' } else { 'detached' }
            """);
        if (ParseAttachmentState(output))
            throw new IOException("Windows did not release the temporary disc mount. Eject that virtual disc in Windows before retrying Setup.");
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
        if (process.ExitCode != 0) throw CommandFailure(process.ExitCode, error);
        return output;
    }

    internal static Exception CommandFailure(int exitCode, string standardError)
    {
        // Redirected Windows PowerShell can serialize errors as CLIXML, including
        // the user's full ISO and temporary paths. Classify without echoing it.
        if (standardError.Contains("Access denied", StringComparison.OrdinalIgnoreCase) ||
            standardError.Contains("0x80041003", StringComparison.OrdinalIgnoreCase))
            return new UnauthorizedAccessException(
                "Windows denied access to disk-image management. Start Setup normally and approve its UAC prompt.");
        return new IOException(
            $"Windows disk-image operation failed (PowerShell exit {exitCode}). Check that the ISO is accessible or extract the disc and select it with Add folder.");
    }

    private static string Literal(string value) => "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";
}
