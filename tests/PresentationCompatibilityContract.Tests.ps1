$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$profile = Get-Content -LiteralPath (Join-Path $root 'assets/compatibility/dgVoodoo-MW4.conf') -Raw
$plan = Get-Content -LiteralPath (Join-Path $root 'src/MW4Remastered.Core/Install/LegacyPresentationCompatibility.cs') -Raw
$worker = Get-Content -LiteralPath (Join-Path $root 'src/MW4Remastered.Installer/InstallWorker.cs') -Raw
$launcher = Get-Content -LiteralPath (Join-Path $root 'src/MW4Remastered.Core/Launch/LaunchOrchestrator.cs') -Raw
$lifecycle = Get-Content -LiteralPath (Join-Path $root 'src/MW4Remastered.Core/Launch/GameWindowLifecycleGuard.cs') -Raw
$lifecycleProbe = Get-Content -LiteralPath (Join-Path $root 'tools/compatibility/WindowLifecycleProbe/Program.cs') -Raw
$configuration = Get-Content -LiteralPath (Join-Path $root 'src/MW4Remastered.Core/Launch/LegacyGameConfiguration.cs') -Raw
$lock = Get-Content -LiteralPath (Join-Path $root 'third_party/dgVoodoo2.lock.json') -Raw | ConvertFrom-Json
$addonPatch = Get-Content -LiteralPath (Join-Path $root 'tools/compatibility/patches/dgVoodoo2-MW4-Presentation.patch') -Raw
$releaseBuilder = Get-Content -LiteralPath (Join-Path $root 'tools/package/build-release.ps1') -Raw

function Assert-True {
    param([Parameter(Mandatory)][bool]$Condition, [Parameter(Mandatory)][string]$Message)
    if (-not $Condition) { throw $Message }
}

