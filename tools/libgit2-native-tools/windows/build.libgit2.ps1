<#
=============================================================================================
build.libgit2.ps1 - build git2-5853918.dll for win-x64 and/or win-arm64, from this repository
=============================================================================================

  NEVER YET RUN. Written on Linux on 2026-10-17, adapted from upstream
  libgit2sharp.nativebinaries' build.libgit2.ps1 (a verbatim copy is in
  ..\reference-upstream\build.libgit2.ps1). Expect to fix something on the first real run;
  fix it IN THE SCRIPT and record it in ..\BUILD-PROVENANCE.txt, then rewrite this header and
  README.txt's status block with what the run established.

USAGE (from a PowerShell prompt; the script finds Visual Studio and cmake itself):

    cd tools\libgit2-native-tools\windows
    .\build.libgit2.ps1 -x64
    .\build.libgit2.ps1 -arm64            # on an ARM64 machine, or cross-built on x64
    .\build.libgit2.ps1 -x64 -arm64
    .\build.libgit2.ps1 -x64 -test        # also build and run libgit2's own test suite (ctest)

WHAT IT KEEPS FROM UPSTREAM, EXACTLY
  * the cmake arguments:  -A x64|ARM64 -D USE_SSH=exec -D USE_HTTPS=Schannel
                          -D BUILD_TESTS=OFF|ON -D BUILD_CLI=OFF
                          -D LIBGIT2_FILENAME=git2-5853918
    then  cmake --build . --config Release  (Visual Studio generator, MSVC, libgit2's own
    defaults: static CRT /MT, /O2 /GL, /LTCG). HTTPS is SCHANNEL - upstream's Windows recipe
    does not use the WinHTTP backend, and neither does this one (README.txt, "WHY SCHANNEL").
  * the -test switch (BUILD_TESTS=ON + ctest -V), which works because ..\libgit2 is vendored
    WITH its tests\ folder;
  * the Assert-Consistent-Naming check on the DLL's version resource.

