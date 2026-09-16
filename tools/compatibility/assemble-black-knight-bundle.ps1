[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$EvidenceDirectory,
    [Parameter(Mandatory)]
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$evidence = (Resolve-Path -LiteralPath $EvidenceDirectory -ErrorAction Stop).Path
$evidenceItem = Get-Item -LiteralPath $evidence -Force
if (-not $evidenceItem.PSIsContainer) { throw "Compatibility evidence must be a directory: $evidence" }
if ($evidenceItem.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "Compatibility evidence directory cannot be a reparse point: $evidence" }
$output = [IO.Path]::GetFullPath($OutputDirectory)
$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$configuration = Join-Path $projectRoot 'assets/compatibility/black-knight-version.json'
if (Test-Path -LiteralPath $output) { throw "Compatibility bundle output already exists: $output" }

$files = [ordered]@{
    'version.dll' = 'ff530c8144ebf82b3f32951c8cd3b1085b6527b46d55348bb1c7f8ef7f83296a'
    'SafeDiscLoader2-LICENSE.txt' = '81cbae84a29ce7e770bf2bc7b178e50bda0ce8de6067aba661b0bc7b05b562f8'
    'SafeDiscLoader2-source-f27286a363aa675a0422141cb96fc8619cf8b9d8.zip' = 'e96558eee5570e4085fcb9b36cbc3c30ae3ff3b2e2c02a67d54c008c920f0490'
    'SafeDiscLoader2-MW4-BlackKnight.patch' = '286de58683edd45065f884b201109815b7252a6d8b3baf896e0a4ea68b03dadb'
}
$configurationHash = '8208a30c47f55c3f497689d7ab9ed1e88c9c7d11e028ee14d8508df15f6f2cbd'

$outputParent = [IO.Path]::GetDirectoryName($output)
if ([string]::IsNullOrWhiteSpace($outputParent)) { throw "Compatibility bundle output must have a parent directory: $output" }
New-Item -ItemType Directory -Path $outputParent -Force | Out-Null
$staging = Join-Path $outputParent ('.' + [IO.Path]::GetFileName($output) + '.staging-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $staging -Force | Out-Null
try {
    foreach ($entry in $files.GetEnumerator()) {
        $source = Join-Path $evidence $entry.Key
        if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { throw "Qualified compatibility artifact is missing: $($entry.Key)" }
        if ((Get-Item -LiteralPath $source -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "Qualified compatibility artifact cannot be a reparse point: $($entry.Key)" }
        $actual = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($actual -ne $entry.Value) { throw "Qualified compatibility artifact SHA-256 mismatch for $($entry.Key): $actual" }
        Copy-Item -LiteralPath $source -Destination (Join-Path $staging $entry.Key)
    }
    $actualConfigurationHash = (Get-FileHash -LiteralPath $configuration -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actualConfigurationHash -ne $configurationHash) {
        throw "Black Knight loader configuration SHA-256 mismatch: $actualConfigurationHash"
    }
    Copy-Item -LiteralPath $configuration -Destination (Join-Path $staging 'version.json')
    [IO.Directory]::Move($staging, $output)
}
finally {
    if (Test-Path -LiteralPath $staging) { Remove-Item -LiteralPath $staging -Recurse -Force }
}

Write-Host "Assembled exact Black Knight compatibility bundle: $output"