Assert-True ($lock.version -eq '2.87.5' -and $lock.archiveSha256 -eq '5ffde6927f7355ca3fdd5d785b581256a8e6539fa13e395a891ade6ba1040850') 'dgVoodoo2 provenance must stay pinned to the evaluated complete archive.'
Assert-True ($lock.profileSha256 -eq '7ea9e4576a421157927de2d41551e3fdef8b4c76cd3adcc2d02ef19706f249a4' -and $plan -match $lock.profileSha256) 'The reviewed MW4 dgVoodoo profile hash must remain recorded and enforced by the install boundary.'
Assert-True ($releaseBuilder -match $lock.profileSha256) 'Release assembly must accept only the same reviewed dgVoodoo profile hash enforced by the install boundary.'
Assert-True ($lock.files.'MS/x86/DDraw.dll' -eq '612a24408a090a3c6f3886557fa18034ee742e94ad0a40ebdf854d2816176c2e' -and $lock.productBindings.Count -eq 3) 'The exact stock x86 wrapper and all three product bindings must remain recorded.'
Assert-True ($profile -match 'Version\s*=\s*0x287' -and $profile -match 'OutputAPI\s*=\s*d3d12_fl12_0' -and $profile -match 'ScalingMode\s*=\s*stretched_ar' -and $profile -match 'FullScreenMode\s*=\s*false' -and $profile -match 'CenterAppWindow\s*=\s*true' -and $profile -match 'WindowedAttributes\s*=\s*borderless,\s*fullscreensize' -and $profile -match 'AppControlledScreenMode\s*=\s*false') 'The profile must preserve each supplied surface aspect in a D3D12-backed centered borderless desktop-sized window, avoiding a physical display-mode switch.'
Assert-True ($profile -match 'SystemHookFlags\s*=\s*cursor' -and $profile -match 'FPSLimit\s*=\s*30' -and $profile -match 'dgVoodooWatermark\s*=\s*false' -and $profile -match '3DfxWatermark\s*=\s*false') 'The MW4-specific cursor and physics-safe 30 FPS limit must remain explicit, and every dgVoodoo watermark path must remain disabled.'
Assert-True ($launcher -match 'ArgumentList\.Add\("-GameTime\.MaxVariableFps"\)' -and $launcher -match 'ArgumentList\.Add\("30"\)') 'The game-owned variable-FPS ceiling must agree with dgVoodoo at the physics-safe 30 FPS limit.'
Assert-True ($profile -match '(?m)^\s*Resolution\s*=\s*unforced\s*$' -and $profile -notmatch '(?m)^\s*Resolution\s*=\s*(desktop|max)\s*$' -and $profile -match '(?m)^ExtraEnumeratedResolutions\s*=\s*1600x1200\s*$' -and $profile -match 'Antialiasing\s*=\s*4x' -and $profile -match 'Filtering\s*=\s*16') 'The remaster profile must enumerate only selected 4:3 game render modes, with 4x MSAA and 16x anisotropic filtering.'
Assert-True ($profile -match 'Resampling\s*=\s*lanczos-3') 'Final monitor scaling must use dgVoodoo high-quality Lanczos-3 resampling.'
Assert-True ($profile -match 'KeepFilterIfPointSampled\s*=\s*true' -and $profile -match 'Bilinear2DOperations\s*=\s*false') 'Forced 3D filtering must leave point-sampled and DirectDraw 2D operations alone.'
Assert-True ($profile -match 'DeframerSize\s*=\s*0') 'The profile must not add a deframer border that scales into a visible top/left seam around gameplay and in-engine cinematics.'
Assert-True ($profile -match 'DesktopBitDepth\s*=\s*32' -and $profile -match 'EnumeratedResolutionBitdepths\s*=\s*32' -and $profile -match 'Default3DRenderFormat\s*=\s*argb8888') 'The remaster profile must expose and render the game in 32-bit SDR color.'
Assert-True ($configuration -match 'ActiveMonitorResolutionProvider' -and $configuration -match 'MonitorFromWindow\(GetForegroundWindow\(\)' -and $configuration -match 'LargestFourByThree' -and $configuration -match 'GameResolutionPreset\.BestFitting' -and $launcher -match 'startInfo\.ArgumentList\.Add\("-f"\)' -and $launcher -match 'resolution\.ToString\(\)') 'Wrapped titles must capture the foreground launcher monitor and request a fitting listed 4:3 game render mode without assuming the primary display.'
Assert-True ($worker -match 'EnsureDefaults' -and $configuration -match 'MaximumGraphicsDefaults' -and $configuration -match '\("DetailTexture", "true"\)' -and $configuration -match '\("ShadowMode", "2"\)' -and $configuration -match '\("FancyWater", "true"\)' -and $configuration -match '\("MaxLights", "8"\)' -and $configuration -match '\("Compositing", "3"\)' -and $configuration -match '\("LoadRadius", "4"\)') 'Setup must upgrade even a pre-existing graphics page to the game-owned Ultra High baseline before wrapper-level enhancement.'
Assert-True ($launcher -notmatch '/gosnovideo') 'Original startup movies are part of the presentation contract and must remain enabled.'
Assert-True ($plan -match 'MW4X' -and $plan -match 'D3DImm\.dll' -and $plan -match '6a0ca214784be04b7c8b547105aa9d79acf4dc26c0b6f8702b437ddca54058b2') 'All three titles must receive the exact stock dgVoodoo DirectX files, including Black Knight under MW4X.'
Assert-True ($plan -match 'SampleAddon\.dll' -and $plan -match $lock.presentationAddon.sampleAddonDllSha256 -and $plan -match 'DirtyGlass\.png') 'All titles must receive the exact reproducible presentation add-on runtime.'
Assert-True ($addonPatch -match 'D3DDeviceObjectCreated' -and $addonPatch -match 'IsGameplay3DActive' -and $addonPatch -match 'isGameplay3DActive \? 2\.0f' -and $addonPatch -match 'cropRight = \(float\) iCtx\.srcRect\.right') 'The seam correction must remain Direct3D-gameplay-only, two source pixels, and bottom-right anchored.'
Assert-True ($addonPatch -match 'Media_VengeanceStartup = 1' -and $addonPatch -match 'observed = Media_VengeanceStartup' -and $addonPatch -match 'mediaState == 0 && IsGameplay3DActive') 'The embedded Vengeance Microsoft/FASA splash must remain explicitly excluded from the gameplay crop.'
Assert-True ($addonPatch -match 'IsVengeanceProcess' -and $addonPatch -match 'MW4\.exe' -and $addonPatch -match 'Content\\\\Movies\\\\GAMEOPEN\.MPG' -and $addonPatch -match 'vengeanceLiveActionDelayMs = 12250' -and $addonPatch -match 'movieAspect = 30\.0f / 17\.0f' -and $addonPatch -match 'D3D12SwapchainPresentEnd' -and $addonPatch -match 'finalTargetDesc\.Width') 'The sole widescreen exception must remain exact-process, exact-asset, post-logo gated with proportional source geometry rendered across the complete final target.'
Assert-True ($launcher -match 'productId == "black-knight"' -and $launcher -notmatch 'ArgumentList\.Add\("-window"\)') 'Black Knight must use dgVoodoo fullscreen presentation rather than the retired native window fallback.'
Assert-True ($launcher -match 'gameWindowLifecycleGuard\.Protect\(processId\)') 'Every launcher-started game process must be handed to the window lifecycle guard.'
Assert-True ($lifecycle -match 'IsIconic\(window\)' -and $lifecycle -match 'SwShowNoActivate' -and $lifecycle -match 'SwpNoActivate' -and $lifecycle -match 'ClipCursor') 'The lifecycle guard must restore minimized MW4 windows without activation and own active-only cursor confinement.'
Assert-True ($lifecycle -match 'EnumWindows' -and $lifecycle -match 'GetWindowThreadProcessId' -and $lifecycle -match 'FindPresentationWindow') 'The lifecycle guard must discover legacy MW4 top-level windows even when Process.MainWindowHandle is transiently zero.'
Assert-True ($lifecycle -match 'Covers\(windowRect, monitorRect\)\) return true' -and $lifecycle -match 'FitOuterToClient\(currentRect, clientRect, monitorInfo\.Monitor\)' -and $lifecycle -match '!clientRect\.Equals\(monitorInfo\.Monitor\)') 'The lifecycle guard must fit the game client to physical monitor bounds without shrinking a framed outer rectangle.'
Assert-True ($lifecycleProbe -match 'PrintWindow' -and $lifecycleProbe -notmatch 'CopyFromScreen') 'The lifecycle probe must capture only the exact game window and never the surrounding desktop.'
Assert-True ($worker -match 'Compatibility.*dgVoodoo2') 'The setup worker must route the packaged dgVoodoo2 bundle into owned game manifests.'
Assert-True ($worker -match 'OwnedInstallFileReplacementTransaction' -and $worker -match 'compatibilityReplacement\.Migrate' -and $worker -match 'DDrawCompat-MW4Mercs\.ini') 'Setup upgrades must atomically add the new wrapper files and retire only the old owned profiles.'

Write-Host 'Presentation compatibility contract tests passed.'
