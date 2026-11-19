================================================================================
MAINTAINER-README: CodeBrix.LibGit2
Notes for people and agents MAINTAINING this repository - not for package
consumers
================================================================================

If you are CONSUMING the NuGet package, stop reading and open
AGENT-README.txt (repo root) instead. Everything below is about the
repository itself: how it is laid out, how it builds, how it is tested, how it
is packaged, where the native libraries come from, and the conventions the
source follows.


PURPOSE AND SCOPE
=================
This repository produces exactly one NuGet package:

    PackageId:  CodeBrix.LibGit2.MitLicenseForever
    Assembly:   CodeBrix.LibGit2 (net10.0 only)
    Project:    src/CodeBrix.LibGit2/CodeBrix.LibGit2.csproj
    License:    MIT (repository LICENSE); the bundled libgit2 native
                binaries are GPLv2 with the libgit2 linking exception
    Consumer documentation: AGENT-README.txt (repo root; packed at the
                            package root)

The library is a port of LibGit2Sharp (the managed wrapper) together with the
libgit2 native library for nine runtime identifiers (win-x64, win-arm64,
osx-x64, osx-arm64, linux-x64, linux-arm64, linux-riscv64, android-arm64,
android-x64), all in the one package. It has no NuGet dependencies.


REPOSITORY LAYOUT
=================
    src/CodeBrix.LibGit2/            the library (the only packable project)
      (root)                         the public API: Repository, Commands
                                     (partial, in Commands/), the
                                     collections, options, results,
                                     exceptions, GlobalSettings, Handlers.cs
                                     (namespace CodeBrix.LibGit2.Handlers),
                                     AssemblyCommitIds.cs,
                                     InternalsVisibleTo.cs
      Commands/                      the static Commands class: Checkout,
                                     Fetch, Pull, Remove, Stage (namespace
                                     CodeBrix.LibGit2, not .Commands)
      Core/                          interop: NativeMethods (P/Invoke + the
                                     NativeLibrary resolver), Proxy, the
                                     native structs, marshalers,
                                     NativeDllName.cs, UniqueId.cs
      Core/Handles/                  the SafeHandle types; Objects.cs is the
                                     hand-maintained file that upstream
                                     generated from a T4 template (see
                                     tools/handles-template/)
    The upstream folder layout (root + Commands/ + Core/ + Core/Handles/) is
    kept as-is so the port stays diffable against upstream.

    tests/CodeBrix.LibGit2.Tests/    the xUnit v3 test project
      TestHelpers/                   BaseFixture (shared setup, temp repos),
                                     Constants, NetworkFact /
                                     NetworkTheory attributes and
                                     NetworkTestSwitch, helper filters
      Properties/AssemblyInfo.cs     [assembly: Parallelization(Mode =
                                     ParallelMode.None)]
      Resources/                     the fixture repositories (see TESTING)

    native_libraries/<rid>/          the committed native binaries, one folder
                                     per RID: the binary (git2-49e408b.dll,
                                     libgit2-49e408b.dylib or
                                     libgit2-49e408b.so),
                                     <binary>.provenance.txt,
                                     LICENSE-Libgit2.txt (libgit2's COPYING,
                                     verbatim), LICENSE-Xdiff.txt, and in the
                                     two android-* folders LICENSE-MbedTls.txt

    tools/libgit2-native-tools/      everything needed to rebuild every native
                                     binary from source with nothing fetched
                                     at build time (see NATIVE LIBRARIES and
                                     EXTRAS-README.txt)
    tools/handles-template/          Objects.tt (upstream's T4 template for
                                     Core/Handles/Objects.cs, kept as
                                     reference material) and its README.txt

    CodeBrix.LibGit2.slnx            the solution; its Solution Items folder
                                     carries .gitignore, AGENT-README.txt,
                                     EXTRAS-README.txt, global.json,
                                     icon-codebrix-128.png, LICENSE,
                                     MAINTAINER-README.txt, README-INDEX.txt,
                                     README.md and THIRD-PARTY-NOTICES.txt;
                                     its Tests folder carries the test project
    global.json                      pins the .NET SDK (10.0.404, rolling
                                     forward to the latest feature band, no
                                     prereleases) and selects the
                                     Microsoft.Testing.Platform test runner;
                                     the family's standard file for
                                     repositories with no Android projects
    .gitignore                       the family .gitignore (see the NOTES on
                                     the nested one in tools/)
    .gitattributes                   LF line endings for this repository's
                                     own files; line-ending handling switched
                                     OFF for the vendored trees and the test
                                     resources (see NOTES)
    LICENSE                          MIT
    icon-codebrix-128.png            the package icon
    README.md                        the nuget.org / GitHub page (packed)
    AGENT-README.txt                 the consumer guide (packed)
    THIRD-PARTY-NOTICES.txt          provenance and licences (packed)
    MAINTAINER-README.txt, EXTRAS-README.txt, README-INDEX.txt
                                     repository-only documents
    AGENTS.md, CLAUDE.md, .clinerules, .cursorrules,
    .cursor/rules/agent-readme.mdc, .windsurfrules,
    .github/copilot-instructions.md, .junie/guidelines.md
                                     the AI-agent pointer stubs (see NOTES)

