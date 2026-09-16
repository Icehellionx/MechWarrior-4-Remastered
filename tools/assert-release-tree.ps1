[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Root,
    [string[]]$AllowedExecutablePaths = @()
)

$ErrorActionPreference = 'Stop'
$resolvedRoot = (Resolve-Path -LiteralPath $Root -ErrorAction Stop).Path
if (-not (Test-Path -LiteralPath $resolvedRoot -PathType Container)) { throw "Release root is not a directory: $resolvedRoot" }
if ((Get-Item -LiteralPath $resolvedRoot -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) {
    throw "Release root must not be a reparse point: $resolvedRoot"
}

$allowedExecutables = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($relativePath in $AllowedExecutablePaths) {
    $normalized = $relativePath.Replace('\', '/').TrimStart('/')
    if ($normalized -match '(^|/)\.\.(/|$)' -or [IO.Path]::IsPathRooted($relativePath)) {
        throw "Allowed executable path must be a contained relative path: $relativePath"
    }
    [void]$allowedExecutables.Add($normalized)
}

$forbiddenExtensions = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
@('.iso','.bin','.cue','.mdf','.mds','.nrg','.ccd','.img','.sub','.pfx','.p12','.snk','.key','.pem','.dmp') | ForEach-Object { [void]$forbiddenExtensions.Add($_) }
$executableExtensions = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
@('.exe','.dll','.sys','.scr','.com','.bat','.cmd','.ps1') | ForEach-Object { [void]$executableExtensions.Add($_) }
$forbiddenNames = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
@('.env','secdrv.sys','cdac14ba.dll','cdac21ba.dll','scshd.csa','scshd.exe','versioninjector.exe','unsafedisc155.exe','unsafedisc155_version.exe') | ForEach-Object { [void]$forbiddenNames.Add($_) }
$forbiddenSegments = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
@('crack','nocd','razor1911') | ForEach-Object { [void]$forbiddenSegments.Add($_) }

$violations = [Collections.Generic.List[string]]::new()
Get-ChildItem -LiteralPath $resolvedRoot -Recurse -Force | ForEach-Object {
    if ($_.Attributes -band [IO.FileAttributes]::ReparsePoint) {
        $violations.Add("reparse point: $($_.FullName)")
        return
    }
    if (-not $_.PSIsContainer) {
        $relative = [IO.Path]::GetRelativePath($resolvedRoot, $_.FullName).Replace('\', '/')
        $segments = $relative.Split('/')
        if ($segments | Where-Object { $forbiddenSegments.Contains($_) }) { $violations.Add("forbidden path segment: $relative") }
        if ($forbiddenExtensions.Contains($_.Extension)) { $violations.Add("forbidden extension: $relative") }
        if ($forbiddenNames.Contains($_.Name)) { $violations.Add("forbidden filename: $relative") }
        if ($_.Name -like '.env*') { $violations.Add("environment/secret file: $relative") }
        if ($_.Name -match '(?i)serial.*key|keygen') { $violations.Add("serial/key material: $relative") }
        if ($executableExtensions.Contains($_.Extension) -and -not $allowedExecutables.Contains($relative)) {
            $violations.Add("executable is not allowlisted: $relative")
        }
    }
}

if ($violations.Count -gt 0) { throw ("Release tree policy failed:`n - " + ($violations -join "`n - ")) }
Write-Host "Release tree policy passed: $resolvedRoot"
