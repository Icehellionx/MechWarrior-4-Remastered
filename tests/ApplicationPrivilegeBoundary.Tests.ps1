$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

function Assert-True {
    param([Parameter(Mandatory)][bool]$Condition, [Parameter(Mandatory)][string]$Message)
    if (-not $Condition) { throw $Message }
}

foreach ($component in @('MW4Remastered.Installer', 'MW4Remastered.Launcher', 'MW4Remastered.RtpPatchHost')) {
    $componentRoot = Join-Path $root "src/$component"
    $projectPath = Join-Path $componentRoot "$component.csproj"
    $manifestPath = Join-Path $componentRoot 'app.manifest'

    [xml]$project = Get-Content -LiteralPath $projectPath -Raw
    [xml]$manifest = Get-Content -LiteralPath $manifestPath -Raw
    $manifestProperty = $project.Project.PropertyGroup.ApplicationManifest
    $executionLevel = $manifest.SelectSingleNode("//*[local-name()='requestedExecutionLevel']")

    Assert-True ($manifestProperty -eq 'app.manifest') "$component must embed its checked-in manifest."
    Assert-True ($null -ne $executionLevel) "$component must declare an execution level."
    Assert-True ($executionLevel.level -eq 'asInvoker') "$component must never request routine elevation."
    Assert-True ($executionLevel.uiAccess -eq 'false') "$component must not request UIAccess."
}

Write-Host 'Application privilege boundary tests passed.'