A TestResults/ folder at the root, when present, is left behind by test
runs; it is ignored and never committed.


BUILDING
========
    dotnet restore CodeBrix.LibGit2.slnx
    dotnet build   CodeBrix.LibGit2.slnx --configuration Release

Debug and Release must both build with 0 warnings and 0 errors.
GenerateDocumentationFile is on: every public member carries an XML doc
comment, CS1591 is fixed at the source, and <NoWarn> is never added.
EnableNETAnalyzers is on; analyzer warnings are fixed at source too.
AllowUnsafeBlocks is on (interop) and IsTrimmable stays true.

Nothing in a dotnet build reaches outside this repository: no download, no
native compile. The natives are committed binaries; the build only packs
them.

VISUAL STUDIO 2026: Jeremy builds and packs from the solution in Visual Studio
2026. That is why Objects.tt lives in tools/handles-template/ and is NOT a
project item: if it were in the project, Visual Studio would attach the
TextTemplatingFileGenerator custom tool to it and could regenerate
src/CodeBrix.LibGit2/Core/Handles/Objects.cs on save or build, silently
replacing the maintained file. Never add the .tt back to the project, and
never add T4 metadata to the csproj (no Generator attribute, no
DependentUpon/AutoGen on Objects.cs, no T4 <Service> entry). Edit Objects.cs
directly; tools/handles-template/README.txt describes regenerating by hand
into a scratch location and diffing.


TESTING
=======
    dotnet test --solution CodeBrix.LibGit2.slnx

THE TEST RUNNER IS Microsoft.Testing.Platform (MTP), selected by global.json
at the repo root:

    "test": { "runner": "Microsoft.Testing.Platform" }

The same file pins the .NET SDK (see REPOSITORY LAYOUT). Keep it committed. MTP output ends in a "Test run summary:" block. If
`dotnet test --solution` ever reports zero tests, build and run the test
assembly directly and read ITS counts:

    dotnet build CodeBrix.LibGit2.slnx
    dotnet tests/CodeBrix.LibGit2.Tests/bin/Debug/net10.0/CodeBrix.LibGit2.Tests.dll

The test project is xUnit v3 + SilverAssertions + CodeBrix.TestMocks (never
Moq), net10.0, and runs serially. It has an InternalsVisibleTo grant from the
library (src/CodeBrix.LibGit2/InternalsVisibleTo.cs).

THE NATIVE LIBRARY FOR TEST RUNS. A ProjectReference brings no native assets,
so the test csproj copies the binary itself: every *.so, *.dylib and *.dll in
$(LibGit2NativeDir) is copied beside CodeBrix.LibGit2.Tests.dll, where the
library's resolver finds it. LibGit2NativeDir defaults to
native_libraries/$(NETCoreSdkRuntimeIdentifier)/ - the committed binary for
the machine's own RID. To test against another build of the same libgit2
commit, pass a folder (with a trailing separator):

    dotnet build CodeBrix.LibGit2.slnx -p:LibGit2NativeDir=/path/to/folder/
    dotnet tests/CodeBrix.LibGit2.Tests/bin/Debug/net10.0/CodeBrix.LibGit2.Tests.dll

With no binary for the host RID, every test that touches libgit2 fails with a
native-library load error.

NETWORK TESTS ARE OPT-IN. The tests that clone, fetch, pull or list remote
references against github.com are marked [NetworkFact] / [NetworkTheory]
(TestHelpers/) and SKIP, with the reason, unless the environment variable

    LIBGIT2_RUN_NETWORK_TESTS=1

