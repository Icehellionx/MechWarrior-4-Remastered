$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$profile = Get-Content -LiteralPath (Join-Path $root 'assets/compatibility/dgVoodoo-MW4.conf') -Raw
$plan = Get-Content -LiteralPath (Join-Path $root 'src/MW4Remastered.Core/Install/LegacyPresentationCompatibility.cs') -Raw
$worker = Get-Content -LiteralPath (Join-Path $root 'src/MW4Remastered.Installer/InstallWorker.cs') -Raw
$launcher = Get-Content -LiteralPath (Join-Path $root 'src/MW4Remastered.Core/Launch/LaunchOrchestrator.cs') -Raw
$configuration = Get-Content -LiteralPath (Join-Path $root 'src/MW4Remastered.Core/Launch/LegacyGameConfiguration.cs') -Raw
$lock = Get-Content -LiteralPath (Join-Path $root 'third_party/dgVoodoo2.lock.json') -Raw | ConvertFrom-Json

function Assert-True {
    param([Parameter(Mandatory)][bool]$Condition, [Parameter(Mandatory)][string]$Message)
    if (-not $Condition) { throw $Message }
}

Assert-True ($lock.version -eq '2.86.5' -and $lock.archiveSha256 -eq '76b6893a0be81e3905a03f30f25202d6dc6128c3b8f7a21f2c33bcfabfa75ddf') 'dgVoodoo2 provenance must stay pinned to the evaluated complete archive.'
Assert-True ($lock.profileSha256 -eq '454ef91d0efeec8ea5302b57f72258a96736f923ffb34f7e893e245babdfa18e') 'The qualified MW4 dgVoodoo profile hash must remain recorded.'
Assert-True ($lock.files.'MS/x86/DDraw.dll' -eq '62f1e1b2ac5196f4a74b35b898ba8644322f976026324e2f767a4096bfb58748' -and $lock.productBindings.Count -eq 3) 'The exact stock x86 wrapper and all three product bindings must remain recorded.'
Assert-True ($profile -match 'OutputAPI\s*=\s*d3d11_fl11_0' -and $profile -match 'ScalingMode\s*=\s*stretched_ar' -and $profile -match 'FullscreenAttributes\s*=\s*fake') 'The profile must use stable D3D11 fake-fullscreen aspect-preserving scaling.'
Assert-True ($profile -match 'SystemHookFlags\s*=\s*cursor' -and $profile -match 'FPSLimit\s*=\s*30' -and $profile -match 'dgVoodooWatermark\s*=\s*false') 'The MW4-specific cursor, frame-pacing, and watermark settings must remain explicit.'
Assert-True ($configuration -match 'DefaultWidth\s*=\s*1024' -and $configuration -match 'DefaultHeight\s*=\s*768' -and $launcher -match 'startInfo\.ArgumentList\.Add\("-f"\)') 'Wrapped titles must request the legacy 4:3 surface for modern presentation.'
Assert-True ($launcher -notmatch '/gosnovideo') 'Original startup movies are part of the presentation contract and must remain enabled.'
Assert-True ($plan -match 'MW4X' -and $plan -match 'D3DImm\.dll' -and $plan -match 'b7401378b2b8e8c18c88a033e77c3ee99f4c7d2ac8cfcc949d79c1dd7fa99767') 'All three titles must receive the exact stock dgVoodoo DirectX files, including Black Knight under MW4X.'
Assert-True ($launcher -match 'productId == "black-knight"' -and $launcher -notmatch 'ArgumentList\.Add\("-window"\)') 'Black Knight must use dgVoodoo fullscreen presentation rather than the retired native window fallback.'
Assert-True ($worker -match 'Compatibility.*dgVoodoo2') 'The setup worker must route the packaged dgVoodoo2 bundle into owned game manifests.'
Assert-True ($worker -match 'OwnedInstallFileReplacementTransaction' -and $worker -match 'compatibilityReplacement\.Migrate' -and $worker -match 'DDrawCompat-MW4Mercs\.ini') 'Setup upgrades must atomically add the new wrapper files and retire only the old owned profiles.'

Write-Host 'Presentation compatibility contract tests passed.'
