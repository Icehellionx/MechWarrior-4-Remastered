[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$SourceRoot,
    [Parameter(Mandatory)]
    [string]$OutputDirectory,
    [string]$MSBuildPath,
    [ValidateSet('Retail', 'Pr1Capture', 'Pr1Runtime')]
    [string]$Mode = 'Retail'
)

$ErrorActionPreference = 'Stop'
$expectedCommit = 'f27286a363aa675a0422141cb96fc8619cf8b9d8'
$expectedLicenseHash = '81cbae84a29ce7e770bf2bc7b178e50bda0ce8de6067aba661b0bc7b05b562f8'
$expectedProjectHash = '0b773980fa286d42fe6454c093ec1feb1dfb33693d19c70ba7cd06cbeded4c13'
$expectedPatchHash = switch ($Mode) {
    'Pr1Capture' { 'e647a85f4d19b4e28032b42ab0ff993b79b69708e2d86dad3ade34115a0ca9f6' }
    'Pr1Runtime' { 'e89e14e6986d7246990f6787e5515b6429242c9e115793af14b682c6cb8238ce' }
    default { '286de58683edd45065f884b201109815b7252a6d8b3baf896e0a4ea68b03dadb' }
}
$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$patchName = switch ($Mode) {
    'Pr1Capture' { 'SafeDiscLoader2-MW4-BlackKnight-PR1-Capture.patch' }
    'Pr1Runtime' { 'SafeDiscLoader2-MW4-BlackKnight-PR1-Runtime.patch' }
    default { 'SafeDiscLoader2-MW4-BlackKnight.patch' }
}
$patch = Join-Path $projectRoot (Join-Path 'third_party/patches' $patchName)

$source = (Resolve-Path -LiteralPath $SourceRoot -ErrorAction Stop).Path
$project = Join-Path $source 'version-proxy.vcxproj'
$license = Join-Path $source 'LICENSE'
if (-not (Test-Path -LiteralPath $project -PathType Leaf)) { throw "Pinned loader project is missing: $project" }
if (-not (Test-Path -LiteralPath $license -PathType Leaf)) { throw "Pinned loader license is missing: $license" }

$actualCommit = (& git -C $source rev-parse HEAD).Trim().ToLowerInvariant()
if ($LASTEXITCODE -ne 0 -or $actualCommit -ne $expectedCommit) {
    throw "SafeDiscLoader2 source revision must be $expectedCommit; found $actualCommit"
}
if ((& git -C $source status --porcelain).Count -ne 0) {
    throw 'SafeDiscLoader2 source tree must be clean before building.'
}

function Assert-Hash {
    param([string]$Path, [string]$Expected, [string]$Label)
    $actual = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actual -ne $Expected) { throw "$Label SHA-256 mismatch: $actual" }
}

Assert-Hash $license $expectedLicenseHash 'SafeDiscLoader2 license'
Assert-Hash $project $expectedProjectHash 'SafeDiscLoader2 project'
Assert-Hash $patch $expectedPatchHash 'SafeDiscLoader2 MW4 patch'

if ([string]::IsNullOrWhiteSpace($MSBuildPath)) {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
    if (-not (Test-Path -LiteralPath $vswhere -PathType Leaf)) { throw 'Visual Studio locator was not found.' }
    $MSBuildPath = (& $vswhere -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1)
}
if ([string]::IsNullOrWhiteSpace($MSBuildPath) -or -not (Test-Path -LiteralPath $MSBuildPath -PathType Leaf)) {
    throw 'MSBuild with the Visual C++ x86/x64 workload is required.'
}