is set (exactly "1"). A default run never leaves the machine. With the
variable set, those tests run against public test repositories on github.com
(the URLs are in the test files). The credential tests additionally skip
themselves unless the PrivateRepo* members of TestHelpers/Constants.cs are
filled in locally; never commit real credentials there. Every other remote
operation in the suite (push, fetch, clone from fixtures) uses local paths
and runs by default.

Some upstream tests skip on real conditions (core.autocrlf rows on non-Windows,
a libgit2 ignorecase limitation, one upstream placeholder marked
Skip = "Not supported by libgit2 as yet"); that is expected.

FIXTURE REPOSITORIES. tests/CodeBrix.LibGit2.Tests/Resources/ holds the
upstream fixture repositories (their .git folders are stored as dot_git/ and
renamed when a test copies them to a temp folder). They are content items
copied to the test output. Do not edit them casually - many tests assert
exact object ids.

    !! COMMIT NOTE: upstream's own Resources/.gitignore ignores names such as
    COMMIT_EDITMSG, info/exclude and logs/ that some fixtures NEED, and
    upstream force-tracks those files. A plain `git add` silently skips
    them. List them, and add them with -f:

        git ls-files --others --ignored --exclude-standard -- \
            tests/CodeBrix.LibGit2.Tests/Resources
        git ls-files --others --ignored --exclude-standard -z -- \
            tests/CodeBrix.LibGit2.Tests/Resources | xargs -0 git add -f

    Once they are committed git tracks them like any other file.


PACKAGING AND PUBLISHING
========================
GeneratePackageOnBuild is true, so every build of the library emits a fresh
.nupkg under src/CodeBrix.LibGit2/bin/<Configuration>/.

Versioning is the CodeBrix date-stamped scheme, computed in the csproj from
System.DateTime.UtcNow as 1.<years-since-2026>.<day-of-year>.<minute-of-day>.
It is monotonically increasing but is NOT SemVer, so major/minor say nothing
about API compatibility. Two builds inside the same UTC minute produce the
SAME version - never publish two packages from one minute. To re-baseline
the minor number, change _VersionBaseYear in the csproj. Do not replace the
version block with a literal <Version>.

What ships inside the nupkg:
    lib/net10.0/CodeBrix.LibGit2.dll + .xml
    runtimes/<rid>/native/<binary> + LICENSE-Libgit2.txt, for each of the
      nine RIDs, plus LICENSE-MbedTls.txt in the two android-* folders
    icon-codebrix-128.png, README.md, AGENT-README.txt,
      THIRD-PARTY-NOTICES.txt (all four from the repo root)
NOT packed: the .provenance.txt files, MAINTAINER-README.txt,
EXTRAS-README.txt, README-INDEX.txt, tests/, tools/.

The csproj's native ItemGroup has one explicit item per file per RID, and
decides which of the files in native_libraries/<rid>/ are packed. Read it
when adding a licence file there: LICENSE-Libgit2.txt and LICENSE-Xdiff.txt
are listed for every RID and LICENSE-MbedTls.txt for the two Android RIDs; a
new licence file is packed only once the ItemGroup lists it.

EVERY NATIVE PACK ITEM IS CONDITIONAL ON ITS BINARY EXISTING, so the project
builds while natives are being produced - and a missing binary silently drops
that RID from the package. Before publishing, open the .nupkg (it is a zip)
and check that runtimes/ holds all nine <rid>/native/ folders, each with its
binary and LICENSE-Libgit2.txt, and LICENSE-MbedTls.txt in both android-*
folders. The licence items are conditional on the BINARY, so a binary present
with its licence file missing fails the pack. PackagePath values use forward
slashes and no trailing separator (an empty path segment can stop NuGet
selecting the native).

PackageLicenseExpression is MIT and PackageRequireLicenseAcceptance is true;
the package Copyright string names the LibGit2Sharp contributors and Jeremy
Ellis and contributors. Jeremy publishes.


NATIVE LIBRARIES
================
The binary name keeps the libgit2 commit prefix - git2-49e408b (Windows
git2-49e408b.dll, macOS libgit2-49e408b.dylib, Linux/Android
libgit2-49e408b.so) - the naming scheme upstream LibGit2Sharp uses, so this
library never collides with an upstream LibGit2Sharp copy or a system libgit2
loaded into the same process. The managed side names it once, in
src/CodeBrix.LibGit2/Core/NativeDllName.cs.

