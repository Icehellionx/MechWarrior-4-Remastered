[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$SourceRepository,
    [Parameter(Mandatory)]
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$source = (Resolve-Path -LiteralPath $SourceRepository).Path
$output = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $output) { throw "DDrawCompat output already exists: $output" }

$tag = 'v0.7.1-mw3-r18'
$commit = '73ac0f47af16a1d28dcda25c3228053beb3eb5f4'
$patchPath = Join-Path $projectRoot 'third_party/patches/DDrawCompat-MW4-Surface-Retry.patch'
$patchHash = 'ad88d82ae9ec03be4a85c7c08ae3dd1e3c9c47fc88821dafdbb2460a42e44d98'
$qualifiedDllHash = 'b589c27402c283f699857aec26948b33595ee93f645891ec4f9607254148b509'
$safeDirectory = $source.Replace('\', '/')
$tagCommit = (& git -c "safe.directory=$safeDirectory" -C $source rev-list -n 1 $tag).Trim()
if ($LASTEXITCODE -ne 0 -or $tagCommit -ne $commit) { throw "DDrawCompat tag $tag does not resolve to the pinned commit." }
if ((Get-FileHash -LiteralPath $patchPath -Algorithm SHA256).Hash.ToLowerInvariant() -ne $patchHash) {
    throw 'DDrawCompat MW4 patch hash mismatch.'
}

$scratch = Join-Path ([IO.Path]::GetTempPath()) 'mw4-ddrawcompat-reproducible-build'
if (Test-Path -LiteralPath $scratch) {
    throw "Deterministic DDrawCompat scratch directory is already occupied: $scratch"
}
try {
    & git -c "safe.directory=$safeDirectory" -c "safe.directory=$safeDirectory/.git" clone --quiet --no-hardlinks $source $scratch
    if ($LASTEXITCODE -ne 0) { throw 'Could not clone the pinned DDrawCompat source locally.' }
    & git -C $scratch checkout --quiet --detach $commit
    if ($LASTEXITCODE -ne 0) { throw 'Could not check out the pinned DDrawCompat source commit.' }
    & git -C $scratch apply --check $patchPath
    if ($LASTEXITCODE -ne 0) { throw 'DDrawCompat MW4 patch does not apply cleanly.' }
    & git -C $scratch apply $patchPath
    if ($LASTEXITCODE -ne 0) { throw 'Could not apply the DDrawCompat MW4 patch.' }

    # The upstream project hard-codes full PDB generation as item metadata, which a
    # command-line MSBuild property cannot override. Release PDB identities and their
    # absolute scratch paths make otherwise equivalent DLLs differ, so disable them in
    # the disposable build tree. This does not modify the pinned source checkout.
    $projectPath = Join-Path $scratch 'DDrawCompat/DDrawCompat.vcxproj'
    $projectText = [IO.File]::ReadAllText($projectPath)
    $projectText = $projectText.Replace('<DebugInformationFormat>ProgramDatabase</DebugInformationFormat>', '<DebugInformationFormat>None</DebugInformationFormat>')
    $projectText = $projectText.Replace('<GenerateDebugInformation>DebugFull</GenerateDebugInformation>', '<GenerateDebugInformation>false</GenerateDebugInformation>')
    $projectText = $projectText.Replace('<ConformanceMode>true</ConformanceMode>', "<ConformanceMode>true</ConformanceMode>`r`n      <AdditionalOptions>/Brepro %(AdditionalOptions)</AdditionalOptions>")
    $projectText = $projectText.Replace('<OptimizeReferences>true</OptimizeReferences>', "<OptimizeReferences>true</OptimizeReferences>`r`n      <AdditionalOptions>/Brepro %(AdditionalOptions)</AdditionalOptions>")
    [IO.File]::WriteAllText($projectPath, $projectText, [Text.UTF8Encoding]::new($false))

    $msbuildCandidates = @(
        'C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\amd64\MSBuild.exe',
        'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\amd64\MSBuild.exe'
    )
    $msbuild = $msbuildCandidates | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
    if (-not $msbuild) { throw 'Visual Studio 2022 C++ build tools were not found.' }
    & $msbuild (Join-Path $scratch 'DDrawCompat.sln') /m:1 /p:Configuration=Release /p:Platform=x86 `
        /p:UseMultiToolTask=false /p:CL_MPCount=1 /p:Deterministic=true `
        /p:DebugInformationFormat=None /p:GenerateDebugInformation=false /v:minimal
    if ($LASTEXITCODE -ne 0) { throw "DDrawCompat build failed with exit code $LASTEXITCODE." }

    $dll = Join-Path $scratch 'Release/ddraw.dll'
    if (-not (Test-Path -LiteralPath $dll -PathType Leaf)) { throw 'DDrawCompat build did not produce Release/ddraw.dll.' }

    function Clear-PeTimestamps {
        param([Parameter(Mandatory)][string]$Path)
        $bytes = [IO.File]::ReadAllBytes($Path)
        if ([BitConverter]::ToUInt16($bytes, 0) -ne 0x5A4D) { throw 'Built DDrawCompat DLL has no DOS header.' }
        $peOffset = [BitConverter]::ToInt32($bytes, 0x3C)
        if ([BitConverter]::ToUInt32($bytes, $peOffset) -ne 0x00004550) { throw 'Built DDrawCompat DLL has no PE header.' }

        # MSVC stamps the COFF header and linker debug-directory entries even for
        # otherwise identical release builds. These fields do not affect execution.
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

    Clear-PeTimestamps $dll
    $actualDllHash = (Get-FileHash -LiteralPath $dll -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actualDllHash -ne $qualifiedDllHash) {
        throw "DDrawCompat build is not the qualified byte-reproducible artifact: $actualDllHash"
    }
    $stream = [IO.File]::OpenRead($dll)
    try {
        $reader = [IO.BinaryReader]::new($stream)
        $stream.Position = 0x3c
        $peOffset = $reader.ReadInt32()
        $stream.Position = $peOffset
        if ($reader.ReadUInt32() -ne 0x00004550 -or $reader.ReadUInt16() -ne 0x014c) {
            throw 'DDrawCompat output is not an x86 PE image.'
        }
    } finally {
        if ($reader) { $reader.Dispose() } else { $stream.Dispose() }
    }

    New-Item -ItemType Directory -Path $output | Out-Null
    Copy-Item -LiteralPath $dll -Destination (Join-Path $output 'ddraw.dll')
    Copy-Item -LiteralPath (Join-Path $scratch 'LICENSE.txt') -Destination (Join-Path $output 'DDrawCompat-LICENSE.txt')
    Copy-Item -LiteralPath $patchPath -Destination (Join-Path $output 'DDrawCompat-MW4-Surface-Retry.patch')
    $archive = Join-Path $output "DDrawCompat-MW3-source-$commit.zip"
    & git -c "safe.directory=$safeDirectory" -C $source archive --format=zip "--output=$archive" $tag
    if ($LASTEXITCODE -ne 0) { throw 'Could not archive the pinned DDrawCompat source revision.' }
} finally {
    if (Test-Path -LiteralPath $scratch) { Remove-Item -LiteralPath $scratch -Recurse -Force }
}

Write-Host "Built pinned, patched DDrawCompat MW4 bundle: $output"
Get-ChildItem -LiteralPath $output -File | ForEach-Object {
    Write-Host "$($_.Name) $((Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant())"
}
