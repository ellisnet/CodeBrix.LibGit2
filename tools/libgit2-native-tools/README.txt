================================================================================
libgit2-native-tools - everything needed to build the libgit2 native libraries
================================================================================

THE RULE THIS FOLDER EXISTS FOR
--------------------------------------------------------------------------------
Jeremy, 2026-09-15 (written for CodeBrix.Ollama's llama-native-tools, and the
rule every CodeBrix native-tools folder is held to):

    "Every single file we need, in order to build these native libraries,
    must be housed in that folder. We must not be pulling from external
    sources during the build process. If, a year from now, I need to redo
    these builds, for some reason - and other source code/repos are not
    available to me - I still need to be able to redo the build. The
    exception is the actual tools like cmake and ninja and those kinds of
    tools - I don't want to vendor a bunch of build tools into this folder.
    But all source code and header files, etc must be in the folder - the
    build process cannot pull from other repos, etc."

Everything below follows from that. The folder is modelled, file for file, on
CodeBrix.Sdl3's sdl3-native-tools/, which was built to the same rule.


HOW THE RULE IS ENFORCED, NOT JUST STATED
--------------------------------------------------------------------------------
The Linux builds, and the Android builds, run inside a container started with
`--network none`. The first thing the Linux build prints is the proof: the
container has no network interface but loopback. If any step ever tried to
fetch a source file or a dependency, it would fail there instead of quietly
working on whichever machine happened to have a connection. The Windows and
macOS scripts fetch nothing either.

libgit2's CMake has a single download path (FetchContent of Chromium's zlib,
only with USE_BUNDLED_ZLIB=Chromium) and no build here takes it; Mbed TLS's
CMake has none. See UPSTREAM.txt.

The things that live outside the folder, by Jeremy's stated exception:
  * the tools installed on each build machine (cmake, ninja, the compilers, a
    container engine, the Android NDK, Visual Studio, Xcode) - each platform
    README lists them, with the command that installs them; no script here
    installs anything;
  * on Linux, the digest-pinned manylinux container images that supply the
    compiler and the glibc floor, and the derived images built on them (cmake
    and ninja wheels and `file` - nothing else; libgit2 needs no -devel
    package). Building a derived image is the one step that uses the network.
    linux/README.txt documents a bare-host route for the day the images are
    gone.