EVERY binary is built from the vendored source with this repository's own
tooling. Nothing is adopted from another project: upstream LibGit2Sharp (and
its native-binaries package) is still on libgit2 1.8, so for libgit2 1.9 there
is no upstream build to take.

Where each shipped binary comes from (full per-RID record, hashes and gate
results: tools/libgit2-native-tools/BUILD-PROVENANCE.txt; per-file record:
native_libraries/<rid>/<binary>.provenance.txt):

  linux-x64, linux-arm64, linux-riscv64
      BUILT on the Linux development machine by
      tools/libgit2-native-tools/linux/build.sh in the digest-pinned manylinux
      containers (x86_64 native; aarch64 and riscv64 under qemu-user
      emulation), with the network disabled. HTTPS = OpenSSL loaded at run
      time (OpenSSL-Dynamic), SSH = exec of the system ssh, bundled zlib,
      PCRE2, llhttp and xdiff. A verification gate (ELF, SONAME, exports,
      NEEDED, glibc floor, build-id, feature summary, smoke test) runs on
      every build.
  android-arm64, android-x64
      BUILT on the Linux development machine by
      tools/libgit2-native-tools/android/build.py with the pinned Android NDK,
      ANDROID_PLATFORM 33, 16 KiB page-size link flags, HTTPS through Mbed TLS
      built first from the vendored tools/libgit2-native-tools/mbedtls/ and
      linked STATICALLY, and no SSH transport.
  win-x64, win-arm64
      BUILT on Jeremy's Windows x64 machine (2026-11-19) by
      tools/libgit2-native-tools/windows/build.libgit2.ps1 (MSVC, static C
      runtime, HTTPS via Schannel, SSH = exec), with -BuildRoot C:\cbx-lg2.
      win-arm64 is cross-built there; its smoke test is compiled but not yet
      run on ARM64 hardware. See BUILDING THE WINDOWS AND MACOS BINARIES
      below.
  osx-arm64, osx-x64
      BUILT on Jeremy's Apple Silicon Mac (2026-11-19) by
      tools/libgit2-native-tools/macos/build.sh (Apple clang, HTTPS via
      SecureTransport, SSH = exec, minimum macOS 14.0, not stripped - as
      upstream). osx-x64 is cross-built there and smoke-tested under Rosetta
      2. See the same section.

A RID that has not been built yet has NO binary in native_libraries/<rid>/
(only its licence files) - as of 2026-11-19 there is none: all nine are built.
The csproj packs each native file only when it exists, so the package still
builds - and silently lacks that platform. DO NOT PUBLISH until all nine are
present (PACKAGING AND PUBLISHING has the check).

How to rebuild one RID or all of them: tools/libgit2-native-tools/README.txt
and the per-platform README.txt it points to. Build tools (cmake, ninja,
compilers, podman, the NDK, Visual Studio, Xcode) come from their normal
sources and are not in the repository; everything compiled is.

The pins in force (deliberate, change only together):
    libgit2           49e408b3208bc3093757a1c2db938d3590f3f412 (tag v1.9.7)
    LibGit2Sharp      master @ eaa698d078941fd5e3cc82b59b885cd35d8cc0f8 (the
                      managed source this port came from; upstream targets
                      libgit2 1.8 - see THE INTEROP AND LIBGIT2 1.9 below)
    Mbed TLS          tag v3.6.7 (Android only)

BUMPING LIBGIT2 touches all of these at once:
  - the vendored tree tools/libgit2-native-tools/libgit2/ (replaced whole,
    never patched, WITHOUT upstream's .github/ folder - this repository
    carries no workflow files, vendored ones included) and
    tools/libgit2-native-tools/UPSTREAM.txt;
  - the build pins (linux/pins.env, android/pins.json; the Windows and macOS
    scripts read the Linux pins) - commit, version, the git2-<short> file
    name, and the expected feature mask;
  - src/CodeBrix.LibGit2/Core/NativeDllName.cs (the native name) and
    src/CodeBrix.LibGit2/AssemblyCommitIds.cs (LibGit2CommitSha);
  - every native item in src/CodeBrix.LibGit2/CodeBrix.LibGit2.csproj (the
    file names are spelled out per RID);
  - EVERY native binary, rebuilt for all nine RIDs - five on the Linux
    machine, two on the Windows machine, two on the Mac - with new
    .provenance.txt files and BUILD-PROVENANCE.txt sections;
  - the managed interop, by the audit described under THE INTEROP AND
    LIBGIT2 1.9;
  - THIRD-PARTY-NOTICES.txt (commit, licence texts, bundled dependencies).
