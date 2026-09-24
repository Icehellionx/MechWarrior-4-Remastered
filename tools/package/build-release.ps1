[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$OutputDirectory,
    [string]$Version = '0.1.0',
    [string]$InnoCompilerPath,
    [string]$SafeDiscLoader2SourceRoot,
    [string]$BlackKnightCaptureBundle,
    [string]$DgVoodooArchive,
    [string]$DgVoodooSourceRoot,
    [string]$MercenariesPr1Archive,
    [switch]$StageOnly,
    [switch]$UseExistingRestore
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$output = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $output) { throw "Release output already exists: $output" }
if ($Version -notmatch '^\d+\.\d+\.\d+(\.\d+)?$') { throw 'Version must contain three or four numeric components.' }
if ($UseExistingRestore -and -not $StageOnly) { throw 'UseExistingRestore is permitted only for an internal stage, never a setup package.' }
[string[]]$restoreOption = if ($UseExistingRestore) { @('--no-restore') } else { @() }

$scratch = Join-Path ([IO.Path]::GetTempPath()) ('mw4-release-build-' + [Guid]::NewGuid().ToString('N'))
$payload = Join-Path $scratch 'payload'
$publishInstaller = Join-Path $scratch 'installer'
$publishLauncher = Join-Path $scratch 'launcher'
$publishPatchHost = Join-Path $scratch 'patch-host'
$captureBundle = if ([string]::IsNullOrWhiteSpace($BlackKnightCaptureBundle)) {
    Join-Path $scratch 'black-knight-capture-bundle'
} else {
    [IO.Path]::GetFullPath($BlackKnightCaptureBundle)
}
try {
    New-Item -ItemType Directory -Path $payload -Force | Out-Null
    & dotnet publish (Join-Path $projectRoot 'src/MW4Remastered.Installer/MW4Remastered.Installer.csproj') `
        --configuration Release --runtime win-x64 --self-contained true `
        -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false --output $publishInstaller @restoreOption
    if ($LASTEXITCODE -ne 0) { throw "Installer publish failed with exit code $LASTEXITCODE." }
    & dotnet publish (Join-Path $projectRoot 'src/MW4Remastered.Launcher/MW4Remastered.Launcher.csproj') `
        --configuration Release --runtime win-x64 --self-contained true `
        -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false --output $publishLauncher @restoreOption
    if ($LASTEXITCODE -ne 0) { throw "Launcher publish failed with exit code $LASTEXITCODE." }
    & dotnet publish (Join-Path $projectRoot 'src/MW4Remastered.RtpPatchHost/MW4Remastered.RtpPatchHost.csproj') `
        --configuration Release --runtime win-x86 --self-contained true `
        -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false --output $publishPatchHost @restoreOption
    if ($LASTEXITCODE -ne 0) { throw "Patch host publish failed with exit code $LASTEXITCODE." }
    if ([string]::IsNullOrWhiteSpace($BlackKnightCaptureBundle)) {
        if ([string]::IsNullOrWhiteSpace($SafeDiscLoader2SourceRoot)) {
            throw 'Provide either SafeDiscLoader2SourceRoot for a source build or BlackKnightCaptureBundle for an exact qualified hosted build.'
        }
        & (Join-Path $projectRoot 'tools/compatibility/build-safedisc-loader2.ps1') `
            -SourceRoot $SafeDiscLoader2SourceRoot -OutputDirectory $captureBundle -Mode Pr1Capture
        if ($LASTEXITCODE -ne 0) { throw "Black Knight capture DLL build failed with exit code $LASTEXITCODE." }
    }

    $qualifiedCaptureFiles = [ordered]@{
        'BlackKnightPr1Capture.dll' = '7fbf1fbd0b251986f0dcd2082f218650eddff40d661ede0deefd4b2ce6b5c6fc'
        'SafeDiscLoader2-LICENSE.txt' = '81cbae84a29ce7e770bf2bc7b178e50bda0ce8de6067aba661b0bc7b05b562f8'
        'SafeDiscLoader2-source-f27286a363aa675a0422141cb96fc8619cf8b9d8.zip' = '78ae295db0382f498829546ff7272db5eb2e713c20eb72ab3552ceaf8477cd17'
        'SafeDiscLoader2-MW4-BlackKnight-PR1-Capture.patch' = 'e647a85f4d19b4e28032b42ab0ff993b79b69708e2d86dad3ade34115a0ca9f6'
    }
    foreach ($entry in $qualifiedCaptureFiles.GetEnumerator()) {
        $path = Join-Path $captureBundle $entry.Key
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Qualified Black Knight capture bundle file is missing: $($entry.Key)" }
        $actual = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($actual -ne $entry.Value) { throw "Black Knight capture bundle hash mismatch for $($entry.Key): $actual" }
    }

    if ([string]::IsNullOrWhiteSpace($DgVoodooArchive)) {
        throw 'Provide DgVoodooArchive containing the exact dgVoodoo2 2.87.5 complete ZIP.'
    }
    $DgVoodooArchive = [IO.Path]::GetFullPath($DgVoodooArchive)
    if (-not (Test-Path -LiteralPath $DgVoodooArchive -PathType Leaf)) { throw 'The dgVoodoo2 archive does not exist.' }
    $dgVoodooArchiveHash = (Get-FileHash -LiteralPath $DgVoodooArchive -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($dgVoodooArchiveHash -ne '5ffde6927f7355ca3fdd5d785b581256a8e6539fa13e395a891ade6ba1040850') {
        throw "Unsupported dgVoodoo2 archive SHA-256: $dgVoodooArchiveHash"
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
    Copy-Item -LiteralPath (Join-Path $captureBundle 'BlackKnightPr1Capture.dll') -Destination (Join-Path $payload 'BlackKnightPr1Capture.dll')
    $captureNotices = New-Item -ItemType Directory -Path (Join-Path $payload 'Compatibility/BlackKnightPr1Capture') -Force
    Copy-Item -LiteralPath (Join-Path $captureBundle 'SafeDiscLoader2-LICENSE.txt') -Destination $captureNotices
    Copy-Item -LiteralPath (Join-Path $captureBundle 'SafeDiscLoader2-source-f27286a363aa675a0422141cb96fc8619cf8b9d8.zip') -Destination $captureNotices
    Copy-Item -LiteralPath (Join-Path $captureBundle 'SafeDiscLoader2-MW4-BlackKnight-PR1-Capture.patch') -Destination $captureNotices
    $presentationDestination = New-Item -ItemType Directory -Path (Join-Path $payload 'Compatibility/dgVoodoo2') -Force
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $dgVoodooZip = [IO.Compression.ZipFile]::OpenRead($DgVoodooArchive)
    try {
        $qualifiedPresentationFiles = [ordered]@{
            'MS/x86/DDraw.dll' = '612a24408a090a3c6f3886557fa18034ee742e94ad0a40ebdf854d2816176c2e'
            'MS/x86/D3DImm.dll' = '93c534f2d17419ea78f15551f7e0aac78b3c503733a840914fa063708a5afe8e'
            'MS/x86/D3D8.dll' = 'd03e2562178db1fcf3493fc0a0b23a465e35c55d73adb3ec095be866cd662704'
            'MS/x86/D3D9.dll' = '6a0ca214784be04b7c8b547105aa9d79acf4dc26c0b6f8702b437ddca54058b2'
        }
        foreach ($entry in $qualifiedPresentationFiles.GetEnumerator()) {
            $sourceEntry = @($dgVoodooZip.Entries | Where-Object { $_.FullName.Replace('\', '/') -ceq $entry.Key })
            if ($sourceEntry.Count -ne 1) { throw "dgVoodoo2 archive is missing exact entry: $($entry.Key)" }
            $destination = Join-Path $presentationDestination ([IO.Path]::GetFileName($entry.Key))
            $input = $sourceEntry[0].Open()
            $target = [IO.File]::Create($destination)
            try { $input.CopyTo($target) } finally { $target.Dispose(); $input.Dispose() }
            $actual = (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash.ToLowerInvariant()
            if ($actual -ne $entry.Value) { throw "dgVoodoo2 payload hash mismatch for $($entry.Key): $actual" }
        }
    } finally {
        $dgVoodooZip.Dispose()
    }
    $dgVoodooProfile = Join-Path $projectRoot 'assets/compatibility/dgVoodoo-MW4.conf'
    $dgVoodooProfileHash = (Get-FileHash -LiteralPath $dgVoodooProfile -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($dgVoodooProfileHash -ne '7ea9e4576a421157927de2d41551e3fdef8b4c76cd3adcc2d02ef19706f249a4') {
        throw "Unsupported MW4 dgVoodoo profile SHA-256: $dgVoodooProfileHash"
    }
    Copy-Item -LiteralPath $dgVoodooProfile -Destination (Join-Path $presentationDestination 'dgVoodoo.conf')

    if ([string]::IsNullOrWhiteSpace($DgVoodooSourceRoot)) {
        throw 'Provide DgVoodooSourceRoot at exact commit de5f360b43c1fa61cc2c47ccfc48bbdd995badf7.'
    }
    $addonBundle = Join-Path $scratch 'dgvoodoo-mw4-addon'
    & (Join-Path $projectRoot 'tools/compatibility/build-dgvoodoo-mw4-addon.ps1') `
        -SourceRoot $DgVoodooSourceRoot -OutputDirectory $addonBundle
    if ($LASTEXITCODE -ne 0) { throw "dgVoodoo MW4 add-on build failed with exit code $LASTEXITCODE." }
    $qualifiedAddonFiles = [ordered]@{
        'SampleAddon.dll' = '24e6fe3e7eea55aa2271223bf08e444597e58580f78126ea440db0e2cefb254b'
        'SampleAddon.ini' = 'f21bb13f1e5ecb33595677ed5f8eba156576fcb2f2f5123138147809ae9b9edb'
        'DirtyGlass.png' = 'dc507d14880cde567b192aaf444769586a906c0165ec2432ee43d0cead4fcbc5'
        'dgVoodoo2-MW4-Presentation.patch' = 'd078f1176bedc53a4224dc55624cc6664b7751043754e8181b239e6094ddd954'
    }
    foreach ($entry in $qualifiedAddonFiles.GetEnumerator()) {
        $path = Join-Path $addonBundle $entry.Key
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Qualified dgVoodoo add-on file is missing: $($entry.Key)" }
        $actual = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($actual -ne $entry.Value) { throw "dgVoodoo add-on hash mismatch for $($entry.Key): $actual" }
        Copy-Item -LiteralPath $path -Destination (Join-Path $presentationDestination $entry.Key)
    }
    Copy-Item -LiteralPath (Join-Path $projectRoot 'third_party/THIRD-PARTY-NOTICES.md') -Destination (Join-Path $payload 'THIRD-PARTY-NOTICES.md')
    Copy-Item -LiteralPath (Join-Path $projectRoot 'third_party/DiscUtils-LICENSE.txt') -Destination (Join-Path $payload 'DiscUtils-LICENSE.txt')

    if ([string]::IsNullOrWhiteSpace($MercenariesPr1Archive)) {
        $MercenariesPr1Archive = Join-Path $projectRoot 'Installation Files/MechWarrior-4-Mercenaries_Fix_Win_EN.zip'
    }
    $MercenariesPr1Archive = [IO.Path]::GetFullPath($MercenariesPr1Archive)
    if (-not (Test-Path -LiteralPath $MercenariesPr1Archive -PathType Leaf)) {
        throw 'The exact official Mercenaries PR1 source archive is required to build the all-in-one package.'
    }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $outer = [IO.Compression.ZipFile]::OpenRead($MercenariesPr1Archive)
    try {
        $candidates = @($outer.Entries | Where-Object {
            [IO.Path]::GetFileName($_.FullName) -ieq 'mercpr1.exe' -and $_.Length -eq 5376000
        })
        if ($candidates.Count -ne 1) { throw 'Mercenaries PR1 source must contain exactly one 5,376,000-byte mercpr1.exe.' }
        $mercPr1Installer = Join-Path $scratch 'mercpr1.exe'
        $input = $candidates[0].Open()
        $target = [IO.File]::Create($mercPr1Installer)
        try { $input.CopyTo($target) } finally { $target.Dispose(); $input.Dispose() }
    } finally {
        $outer.Dispose()
    }
    $mercPr1Hash = (Get-FileHash -LiteralPath $mercPr1Installer -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($mercPr1Hash -ne '0c3d0094448e6fe5d2a30fb9ebb24001e8e8c03b39aff9232856bd30000efb30') {
        throw "Unsupported Mercenaries PR1 installer hash: $mercPr1Hash"
    }
    $updateDestination = New-Item -ItemType Directory -Path (Join-Path $payload 'Updates/MercenariesPR1') -Force
    $nested = [IO.Compression.ZipFile]::OpenRead($mercPr1Installer)
    try {
        $qualifiedUpdateFiles = [ordered]@{
            'Patchw32.dll' = @{ Length = 185344; Sha256 = '0ec6e25234ad74489eb1890d4de57bb6140bb8196bdc4a5dcac90dd9d16eb2dd' }
            'English/MW4MERCS.RTP' = @{ Length = 5119691; Sha256 = 'd3ebf1c2dc098a8e55d7cbeb112426fb413dfd23595a1ed078cd35fe7a551a65' }
        }
        foreach ($entry in $qualifiedUpdateFiles.GetEnumerator()) {
            $sourceEntry = @($nested.Entries | Where-Object { $_.FullName.Replace('\', '/') -ieq $entry.Key })
            if ($sourceEntry.Count -ne 1 -or $sourceEntry[0].Length -ne $entry.Value.Length) {
                throw "Mercenaries PR1 is missing qualified entry: $($entry.Key)"
            }
            $payloadRelativePath = if ($entry.Key -ieq 'Patchw32.dll') { 'Patchw32.dat' } else { $entry.Key }
            $destination = Join-Path $updateDestination $payloadRelativePath
            New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($destination)) -Force | Out-Null
            $input = $sourceEntry[0].Open()
            $target = [IO.File]::Create($destination)
            try { $input.CopyTo($target) } finally { $target.Dispose(); $input.Dispose() }
            $actual = (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash.ToLowerInvariant()
            if ($actual -ne $entry.Value.Sha256) { throw "Mercenaries PR1 payload hash mismatch: $($entry.Key)" }
        }
    } finally {
        $nested.Dispose()
    }

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
        'BlackKnightPr1Capture.dll',
        'Compatibility/dgVoodoo2/DDraw.dll',
        'Compatibility/dgVoodoo2/D3DImm.dll',
        'Compatibility/dgVoodoo2/D3D8.dll',
        'Compatibility/dgVoodoo2/D3D9.dll'
        'Compatibility/dgVoodoo2/SampleAddon.dll'
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
