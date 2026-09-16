$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $root 'src/MW4Remastered.CompatLauncher/MW4Remastered.CompatLauncher.csproj'
$manifestPath = Join-Path $root 'src/MW4Remastered.CompatLauncher/app.manifest'
$sourcePath = Join-Path $root 'src/MW4Remastered.CompatLauncher/Program.cs'

[xml]$project = Get-Content -LiteralPath $projectPath -Raw
[xml]$manifest = Get-Content -LiteralPath $manifestPath -Raw
$source = Get-Content -LiteralPath $sourcePath -Raw

function Assert-True {
    param([Parameter(Mandatory)][bool]$Condition, [Parameter(Mandatory)][string]$Message)
    if (-not $Condition) { throw $Message }
}

$properties = $project.Project.PropertyGroup
Assert-True ($properties.PlatformTarget -eq 'x86') 'Compatibility launcher must remain 32-bit for the 32-bit MW4 processes.'
Assert-True ($properties.RuntimeIdentifier -eq 'win-x86') 'Compatibility launcher must retain the win-x86 runtime identifier.'
Assert-True ($properties.IncludeSourceRevisionInInformationalVersion -eq 'false') 'Compatibility helper output must not create a circular hash dependency on the repository commit.'

$executionLevel = $manifest.SelectSingleNode("//*[local-name()='requestedExecutionLevel']")
Assert-True ($null -ne $executionLevel) 'Compatibility launcher must declare an execution level.'
Assert-True ($executionLevel.level -eq 'asInvoker') 'Game launch must not request elevation.'
Assert-True ($source -notmatch 'PROCESS_ALL_ACCESS') 'Compatibility launcher must not request unrestricted process access.'
Assert-True ($source -match '"MW4\.exe"' -and $source -match '"MW4X\.exe"' -and $source -match '"MW4Mercs\.exe"') 'Compatibility launcher allowlist must cover exactly the three product executables.'
Assert-True ($source -match 'Path\.Combine\(baseDirectory, "version\.dll"\)') 'Compatibility DLL selection must remain adjacent and fixed.'
Assert-True ($source -match 'TerminateProcess\(processInformation\.Process, 1\)') 'A failed setup must terminate the suspended child.'

Write-Host 'Compatibility launcher boundary tests passed.'