A binary from a different libgit2 commit under the old name loads and then
misbehaves; never mix commits.


THE INTEROP AND LIBGIT2 1.9
===========================
The managed source was ported from LibGit2Sharp, whose interop is written
against libgit2 1.8. Upstream has not moved to 1.9, so there was nothing to
port: the interop was adapted here (2026-11-18) by comparing every bound
function, struct, callback, enum and *_VERSION constant between the 1.8.6 and
1.9.7 headers. If upstream LibGit2Sharp later publishes its own 1.9 support,
compare it with what is here before taking it.

What differed and was changed (all in src/CodeBrix.LibGit2/):
  - Core/GitBlame.cs          git_blame_hunk gained final_committer,
                              orig_committer and summary. With the old layout
                              a blame reads a bad pointer and the process
                              crashes.
  - Core/GitRemoteCallbacks.cs  git_remote_callbacks gained a trailing
                              update_refs slot (left null, so libgit2 falls
                              back to update_tips). Without it libgit2 reads
                              past the struct.
  - Core/GitConfigEntry.cs    git_config_entry lost its trailing free member.
  - Core/GitCheckoutOpts.cs   the checkout strategy values changed:
                              GIT_CHECKOUT_SAFE is now 0 and GIT_CHECKOUT_NONE
                              is 1 << 30. Clone-without-checkout sends exactly
                              NONE (libgit2 tests for equality), and
                              Repository.RemoveUntrackedFiles sends NONE with
                              its flags so tracked files are still left alone.
  - Core/Proxy.cs             git_libgit2_features() returns more bits in 1.9;
                              they are masked to the four the public
                              BuiltInFeatures enum defines, so the public
                              value is unchanged.
No public API changed. All 352 bound functions exist in 1.9.7 with the same
signatures. Three tests were added that fail without these changes (in
CloneTests, CleanTests and BlameTests), and GlobalSettingsTests.SetExtensions
lists the two extensions libgit2 1.9 added.

Known difference in behaviour, not covered by a test: with the default file
conflict strategy, cherry-pick and revert now pass a strategy of 0, which
libgit2 1.9 replaces with its own default (it adds "allow conflicts"); under
1.8 the managed code passed a non-zero "safe" value and libgit2 kept it. 1.9
has no non-zero plain "safe" value, so the old behaviour cannot be reproduced
exactly. libgit2 still refuses when uncommitted changes would be overwritten.

When auditing a future libgit2: a struct that is one field off corrupts memory
silently and may pass most tests. Check every [StructLayout] type and every
hard-coded native value against the new headers, not only the functions.


BUILDING THE WINDOWS AND MACOS BINARIES
=======================================
Four of the nine binaries cannot be built on the Linux development machine.
The two scripts below were written on Linux. The macOS one was FIRST RUN on
2026-11-19 and needed two fixes (recorded in
tools/libgit2-native-tools/BUILD-PROVENANCE.txt); the Windows one was FIRST
RUN the same day and needed none - but it must be given a short neutral
-BuildRoot (step 2 below). If either breaks on a later run, fix the script,
note what was wrong in BUILD-PROVENANCE.txt, and update the STATUS block at
the top of that platform's README.txt.

Before either: pull the repository so the machine has the 1.9.7 tree, pins and
scripts. The scripts download nothing and install nothing; they stop with a
message when a prerequisite is missing.

