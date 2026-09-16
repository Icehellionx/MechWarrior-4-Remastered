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
if (Test-Path -LiteralPath $output) { throw "Compatibility bundle output already exists: $output" }

$files = [ordered]@{
    'MW4RemasteredCompatLauncher.exe' = 'b80b440bf349438527e28547bf40276defc309c64b972400acf625a974eaab8d'
    'version.dll' = 'f26710840b1b6b0537c05b3e97c63171708c129b76a77ec3191d5652ee024959'
    'SafeDiscLoader2-LICENSE.txt' = '81cbae84a29ce7e770bf2bc7b178e50bda0ce8de6067aba661b0bc7b05b562f8'
    'SafeDiscLoader2-source-f27286a363aa675a0422141cb96fc8619cf8b9d8.zip' = '78ae295db0382f498829546ff7272db5eb2e713c20eb72ab3552ceaf8477cd17'
}

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
    [IO.Directory]::Move($staging, $output)
}
finally {
    if (Test-Path -LiteralPath $staging) { Remove-Item -LiteralPath $staging -Recurse -Force }
}

Write-Host "Assembled exact Black Knight compatibility bundle: $output"
