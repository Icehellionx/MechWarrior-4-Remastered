[CmdletBinding()]
param([Parameter(Mandatory)][string] $OutputDirectory)

$ErrorActionPreference = 'Stop'
$output = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $output) { throw "Output already exists: $output" }
$source = Join-Path $PSScriptRoot 'Issue10DecoderBlock.cpp'
$exports = Join-Path $PSScriptRoot 'Issue10DecoderBlock.def'
$vsCandidates = @(
    'C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/Common7/Tools/VsDevCmd.bat',
    'C:/Program Files/Microsoft Visual Studio/2022/Community/Common7/Tools/VsDevCmd.bat'
)
$vsDevCmd = $vsCandidates | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
if (-not $vsDevCmd) { throw 'Visual Studio 2022 C++ Build Tools are required.' }

New-Item -ItemType Directory -Path $output | Out-Null
try {
    $batch = Join-Path $output 'build.cmd'
    $lines = @(
        '@echo off',
        "call `"$vsDevCmd`" -arch=x86 -host_arch=x64 >nul",
        'if errorlevel 1 exit /b 1',
        "cl /nologo /W4 /EHsc /LD `"$source`" `"$exports`" /link /Brepro /OUT:`"$output\Issue10DecoderBlock.dll`"",
        'exit /b %errorlevel%'
    )
    [IO.File]::WriteAllLines($batch, $lines, (New-Object Text.ASCIIEncoding))
    Push-Location $output
    try { & cmd.exe /c $batch; if ($LASTEXITCODE -ne 0) { throw "x86 build failed: $LASTEXITCODE" } }
    finally { Pop-Location }
    $dll = Join-Path $output 'Issue10DecoderBlock.dll'
    if (-not (Test-Path -LiteralPath $dll -PathType Leaf)) { throw 'Build did not emit the DLL.' }
    $hash = (Get-FileHash -LiteralPath $dll -Algorithm SHA256).Hash.ToLowerInvariant()
    Write-Host "Issue10DecoderBlock.dll $((Get-Item -LiteralPath $dll).Length) $hash"
}
finally {
    foreach ($name in @('build.cmd', 'Issue10DecoderBlock.obj', 'Issue10DecoderBlock.lib', 'Issue10DecoderBlock.exp')) {
        $path = Join-Path $output $name
        if (Test-Path -LiteralPath $path -PathType Leaf) { Remove-Item -LiteralPath $path }
    }
}