ON THE WINDOWS x64 MACHINE (builds win-x64 AND win-arm64)
----------------------------------------------------------
DONE 2026-11-19 for libgit2 1.9.7 (Dell Precision 7770, Windows 11 Pro,
Visual Studio Professional 2026 18.12.1): both RIDs built, gated and adopted;
win-x64 passed the smoke test and the managed suite, win-arm64 is cross-built
with its smoke test unrun. The steps below are what to do the next time.
Detailed reference: tools/libgit2-native-tools/windows/README.txt.

 1. Prerequisites (once):
      - Visual Studio 2022 or newer (or its Build Tools) with the workload
        "Desktop development with C++";
      - for win-arm64, ALSO the individual component "MSVC v14x - VS 20xx C++
        ARM64/ARM64EC build tools (Latest)" (Visual Studio Installer ->
        Modify -> Individual components). Without it the arm64 build stops;
      - CMake 3.5.1 or newer on PATH (winget install Kitware.CMake), or use a
        "Developer PowerShell for VS", which has Visual Studio's own copy;
      - PowerShell 5.1 (built in) or 7. If scripts are blocked, run once in
        that window:  Set-ExecutionPolicy -Scope Process Bypass

 2. Build both, from PowerShell:
        cd tools\libgit2-native-tools\windows
        .\build.libgit2.ps1 -x64 -arm64 -BuildRoot C:\cbx-lg2
    The -BuildRoot matters: libgit2 compiles source file paths into the DLL
    and MSVC cannot remap them, so the default build root under %TEMP% would
    put the builder's user-profile path into a shipped binary. If Strawberry
    Perl is installed, take C:\Strawberry\c\bin off PATH for the build (its
    older MinGW cmake would otherwise be found first).
    Each architecture is configured, built, and checked by the script's gate
    (correct machine type; all required exports; operating-system imports
    only - a Visual C++ runtime import means the static runtime did not take;
    the feature summary; the import library). Results are written to
    tools\libgit2-native-tools\output\win-x64\ and ...\win-arm64\.

 3. What can and cannot be tested there:
      - win-x64 is smoke-tested by the script (a small C program that loads
        the DLL, checks version 1.9.7 and the feature mask, and creates a
        repository).
      - win-arm64 is CROSS-built. An x64 machine cannot run it, so the script
        builds the smoke test and says where it is, but does not run it. It
        stays unexecuted until someone runs it on ARM64 Windows; record that
        in its provenance.
      - The expected feature mask in the script (1983) was DERIVED from the
        libgit2 source and OBSERVED on win-x64 on 2026-11-19. If a later
        smoke test reports another number, find out which feature differs
        before changing the script.
      - The list of allowed imported DLLs was written from the 1.8.6 builds.
        If 1.9.7 imports another Windows system DLL the gate stops; that is
        something to look at and record, then add to the list.

 4. Adopt each result (for win-x64, and the same for win-arm64):
      - read output\win-x64\BUILD-INFO.txt;
      - copy git2-49e408b.dll AND git2-49e408b.lib from output\win-x64\ to
        native_libraries\win-x64\ (the two LICENSE-*.txt files there are
        already current). The .lib is the DLL's import library: native tools
        (the CodeBrix.WebPlatform dx build is one) link against it. It is
        committed but NOT packed into the NuGet package;
      - write native_libraries\win-x64\git2-49e408b.dll.provenance.txt from
        BUILD-INFO.txt, including the .lib's size and SHA-256 (the files in
        native_libraries\linux-x64\ are the model);
      - replace that RID's PENDING section in
        tools\libgit2-native-tools\BUILD-PROVENANCE.txt.

 5. Run the managed tests on Windows (they exercise the x64 DLL):
        dotnet build CodeBrix.LibGit2.slnx -c Release
        dotnet test --solution CodeBrix.LibGit2.slnx -c Release
    The Linux result to compare with: 1381 tests, 0 failed, 34 skipped.
    Result 2026-11-19 (win-x64 DLL, Debug and Release, run as dotnet test
    --project tests/CodeBrix.LibGit2.Tests): 1381 tests, 0 failed, 32
    skipped - the first proof of the 1.9 interop on Windows. The win-arm64
    DLL has not been run under the suite (no ARM64 machine yet).

