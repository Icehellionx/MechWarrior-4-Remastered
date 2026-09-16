[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$CompatibilityEvidenceDirectory,
    [Parameter(Mandatory)]
    [string]$OutputDirectory,
    [string]$Version = '0.1.0',
    [string]$InnoCompilerPath,
    [switch]$StageOnly
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$output = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $output) { throw "Release output already exists: $output" }
if ($Version -notmatch '^\d+\.\d+\.\d+(\.\d+)?$') { throw 'Version must contain three or four numeric components.' }

$scratch = Join-Path ([IO.Path]::GetTempPath()) ('mw4-release-build-' + [Guid]::NewGuid().ToString('N'))
$payload = Join-Path $scratch 'payload'
$publishInstaller = Join-Path $scratch 'installer'
$publishLauncher = Join-Path $scratch 'launcher'
$publishPatchHost = Join-Path $scratch 'patch-host'
try {
    New-Item -ItemType Directory -Path $payload -Force | Out-Null
    & dotnet publish (Join-Path $projectRoot 'src/MW4Remastered.Installer/MW4Remastered.Installer.csproj') `
        --configuration Release --runtime win-x64 --self-contained true `
        -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false --output $publishInstaller
    if ($LASTEXITCODE -ne 0) { throw "Installer publish failed with exit code $LASTEXITCODE." }
    & dotnet publish (Join-Path $projectRoot 'src/MW4Remastered.Launcher/MW4Remastered.Launcher.csproj') `
        --configuration Release --runtime win-x64 --self-contained true `
        -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false --output $publishLauncher
    if ($LASTEXITCODE -ne 0) { throw "Launcher publish failed with exit code $LASTEXITCODE." }
    & dotnet publish (Join-Path $projectRoot 'src/MW4Remastered.RtpPatchHost/MW4Remastered.RtpPatchHost.csproj') `
        --configuration Release --runtime win-x86 --self-contained true `
        -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false --output $publishPatchHost
    if ($LASTEXITCODE -ne 0) { throw "Patch host publish failed with exit code $LASTEXITCODE." }

    $installerFiles = @(Get-ChildItem -LiteralPath $publishInstaller -File)
    $launcherFiles = @(Get-ChildItem -LiteralPath $publishLauncher -File)
    $patchHostFiles = @(Get-ChildItem -LiteralPath $publishPatchHost -File)
    if ($installerFiles.Count -ne 1 -or $installerFiles[0].Name -ne 'MW4RemasteredInstaller.exe') {
        throw 'Installer publish must produce exactly one self-contained executable.'
    }
    if ($launcherFiles.Count -ne 1 -or $launcherFiles[0].Name -ne 'MW4RemasteredLauncher.exe') {
        throw 'Launcher publish must produce exactly one self-contained executable.'
    }
    if ($patchHostFiles.Count -ne 1 -or $patchHostFiles[0].Name -ne 'MW4RemasteredRtpPatchHost.exe') {
        throw 'Patch host publish must produce exactly one self-contained executable.'
    }

    Copy-Item -LiteralPath $installerFiles[0].FullName -Destination (Join-Path $payload $installerFiles[0].Name)
    Copy-Item -LiteralPath $launcherFiles[0].FullName -Destination (Join-Path $payload $launcherFiles[0].Name)
    Copy-Item -LiteralPath $patchHostFiles[0].FullName -Destination (Join-Path $payload $patchHostFiles[0].Name)
    Copy-Item -LiteralPath (Join-Path $projectRoot 'third_party/THIRD-PARTY-NOTICES.md') -Destination (Join-Path $payload 'THIRD-PARTY-NOTICES.md')

    $manualSource = Join-Path $projectRoot 'output/pdf'
    $manualLockPath = Join-Path $projectRoot 'tools/manuals/manuals.lock.json'
    $manualLock = Get-Content -LiteralPath $manualLockPath -Raw | ConvertFrom-Json
    if ($manualLock.schemaVersion -ne 1 -or @($manualLock.manuals).Count -ne 3) {
        throw 'Manual lock must declare exactly the three qualified version-1 outputs.'
    }
    $manualFiles = @(Get-ChildItem -LiteralPath $manualSource -File -Filter '*.pdf')
    if ($manualFiles.Count -ne 3) { throw 'Manual output directory must contain exactly three PDFs.' }
    $manualDestination = New-Item -ItemType Directory -Path (Join-Path $payload 'Manuals')
    foreach ($manual in $manualLock.manuals) {
        $source = Join-Path $manualSource $manual.name
        if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { throw "Qualified manual output is missing: $($manual.name)" }
        $actualHash = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($actualHash -ne $manual.outputSha256) { throw "Qualified manual output hash mismatch: $($manual.name)" }
        Copy-Item -LiteralPath $source -Destination (Join-Path $manualDestination $manual.name)
    }
    & (Join-Path $projectRoot 'tools/compatibility/assemble-black-knight-bundle.ps1') `
        -EvidenceDirectory $CompatibilityEvidenceDirectory `
        -OutputDirectory (Join-Path $payload 'Compatibility/BlackKnight')

    & (Join-Path $projectRoot 'tools/assert-release-tree.ps1') -Root $payload -AllowedExecutablePaths @(
        'MW4RemasteredInstaller.exe',
        'MW4RemasteredLauncher.exe',
        'MW4RemasteredRtpPatchHost.exe',
        'Compatibility/BlackKnight/MW4RemasteredCompatLauncher.exe',
        'Compatibility/BlackKnight/version.dll'
    )

    if ($StageOnly) {
        New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($output)) -Force | Out-Null
        [IO.Directory]::Move($payload, $output)
        Write-Host "Validated release payload staged at: $output"
        return
    }

    if ([string]::IsNullOrWhiteSpace($InnoCompilerPath)) {
        $candidates = @(
            (Join-Path $env:LOCALAPPDATA 'Programs/Inno Setup 7/ISCC.exe'),
            (Join-Path $env:ProgramFiles 'Inno Setup 7/ISCC.exe'),
            (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6/ISCC.exe')
        )
        $InnoCompilerPath = $candidates | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
    }
    if ([string]::IsNullOrWhiteSpace($InnoCompilerPath) -or -not (Test-Path -LiteralPath $InnoCompilerPath -PathType Leaf)) {
        throw 'Inno Setup compiler was not found. Use -StageOnly to build a validated payload without compiling setup.'
    }

    New-Item -ItemType Directory -Path $output -Force | Out-Null
    & $InnoCompilerPath `
        "/DPayloadRoot=$payload" `
        "/DPackageOutput=$output" `
        "/DAppVersion=$Version" `
        "/DProjectRoot=$projectRoot" `
        (Join-Path $projectRoot 'packaging/MechWarrior4Remastered.iss')
    if ($LASTEXITCODE -ne 0) { throw "Inno Setup compilation failed with exit code $LASTEXITCODE." }

    $setups = @(Get-ChildItem -LiteralPath $output -File -Filter '*.exe')
    if ($setups.Count -ne 1) { throw 'Package compilation must produce exactly one setup executable.' }
    $setupHash = (Get-FileHash -LiteralPath $setups[0].FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    Set-Content -LiteralPath (Join-Path $output ($setups[0].Name + '.sha256')) `
        -Value "$setupHash  $($setups[0].Name)" -Encoding ascii
    & (Join-Path $projectRoot 'tools/assert-release-tree.ps1') -Root $output -AllowedExecutablePaths $setups[0].Name
    Write-Host "Built setup package: $($setups[0].FullName)"
    Write-Host "SHA-256: $setupHash"
}
finally {
    if (Test-Path -LiteralPath $scratch) {
        $resolvedScratch = (Resolve-Path -LiteralPath $scratch).Path
        $tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
        if (-not $resolvedScratch.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing unsafe release-build cleanup: $resolvedScratch"
        }
        Remove-Item -LiteralPath $resolvedScratch -Recurse -Force
    }
}
