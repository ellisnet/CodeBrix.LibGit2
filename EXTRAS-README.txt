================================================================================
EXTRAS-README: CodeBrix.LibGit2
Samples, tools and other content in this repository that is not part of a NuGet
package
================================================================================

This repository ships no sample applications or demos. Exactly one project is
packable - src/CodeBrix.LibGit2 - and everything else listed below exists to
build, rebuild, test or document it. None of it is included in the
CodeBrix.LibGit2.MitLicenseForever package, except the committed native
binaries in native_libraries/ (and the licence files beside them), which the
package redistributes.

For runnable, compilable usage of the library, read the test project: the
"WORKING EXAMPLES ON GITHUB" section of AGENT-README.txt maps each feature
area to the test file that exercises it.


TEST PROJECT
============
    tests/CodeBrix.LibGit2.Tests/

The only non-package project in the solution. xUnit v3 + SilverAssertions +
CodeBrix.TestMocks; run it as described in MAINTAINER-README.txt (TESTING).
It copies the committed native binary for the host's RID beside the test
assembly, and keeps its network tests switched off unless
LIBGIT2_RUN_NETWORK_TESTS=1 is set.

    tests/CodeBrix.LibGit2.Tests/Resources/
        The upstream fixture repositories (bare and with working trees,
        merge, revert, pack-builder, submodule, worktree and shallow
        fixtures, plus expected outputs and archives). Their .git folders
        are stored as dot_git/ and are renamed when a test copies a fixture
        to a temporary folder. Copied to the test output; never shipped.
        Some files in here are matched by upstream's Resources/.gitignore
        and must be added with `git add -f` -
        see MAINTAINER-README.txt (TESTING).

    tests/CodeBrix.LibGit2.Tests/TestHelpers/
        The shared test base (BaseFixture), constants, the NetworkFact /
        NetworkTheory attributes and their NetworkTestSwitch, and the helper
        filters used by the filter tests.


NATIVE LIBRARIES
================
    native_libraries/<rid>/          the binary (git2-5853918.dll,
                                     libgit2-5853918.dylib or
                                     libgit2-5853918.so),
                                     <binary>.provenance.txt,
                                     LICENSE-Libgit2.txt, LICENSE-Xdiff.txt,
                                     and LICENSE-MbedTls.txt in the two
                                     android-* folders

The committed binaries the library csproj packs under runtimes/<rid>/native/,
for win-x64, win-arm64, osx-x64, osx-arm64, linux-x64, linux-arm64,
linux-riscv64, android-arm64 and android-x64. Each .provenance.txt records how
that file was produced - built here from the vendored libgit2 source, or
adopted from a recorded upstream build of the same libgit2 commit - with its
SHA-256. The provenance files are not packed. Nothing in a dotnet build
changes this folder; only the build scripts below produce new binaries, and
copying a result in here is a deliberate step recorded in
BUILD-PROVENANCE.txt.


LIBGIT2 NATIVE BUILD TOOLS
==========================
    tools/libgit2-native-tools/

Everything needed to rebuild every native binary from source without any
other repository and without fetching anything at build time. None of it is
packed. tools/libgit2-native-tools/README.txt is the authoritative map; in
short:

  libgit2/              the vendored, UNMODIFIED libgit2 source tree at the
                        pinned commit, including its tests and every bundled
                        dependency (deps/)
  mbedtls/              the vendored, UNMODIFIED Mbed TLS source tree (with
                        its framework/ submodule), used only by the Android
                        builds
  UPSTREAM.txt          where each vendored tree came from: URL, commit/tag,
                        licence, the exact copy commands and a tree checksum
  linux/                container builds (podman or docker, network disabled,
                        digest-pinned manylinux images) of linux-x64,
                        linux-arm64 and linux-riscv64, with a verification
                        gate and a C smoke test
  android/              the NDK build of android-arm64 and android-x64, with
                        Mbed TLS built first and linked statically
  windows/              build.libgit2.ps1 for win-x64 / win-arm64 (MSVC,
                        Schannel)
  macos/                build.sh for osx-arm64 / osx-x64 (SecureTransport)
  reference-upstream/   a VERBATIM record of the upstream native-package
                        build scripts, Dockerfiles and props the adopted
                        Windows and macOS binaries were made with. Nothing
                        runs, sources or reads these files; they are kept
                        only so the recipe stays on record. Do not edit them.
  unstripped/           the committed pre-strip twins of the binaries built
                        here (Linux and Android), with SHA256SUMS, for crash
                        triage; never shipped and never an input to a build
  output/               where every build writes its results (git-ignored
                        except its README.txt; disposable)
  BUILD-PROVENANCE.txt  the per-RID record: who built or adopted each
                        shipped binary, from what, with what, and the result
  LICENSE-Xdiff.txt     the bundled xdiff code's own notices, verbatim from
                        its file headers; every build copies it beside its
                        binary
  .gitignore            re-includes this folder's contents from the root
                        .gitignore; its first rule is load-bearing

Build tools (cmake, ninja, compilers, a container engine, the Android NDK,
Visual Studio, Xcode) are NOT vendored; each platform README.txt names them
and how to install them. The vendored trees are upstream content, not
project source: do not index, refactor, reformat or patch them.


HANDLES TEMPLATE
================
    tools/handles-template/

Objects.tt is upstream's T4 text template that generated
src/CodeBrix.LibGit2/Core/Handles/Objects.cs (one SafeHandle subclass per
libgit2 native object type), with its namespace updated. It is reference
material only and deliberately NOT a project item, so Visual Studio can never
regenerate Objects.cs from it; Objects.cs is maintained by hand. README.txt in
that folder explains why, and how to regenerate by hand into a scratch
location and diff when a handle type is added or removed.


================================================================================
END OF EXTRAS-README
================================================================================