ON THE APPLE SILICON MAC (builds osx-arm64 AND osx-x64)
--------------------------------------------------------
DONE 2026-11-19 for libgit2 1.9.7 (Mac mini, Apple M2 Pro, macOS 27.2.1,
Xcode 27.2): both slices built, gated, reproduced byte-identically and
adopted; the steps below are what to do the next time.
Detailed reference: tools/libgit2-native-tools/macos/README.txt.

 1. Prerequisites (once):
      - Xcode or the Command Line Tools:  xcode-select --install
      - CMake 3.5.1 or newer on PATH:  brew install cmake  (or CMake.app with
        its command-line tools installed);
      - optional, to RUN the Intel binary's smoke test:
          softwareupdate --install-rosetta

 2. THE MINIMUM macOS IS 14.0, the same as the binaries being replaced. The
    rule: a new binary must not require a newer macOS than the old one did.
    The script passes that deployment target by default, and its gate FAILS
    a binary whose recorded minimum ("minos") is anything else - without the
    setting, a build on a current Mac would require that Mac's own, newer,
    macOS. Leave it alone unless the floor is being changed on purpose
    (DEPLOYMENT_TARGET=<version> ./build.sh); the minimum is recorded in
    BUILD-INFO.txt and belongs in the provenance files and
    BUILD-PROVENANCE.txt.

 3. Build both, from Terminal:
        cd tools/libgit2-native-tools/macos
        ./build.sh
    (./build.sh arm64 or ./build.sh x64 builds one.) Each slice is built
    separately - two thin files, not one universal file - and checked by the
    gate (exactly the requested architecture; links only system libraries;
    all required exports; the minimum macOS; the feature summary). Results
    are written to tools/libgit2-native-tools/output/osx-arm64/ and
    .../osx-x64/.

 4. What can and cannot be tested there:
      - osx-arm64 is smoke-tested natively.
      - osx-x64 is cross-built; its smoke test runs only if Rosetta 2 is
        installed. Under Rosetta it is still not real Intel hardware; say so
        in its provenance.
      - The expected feature mask in the script (1791) was DERIVED from the
        libgit2 source and OBSERVED on both slices on 2026-11-19. If a later
        smoke test reports another number, find out which feature differs
        before changing the script.
      - The arm64 file is signed ad hoc by the linker; the x64 file is
        unsigned. Neither is notarised; .NET loads them by path, which does
        not need it.
      - The dylibs are not stripped (as upstream), so unlike Linux and
        Android there is no unstripped twin to keep.

 5. Adopt each result (for osx-arm64, and the same for osx-x64):
      - read output/osx-arm64/BUILD-INFO.txt (the minimum macOS above all);
      - copy libgit2-49e408b.dylib to native_libraries/osx-arm64/ (the two
        LICENSE-*.txt files there are already current);
      - write native_libraries/osx-arm64/libgit2-49e408b.dylib.provenance.txt
        from BUILD-INFO.txt;
      - replace that RID's section in
        tools/libgit2-native-tools/BUILD-PROVENANCE.txt.

 6. Run the managed tests on the Mac (they exercise the arm64 dylib):
        dotnet build CodeBrix.LibGit2.slnx -c Release
        dotnet test --solution CodeBrix.LibGit2.slnx -c Release
    First proof of the 1.9 interop on macOS, including the one binding that
    is special there (the variadic options call on Apple Silicon).
    Result 2026-11-19 (osx-arm64 dylib, Release): 1381 tests, 0 failed, 34
    skipped - the same as Linux. The osx-x64 dylib has had only its smoke
    test (under Rosetta 2): the managed suite was not run against it, as
    the build Mac has no x64 .NET runtime.

AFTER BOTH MACHINES
-------------------
  - native_libraries/ must hold a binary in all nine RID folders, each with
    its .provenance.txt, and BUILD-PROVENANCE.txt must have no PENDING
    section left.
  - Commit the binaries, the .lib files, the provenance files and any script
    fixes together.
  - Build the package and check it (PACKAGING AND PUBLISHING): unzip the
    .nupkg and confirm runtimes/<rid>/native/ exists for all nine RIDs. Only
    then publish.
  - Consumers that pin the old package (CodeBrix.RustTools.CrateBuilder is
    one) need only the version bump: no public API changed.


