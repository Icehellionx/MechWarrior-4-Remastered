[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [hashtable]$Images
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$probe = Join-Path $repositoryRoot 'src\MW4Remastered.MediaProbe\bin\Release\net10.0\MW4Remastered.MediaProbe.dll'
if (-not (Test-Path -LiteralPath $probe -PathType Leaf)) {
    throw "Build the media probe before running this smoke test: $probe"
}

$failures = [Collections.Generic.List[string]]::new()
foreach ($expectedLayout in ($Images.Keys | Sort-Object)) {
    $imagePath = [IO.Path]::GetFullPath([string]$Images[$expectedLayout])
    if (-not (Test-Path -LiteralPath $imagePath -PathType Leaf)) {
        $failures.Add("Missing image for ${expectedLayout}: $imagePath")
        continue
    }

    $ownedMount = $false
    try {
        $existing = Get-DiskImage -ImagePath $imagePath -ErrorAction Stop
        if ($existing.Attached) {
            $failures.Add("Refusing to reuse pre-attached image: $imagePath")
            continue
        }

        $image = Mount-DiskImage -ImagePath $imagePath -PassThru -ErrorAction Stop
        $ownedMount = $true
        $volume = $image | Get-Volume
        if (-not $volume.DriveLetter) { throw "Mounted image has no drive letter: $imagePath" }

        $output = @(& dotnet $probe ($volume.DriveLetter + ':\'))
        if ($LASTEXITCODE -ne 0) {
            $failures.Add("Probe rejected ${expectedLayout}: $($output -join ' | ')")
            continue
        }

        $actualLayout = ($output | Where-Object { $_ -like 'Layout: *' } | Select-Object -First 1) -replace '^Layout:\s*', ''
        if ($actualLayout -ne $expectedLayout) {
            $failures.Add("Expected $expectedLayout but recognized $actualLayout for $imagePath")
        }
    }
    finally
    {
        if ($ownedMount) { Dismount-DiskImage -ImagePath $imagePath -ErrorAction Stop | Out-Null }
    }
}

foreach ($imagePath in $Images.Values) {
    $resolved = [IO.Path]::GetFullPath([string]$imagePath)
    if ((Get-DiskImage -ImagePath $resolved -ErrorAction Stop).Attached) {
        $failures.Add("Image remained attached after smoke test: $resolved")
    }
}

if ($failures.Count -gt 0) { throw ($failures -join [Environment]::NewLine) }
Write-Host "Media recognition smoke passed for $($Images.Count) image(s); all owned mounts were released."
