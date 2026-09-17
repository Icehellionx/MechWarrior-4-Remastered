[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$OutputDirectory,
    [string]$Version = '0.1.0',
    [string]$InnoCompilerPath,
    [string]$SafeDiscLoader2SourceRoot,
    [string]$BlackKnightRuntimeBundle,
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
$runtimeBundle = if ([string]::IsNullOrWhiteSpace($BlackKnightRuntimeBundle)) {
    Join-Path $scratch 'black-knight-runtime-bundle'
} else {
    [IO.Path]::GetFullPath($BlackKnightRuntimeBundle)
}
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
    if ([string]::IsNullOrWhiteSpace($BlackKnightRuntimeBundle)) {
        if ([string]::IsNullOrWhiteSpace($SafeDiscLoader2SourceRoot)) {
            throw 'Provide either SafeDiscLoader2SourceRoot for a source build or BlackKnightRuntimeBundle for an exact qualified hosted build.'
        }
        & (Join-Path $projectRoot 'tools/compatibility/build-safedisc-loader2.ps1') `
            -SourceRoot $SafeDiscLoader2SourceRoot -OutputDirectory $runtimeBundle -Mode Pr1Runtime
        if ($LASTEXITCODE -ne 0) { throw "Black Knight runtime DLL build failed with exit code $LASTEXITCODE." }
    }

    $qualifiedRuntimeFiles = [ordered]@{
        'version.dll' = 'f534b642defe15ea234988ba0b3b8f1a467aa76ee478cd3e862e6265bbd8bb1c'
        'SafeDiscLoader2-LICENSE.txt' = '81cbae84a29ce7e770bf2bc7b178e50bda0ce8de6067aba661b0bc7b05b562f8'
        'SafeDiscLoader2-source-f27286a363aa675a0422141cb96fc8619cf8b9d8.zip' = '78ae295db0382f498829546ff7272db5eb2e713c20eb72ab3552ceaf8477cd17'
        'SafeDiscLoader2-MW4-BlackKnight-PR1-Runtime.patch' = 'e89e14e6986d7246990f6787e5515b6429242c9e115793af14b682c6cb8238ce'
    }
    foreach ($entry in $qualifiedRuntimeFiles.GetEnumerator()) {
        $path = Join-Path $runtimeBundle $entry.Key
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Qualified Black Knight runtime bundle file is missing: $($entry.Key)" }
        $actual = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($actual -ne $entry.Value) { throw "Black Knight runtime bundle hash mismatch for $($entry.Key): $actual" }
    }

    $installerFiles = @(Get-ChildItem -LiteralPath $publishInstaller -File)
    $launcherFiles = @(Get-ChildItem -LiteralPath $publishLauncher -File)
    $patchHostFiles = @(Get-ChildItem -LiteralPath $publishPatchHost -File)
    if ($installerFiles.Count -ne 1 -or $installerFiles[0].Name -ne 'MW4RemasteredInstallWorker.exe') {
        throw 'Install worker publish must produce exactly one self-contained executable.'
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
    Copy-Item -LiteralPath (Join-Path $runtimeBundle 'version.dll') -Destination (Join-Path $payload 'BlackKnightRuntime.dll')
    $runtimeNotices = New-Item -ItemType Directory -Path (Join-Path $payload 'Compatibility/BlackKnightRuntime') -Force
    Copy-Item -LiteralPath (Join-Path $runtimeBundle 'SafeDiscLoader2-LICENSE.txt') -Destination $runtimeNotices
    Copy-Item -LiteralPath (Join-Path $runtimeBundle 'SafeDiscLoader2-source-f27286a363aa675a0422141cb96fc8619cf8b9d8.zip') -Destination $runtimeNotices
    Copy-Item -LiteralPath (Join-Path $runtimeBundle 'SafeDiscLoader2-MW4-BlackKnight-PR1-Runtime.patch') -Destination $runtimeNotices
    Copy-Item -LiteralPath (Join-Path $projectRoot 'third_party/THIRD-PARTY-NOTICES.md') -Destination (Join-Path $payload 'THIRD-PARTY-NOTICES.md')

    $manualSource = Join-Path $projectRoot 'output/pdf'
    $coverSource = Join-Path $projectRoot 'output/manual-covers'
    $manualLockPath = Join-Path $projectRoot 'tools/manuals/manuals.lock.json'
    $manualLock = Get-Content -LiteralPath $manualLockPath -Raw | ConvertFrom-Json
    if ($manualLock.schemaVersion -ne 2 -or @($manualLock.manuals).Count -ne 3) {
        throw 'Manual lock must declare exactly the three qualified version-2 PDF and cover outputs.'
    }
    $manualFiles = @(Get-ChildItem -LiteralPath $manualSource -File -Filter '*.pdf')
    if ($manualFiles.Count -ne 3) { throw 'Manual output directory must contain exactly three PDFs.' }
    $coverFiles = @(Get-ChildItem -LiteralPath $coverSource -File -Filter '*.cover.png')
    if ($coverFiles.Count -ne 3) { throw 'Manual cover output directory must contain exactly three cover PNGs.' }
    $manualDestination = New-Item -ItemType Directory -Path (Join-Path $payload 'Manuals')
    foreach ($manual in $manualLock.manuals) {
        $source = Join-Path $manualSource $manual.name
        if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { throw "Qualified manual output is missing: $($manual.name)" }
        $actualHash = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($actualHash -ne $manual.outputSha256) { throw "Qualified manual output hash mismatch: $($manual.name)" }
        Copy-Item -LiteralPath $source -Destination (Join-Path $manualDestination $manual.name)

        $cover = Join-Path $coverSource $manual.coverName
        if (-not (Test-Path -LiteralPath $cover -PathType Leaf)) { throw "Qualified manual cover is missing: $($manual.coverName)" }
        $actualCoverHash = (Get-FileHash -LiteralPath $cover -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($actualCoverHash -ne $manual.coverSha256) { throw "Qualified manual cover hash mismatch: $($manual.coverName)" }
        Copy-Item -LiteralPath $cover -Destination (Join-Path $manualDestination $manual.coverName)
    }
    & (Join-Path $projectRoot 'tools/assert-release-tree.ps1') -Root $payload -AllowedExecutablePaths @(
        'MW4RemasteredInstallWorker.exe',
        'MW4RemasteredLauncher.exe',
        'MW4RemasteredRtpPatchHost.exe',
        'BlackKnightRuntime.dll'
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
