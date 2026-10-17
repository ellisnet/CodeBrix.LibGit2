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
                                     per RID: the binary (git2-5853918.dll,
                                     libgit2-5853918.dylib or
                                     libgit2-5853918.so),
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
    global.json                      selects the Microsoft.Testing.Platform
                                     test runner; pins no SDK
    .gitignore                       the family .gitignore (see the NOTES on
                                     the nested one in tools/)
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

    { "test": { "runner": "Microsoft.Testing.Platform" } }

That file does NOT pin an SDK version; it exists solely to select the runner.
Keep it committed. MTP output ends in a "Test run summary:" block. If
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
The binary name keeps the libgit2 commit prefix - git2-5853918 (Windows
git2-5853918.dll, macOS libgit2-5853918.dylib, Linux/Android
libgit2-5853918.so) - exactly the name upstream LibGit2Sharp uses, so this
library never collides with an upstream LibGit2Sharp copy or a system libgit2
loaded into the same process. The managed side names it once, in
src/CodeBrix.LibGit2/Core/NativeDllName.cs.

Where each shipped binary comes from (full per-RID record, hashes and gate
results: tools/libgit2-native-tools/BUILD-PROVENANCE.txt; per-file record:
native_libraries/<rid>/<binary>.provenance.txt):

  linux-x64, linux-arm64, linux-riscv64
      BUILT IN THIS REPOSITORY by tools/libgit2-native-tools/linux/build.sh
      in the digest-pinned manylinux containers (x86_64 native; aarch64 and
      riscv64 under qemu-user emulation), with the network disabled, from the
      vendored libgit2 source. HTTPS = OpenSSL loaded at run time
      (OpenSSL-Dynamic), SSH = exec of the system ssh, bundled zlib, PCRE2,
      llhttp and xdiff. A verification gate (ELF, SONAME, exports, NEEDED,
      glibc floor, build-id, feature summary, smoke test) runs on every
      build.
  android-arm64, android-x64
      BUILT IN THIS REPOSITORY by tools/libgit2-native-tools/android/build.py
      with the pinned Android NDK, ANDROID_PLATFORM 33, 16 KiB page-size link
      flags, HTTPS through Mbed TLS built first from the vendored
      tools/libgit2-native-tools/mbedtls/ and linked STATICALLY, and no SSH
      transport.
  win-x64, win-arm64, osx-x64, osx-arm64
      ADOPTED, unchanged, from the LibGit2Sharp.NativeBinaries 2.0.324
      package on nuget.org, which built them from the same libgit2 commit
      (Windows: MSVC, HTTPS via Schannel; macOS: SecureTransport; both with
      SSH = exec). The adopted macOS binaries carry a minimum macOS of 14.0.
      tools/libgit2-native-tools/windows/build.libgit2.ps1 and
      tools/libgit2-native-tools/macos/build.sh rebuild them from the
      vendored source; they have NOT yet been run - that needs Jeremy's
      Windows machine and Mac. When they are, the binaries, the
      .provenance.txt files and BUILD-PROVENANCE.txt change together.

How to rebuild one RID or all of them: tools/libgit2-native-tools/README.txt
and the per-platform README.txt it points to. Build tools (cmake, ninja,
compilers, podman, the NDK, Visual Studio, Xcode) come from their normal
sources and are not in the repository; everything compiled is.

The pins in force (deliberate, change only together):
    libgit2           5853918c4c6a7b12f8becf4bd11ff4362ebb9020 (tag v1.8.6)
    LibGit2Sharp      master @ eaa698d078941fd5e3cc82b59b885cd35d8cc0f8 (the
                      managed source this port came from; it targets that
                      libgit2 commit)
    Mbed TLS          tag v3.6.7 (Android only)
    adopted natives   LibGit2Sharp.NativeBinaries 2.0.324

BUMPING LIBGIT2 touches all of these at once:
  - the vendored tree tools/libgit2-native-tools/libgit2/ (replaced whole,
    never patched) and tools/libgit2-native-tools/UPSTREAM.txt;
  - the build pins (linux/pins.env, android/pins.json; the Windows and macOS
    scripts read the Linux pins) - commit, version and the git2-<short>
    file name;
  - src/CodeBrix.LibGit2/Core/NativeDllName.cs (the native name) and
    src/CodeBrix.LibGit2/AssemblyCommitIds.cs (LibGit2CommitSha);
  - every native item in src/CodeBrix.LibGit2/CodeBrix.LibGit2.csproj (the
    file names are spelled out per RID);
  - EVERY native binary, rebuilt (or re-adopted) for all nine RIDs, with new
    .provenance.txt files and BUILD-PROVENANCE.txt sections;
  - the managed interop (Core/NativeMethods.cs and the native structs) if
    the C API changed - in practice, port the matching upstream LibGit2Sharp
    changes, since its interop tracks one libgit2 commit;
  - THIRD-PARTY-NOTICES.txt (commit, licence texts, bundled dependencies).
A binary from a different libgit2 commit under the old name loads and then
misbehaves; never mix commits.


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
  - tools/libgit2-native-tools/libgit2/: libgit2 at the pinned commit, full
    and unmodified (GPLv2 with the linking exception).
  - tools/libgit2-native-tools/mbedtls/: Mbed TLS v3.6.7 with its
    framework/ submodule, unmodified (Apache-2.0 OR GPL-2.0-or-later; taken
    under Apache-2.0).
  - tools/libgit2-native-tools/reference-upstream/: verbatim copies of
    LibGit2Sharp.NativeBinaries' build files (MIT), a record only.
  - tools/libgit2-native-tools/UPSTREAM.txt records the URL, commit, copy
    command and tree checksum of each vendored tree. THIRD-PARTY-NOTICES.txt
    has the full notices and licence texts.


NOTES
=====
  - Native library resolution (src/CodeBrix.LibGit2/Core/NativeMethods.cs)
    tries, in order: GlobalSettings.NativeLibraryPath (a folder; the file is
    looked for as <folder>/git2-5853918 + .dll/.so/.dylib, i.e. WITHOUT the
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
  - The AI-agent pointer stubs at the repo root (AGENTS.md, CLAUDE.md,
    .clinerules, .cursorrules, .cursor/rules/agent-readme.mdc,
    .windsurfrules, .github/copilot-instructions.md, .junie/guidelines.md)
    are byte-exact copies of the CodeBrix.SkiaSvg canonical; never edit them
    per repo.


================================================================================
END OF MAINTAINER-README
================================================================================