WHAT IT CHANGES, AND WHY
  * the source is ..\libgit2 (the vendored, unmodified snapshot), COPIED to a scratch folder
    and built there - upstream built inside its libgit2 submodule;
  * the pins (version, commit, file name, required exports) come from ..\linux\pins.env, the
    one place every platform reads them from;
  * output goes to ..\output\win-<arch>\ (not nuget.package\runtimes), with LICENSE-Libgit2.txt
    (libgit2's COPYING), LICENSE-Xdiff.txt, BUILD-INFO.txt and SHA256SUMS.txt beside the DLL;
  * a verification gate runs before anything is called a result (README.txt, THE GATE).
  * no win-x86: CodeBrix.LibGit2 ships win-x64 and win-arm64 only.

It installs nothing. Anything missing is named, with the command that installs it, and the
script stops.
=============================================================================================
#>

[CmdletBinding()]
param(
    [switch] $x64,
    [switch] $arm64,
    [switch] $test,
    # Where cmake builds and the gate runs. Kept out of the repository so the vendored source
    # and the output tree stay clean.
    [string] $BuildRoot = (Join-Path $env:TEMP 'codebrix-libgit2-build')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ScriptDir = $PSScriptRoot
$ToolsDir  = Split-Path -Parent $ScriptDir
$OutputDir = Join-Path $ToolsDir 'output'

# ---------------------------------------------------------------------------------------------
# Pins - ..\linux\pins.env is the one source of truth. Only its platform-neutral keys are used.
# ---------------------------------------------------------------------------------------------
function Read-Pins {
    param([Parameter(Mandatory)][string] $Path)
    if (-not (Test-Path -LiteralPath $Path)) {
        throw "pins.env was not found at $Path. It is part of the repository - if it is missing after a clone, check the root .gitignore's blanket '*.env' rule (see ..\.gitignore)."
    }
    $pins = @{}
    foreach ($line in Get-Content -LiteralPath $Path) {
        $trimmed = $line.Trim()
        if ($trimmed -eq '' -or $trimmed.StartsWith('#')) { continue }
        $eq = $trimmed.IndexOf('=')
        if ($eq -lt 1) { continue }
        $key = $trimmed.Substring(0, $eq).Trim()
        if ($key -notmatch '^[A-Z0-9_]+$') { continue }      # continuation lines of a multi-line value
        $value = $trimmed.Substring($eq + 1).Trim()
        if ($value.Length -ge 2 -and $value.StartsWith('"') -and $value.EndsWith('"')) {
            $value = $value.Substring(1, $value.Length - 2)
        }
        $pins[$key] = $value
    }
    foreach ($required in @('LIBGIT2_DIR', 'LIBGIT2_VERSION', 'LIBGIT2_COMMIT', 'LIBGIT2_FILENAME', 'REQUIRED_SYMBOLS', 'LICENSE_FILE_NAME')) {
        if (-not $pins.ContainsKey($required)) { throw "pins.env has no $required." }
    }
    return $pins
}

$Pins = Read-Pins (Join-Path $ToolsDir 'linux\pins.env')
$DllName = "$($Pins.LIBGIT2_FILENAME).dll"
$SourceDir = Join-Path $ToolsDir $Pins.LIBGIT2_DIR

# The bit mask git_libgit2_features() must return: THREADS 1 | HTTPS 2 | SSH 4 | NSEC 8 = 15 -
# what the adopted upstream 2.0.324 DLLs return (read from their machine code on 2026-10-17;
# ..\BUILD-PROVENANCE.txt). A build that differs from upstream here fails the smoke test.
$ExpectedFeatures = '15'

# DLLs the shipped library may import: the operating system only. The static CRT means no
# VCRUNTIME*/MSVCP*/ucrtbase/api-ms-win-crt-* - the gate fails on any of those.
$AllowedImports = @('ADVAPI32.dll', 'CRYPT32.dll', 'KERNEL32.dll', 'Secur32.dll', 'WS2_32.dll',
                    'ole32.dll', 'RPCRT4.dll', 'USER32.dll', 'SHELL32.dll', 'bcrypt.dll')

# ---------------------------------------------------------------------------------------------
# Upstream's helpers, kept as they were (Run-Command, Find-CMake, Ensure-Property,
# Assert-Consistent-Naming).
# ---------------------------------------------------------------------------------------------
function Run-Command([scriptblock]$Command, [switch]$Fatal, [switch]$Quiet) {
    $output = ""
    if ($Quiet) {
        $output = & $Command 2>&1
    } else {
        & $Command
    }
    if (!$Fatal) { return }
    $exitCode = 0
    if ($LastExitCode -ne 0) {
        $exitCode = $LastExitCode
    } elseif (!$?) {
        $exitCode = 1
    } else {
        return
    }
    $message = "``$Command`` failed (exit $exitCode)"
    if ($output) {
        Write-Host -ForegroundColor yellow $output
        $message += ". See output above."
    }
    throw $message
}

function Find-CMake {
    $cmake = @(Get-Command cmake.exe -ErrorAction SilentlyContinue)[0]
    if ($cmake) { return $cmake.Definition }
    foreach ($root in @($env:ProgramFiles, ${env:ProgramFiles(x86)})) {
        if (-not $root) { continue }
        $found = @(Resolve-Path (Join-Path $root 'CMake*\bin\cmake.exe') -ErrorAction SilentlyContinue)
        if ($found) { return $found[-1].Path }
    }
    throw @"
cmake.exe was not found (not on PATH, not under Program Files). Install it - this script does not:
    winget install Kitware.CMake
or use the copy inside Visual Studio ("C++ CMake tools for Windows" component) from a
Developer PowerShell.
"@
}

function Ensure-Property($expected, $propertyValue, $propertyName, $path) {
    if ($propertyValue -eq $expected) { return }
    throw "Error: Invalid '$propertyName' property in generated '$path' (Expected: $expected - Actual: $propertyValue)"
}

function Assert-Consistent-Naming($expected, $path) {
    $dll = Get-Item $path
    Ensure-Property $expected $dll.Name "Name" $dll.Fullname
    Ensure-Property $expected $dll.VersionInfo.InternalName "VersionInfo.InternalName" $dll.Fullname
    Ensure-Property $expected $dll.VersionInfo.OriginalFilename "VersionInfo.OriginalFilename" $dll.Fullname
}

# ---------------------------------------------------------------------------------------------
# Visual Studio: found with vswhere (installed with every Visual Studio since 2017). Its
# vcvarsall.bat supplies cl.exe and dumpbin.exe for the gate; CMake's Visual Studio generator
# finds the compiler on its own.
# ---------------------------------------------------------------------------------------------
function Find-VisualStudio {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (-not (Test-Path -LiteralPath $vswhere)) {
        throw @"
Visual Studio was not found (no vswhere.exe). Install Visual Studio 2022 or newer, or the Build
Tools, with the "Desktop development with C++" workload - for win-arm64 also the component
"MSVC ... ARM64/ARM64EC build tools". This script does not install anything:
    winget install Microsoft.VisualStudio.2022.BuildTools
"@
    }
    $path = & $vswhere -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
    if (-not $path) { throw "No Visual Studio instance with the C++ x64/x86 build tools was found (vswhere)." }
    return $path.Trim()
}

function Import-DeveloperEnvironment {
    param([Parameter(Mandatory)][string] $VsPath, [Parameter(Mandatory)][string] $VcVarsArch)
    $vcvars = Join-Path $VsPath 'VC\Auxiliary\Build\vcvarsall.bat'
    if (-not (Test-Path -LiteralPath $vcvars)) { throw "vcvarsall.bat not found at $vcvars" }
    $lines = & cmd.exe /s /c "`"$vcvars`" $VcVarsArch > nul && set"
    if ($LASTEXITCODE -ne 0) { throw "vcvarsall.bat $VcVarsArch failed - is the matching MSVC toolset installed?" }
    foreach ($line in $lines) {
        $eq = $line.IndexOf('=')
        if ($eq -gt 0) { [Environment]::SetEnvironmentVariable($line.Substring(0, $eq), $line.Substring($eq + 1), 'Process') }
    }
    foreach ($tool in @('cl.exe', 'dumpbin.exe')) {
        if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) { throw "$tool is not on PATH after vcvarsall $VcVarsArch." }
    }
}

function Get-PeMachine([string] $Path) {
    $bytes = [System.IO.File]::ReadAllBytes($Path)
    $pe = [BitConverter]::ToInt32($bytes, 0x3C)
    return [BitConverter]::ToUInt16($bytes, $pe + 4)
}

$script:GateFailures = New-Object System.Collections.Generic.List[string]
function Add-GatePass([string] $Message) { Write-Host "  [ok] $Message" }
function Add-GateFail([string] $Message) { Write-Host "  [FAIL] $Message"; $script:GateFailures.Add($Message) }

# ---------------------------------------------------------------------------------------------
# One architecture: copy, configure, build, (test), gate, record.
# ---------------------------------------------------------------------------------------------
function Invoke-Libgit2Build {
    param(
        [Parameter(Mandatory)][string] $Rid,             # win-x64 | win-arm64
        [Parameter(Mandatory)][string] $Platform,        # x64 | ARM64  (cmake -A)
        [Parameter(Mandatory)][int]    $ExpectedMachine, # 0x8664 | 0xAA64
        [Parameter(Mandatory)][string] $VcVarsArch,      # vcvarsall argument for the gate tools
        [Parameter(Mandatory)][bool]   $CanRun,          # can this host execute the result?
        [Parameter(Mandatory)][string] $VsPath,
        [Parameter(Mandatory)][string] $CMake
    )
    $script:GateFailures.Clear()
    $started = Get-Date
    $root = Join-Path $BuildRoot $Rid
    $scratch = Join-Path $root 'libgit2'
    $build = Join-Path $root 'build'
    $out = Join-Path $OutputDir $Rid

    Write-Host ""
    Write-Host "=============================================================================="
    Write-Host " BUILD $Rid  (libgit2 $($Pins.LIBGIT2_VERSION), commit $($Pins.LIBGIT2_COMMIT))"
    Write-Host "=============================================================================="

    # Source: copy the vendored tree; ..\libgit2 itself is never written to.
    if (Test-Path -LiteralPath $root) { Remove-Item -LiteralPath $root -Recurse -Force }
    New-Item -ItemType Directory -Path $root | Out-Null
    Copy-Item -LiteralPath $SourceDir -Destination $scratch -Recurse
    New-Item -ItemType Directory -Path $build | Out-Null

    $buildTests = if ($test) { 'ON' } else { 'OFF' }
    $cmakeArgs = @('-S', $scratch, '-B', $build, '-A', $Platform,
                   '-D', 'USE_SSH=exec', '-D', 'USE_HTTPS=Schannel',
                   '-D', "BUILD_TESTS=$buildTests", '-D', 'BUILD_CLI=OFF',
                   '-D', "LIBGIT2_FILENAME=$($Pins.LIBGIT2_FILENAME)")
    Write-Host "  cmake $($cmakeArgs -join ' ')"
    $configure = & $CMake @cmakeArgs 2>&1
    $configure | Set-Content -LiteralPath (Join-Path $root 'configure.log') -Encoding UTF8
    if ($LASTEXITCODE -ne 0) { $configure | Select-Object -Last 60 | Write-Host; throw "cmake configure failed for $Rid" }
    $summary = @()
    $inSummary = $false
    foreach ($line in $configure) {
        $text = "$line"
        if ($text -match '^-- Enabled features:') { $inSummary = $true }
        if ($text -match '^-- Configuring done') { $inSummary = $false }
        if ($inSummary) { $summary += ($text -replace '^-- ', '') }
    }
    $summary | ForEach-Object { Write-Host "    $_" }

    Run-Command -Fatal { & $CMake --build $build --config Release }
    if ($test) { Run-Command -Fatal { & (Join-Path (Split-Path -Parent $CMake) 'ctest.exe') -V --test-dir $build -C Release } }

    $built = Join-Path $build "Release\$DllName"
    if (-not (Test-Path -LiteralPath $built)) { throw "the build did not produce $built" }
    Assert-Consistent-Naming $DllName $built

    if (Test-Path -LiteralPath $out) { Remove-Item -LiteralPath $out -Recurse -Force }
    New-Item -ItemType Directory -Path $out | Out-Null
    Copy-Item -LiteralPath $built -Destination $out
    $pdb = Join-Path $build "Release\$($Pins.LIBGIT2_FILENAME).pdb"
    if (Test-Path -LiteralPath $pdb) {
        New-Item -ItemType Directory -Path (Join-Path $out 'unstripped') | Out-Null
        Copy-Item -LiteralPath $pdb -Destination (Join-Path $out 'unstripped')
    }
    Copy-Item -LiteralPath (Join-Path $SourceDir 'COPYING') -Destination (Join-Path $out $Pins.LICENSE_FILE_NAME)
    Copy-Item -LiteralPath (Join-Path $ToolsDir 'LICENSE-Xdiff.txt') -Destination (Join-Path $out 'LICENSE-Xdiff.txt')
    $dll = Join-Path $out $DllName

    # ---- THE GATE ------------------------------------------------------------------------
    Write-Host "--- verifying ---"
    Import-DeveloperEnvironment -VsPath $VsPath -VcVarsArch $VcVarsArch

    $machine = Get-PeMachine $dll
    if ($machine -eq $ExpectedMachine) { Add-GatePass ('PE machine 0x{0:X4}' -f $machine) }
    else { Add-GateFail ('PE machine 0x{0:X4}, expected 0x{1:X4} - the -A platform did not take effect' -f $machine, $ExpectedMachine) }

    $exportsText = & dumpbin.exe /nologo /exports $dll
    $exports = @($exportsText | ForEach-Object { if ("$_" -match '^\s+\d+\s+[0-9A-Fa-f]+\s+[0-9A-Fa-f]{8}\s+(\S+)') { $Matches[1] } })
    $missing = @($Pins.REQUIRED_SYMBOLS -split '\s+' | Where-Object { $_ -and ($exports -notcontains $_) })
    if ($exports.Count -eq 0) { Add-GateFail 'no exports parsed from dumpbin /exports - check the parser against its output' }
    elseif ($missing.Count -gt 0) { Add-GateFail "missing exports: $($missing -join ' ')" }
    else { Add-GatePass "$($exports.Count) exports, all required ones present" }

    $importsText = & dumpbin.exe /nologo /imports $dll
    $imports = @($importsText | ForEach-Object { if ("$_" -match '^\s{4}(\S+\.dll)\s*$') { $Matches[1] } } | Sort-Object -Unique)
    $crt = @($imports | Where-Object { $_ -match '^(VCRUNTIME|MSVCP|ucrtbase|api-ms-win-crt)' })
    $unexpected = @($imports | Where-Object { $AllowedImports -notcontains $_ })
    if ($crt.Count -gt 0) { Add-GateFail "C runtime DLLs imported (static CRT expected): $($crt -join ' ')" }
    if ($unexpected.Count -gt 0) { Add-GateFail "unexpected imports: $($unexpected -join ' ')" }
    else { Add-GatePass "imports are OS-only: $($imports -join ' ')" }

    foreach ($want in @('HTTPS, using Schannel', 'SSH, using OpenSSH exec support', 'SHA1, using CollisionDetection', 'regex, using bundled PCRE', 'zlib, using bundled zlib')) {
        if ($summary -contains " * $want") { Add-GatePass "feature: $want" } else { Add-GateFail "feature summary lacks: $want" }
    }

    $smokeDir = Join-Path $root 'smoke'
    New-Item -ItemType Directory -Path $smokeDir | Out-Null
    Push-Location $smokeDir
    try { & cl.exe /nologo /O1 (Join-Path $ToolsDir 'linux\smoke-test.c') /Fe:smoke-test.exe | Out-Null } finally { Pop-Location }
    $smokeExe = Join-Path $smokeDir 'smoke-test.exe'
    $smokeOut = '(not run)'
    if (-not (Test-Path -LiteralPath $smokeExe)) { Add-GateFail 'smoke-test.c did not compile' }
    elseif ($CanRun) {
        $smokeOut = (& $smokeExe $dll $Pins.LIBGIT2_VERSION $ExpectedFeatures 2>&1) -join "`n"
        $smokeOut -split "`n" | ForEach-Object { Write-Host "    $_" }
        if ($LASTEXITCODE -eq 0) { Add-GatePass 'smoke test PASSED' } else { Add-GateFail "smoke test exited $LASTEXITCODE" }
    } else {
        Write-Host "  [skip] smoke test compiled for $Rid but this host cannot run it; run $smokeExe on a $Rid machine"
    }

    if ($script:GateFailures.Count -gt 0) {
        throw "VERIFICATION FAILED for $Rid ($($script:GateFailures.Count) check(s)). output\$Rid is left for inspection but must not be adopted."
    }

    # ---- RECORD ---------------------------------------------------------------------------
    $sha = (Get-FileHash -Algorithm SHA256 -LiteralPath $dll).Hash.ToLowerInvariant()
    $size = (Get-Item -LiteralPath $dll).Length
    $cl = ((& cmd.exe /c 'cl.exe 2>&1') | Select-Object -First 1)
    $cmakeVersion = ((& $CMake --version) | Select-Object -First 1)
    $info = @"
libgit2 native library - build information
==============================================================================
RID              : $Rid
Built            : $($started.ToUniversalTime().ToString('yyyy-MM-dd HH:mm:ss')) UTC
Build duration   : $([int]((Get-Date) - $started).TotalSeconds)s
Built by         : tools/libgit2-native-tools/windows/build.libgit2.ps1
Build host       : $env:COMPUTERNAME, $env:PROCESSOR_ARCHITECTURE, $([Environment]::OSVersion.VersionString)
Visual Studio    : $VsPath
Compiler         : $cl
cmake            : $cmakeVersion

libgit2          : $($Pins.LIBGIT2_VERSION), commit $($Pins.LIBGIT2_COMMIT)
Source           : tools/libgit2-native-tools/libgit2/ (vendored, unmodified; copied to scratch)
cmake options    : $($cmakeArgs[4..($cmakeArgs.Count - 1)] -join ' ')
                   then cmake --build . --config Release

Feature summary (libgit2's own CMake summary)
------------------------------------------------------------------------------
$($summary -join "`n")

Result
------------------------------------------------------------------------------
File             : $DllName
Size             : $size bytes
SHA256           : $sha
PE machine       : $('0x{0:X4}' -f $machine)
Exports          : $($exports.Count)
Imports          : $($imports -join ' ')
PDB              : $(if (Test-Path -LiteralPath $pdb) { 'unstripped\' + (Split-Path -Leaf $pdb) } else { 'none (libgit2 Release links without /DEBUG, as upstream)' })
Gate             : PASSED

Smoke test
------------------------------------------------------------------------------
$smokeOut
"@
    $info | Set-Content -LiteralPath (Join-Path $out 'BUILD-INFO.txt') -Encoding UTF8
    Get-ChildItem -LiteralPath $out -File -Recurse | Where-Object { $_.Name -ne 'SHA256SUMS.txt' } | ForEach-Object {
        '{0}  {1}' -f (Get-FileHash -Algorithm SHA256 -LiteralPath $_.FullName).Hash.ToLowerInvariant(), $_.FullName.Substring($out.Length + 1).Replace('\', '/')
    } | Set-Content -LiteralPath (Join-Path $out 'SHA256SUMS.txt') -Encoding ASCII
    Write-Host "  $dll"
    Write-Host "  sha256 $sha"
}

# ---------------------------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------------------------
if (-not $x64 -and -not $arm64) {
    Write-Error "usage: $($MyInvocation.MyCommand.Name) [-x64] [-arm64] [-test]"
    exit 2
}
if (-not (Test-Path -LiteralPath (Join-Path $SourceDir 'include\git2\version.h'))) {
    throw "the vendored libgit2 source is missing from $SourceDir. It is part of the repository - restore it from git."
}
$headerVersion = (Select-String -LiteralPath (Join-Path $SourceDir 'include\git2\version.h') -Pattern '^#define LIBGIT2_VERSION\s+"([^"]+)"').Matches[0].Groups[1].Value
if ($headerVersion -ne $Pins.LIBGIT2_VERSION) { throw "include\git2\version.h says $headerVersion, pins.env says $($Pins.LIBGIT2_VERSION)" }

$cmake = Find-CMake
$vs = Find-VisualStudio
$hostArch = $env:PROCESSOR_ARCHITECTURE   # AMD64 or ARM64

if ($x64) {
    Invoke-Libgit2Build -Rid 'win-x64' -Platform 'x64' -ExpectedMachine 0x8664 `
        -VcVarsArch $(if ($hostArch -eq 'ARM64') { 'arm64_amd64' } else { 'x64' }) `
        -CanRun $true -VsPath $vs -CMake $cmake        # ARM64 Windows runs x64 code too
}
if ($arm64) {
    Invoke-Libgit2Build -Rid 'win-arm64' -Platform 'ARM64' -ExpectedMachine 0xAA64 `
        -VcVarsArch $(if ($hostArch -eq 'ARM64') { 'arm64' } else { 'x64_arm64' }) `
        -CanRun ($hostArch -eq 'ARM64') -VsPath $vs -CMake $cmake
}
Write-Host "Done. Adopt with ADOPTING A BUILT BINARY in README.txt."