$buildScratch = Join-Path ([IO.Path]::GetTempPath()) ('mw4-safedisc-loader2-' + [Guid]::NewGuid().ToString('N'))
& git -C $source worktree add --detach $buildScratch $expectedCommit
if ($LASTEXITCODE -ne 0) { throw 'Could not create an isolated SafeDiscLoader2 build worktree.' }
try {
& git -C $buildScratch apply $patch
if ($LASTEXITCODE -ne 0) { throw 'Could not apply the pinned MW4 Black Knight source patch.' }
$project = Join-Path $buildScratch 'version-proxy.vcxproj'

& $MSBuildPath $project /m /t:Rebuild /p:Configuration=Release /p:Platform=Win32 /p:PlatformToolset=v143 /p:Deterministic=true /verbosity:minimal
if ($LASTEXITCODE -ne 0) { throw "SafeDiscLoader2 build failed with exit code $LASTEXITCODE." }

$builtDll = Join-Path $buildScratch 'Release/version.dll'
if (-not (Test-Path -LiteralPath $builtDll -PathType Leaf)) { throw "Build did not produce the expected DLL: $builtDll" }

function Clear-PeTimestamps {
    param([Parameter(Mandatory)][string]$Path)
    $bytes = [IO.File]::ReadAllBytes($Path)
    if ([BitConverter]::ToUInt16($bytes, 0) -ne 0x5A4D) { throw 'Built compatibility DLL has no DOS header.' }
    $peOffset = [BitConverter]::ToInt32($bytes, 0x3C)
    if ([BitConverter]::ToUInt32($bytes, $peOffset) -ne 0x00004550) { throw 'Built compatibility DLL has no PE header.' }

    [Array]::Clear($bytes, $peOffset + 8, 4)
    $sectionCount = [BitConverter]::ToUInt16($bytes, $peOffset + 6)
    $optionalSize = [BitConverter]::ToUInt16($bytes, $peOffset + 20)
    $optionalOffset = $peOffset + 24
    $magic = [BitConverter]::ToUInt16($bytes, $optionalOffset)
    $dataDirectoryOffset = $optionalOffset + $(if ($magic -eq 0x10B) { 96 } elseif ($magic -eq 0x20B) { 112 } else { throw 'Unsupported PE optional-header format.' })
    $debugRva = [BitConverter]::ToUInt32($bytes, $dataDirectoryOffset + (6 * 8))
    $debugSize = [BitConverter]::ToUInt32($bytes, $dataDirectoryOffset + (6 * 8) + 4)
    $sectionOffset = $optionalOffset + $optionalSize
    $debugFileOffset = $null
    for ($index = 0; $index -lt $sectionCount; $index++) {
        $header = $sectionOffset + ($index * 40)
        $virtualSize = [BitConverter]::ToUInt32($bytes, $header + 8)
        $virtualAddress = [BitConverter]::ToUInt32($bytes, $header + 12)
        $rawSize = [BitConverter]::ToUInt32($bytes, $header + 16)
        $rawOffset = [BitConverter]::ToUInt32($bytes, $header + 20)
        $mappedSize = [Math]::Max($virtualSize, $rawSize)
        if ($debugRva -ge $virtualAddress -and $debugRva -lt ($virtualAddress + $mappedSize)) {
            $debugFileOffset = $rawOffset + ($debugRva - $virtualAddress)
            break
        }
    }
    if ($debugSize -gt 0 -and $null -eq $debugFileOffset) { throw 'Could not map the PE debug directory.' }
    for ($offset = 0; $offset -lt $debugSize; $offset += 28) {
        [Array]::Clear($bytes, [int]($debugFileOffset + $offset + 4), 4)
    }
    [IO.File]::WriteAllBytes($Path, $bytes)
}

# MSVC stamps the COFF header and IMAGE_DEBUG_DIRECTORY even for otherwise identical
# release builds. These fields do not affect execution; clearing them makes the declared
# source/toolset build bit-reproducible without patching executable code or data.
Clear-PeTimestamps $builtDll

$stream = [IO.File]::OpenRead($builtDll)
try {
    $reader = [IO.BinaryReader]::new($stream)
    if ($reader.ReadUInt16() -ne 0x5A4D) { throw 'Built compatibility DLL has no DOS header.' }
    $stream.Position = 0x3C
    $peOffset = $reader.ReadInt32()
    $stream.Position = $peOffset
    if ($reader.ReadUInt32() -ne 0x00004550) { throw 'Built compatibility DLL has no PE header.' }
    if ($reader.ReadUInt16() -ne 0x014C) { throw 'Built compatibility DLL is not x86.' }
}
finally {
    if ($null -ne $reader) { $reader.Dispose() } else { $stream.Dispose() }
}

$output = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $output -Force | Out-Null
$outputDllName = if ($Mode -eq 'Pr1Capture') { 'BlackKnightPr1Capture.dll' } else { 'version.dll' }
$outputDll = Join-Path $output $outputDllName
Copy-Item -LiteralPath $builtDll -Destination $outputDll -Force
Copy-Item -LiteralPath $license -Destination (Join-Path $output 'SafeDiscLoader2-LICENSE.txt') -Force
Copy-Item -LiteralPath $patch -Destination (Join-Path $output $patchName) -Force
$sourceArchive = Join-Path $output "SafeDiscLoader2-source-$expectedCommit.zip"
& git -C $source archive --format=zip --output=$sourceArchive $expectedCommit
if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $sourceArchive -PathType Leaf)) {
    throw 'Could not create the corresponding GPL source archive.'
}

$dll = Get-Item -LiteralPath $outputDll
$metadata = [ordered]@{
    name = 'SafeDiscLoader2'
    mode = $Mode
    sourceRepository = 'https://github.com/nckstwrt/SafeDiscLoader2.git'
    sourceCommit = $expectedCommit
    localPatchSha256 = $expectedPatchHash
    license = 'GPL-3.0-only'
    platform = 'Win32'
    configuration = 'Release'
    platformToolset = 'v143'
    msbuildPath = [IO.Path]::GetFullPath($MSBuildPath)
    msbuildVersion = (& $MSBuildPath -version -nologo | Select-Object -Last 1).Trim()
    versionDllSize = $dll.Length
    versionDllSha256 = (Get-FileHash -LiteralPath $outputDll -Algorithm SHA256).Hash.ToLowerInvariant()
    sourceArchiveSha256 = (Get-FileHash -LiteralPath $sourceArchive -Algorithm SHA256).Hash.ToLowerInvariant()
}
$metadata | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $output 'build-metadata.json') -Encoding utf8

Write-Host "Built pinned SafeDiscLoader2 $Mode x86 DLL: $outputDll"
Write-Host "SHA-256: $($metadata.versionDllSha256)"
}
finally {
    & git -C $source worktree remove --force $buildScratch 2>$null
    if (Test-Path -LiteralPath $buildScratch) {
        $resolvedScratch = [IO.Path]::GetFullPath($buildScratch)
        $tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
        if (-not $resolvedScratch.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing unsafe compatibility-build cleanup: $resolvedScratch"
        }
        Remove-Item -LiteralPath $resolvedScratch -Recurse -Force
    }
}
