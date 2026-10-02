[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$SourceRoot,
    [Parameter(Mandatory)][string]$OutputDirectory,
    [switch]$Requalify,
    [string]$MSBuildPath = 'C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/MSBuild.exe',
    [string]$VsDevCmd = 'C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/Common7/Tools/VsDevCmd.bat'
)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$lock = Get-Content -LiteralPath (Join-Path $root 'third_party/dinputto8-mw4-trial.lock.json') -Raw | ConvertFrom-Json
$source = (Resolve-Path -LiteralPath $SourceRoot).Path
$logging = Join-Path $source 'External/Logging'
$output = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $output) { throw 'Trial output must be a fresh directory.' }
if (-not $output.StartsWith($root + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Trial output must be inside the project workspace.'
}
foreach ($pair in @(@($source, $lock.commit), @($logging, $lock.loggingCommit))) {
    $revision = & git -C $pair[0] rev-parse HEAD
    if ($LASTEXITCODE -ne 0 -or $revision.Trim() -ne $pair[1]) { throw 'Unexpected upstream source revision.' }
}
foreach ($pair in @(@((Join-Path $source 'License.txt'), $lock.licenseSha256),
                    @((Join-Path $root $lock.localPatch), $lock.localPatchSha256),
                    @((Join-Path $root $lock.policy), $lock.policySha256))) {
    if ((Get-FileHash -LiteralPath $pair[0] -Algorithm SHA256).Hash.ToLowerInvariant() -ne $pair[1]) {
        throw 'Trial source/patch/license hash mismatch.'
    }
}
if (-not (Test-Path -LiteralPath $MSBuildPath) -or -not (Test-Path -LiteralPath $VsDevCmd)) {
    throw 'Visual Studio C++ x86 build tools are required.'
}
[IO.Directory]::CreateDirectory($output) | Out-Null
$build = Join-Path $output 'source'
& git -C $source archive --format=zip "--output=$output/upstream.zip" $lock.commit
if ($LASTEXITCODE -ne 0) { throw 'Upstream archive failed.' }
Expand-Archive -LiteralPath (Join-Path $output 'upstream.zip') -DestinationPath $build
& git -C $logging archive --format=zip "--output=$output/logging.zip" $lock.loggingCommit
if ($LASTEXITCODE -ne 0) { throw 'Logging source archive failed.' }
Expand-Archive -LiteralPath (Join-Path $output 'logging.zip') -DestinationPath (Join-Path $build 'External/Logging')
$relativeBuild = [IO.Path]::GetRelativePath($root, $build).Replace('\', '/')
& git -C $root apply "--directory=$relativeBuild" --check (Join-Path $root $lock.localPatch)
if ($LASTEXITCODE -ne 0) { throw 'Trial patch preflight failed.' }
& git -C $root apply "--directory=$relativeBuild" (Join-Path $root $lock.localPatch)
if ($LASTEXITCODE -ne 0) { throw 'Trial patch failed.' }
if ([IO.File]::ReadAllText((Join-Path $build 'IDirectInputDeviceX.cpp')) -notmatch 'mw4input::ClearHiddenState' -or
    [IO.File]::ReadAllText((Join-Path $build 'IDirectInputDeviceX.h')) -notmatch 'DWORD ButtonLimit\(\)') {
    throw 'Trial patch did not modify the build inputs.'
}
Copy-Item -LiteralPath (Join-Path $root $lock.policy) -Destination (Join-Path $build 'MW4ButtonPolicy.h')
# Deterministic linker option belongs only to this trial build; no upstream files are edited in-place.
$project = Join-Path $build 'dinputto8.vcxproj'
$xml = [IO.File]::ReadAllText($project).Replace('<OptimizeReferences>true</OptimizeReferences>',
    '<OptimizeReferences>true</OptimizeReferences><GenerateDebugInformation>false</GenerateDebugInformation><EnableCOMDATFolding>false</EnableCOMDATFolding><AdditionalOptions>/Brepro /OPT:NOICF %(AdditionalOptions)</AdditionalOptions>')
$xml = $xml.Replace('</ClCompile>', '<DebugInformationFormat>None</DebugInformationFormat><AdditionalOptions>/Brepro %(AdditionalOptions)</AdditionalOptions></ClCompile>')
[IO.File]::WriteAllText($project, $xml, [Text.UTF8Encoding]::new($false))
& $MSBuildPath $project /m /t:Rebuild /p:Configuration=Workflow /p:Platform=Win32 /p:PlatformToolset=v143 "/p:SolutionDir=$build/" /p:LinkIncremental=false /verbosity:minimal
if ($LASTEXITCODE -ne 0) { throw 'Adapter compilation failed.' }
$dll = Join-Path $build 'bin/Win32/Workflow/dinput.dll'
Copy-Item -LiteralPath $dll -Destination (Join-Path $output 'dinput.dll')
if (-not $Requalify -and $lock.candidateBinarySha256 -and
    ((Get-FileHash -LiteralPath $dll -Algorithm SHA256).Hash.ToLowerInvariant() -ne $lock.candidateBinarySha256 -or
     (Get-Item -LiteralPath $dll).Length -ne $lock.candidateBinaryBytes)) {
    throw 'Trial binary differs from the recorded candidate; retain it as evidence and requalify before use.'
}
Copy-Item -LiteralPath (Join-Path $source 'License.txt') -Destination (Join-Path $output 'dinputto8-LICENSE.txt')
$test = Join-Path $root 'tests/MW4ButtonPolicy.Tests.cpp'
$probe = Join-Path $PSScriptRoot 'DirectInputAdapterProbe.cpp'
$batch = Join-Path $output 'compile-tests.cmd'
[IO.File]::WriteAllLines($batch, @(
    '@echo off', "call `"$VsDevCmd`" -arch=x86 -host_arch=x64 >nul", 'if errorlevel 1 exit /b 1',
    "cl /nologo /W4 /WX /EHsc /std:c++17 /MT `"$test`" /Fe:`"$output\MW4ButtonPolicy.Tests.exe`" /Fo:`"$output\policy.obj`"",
    'if errorlevel 1 exit /b 1', "`"$output\MW4ButtonPolicy.Tests.exe`"", 'if errorlevel 1 exit /b 1',
    "cl /nologo /W4 /WX /EHsc /std:c++17 /MT `"$probe`" /Fe:`"$output\DirectInputAdapterProbe.exe`" /Fo:`"$output\probe.obj`" dinput8.lib dxguid.lib user32.lib",
    'exit /b %errorlevel%'
), [Text.ASCIIEncoding]::new())
& cmd.exe /c $batch
if ($LASTEXITCODE -ne 0) { throw 'Policy/probe compilation or tests failed.' }
$wrapperProject = Join-Path $build 'wrapper-tests.vcxproj'
$wrapperTestSource = Join-Path $root 'tests/DirectInputWrapper.Tests.cpp'
Copy-Item -LiteralPath $wrapperTestSource -Destination (Join-Path $build 'DirectInputWrapper.Tests.cpp')
$testXml = $xml.Replace('<ConfigurationType>DynamicLibrary</ConfigurationType>', '<ConfigurationType>Application</ConfigurationType>')
$testXml = $testXml.Replace('<SubSystem>Windows</SubSystem>', '<SubSystem>Console</SubSystem>')
$testXml = $testXml.Replace('<ModuleDefinitionFile>dinput.def</ModuleDefinitionFile>', '')
$testXml = $testXml.Replace('<ClCompile Include="ClassFactory.cpp" />',
    '<ClCompile Include="DirectInputWrapper.Tests.cpp" /><ClCompile Include="ClassFactory.cpp" />')
[IO.File]::WriteAllText($wrapperProject, $testXml, [Text.UTF8Encoding]::new($false))
& $MSBuildPath $wrapperProject /m /t:Rebuild /p:Configuration=Workflow /p:Platform=Win32 /p:PlatformToolset=v143 "/p:SolutionDir=$build/" "/p:IntDir=$output/wrapper-object/" /p:TargetName=DirectInputWrapper.Tests /p:LinkIncremental=false /verbosity:minimal
if ($LASTEXITCODE -ne 0) { throw 'Actual wrapper test compilation failed.' }
$wrapperExe = Join-Path $build 'bin/Win32/Workflow/DirectInputWrapper.Tests.exe'
& $wrapperExe
if ($LASTEXITCODE -ne 0) { throw 'Actual wrapper tests failed.' }
[ordered]@{
    UpstreamCommit = $lock.commit; LoggingCommit = $lock.loggingCommit
    LocalPatchSha256 = $lock.localPatchSha256; PolicySha256 = $lock.policySha256
    AdapterSha256 = (Get-FileHash -LiteralPath (Join-Path $output 'dinput.dll') -Algorithm SHA256).Hash.ToLowerInvariant()
    AdapterBytes = (Get-Item -LiteralPath (Join-Path $output 'dinput.dll')).Length
    Qualification = 'Synthetic policy and actual-wrapper tests only; run the real adapter probe and game matrix in the isolated VM before promotion.'
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'build-evidence.json') -Encoding UTF8
Get-Content -LiteralPath (Join-Path $output 'build-evidence.json')
