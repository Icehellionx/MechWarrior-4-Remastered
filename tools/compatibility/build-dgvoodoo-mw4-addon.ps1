[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$SourceRoot,
    [Parameter(Mandatory)]
    [string]$OutputDirectory,
    [string]$MsBuildPath,
    [string]$FxcPath,
    [ValidateSet(0, 1, 2, 5)]
    [int]$ExperimentalGameplayCropPixels = 5
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$source = [IO.Path]::GetFullPath($SourceRoot)
$output = [IO.Path]::GetFullPath($OutputDirectory)
$expectedCommit = 'de5f360b43c1fa61cc2c47ccfc48bbdd995badf7'

if (-not (Test-Path -LiteralPath $source -PathType Container)) { throw "dgVoodoo2 source root is missing: $source" }
if (Test-Path -LiteralPath $output) { throw "Add-on output already exists: $output" }
$actualCommit = (& git -C $source rev-parse HEAD 2>$null).Trim()
if ($LASTEXITCODE -ne 0 -or $actualCommit -ne $expectedCommit) {
    throw "dgVoodoo2 source must be exact commit $expectedCommit; found '$actualCommit'."
}

if ([string]::IsNullOrWhiteSpace($MsBuildPath)) {
    $candidates = @(
        'C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/MSBuild.exe',
        'C:/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe'
    )
    $MsBuildPath = $candidates | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
}
if ([string]::IsNullOrWhiteSpace($MsBuildPath) -or -not (Test-Path -LiteralPath $MsBuildPath -PathType Leaf)) {
    throw 'Visual Studio 2022 MSBuild with the v143 C++ toolset is required.'
}

if ([string]::IsNullOrWhiteSpace($FxcPath)) {
    $kits = 'C:/Program Files (x86)/Windows Kits/10/bin'
    $FxcPath = Get-ChildItem -LiteralPath $kits -Recurse -Filter fxc.exe -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -match '[\\/]x86[\\/]fxc\.exe$' } |
        Sort-Object FullName -Descending |
        Select-Object -ExpandProperty FullName -First 1
}
if ([string]::IsNullOrWhiteSpace($FxcPath) -or -not (Test-Path -LiteralPath $FxcPath -PathType Leaf)) {
    throw 'Windows SDK fxc.exe is required to build the presentation shader.'
}

$scratch = Join-Path ([IO.Path]::GetTempPath()) ('mw4-dgvoodoo-addon-' + [Guid]::NewGuid().ToString('N'))
try {
    $scratchSource = Join-Path $scratch 'source'
    New-Item -ItemType Directory -Path $scratchSource -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $source 'dgVoodooAPI') -Destination $scratchSource -Recurse

    $patch = Join-Path $projectRoot 'tools/compatibility/patches/dgVoodoo2-MW4-Presentation.patch'
    if ($ExperimentalGameplayCropPixels -ne 5) {
        # Keep the production patch byte-for-byte pinned. This explicit test
        # option changes only the two gameplay source-coordinate offsets in a
        # disposable scratch copy; the output carries its exact variant patch.
        $variantPatch = Join-Path $scratch 'dgVoodoo2-MW4-Presentation-experiment.patch'
        $patchText = [IO.File]::ReadAllText($patch)
        $original = '(isGameplay3DActive ? 5.0f : 0.0f)'
        $matches = [regex]::Matches($patchText, [regex]::Escape($original)).Count
        if ($matches -ne 2) { throw "Expected two gameplay crop offsets; found $matches." }
        $replacement = '(isGameplay3DActive ? ' + $ExperimentalGameplayCropPixels.ToString([Globalization.CultureInfo]::InvariantCulture) + '.0f : 0.0f)'
        [IO.File]::WriteAllText($variantPatch, $patchText.Replace($original, $replacement))
        $patch = $variantPatch
    }
    & git -C $scratchSource apply -p0 --check $patch
    if ($LASTEXITCODE -ne 0) { throw "MW4 presentation patch validation failed with exit code $LASTEXITCODE." }
    & git -C $scratchSource apply -p0 $patch
    if ($LASTEXITCODE -ne 0) { throw "MW4 presentation patch failed with exit code $LASTEXITCODE." }

    $addonSource = Join-Path $scratchSource 'dgVoodooAPI/Samples/D3D12 Addon'
    & $FxcPath /nologo /T ps_5_1 /E main /O3 `
        /Fo (Join-Path $addonSource 'Shaders/PSGlass.pso') `
        (Join-Path $addonSource 'Shaders/PSGlass.hlsl')
    if ($LASTEXITCODE -ne 0) { throw "Presentation shader build failed with exit code $LASTEXITCODE." }

    & $MsBuildPath (Join-Path $scratchSource 'dgVoodooAPI/Samples/Samples.sln') `
        /t:SampleAddon /p:Configuration=Release /p:Platform=x86 /m /v:minimal
    if ($LASTEXITCODE -ne 0) { throw "Presentation add-on build failed with exit code $LASTEXITCODE." }

    New-Item -ItemType Directory -Path $output -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $scratchSource 'dgVoodooAPI/Samples/Bin/Win32/Release/SampleAddon.dll') -Destination $output
    Copy-Item -LiteralPath (Join-Path $addonSource 'SampleAddon.ini') -Destination $output
    Copy-Item -LiteralPath (Join-Path $addonSource 'DirtyGlass.png') -Destination $output
    Copy-Item -LiteralPath $patch -Destination (Join-Path $output 'dgVoodoo2-MW4-Presentation.patch')

    Get-ChildItem -LiteralPath $output -File | Sort-Object Name | ForEach-Object {
        $hash = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        Write-Host "$($_.Name) $($_.Length) $hash"
    }
}
finally {
    if (Test-Path -LiteralPath $scratch) {
        $resolved = (Resolve-Path -LiteralPath $scratch).Path
        $tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
        if (-not $resolved.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing unsafe add-on scratch cleanup: $resolved"
        }
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
}