CODING CONVENTIONS
==================
  - File layout: the using block (System.* first, alphabetical, one
    contiguous block), one blank line, the file-scoped namespace line, one
    blank line, the body. No BOM, LF line endings.
  - Ported files carry `namespace CodeBrix.LibGit2[.X]; //was previously:
    LibGit2Sharp[.X];` on the namespace line. New-in-family files
    (NativeDllName.cs, UniqueId.cs, AssemblyCommitIds.cs,
    InternalsVisibleTo.cs, the NetworkFact/NetworkTheory/NetworkTestSwitch
    helpers) have no header and no //was previously: comment.
  - File-scoped namespaces only. No global usings, no <ImplicitUsings>.
  - Nullable reference types are OFF: never `?` on reference types, never
    the `!` operator. Value-type nullables (int?, TagFetchMode?) are fine.
  - XML doc comments on every public member; CS1591 is fixed at the source.
  - No #pragma warning and no <NoWarn>: fix the warning at its source.
  - Type and member names stay upstream's (Repository, LibGit2SharpException,
    UserCancelledException ...) - renaming them would break every consumer
    moving over from LibGit2Sharp.
  - The LEAKS_TRACKING / LEAKS_IDENTIFYING conditional blocks are upstream's
    opt-in leak diagnostics; they are kept verbatim and not compiled by
    default.
  - Tests: xUnit v3 + SilverAssertions + CodeBrix.TestMocks. Test classes
    are named <Area>Tests (files <Area>Tests.cs); the test METHOD names are
    kept exactly as upstream wrote them (PascalCase), following the family
    precedent for large ported suites, so the suite stays diffable against
    upstream. Tests that reach a remote host use [NetworkFact] /
    [NetworkTheory], never a plain [Fact]; pass
    TestContext.Current.CancellationToken to anything cancellable in new
    tests.


PROVENANCE / VENDORED SOURCES
=============================
  - Managed library and tests: ported from LibGit2Sharp (MIT, LibGit2Sharp
    contributors) master @ eaa698d078941fd5e3cc82b59b885cd35d8cc0f8.
    Namespace mapping LibGit2Sharp -> CodeBrix.LibGit2,
    LibGit2Sharp.Core -> CodeBrix.LibGit2.Core, LibGit2Sharp.Handlers ->
    CodeBrix.LibGit2.Handlers, LibGit2Sharp.Tests -> CodeBrix.LibGit2.Tests.
  - tools/libgit2-native-tools/libgit2/: libgit2 at the pinned commit,
    unmodified, without upstream's .github/ folder (GPLv2 with the linking
    exception).
  - tools/libgit2-native-tools/mbedtls/: Mbed TLS v3.6.7 with its
    framework/ submodule, unmodified (Apache-2.0 OR GPL-2.0-or-later; taken
    under Apache-2.0).
  - tools/libgit2-native-tools/reference-upstream/: verbatim copies of
    LibGit2Sharp.NativeBinaries' build files (MIT) as of its libgit2 1.8
    packaging; a historical record only.
  - tools/libgit2-native-tools/UPSTREAM.txt records the URL, commit, copy
    command and tree checksum of each vendored tree. THIRD-PARTY-NOTICES.txt
    has the full notices and licence texts.


NOTES
=====
  - Native library resolution (src/CodeBrix.LibGit2/Core/NativeMethods.cs)
    tries, in order: GlobalSettings.NativeLibraryPath (a folder; the file is
    looked for as <folder>/git2-49e408b + .dll/.so/.dylib, i.e. WITHOUT the
    "lib" prefix the Linux/macOS binaries have), then the normal .NET probing
    (deps.json native assets, the assembly and application folders), then on
    Linux a scan of <app>/runtimes/*-<arch>/native/. A PackageReference
    consumer is served by the second step. The no-prefix behaviour of the
    first step is upstream's and is documented for consumers in
    AGENT-README.txt.
  - Version.LibGit2SharpCommitSha reports the upstream commit this port came
    from (AssemblyCommitIds.cs), and Version.InformationalVersion is the
    date-stamped package version plus the source revision.
  - tools/libgit2-native-tools/.gitignore starts with a load-bearing "!*":
    the family .gitignore ignores names that occur inside the vendored trees
    and the build pins (*.env, Debug/, ARM64/, *.obj ...). Read its comment
    before touching it.
  - .gitattributes sets LF for this repository's own files and switches
    line-ending handling OFF (-text) for tools/libgit2-native-tools/libgit2/,
    .../mbedtls/, .../reference-upstream/ and
    tests/CodeBrix.LibGit2.Tests/Resources/. Those folders hold more than a
    thousand files that are CRLF or mixed ON PURPOSE (upstream sources and
    test fixtures whose line endings are what is being tested). Never add a
    rule that normalises them; each also has .gitattributes files of its own.
  - The AI-agent pointer stubs at the repo root (AGENTS.md, CLAUDE.md,
    .clinerules, .cursorrules, .cursor/rules/agent-readme.mdc,
    .windsurfrules, .github/copilot-instructions.md, .junie/guidelines.md)
    are byte-exact copies of the CodeBrix.SkiaSvg canonical; never edit them
    per repo.


================================================================================
END OF MAINTAINER-README
================================================================================
