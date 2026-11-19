================================================================================
tools/libgit2-native-tools/macos - building libgit2-49e408b.dylib for
                                   osx-arm64 and osx-x64
================================================================================

>>> STATUS 2026-11-18: libgit2 1.9.7. build.sh is WRITTEN BUT NEVER RUN - it
    was written (2026-10-17) and re-pointed at 1.9.7 (2026-11-18) on Linux,
    which has no Apple SDK. osx-arm64 and osx-x64 are PENDING: there is NO
    binary for them in ../../../native_libraries/. The 1.8.6 dylibs adopted
    from LibGit2Sharp.NativeBinaries 2.0.324 were removed (wrong ABI under an
    old name), and upstream has published no libgit2 1.9 binaries, so these
    two RIDs must be built HERE, with this script, on Jeremy's Mac. Expect to
    fix something on the first real run; fix it in the script, record it in
    ../BUILD-PROVENANCE.txt, and rewrite this block. <<<

WHAT THIS IS
--------------------------------------------------------------------------------
build.sh is upstream's build.libgit2.sh, Darwin branch (verbatim copy, at
upstream's libgit2 1.8.6 release: ../reference-upstream/build.libgit2.sh),
turned into a gated, repository-only
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
    DEPLOYMENT_TARGET=13.0 ./build.sh     # another floor; see THE MACOS FLOOR

  Either slice builds on either kind of Mac (CMAKE_OSX_ARCHITECTURES). The
  smoke test runs a slice where the Mac can execute it: natively, or osx-x64
  under Rosetta 2 on Apple Silicon (install Rosetta with
  `softwareupdate --install-rosetta` if you want that run).

THE BUILD - UPSTREAM'S RECIPE, EXACTLY
--------------------------------------------------------------------------------
    cmake -DCMAKE_BUILD_TYPE=Release -DBUILD_TESTS=OFF -DUSE_SSH=exec
          -DLIBGIT2_FILENAME=git2-49e408b -DCMAKE_OSX_ARCHITECTURES=arm64|x86_64
          -DUSE_HTTPS=ON -DUSE_BUNDLED_ZLIB=ON
    cmake --build .

  * every one of these options exists, with the same meaning, in libgit2
    1.9.7's CMake (1.9 turned USE_HTTPS into a string option, but ON still
    means "auto-select"); the file name comes from ../linux/pins.env;
  * USE_HTTPS=ON on a Mac auto-selects SecureTransport (Security.framework);
    SHA-1 is the bundled collision-detecting one, SHA-256 CommonCrypto;
  * libgit2 links /usr/lib/libiconv.2.dylib on Apple (USE_ICONV defaults ON
    there) and uses the system regcomp_l for regular expressions - both part
    of macOS, as in the upstream dylibs;
  * CMake writes libgit2-49e408b.1.9.7.dylib plus symlinks; like upstream, the
    script copies libgit2-49e408b.dylib (cp follows the link). Its install
    name stays @rpath/libgit2-49e408b.1.9.dylib, as built - .NET loads the
    file by path, so the install name is not consulted;
  * NOT stripped, as upstream. The arm64 linker signs its output ad hoc; the
    x86_64 output is unsigned - again as upstream.

THE MACOS FLOOR
--------------------------------------------------------------------------------
Upstream passes no CMAKE_OSX_DEPLOYMENT_TARGET, so the floor is whatever the
build Mac's toolchain defaults to - for the 1.8.6 dylibs adopted from 2.0.324
(shipped until 2026-11-18), built on a macos-14 runner, LC_BUILD_VERSION minos
14.0 (macOS 14 Sonoma) on BOTH slices, so they refused to load on macOS 13 or
older. A 1.9.7 build on a current Mac, with no DEPLOYMENT_TARGET, typically
gets that Mac's own macOS version as its floor - likely HIGHER than 14.0.
The owner's rule (2026-11-18) is that the new binaries must not require a
newer macOS than the ones they replace, so build.sh DEPARTS FROM UPSTREAM
HERE: it passes -DCMAKE_OSX_DEPLOYMENT_TARGET=14.0 by default, and the gate
FAILS a dylib whose minos is not exactly the requested value. Set
DEPLOYMENT_TARGET to choose another floor deliberately (a lower one is
allowed; a higher one breaks the rule), and say so in ../BUILD-PROVENANCE.txt.
If the installed Xcode's SDK cannot target 14.0 the build stops; that would
need a decision, not a silent raise. SecureTransport is deprecated by Apple; the upstream
dylibs use it all the same.

THE GATE
--------------------------------------------------------------------------------
  1. lipo -archs is exactly the requested slice (thin, not universal).
  2. Install name recorded; no LC_RPATH.
  3. Links only the system: CoreFoundation, Security, libiconv, libSystem
     (libz is allowed by the list but is bundled, so it does not appear).
  4. Exports - every REQUIRED_SYMBOLS name from ../linux/pins.env (nm -gU);
     954 for the 1.9.7 Linux and Android builds (944 for 1.8.6).
  5. minos is exactly DEPLOYMENT_TARGET, 14.0 by default (see THE MACOS FLOOR).
  6. Feature summary - HTTPS using SecureTransport, SSH using OpenSSH exec,
     SHA1 CollisionDetection, zlib bundled.
  7. Smoke test - ../linux/smoke-test.c built with cc -arch <slice> and run
     against the dylib (version 1.9.7, git_libgit2_features() == 1791 =
     THREADS | HTTPS | SSH | NSEC | HTTP_PARSER | REGEX | I18N | AUTH_NTLM |
     COMPRESSION | SHA1, init, blob hash, repository init/write/open,
     shutdown). libgit2 1.9 widened git_feature_t; the 1.8.6 dylibs returned
     15. 1791 is DERIVED from src/libgit2/libgit2.c (I18N from iconv, ON on
     Apple; AUTH_NTLM from the bundled ntlmclient over CommonCrypto), not yet
     observed - if the first run reports another value, find out why before
     changing EXPECTED_FEATURES in build.sh.
     No CA location is passed: SecureTransport uses the keychain, and libgit2
     refuses GIT_OPT_SET_SSL_CERT_LOCATIONS for it.

OUTPUT (../output/osx-<arch>/)
--------------------------------------------------------------------------------
  libgit2-49e408b.dylib, LICENSE-Libgit2.txt (libgit2's COPYING),
  LICENSE-Xdiff.txt (../LICENSE-Xdiff.txt), smoke-test, BUILD-INFO.txt,
  SHA256SUMS.txt.

ADOPTING A BUILT DYLIB
--------------------------------------------------------------------------------
  1. Read BUILD-INFO.txt (minos above all).
  2. Copy libgit2-49e408b.dylib, LICENSE-Libgit2.txt and LICENSE-Xdiff.txt to
     ../../../native_libraries/osx-<arch>/ (the licence files are already
     there; the copies from the build are identical).
  3. Write native_libraries/osx-<arch>/libgit2-49e408b.dylib.provenance.txt
     from BUILD-INFO.txt (the Linux ones are the model), minos above all.
  4. Replace that RID's PENDING section in ../BUILD-PROVENANCE.txt.
  5. Run the managed test suite on a Mac before publishing.
================================================================================
