================================================================================
tools/libgit2-native-tools/windows - building git2-5853918.dll for win-x64 and
                                     win-arm64
================================================================================

>>> STATUS 2026-10-17: build.libgit2.ps1 is WRITTEN BUT NEVER RUN - it was
    written on Linux, where it cannot run. The win-x64 and win-arm64 DLLs in
    ../../../native_libraries/ are ADOPTED from LibGit2Sharp.NativeBinaries
    2.0.324 (see ../BUILD-PROVENANCE.txt), which built them from this same
    libgit2 commit with the recipe this script reproduces. Expect to fix
    something on the first real run; fix it in the script, record it in
    ../BUILD-PROVENANCE.txt, and rewrite this block. <<<

WHAT THIS IS
--------------------------------------------------------------------------------
One script, build.libgit2.ps1, adapted from upstream's own build.libgit2.ps1
(verbatim copy: ../reference-upstream/build.libgit2.ps1). It builds libgit2
from ../libgit2 - the vendored, unmodified snapshot - with MSVC through CMake's
Visual Studio generator, gates the result, and writes ../output/win-<arch>/.
It downloads nothing and installs nothing. There is deliberately no MinGW
route: it would not match upstream's MSVC build.

PREREQUISITES (checked, never installed)
--------------------------------------------------------------------------------
  * Windows 10/11, x64 or ARM64.
  * Visual Studio 2022 or newer, or its Build Tools, with the workload
    "Desktop development with C++"; for win-arm64 also the individual
    component "MSVC v14x - VS 20xx C++ ARM64/ARM64EC build tools (Latest)".
      winget install Microsoft.VisualStudio.2022.BuildTools
    (then add the workload in the Visual Studio Installer). vswhere.exe, which
    every Visual Studio installs, is how the script finds it; vcvarsall.bat
    supplies cl.exe and dumpbin.exe for the gate.
  * CMake 3.5.1 or newer on PATH, or under Program Files\CMake*:
      winget install Kitware.CMake
    (Visual Studio's own copy works too, from a Developer PowerShell.)
  * PowerShell 5.1 (built in) or 7.

USAGE
--------------------------------------------------------------------------------
    cd tools\libgit2-native-tools\windows
    .\build.libgit2.ps1 -x64
    .\build.libgit2.ps1 -arm64
    .\build.libgit2.ps1 -x64 -arm64
    .\build.libgit2.ps1 -x64 -test     # + libgit2's own clar suite via ctest
    .\build.libgit2.ps1 -x64 -BuildRoot D:\scratch\lg2

  win-x64 builds and is smoke-tested on an x64 or an ARM64 machine (ARM64
  Windows runs x64 code). win-arm64 cross-builds on x64 (vcvarsall x64_arm64)
  and is smoke-tested only on an ARM64 machine; on x64 the smoke test is
  compiled and the script says where it is.

THE BUILD - UPSTREAM'S RECIPE, EXACTLY
--------------------------------------------------------------------------------
    cmake -A x64|ARM64 -D USE_SSH=exec -D USE_HTTPS=Schannel
          -D BUILD_TESTS=OFF -D BUILD_CLI=OFF -D LIBGIT2_FILENAME=git2-5853918
    cmake --build . --config Release

  * no -DUSE_BUNDLED_ZLIB - upstream passes none on Windows; with no system
    zlib, libgit2 compiles deps/zlib (the feature summary says "using bundled
    zlib", and the gate checks it);
  * libgit2's own MSVC defaults do the rest: STATIC_CRT=ON (/MT - so no
    Visual C++ Redistributable is needed), /O2 /GL /Gy, /LTCG /OPT:REF
    /OPT:ICF. Release links without /DEBUG, so no .pdb is produced - also as
    upstream (the 2.0.324 DLLs carry only a POGO debug-directory entry). If a
    future run does produce one it lands in output\win-<arch>\unstripped\;
  * Assert-Consistent-Naming (upstream's check) then confirms the DLL's name,
    InternalName and OriginalFilename are all git2-5853918.dll - libgit2
    builds its version resource from LIBGIT2_FILENAME.

WHY SCHANNEL (and not WinHTTP)
  libgit2 1.8.6 has two Windows TLS routes: USE_HTTPS=WinHTTP (the whole HTTP
  stack is Windows') and USE_HTTPS=Schannel (libgit2's own HTTP client over
  Windows' TLS). Upstream's recipe - and so the shipped 2.0.324 DLLs - uses
  Schannel: their imports are ADVAPI32, CRYPT32, KERNEL32, Secur32 and
  WS2_32, with no winhttp.dll. This script keeps Schannel so a rebuild is the
  same library. The SHA-1 is the bundled collision-detecting one, SHA-256
  Windows' CNG ("Win32").

THE GATE
--------------------------------------------------------------------------------
  1. PE machine 0x8664 (x64) / 0xAA64 (ARM64) - the -A platform took effect.
  2. Exports - every REQUIRED_SYMBOLS name from ../linux/pins.env (dumpbin
     /exports); the count is recorded (944 upstream).
  3. Imports - operating-system DLLs only; any VCRUNTIME*, MSVCP*, ucrtbase
     or api-ms-win-crt-* import fails it (the static CRT did not take).
  4. Feature summary - HTTPS using Schannel, SSH using OpenSSH exec, SHA1
     CollisionDetection, regex bundled PCRE, zlib bundled.
  5. Smoke test - ../linux/smoke-test.c built with cl and run against the DLL
     (version 1.8.6, git_libgit2_features() == 15 = THREADS | HTTPS | SSH |
     NSEC, as the upstream DLLs return - read from their machine code -, init,
     blob hash, repository init/write/open, shutdown). No CA location is
     passed: Schannel uses the Windows certificate store, and libgit2 refuses
     GIT_OPT_SET_SSL_CERT_LOCATIONS for it.

OUTPUT (../output/win-<arch>/)
--------------------------------------------------------------------------------
  git2-5853918.dll, LICENSE-Libgit2.txt (libgit2's COPYING), LICENSE-Xdiff.txt
  (../LICENSE-Xdiff.txt), BUILD-INFO.txt, SHA256SUMS.txt.

ADOPTING A BUILT DLL
--------------------------------------------------------------------------------
  1. Read BUILD-INFO.txt.
  2. Copy git2-5853918.dll, LICENSE-Libgit2.txt and LICENSE-Xdiff.txt to
     ..\..\..\native_libraries\win-<arch>\ (replacing the adopted upstream DLL).
  3. Rewrite native_libraries\win-<arch>\git2-5853918.dll.provenance.txt from
     BUILD-INFO.txt - it then says BUILT, not ADOPTED.
  4. Replace that RID's section in ..\BUILD-PROVENANCE.txt.
  5. Run the managed test suite on Windows before publishing.
================================================================================
