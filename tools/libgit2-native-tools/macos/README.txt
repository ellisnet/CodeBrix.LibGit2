================================================================================
tools/libgit2-native-tools/macos - building libgit2-5853918.dylib for
                                   osx-arm64 and osx-x64
================================================================================

>>> STATUS 2026-10-17: build.sh is WRITTEN BUT NEVER RUN - it was written on
    Linux, which has no Apple SDK. The osx-arm64 and osx-x64 dylibs in
    ../../../native_libraries/ are ADOPTED from LibGit2Sharp.NativeBinaries
    2.0.324 (see ../BUILD-PROVENANCE.txt), which built them from this same
    libgit2 commit with the recipe this script reproduces. Expect to fix
    something on the first real run; fix it in the script, record it in
    ../BUILD-PROVENANCE.txt, and rewrite this block. <<<

WHAT THIS IS
--------------------------------------------------------------------------------
build.sh is upstream's build.libgit2.sh, Darwin branch (verbatim copy:
../reference-upstream/build.libgit2.sh), turned into a gated, repository-only
build: it builds libgit2 from ../libgit2 - the vendored, unmodified snapshot -
with Apple clang, checks the result and writes ../output/osx-<arch>/. It
downloads nothing and installs nothing.

PREREQUISITES (checked, never installed)
--------------------------------------------------------------------------------
  * macOS on Apple Silicon or Intel.
  * Xcode or the Command Line Tools (clang, the macOS SDK, otool, nm, lipo,
    codesign):      xcode-select --install
  * CMake 3.5.1 or newer on PATH:      brew install cmake
    (or CMake.app from cmake.org with its command-line tools installed)

USAGE
--------------------------------------------------------------------------------
    cd tools/libgit2-native-tools/macos
    ./build.sh               # both slices
    ./build.sh arm64         # osx-arm64 only
    ./build.sh x64           # osx-x64 only
    DEPLOYMENT_TARGET=12.0 ./build.sh     # see THE MACOS FLOOR first

  Either slice builds on either kind of Mac (CMAKE_OSX_ARCHITECTURES). The
  smoke test runs a slice where the Mac can execute it: natively, or osx-x64
  under Rosetta 2 on Apple Silicon (install Rosetta with
  `softwareupdate --install-rosetta` if you want that run).

THE BUILD - UPSTREAM'S RECIPE, EXACTLY
--------------------------------------------------------------------------------
    cmake -DCMAKE_BUILD_TYPE=Release -DBUILD_TESTS=OFF -DUSE_SSH=exec
          -DLIBGIT2_FILENAME=git2-5853918 -DCMAKE_OSX_ARCHITECTURES=arm64|x86_64
          -DUSE_HTTPS=ON -DUSE_BUNDLED_ZLIB=ON
    cmake --build .

  * USE_HTTPS=ON on a Mac auto-selects SecureTransport (Security.framework);
    SHA-1 is the bundled collision-detecting one, SHA-256 CommonCrypto;
  * libgit2 links /usr/lib/libiconv.2.dylib on Apple (USE_ICONV defaults ON
    there) and uses the system regcomp_l for regular expressions - both part
    of macOS, as in the upstream dylibs;
  * CMake writes libgit2-5853918.1.8.6.dylib plus symlinks; like upstream, the
    script copies libgit2-5853918.dylib (cp follows the link). Its install
    name stays @rpath/libgit2-5853918.1.8.dylib, as built - .NET loads the
    file by path, so the install name is not consulted;
  * NOT stripped, as upstream. The arm64 linker signs its output ad hoc; the
    x86_64 output is unsigned - again as upstream.

THE MACOS FLOOR
--------------------------------------------------------------------------------
Upstream passes no CMAKE_OSX_DEPLOYMENT_TARGET, so the floor is whatever the
build Mac's toolchain defaults to - for the adopted 2.0.324 dylibs, built on a
macos-14 runner, LC_BUILD_VERSION minos 14.0 (macOS 14 Sonoma) on BOTH slices.
That is what the package ships today, and it means the osx-* libraries refuse
to load on macOS 13 or older. build.sh keeps upstream's behaviour by default
(DEPLOYMENT_TARGET unset) and records the minos it got; set DEPLOYMENT_TARGET
to choose a lower floor deliberately, and say so in ../BUILD-PROVENANCE.txt.
SecureTransport is deprecated by Apple; the upstream dylibs use it all the same.

THE GATE
--------------------------------------------------------------------------------
  1. lipo -archs is exactly the requested slice (thin, not universal).
  2. Install name recorded; no LC_RPATH.
  3. Links only the system: CoreFoundation, Security, libiconv, libSystem
     (libz is allowed by the list but is bundled, so it does not appear).
  4. Exports - every REQUIRED_SYMBOLS name from ../linux/pins.env (nm -gU);
     944 upstream.
  5. minos recorded (see THE MACOS FLOOR).
  6. Feature summary - HTTPS using SecureTransport, SSH using OpenSSH exec,
     SHA1 CollisionDetection, zlib bundled.
  7. Smoke test - ../linux/smoke-test.c built with cc -arch <slice> and run
     against the dylib (version 1.8.6, git_libgit2_features() == 15 = THREADS
     | HTTPS | SSH | NSEC, as the upstream dylibs return - read from their
     machine code -, init, blob hash, repository init/write/open, shutdown).
     No CA location is passed: SecureTransport uses the keychain, and libgit2
     refuses GIT_OPT_SET_SSL_CERT_LOCATIONS for it.

OUTPUT (../output/osx-<arch>/)
--------------------------------------------------------------------------------
  libgit2-5853918.dylib, LICENSE-Libgit2.txt (libgit2's COPYING),
  LICENSE-Xdiff.txt (../LICENSE-Xdiff.txt), smoke-test, BUILD-INFO.txt,
  SHA256SUMS.txt.

ADOPTING A BUILT DYLIB
--------------------------------------------------------------------------------
  1. Read BUILD-INFO.txt (minos above all).
  2. Copy libgit2-5853918.dylib, LICENSE-Libgit2.txt and LICENSE-Xdiff.txt to
     ../../../native_libraries/osx-<arch>/ (replacing the adopted dylib).
  3. Rewrite native_libraries/osx-<arch>/libgit2-5853918.dylib.provenance.txt
     from BUILD-INFO.txt - it then says BUILT, not ADOPTED.
  4. Replace that RID's section in ../BUILD-PROVENANCE.txt.
  5. Run the managed test suite on a Mac before publishing.
================================================================================
