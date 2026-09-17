$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$profile = Get-Content -LiteralPath (Join-Path $root 'assets/compatibility/DDrawCompat-MW4.ini') -Raw
$plan = Get-Content -LiteralPath (Join-Path $root 'src/MW4Remastered.Core/Install/LegacyPresentationCompatibility.cs') -Raw
$worker = Get-Content -LiteralPath (Join-Path $root 'src/MW4Remastered.Installer/InstallWorker.cs') -Raw
$launcher = Get-Content -LiteralPath (Join-Path $root 'src/MW4Remastered.Core/Launch/LaunchOrchestrator.cs') -Raw
$configuration = Get-Content -LiteralPath (Join-Path $root 'src/MW4Remastered.Core/Launch/LegacyGameConfiguration.cs') -Raw
$lock = Get-Content -LiteralPath (Join-Path $root 'third_party/DDrawCompat-MW3.lock.json') -Raw | ConvertFrom-Json

function Assert-True {
    param([Parameter(Mandatory)][bool]$Condition, [Parameter(Mandatory)][string]$Message)
    if (-not $Condition) { throw $Message }
}

Assert-True ($lock.commit -eq '73ac0f47af16a1d28dcda25c3228053beb3eb5f4' -and $lock.dllSha256 -eq 'b589c27402c283f699857aec26948b33595ee93f645891ec4f9607254148b509') 'DDrawCompat provenance must stay pinned to the qualified MW4-patched r18 build.'
Assert-True ($lock.mw4PatchSha256 -eq 'ad88d82ae9ec03be4a85c7c08ae3dd1e3c9c47fc88821dafdbb2460a42e44d98' -and $lock.reproducibility -match 'byte-identical') 'The exact MW4 patch and clean-build reproducibility evidence must remain recorded.'
Assert-True ($profile -match 'DisplayAspectRatio\s*=\s*4:3' -and $profile -match 'ResolutionScale\s*=\s*display\(1\)' -and $profile -match 'SupportedResolutions\s*=\s*800x600, 1024x768') 'The profile must preserve a 4:3 legacy surface and desktop scaling.'
Assert-True ($profile -match 'AltTabFix\s*=\s*noactivateapp\(1\)') 'MW4 must preserve video-memory surfaces across Alt-Tab while still delivering focus notifications to the game.'
Assert-True ($configuration -match 'DefaultWidth\s*=\s*1024' -and $configuration -match 'DefaultHeight\s*=\s*768' -and $launcher -match 'startInfo\.ArgumentList\.Add\("-f"\)') 'Wrapped titles must request the legacy 4:3 surface without entering native exclusive fullscreen.'
Assert-True ($launcher -match '/gosnovideo') 'Startup movies must remain disabled until the DirectShow/DDraw presentation path is qualified.'
Assert-True ($plan -notmatch 'MW4X/ddraw\.dll' -and $plan -notmatch 'DDrawCompat-MW4X\.ini' -and $plan -match 'DDrawCompat-MW4Mercs\.ini') 'Vengeance and Mercenaries receive app-local presentation wrappers; Black Knight must not receive the proxy that conflicts with its PR1 runtime.'
Assert-True ($launcher -match 'productId == "black-knight"' -and $launcher -match 'ArgumentList\.Add\("-window"\)') 'Black Knight must retain its independently proven minimal windowed fallback.'
Assert-True ($worker -match 'Compatibility.*DDrawCompat') 'The setup worker must route the packaged presentation bundle into owned game manifests.'

Write-Host 'Presentation compatibility contract tests passed.'
