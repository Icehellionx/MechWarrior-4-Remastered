$ErrorActionPreference = 'Stop'
$root = Join-Path ([IO.Path]::GetTempPath()) ('mw4-release-policy-' + [Guid]::NewGuid().ToString('N'))

function Assert-Rejected {
    param([Parameter(Mandatory)][scriptblock]$Action, [Parameter(Mandatory)][string]$ExpectedPattern)
    try {
        & $Action
        throw "Expected rejection matching: $ExpectedPattern"
    }
    catch {
        if ($_.Exception.Message -notmatch $ExpectedPattern) { throw }
    }
}

try {
    New-Item -ItemType Directory -Path $root | Out-Null
    [IO.File]::WriteAllText((Join-Path $root 'README.txt'), 'synthetic fixture')
    [IO.File]::WriteAllText((Join-Path $root 'Launcher.exe'), 'synthetic fixture')
    & "$PSScriptRoot\..\tools\assert-release-tree.ps1" -Root $root -AllowedExecutablePaths 'Launcher.exe'

    [IO.File]::WriteAllText((Join-Path $root 'game.iso'), 'synthetic fixture')
    Assert-Rejected { & "$PSScriptRoot\..\tools\assert-release-tree.ps1" -Root $root -AllowedExecutablePaths 'Launcher.exe' } 'forbidden extension'
    Remove-Item -LiteralPath (Join-Path $root 'game.iso') -Force

    [IO.File]::WriteAllText((Join-Path $root 'unknown.exe'), 'synthetic fixture')
    Assert-Rejected { & "$PSScriptRoot\..\tools\assert-release-tree.ps1" -Root $root -AllowedExecutablePaths 'Launcher.exe' } 'not allowlisted'
    Remove-Item -LiteralPath (Join-Path $root 'unknown.exe') -Force

    [IO.File]::WriteAllText((Join-Path $root 'SECDRV.SYS'), 'synthetic fixture')
    Assert-Rejected { & "$PSScriptRoot\..\tools\assert-release-tree.ps1" -Root $root -AllowedExecutablePaths 'Launcher.exe' } 'forbidden filename'
    Remove-Item -LiteralPath (Join-Path $root 'SECDRV.SYS') -Force

    [IO.File]::WriteAllText((Join-Path $root '.env.production'), 'synthetic fixture')
    Assert-Rejected { & "$PSScriptRoot\..\tools\assert-release-tree.ps1" -Root $root -AllowedExecutablePaths 'Launcher.exe' } 'environment/secret file'
    Remove-Item -LiteralPath (Join-Path $root '.env.production') -Force

    New-Item -ItemType Directory -Path (Join-Path $root 'Crack') | Out-Null
    [IO.File]::WriteAllText((Join-Path $root 'Crack\note.txt'), 'synthetic fixture')
    Assert-Rejected { & "$PSScriptRoot\..\tools\assert-release-tree.ps1" -Root $root -AllowedExecutablePaths 'Launcher.exe' } 'forbidden path segment'

    Write-Host 'Release tree policy tests passed.'
}
finally
{
    if (Test-Path -LiteralPath $root) {
        $resolved = (Resolve-Path -LiteralPath $root).Path
        $temp = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
        if (-not $resolved.StartsWith($temp, [StringComparison]::OrdinalIgnoreCase)) { throw "Unsafe test cleanup path: $resolved" }
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
}
