[CmdletBinding()]
param(
    [string[]]$InputDirectories = @('Installation Files', 'Manuals'),
    [string]$OutputPath = '.local/media-inventory.json'
)

$ErrorActionPreference = 'Stop'
$workspace = (Resolve-Path -LiteralPath $PSScriptRoot\..).Path
$resolvedOutput = [IO.Path]::GetFullPath((Join-Path $workspace $OutputPath))
$localRoot = [IO.Path]::GetFullPath((Join-Path $workspace '.local'))
if (-not $resolvedOutput.StartsWith($localRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Inventory output must stay under the ignored .local directory.'
}

$items = foreach ($inputDirectory in $InputDirectories) {
    $resolvedInput = [IO.Path]::GetFullPath((Join-Path $workspace $inputDirectory))
    if (-not $resolvedInput.StartsWith($workspace + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Input directory escapes the workspace: $inputDirectory"
    }
    if (-not (Test-Path -LiteralPath $resolvedInput -PathType Container)) { continue }

    Get-ChildItem -LiteralPath $resolvedInput -File -Recurse | Sort-Object FullName | ForEach-Object {
        $relativePath = [IO.Path]::GetRelativePath($workspace, $_.FullName).Replace('\', '/')
        [ordered]@{
            relativePath = $relativePath
            length = $_.Length
            extension = $_.Extension.ToLowerInvariant()
            sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        }
    }
}

$document = [ordered]@{
    schemaVersion = 1
    generatedUtc = [DateTime]::UtcNow.ToString('o')
    note = 'Local evidence only. Hashes do not grant redistribution rights or qualify an edition.'
    files = @($items)
}

$parent = Split-Path -Parent $resolvedOutput
New-Item -ItemType Directory -Path $parent -Force | Out-Null
[IO.File]::WriteAllText($resolvedOutput, ($document | ConvertTo-Json -Depth 5), [Text.UTF8Encoding]::new($false))
Write-Host "Inventoried $($document.files.Count) files to $resolvedOutput"
