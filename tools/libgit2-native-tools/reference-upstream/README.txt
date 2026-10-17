================================================================================
tools/libgit2-native-tools/reference-upstream - HOW UPSTREAM BUILT 2.0.324
================================================================================

A RECORD, NOT A TOOL. These are VERBATIM copies of the build files of upstream
LibGit2Sharp.NativeBinaries at commit 2b95030f15e55dfe70e9fdbb2b3d304a5c95ad23
(2026-07-20) - the commit the 2.0.324 package's nuspec names, and the package
whose win-* and osx-* binaries this repository currently ships (see
../BUILD-PROVENANCE.txt). They are kept so that anyone can see, a year from
now, exactly what recipe produced the adopted binaries and what our own scripts
were adapted from.

NO SCRIPT IN THIS REPOSITORY RUNS, SOURCES OR READS ANY FILE HERE. Our builds
are ../linux/, ../android/, ../windows/ and ../macos/. In particular the
kmturley/crossbuild and multiarch/alpine images that docker/linux and
docker/musl name are NOT used: the Linux builds here use the digest-pinned
manylinux images instead (ruling 2026-10-17). Do not "fix" these files - they
are a record of upstream as it was.

  build.libgit2.sh          upstream's Linux + macOS build (cmake flags; the
                            Linux branch's flags are ../linux/pins.env's
                            LIBGIT2_CMAKE_OPTIONS, plus -DREGEX_BACKEND=builtin)
  build.libgit2.ps1         upstream's Windows build (-x86 / -x64 / -arm64,
                            Schannel); ../windows/build.libgit2.ps1 is adapted
                            from it
  dockerbuild.sh            upstream's CI driver for the Linux RIDs
  docker/linux              its glibc Dockerfile (kmturley/crossbuild)
  docker/musl               its musl Dockerfile (multiarch/alpine 3.13)
  UpdateLibgit2ToSha.ps1    upstream's script for moving to a new libgit2
                            commit
  nuget.package/build/LibGit2Sharp.NativeBinaries.props
  nuget.package/build/net46/LibGit2Sharp.NativeBinaries.props
                            how upstream's package exposed the native name
                            (libgit2_filename = git2-5853918) to MSBuild - the
                            managed side here replaces this with a constant
  nuget.package/libgit2/libgit2_hash.txt
                            the pinned libgit2 commit,
                            5853918c4c6a7b12f8becf4bd11ff4362ebb9020

Copied 2026-10-17 from the read-only clone ~/GitHome/libgit2sharp.nativebinaries
with `cp -p`. Upstream's licence for these files: MIT, Copyright (c)
LibGit2Sharp contributors. sha256 of each copy (identical to the clone's):

    UpdateLibgit2ToSha.ps1
        d21da57b57187ec5bc1fa03cef261218083cc10046210173fe98dd0b8ebdbf06
    build.libgit2.ps1
        a554b71b544b8bb13d05288b37c12c16902f38c21e1de47819d8f4a65b04fc22
    build.libgit2.sh
        593cb18634fed137e03c4d4e9b933070cfc534af0d7f566f573c8cb937f10409
    docker/linux
        4ae04497727c57fea2eb05100c44ed78a5aa700b3b6b84d2e9490c705c093eaf
    docker/musl
        63ec7cd0bb9a1702de15564f493242767e3afaf0dee0ad0738cbaa39d9a0615f
    dockerbuild.sh
        604d2a71f301e30a48b85339b4394a940d7ef44b8fc1ec4cae617ecb1a2a04d9
    nuget.package/build/LibGit2Sharp.NativeBinaries.props
        2e6493c6a411a819560b384a97f79c2acc3522b61506829ed16f1d63f45ca572
    nuget.package/build/net46/LibGit2Sharp.NativeBinaries.props
        f1bd5e75f96c0905a4dd9e6c6bbcdb196ed4b4d15ad61eec003193715f7f9ecd
    nuget.package/libgit2/libgit2_hash.txt
        4891c695323054193c0242dd1f17b33ce21a57849925ec3c3af9a8fef55289cd
================================================================================