FOLDER MAP
--------------------------------------------------------------------------------
  README.txt              this document
  BUILD-PROVENANCE.txt    what was actually built (or adopted), when, by what,
                          with what hashes - one section per runtime
                          identifier, and the comparison with upstream's Linux
                          binaries
  UPSTREAM.txt            where every vendored tree came from: commit, tag,
                          dates, licence, the exact copy commands, tree
                          checksums
  LICENSE-Xdiff.txt       the bundled xdiff's own copyright notices (LGPL-2.1
                          and, for xhistogram.c, the Eclipse Distribution
                          License), verbatim from the libgit2/deps/xdiff/
                          headers - libgit2's COPYING does not carry them.
                          Every build copies it beside its binary
  .gitignore              re-includes this folder's contents from the root
                          .gitignore (which ignores names that occur inside the
                          vendored trees, and *.env) and ignores output/. Its
                          first rule is load-bearing - read the comment before
                          editing it

  libgit2/                THE VENDORED LIBGIT2 - the full, unmodified tree at
                          5853918c4c6a7b12f8becf4bd11ff4362ebb9020 (tag
                          v1.8.6), including tests/ and every bundled
                          dependency in deps/. GPLv2 + linking exception
  mbedtls/                THE VENDORED MBED TLS - the full, unmodified 3.6.7
                          LTS tree (tag v3.6.7) with its framework/ submodule.
                          Apache-2.0 OR GPL-2.0-or-later. Used ONLY by the two
                          Android builds
  reference-upstream/     verbatim copies of upstream
                          LibGit2Sharp.NativeBinaries' build files
                          (build.libgit2.sh / .ps1, dockerbuild.sh,
                          docker/linux + musl, UpdateLibgit2ToSha.ps1, the
                          nuget.package props and libgit2_hash.txt) - a RECORD
                          of how the adopted binaries were made; no script
                          here uses them

  linux/                  linux-x64, linux-arm64, linux-riscv64
                          pins.env, build.sh, container-build.sh,
                          Containerfile.<arch>, smoke-test.c (shared by every
                          platform's gate), README.txt
  android/                android-arm64, android-x64 (libgit2 + statically
                          linked Mbed TLS, NDK r30, API 33, 16 KB pages)
                          pins.json, build.py, verify.py, README.txt
  windows/                win-x64, win-arm64. build.libgit2.ps1 (upstream's
                          script adapted: MSVC + CMake, Schannel), README.txt.
                          Written 2026-10-17, UNRUN: the shipped win-* DLLs
                          are ADOPTED from LibGit2Sharp.NativeBinaries 2.0.324
  macos/                  osx-arm64, osx-x64. build.sh (upstream's Darwin
                          branch adapted: SecureTransport), README.txt.
                          Written 2026-10-17, UNRUN: the shipped osx-* dylibs
                          are ADOPTED from 2.0.324

  unstripped/             COMMITTED. The pre-strip twin of every native built
                          here, one folder per RID, with SHA256SUMS and a
                          README.txt carrying the rule for keeping them in step
                          with the binaries in ../../native_libraries/<rid>/.
                          For crash triage: never shipped, never an input to
                          any build

  output/                 build results (git-ignored except its README.txt).
                          Disposable - the pre-strip copies that are meant to
                          last live in unstripped/, not here


WHICH README TO READ
--------------------------------------------------------------------------------
  Building on Linux    -> linux/README.txt
  Building for Android -> android/README.txt
  Building on Windows  -> windows/README.txt
  Building on a Mac    -> macos/README.txt

Each one lists the tools to install on that machine, with the exact command,
and nothing else is needed.


THE NINE RUNTIME IDENTIFIERS
--------------------------------------------------------------------------------
  RID            built by                                  shipped file
  -------------  ----------------------------------------- ---------------------
  linux-x64      linux/build.sh x64                        libgit2-5853918.so
  linux-arm64    linux/build.sh arm64                      libgit2-5853918.so
  linux-riscv64  linux/build.sh riscv64                    libgit2-5853918.so
  android-arm64  android/build.py --arch arm64             libgit2-5853918.so
  android-x64    android/build.py --arch x64               libgit2-5853918.so
  win-x64        ADOPTED; rebuild: windows/build.libgit2.ps1 -x64
                                                           git2-5853918.dll
  win-arm64      ADOPTED; rebuild: windows/build.libgit2.ps1 -arm64
                                                           git2-5853918.dll
  osx-x64        ADOPTED; rebuild: macos/build.sh x64      libgit2-5853918.dylib
  osx-arm64      ADOPTED; rebuild: macos/build.sh arm64    libgit2-5853918.dylib

The three Linux and the two Android RIDs are BUILT HERE from libgit2/ (and
mbedtls/). The Windows and macOS binaries are ADOPTED from
LibGit2Sharp.NativeBinaries 2.0.324, which built them from this same libgit2
commit (ruling of 2026-10-17); the scripts that rebuild them from libgit2/
live in windows/ and macos/ all the same, so every target can be rebuilt from
this repository alone. Upstream's win-x86, linux-arm, linux-ppc64le and musl
RIDs are not shipped. BUILD-PROVENANCE.txt says, per RID, which route each
shipped binary took.

The shipped binaries live in ../../native_libraries/<rid>/, each with a
<file>.provenance.txt beside it; the package packs them into
runtimes/<rid>/native/.

THE NAMES KEEP THE COMMIT SUFFIX on purpose - libgit2-5853918.so,
git2-5853918.dll, libgit2-5853918.dylib, exactly upstream's names
(-DLIBGIT2_FILENAME=git2-5853918) - so this library can never collide with
upstream LibGit2Sharp's copy or with a system libgit2 loaded into the same
process. The Linux SONAME (libgit2-5853918.so.1.8) stays inside the file, as
built; on Android the SONAME is the plain file name.

Every RID folder also gets LICENSE-Libgit2.txt - a verbatim copy of libgit2's
COPYING (GPLv2 with the linking exception, plus the bundled dependencies'
licences) - and LICENSE-Xdiff.txt; the two Android folders also get
LICENSE-MbedTls.txt (Mbed TLS's LICENSE). The names are package-unique
(LICENSE-SDL3.txt, LICENSE-Dav1d.txt are the family's precedents) because these
files land in a consuming application's OUTPUT FOLDER, where a file named
plainly LICENSE collides with any other package that ships one there.


THE VENDORED COMMITS
--------------------------------------------------------------------------------
  libgit2   5853918c4c6a7b12f8becf4bd11ff4362ebb9020 = tag v1.8.6 (2026-07-18)
            - the commit LibGit2Sharp.NativeBinaries 2.0.324 pins, so the
            managed wrapper, this source and every shipped binary describe one
            C API.
  Mbed TLS  068ff080b369adfac81509f9b57b2afabaf82dc5 = tag v3.6.7, the newest
            3.6 LTS release on 2026-10-17, with framework/ at dde0c4a0e448.

UPSTREAM.txt explains both and records how to verify the snapshots.


LICENCES AND NOTICES
--------------------------------------------------------------------------------
../../THIRD-PARTY-NOTICES.txt, at the root of this repository, carries libgit2's
licence and the notices for everything else the repository incorporates. Read
it before changing what this folder vendors.


A ONE-MINUTE TOUR (on Linux)
--------------------------------------------------------------------------------
    cd linux
    ./build.sh x64              # about ten seconds once the image exists
    cat ../output/linux-x64/BUILD-INFO.txt
    cd ../output && sha256sum -c SHA256SUMS
================================================================================
