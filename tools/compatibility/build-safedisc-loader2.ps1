[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$SourceRoot,
    [Parameter(Mandatory)]
    [string]$OutputDirectory,
    [string]$MSBuildPath
)

$ErrorActionPreference = 'Stop'
$expectedCommit = 'f27286a363aa675a0422141cb96fc8619cf8b9d8'
$expectedLicenseHash = '81cbae84a29ce7e770bf2bc7b178e50bda0ce8de6067aba661b0bc7b05b562f8'
$expectedProjectHash = '0b773980fa286d42fe6454c093ec1feb1dfb33693d19c70ba7cd06cbeded4c13'

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

if ([string]::IsNullOrWhiteSpace($MSBuildPath)) {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
    if (-not (Test-Path -LiteralPath $vswhere -PathType Leaf)) { throw 'Visual Studio locator was not found.' }
    $MSBuildPath = (& $vswhere -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1)
}
if ([string]::IsNullOrWhiteSpace($MSBuildPath) -or -not (Test-Path -LiteralPath $MSBuildPath -PathType Leaf)) {
    throw 'MSBuild with the Visual C++ x86/x64 workload is required.'
}

& $MSBuildPath $project /m /t:Rebuild /p:Configuration=Release /p:Platform=Win32 /p:PlatformToolset=v143 /p:Deterministic=true /verbosity:minimal
if ($LASTEXITCODE -ne 0) { throw "SafeDiscLoader2 build failed with exit code $LASTEXITCODE." }

$builtDll = Join-Path $source 'Release/version.dll'
if (-not (Test-Path -LiteralPath $builtDll -PathType Leaf)) { throw "Build did not produce the expected DLL: $builtDll" }

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
$outputDll = Join-Path $output 'version.dll'
Copy-Item -LiteralPath $builtDll -Destination $outputDll -Force
Copy-Item -LiteralPath $license -Destination (Join-Path $output 'SafeDiscLoader2-LICENSE.txt') -Force
$sourceArchive = Join-Path $output "SafeDiscLoader2-source-$expectedCommit.zip"
& git -C $source archive --format=zip --output=$sourceArchive $expectedCommit
if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $sourceArchive -PathType Leaf)) {
    throw 'Could not create the corresponding GPL source archive.'
}

$dll = Get-Item -LiteralPath $outputDll
$metadata = [ordered]@{
    name = 'SafeDiscLoader2'
    sourceRepository = 'https://github.com/nckstwrt/SafeDiscLoader2.git'
    sourceCommit = $expectedCommit
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

Write-Host "Built pinned SafeDiscLoader2 x86 DLL: $outputDll"
Write-Host "SHA-256: $($metadata.versionDllSha256)"
