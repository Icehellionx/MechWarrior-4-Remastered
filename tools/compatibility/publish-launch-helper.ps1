[CmdletBinding()]
param(
    [string]$ProjectPath = 'src/MW4Remastered.CompatLauncher/MW4Remastered.CompatLauncher.csproj',
    [Parameter(Mandatory)]
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$project = (Resolve-Path -LiteralPath $ProjectPath -ErrorAction Stop).Path
$output = [IO.Path]::GetFullPath($OutputDirectory)
$scratch = Join-Path ([IO.Path]::GetTempPath()) ('mw4-compat-publish-' + [Guid]::NewGuid().ToString('N'))

try {
    & dotnet publish $project --configuration Release --runtime win-x86 --self-contained true `
        -p:PublishSingleFile=true -p:PublishTrimmed=true -p:TrimMode=full `
        -p:DebugType=None -p:DebugSymbols=false --output $scratch
    if ($LASTEXITCODE -ne 0) { throw "Compatibility helper publish failed with exit code $LASTEXITCODE." }

    $published = Get-ChildItem -LiteralPath $scratch -File
    if ($published.Count -ne 1 -or $published[0].Name -ne 'MW4RemasteredCompatLauncher.exe') {
        throw 'Compatibility helper publish must produce exactly one executable.'
    }

    $helper = $published[0].FullName
    $stream = [IO.File]::OpenRead($helper)
    try {
        $reader = [IO.BinaryReader]::new($stream)
        if ($reader.ReadUInt16() -ne 0x5A4D) { throw 'Published compatibility helper has no DOS header.' }
        $stream.Position = 0x3C
        $peOffset = $reader.ReadInt32()
        $stream.Position = $peOffset
        if ($reader.ReadUInt32() -ne 0x00004550) { throw 'Published compatibility helper has no PE header.' }
        if ($reader.ReadUInt16() -ne 0x014C) { throw 'Published compatibility helper is not x86.' }
    }
    finally {
        if ($null -ne $reader) { $reader.Dispose() } else { $stream.Dispose() }
    }

    $manifestTool = Get-ChildItem -LiteralPath (Join-Path ${env:ProgramFiles(x86)} 'Windows Kits/10/bin') `
        -Filter mt.exe -Recurse -ErrorAction Stop |
        Where-Object { $_.Directory.Name -eq 'x64' } |
        Sort-Object FullName -Descending |
        Select-Object -First 1
    if ($null -eq $manifestTool) { throw 'Windows manifest tool was not found.' }
    $manifestPath = Join-Path $scratch 'embedded.manifest'
    & $manifestTool.FullName "-inputresource:$helper;#1" "-out:$manifestPath"
    if ($LASTEXITCODE -ne 0) { throw 'Could not extract the compatibility helper manifest.' }
    [xml]$manifest = Get-Content -LiteralPath $manifestPath -Raw
    $executionLevel = $manifest.SelectSingleNode("//*[local-name()='requestedExecutionLevel']")
    if ($null -eq $executionLevel -or $executionLevel.level -ne 'asInvoker') {
        throw 'Published compatibility helper must be explicitly asInvoker.'
    }

    New-Item -ItemType Directory -Path $output -Force | Out-Null
    $destination = Join-Path $output 'MW4RemasteredCompatLauncher.exe'
    Copy-Item -LiteralPath $helper -Destination $destination -Force
    $file = Get-Item -LiteralPath $destination
    [ordered]@{
        name = $file.Name
        runtime = 'win-x86'
        selfContained = $true
        singleFile = $true
        trimmed = $true
        executionLevel = 'asInvoker'
        size = $file.Length
        sha256 = (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash.ToLowerInvariant()
    } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'launch-helper-metadata.json') -Encoding utf8

    Write-Host "Published self-contained x86 compatibility helper: $destination"
}
finally {
    if (Test-Path -LiteralPath $scratch) {
        $resolvedScratch = (Resolve-Path -LiteralPath $scratch).Path
        $tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
        if (-not $resolvedScratch.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing unsafe publish cleanup: $resolvedScratch"
        }
        Remove-Item -LiteralPath $resolvedScratch -Recurse -Force
    }
}
