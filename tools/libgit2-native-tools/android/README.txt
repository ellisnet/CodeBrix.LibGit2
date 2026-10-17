================================================================================
Android libgit2 - libgit2-5853918.so for ARM64 and x64 (Android 13 / API 33
minimum), HTTPS through statically linked Mbed TLS
================================================================================

The sources are ../libgit2 (libgit2 1.8.6, commit
5853918c4c6a7b12f8becf4bd11ff4362ebb9020) and ../mbedtls (Mbed TLS 3.6.7, tag
v3.6.7), the same unmodified vendored snapshots every build uses - see
../UPSTREAM.txt. No patches. Nothing is downloaded during a build, and nothing
in a dotnet build invokes these scripts.

  pins.json      NDK revision, API floor, cmake/ninja minimums, the libgit2 and
                 Mbed TLS identities, the compiled-in CA location, the allowed
                 NEEDED list, the required exports, the Mbed TLS symbols that
                 must be linked in, the required / forbidden feature defines,
                 and the git_libgit2_features() value
  build.py       builds both ABIs (or one) and runs the gates
  verify.py      the static gates, also usable on its own against any
                 libgit2-5853918.so

There is no upstream for this: LibGit2Sharp.NativeBinaries ships no Android
binaries. The recipe is CodeBrix.Sdl3's android/ recipe (NDK toolchain file,
API 33, 16 KB pages, container route, both twins gated) with libgit2's own
options for an Android target.

OUTPUT
------
  android-arm64 -> ABI arm64-v8a, aarch64-linux-android33-clang
  android-x64   -> ABI x86_64,    x86_64-linux-android33-clang

Both libraries are ELF64 libgit2-5853918.so with SONAME libgit2-5853918.so
(CMake's Android platform does not version shared libraries, so there is no
.so.1.8.6 + symlinks as on Linux - one plain file, the Android convention) and
API 33 in their Android ELF note. They NEED only libc.so, libm.so and libdl.so.
zlib, PCRE2, llhttp, xdiff, ntlmclient, SHA-1 collision detection AND Mbed TLS
are all inside the one file; there is no libz.so, no libc++_shared.so and no
second library to ship.

  git_libgit2_features() = 11 = THREADS | HTTPS | NSEC. No SSH: Android has no
  `ssh` executable for USE_SSH=exec to run, so SSH is OFF (libssh2 would be a
  second vendored dependency and is not in the plan). ssh:// and scp-style
  git@host: URLs therefore cannot be used on Android; https:// can.

libgit2's CMake feature summary, both ABIs:
    nanoseconds, CERT_LOCATION (/system/etc/security/cacerts),
    HTTPS using mbedTLS, SHA1 using CollisionDetection, SHA256 using mbedTLS,
    http-parser bundled, regex bundled PCRE, xdiff bundled, zlib bundled,
    futimens, threadsafe, ntlmclient
  disabled: SSH, GSSAPI, iconv, experimental SHA256 API, debug allocators

Both link with -z max-page-size=16384 and -z common-page-size=16384, so they
load on 4 KB and 16 KB page-size devices; the application's other native
libraries and its APK packaging must also support the device's page size. See:
  https://developer.android.com/guide/practices/page-sizes

HOW HTTPS WORKS ON ANDROID - AND THE CA LOCATION
------------------------------------------------
Android has no OpenSSL a third-party library may use, so HTTPS is Mbed TLS,
compiled from ../mbedtls FIRST, per ABI, as static libraries (libmbedtls.a,
libmbedx509.a, libmbedcrypto.a) with ENABLE_TESTING=OFF, ENABLE_PROGRAMS=OFF,
GEN_FILES=OFF and Mbed TLS's default configuration, unmodified. libgit2 is then
configured with -DUSE_HTTPS=mbedTLS and pointed at them by pre-seeding the
cache variables its cmake/FindmbedTLS.cmake would otherwise search for
(MBEDTLS_INCLUDE_DIR, MBEDTLS_LIBRARY, MBEDX509_LIBRARY, MBEDCRYPTO_LIBRARY -
the NDK toolchain confines find_library() to the sysroot, so a search would not
find them). SHA-256 then also comes from Mbed TLS; SHA-1 stays the bundled
collision-detecting implementation; NTLM uses Mbed TLS's MD5/DES/HMAC and
ntlmclient's own MD4.

-Wl,--exclude-libs,ALL keeps every Mbed TLS symbol PRIVATE to the library:
nothing named mbedtls_* or psa_* is exported (verify.py gates it), so an app
that loads another copy of Mbed TLS - or BoringSSL - cannot clash with this
one. The export table is 944 names, the same set as every Linux, Windows and
macOS build.

CERT_LOCATION. With the Mbed TLS backend libgit2 loads a default CA store when
it initialises TLS, from GIT_DEFAULT_CERT_LOCATION. build.py sets
-DCERT_LOCATION=/system/etc/security/cacerts, Android's system CA directory
(checked on the API 35 emulator: 145 certificates there, and the same 145 in
the updatable /apex/com.android.conscrypt/cacerts that Android 14+ also has).
Two consequences worth knowing:
  * libgit2's cmake/SelectHTTPSBackend.cmake insists that the path EXISTS on
    the BUILD machine (it was written for native builds). Inside the throwaway
    build container build.py creates an empty folder of that name; on a bare
    host it stops and tells you to create one. Nothing is read from it at
    build time - the value is only compiled in.
  * Without CERT_LOCATION that CMake file runs `openssl version -d` on the
    build machine and bakes in ITS CA path (the image's /etc/pki/tls/cert.pem)
    - useless on a phone. Setting it is therefore required, not cosmetic.
An app can still override the store at run time with
git_libgit2_opts(GIT_OPT_SET_SSL_CERT_LOCATIONS, file, path).

THE C90 GOTCHA (first-run fix, 2026-10-17)
------------------------------------------
libgit2 1.8.6 compiles its own targets as strict ISO C90 (C_STANDARD 90 and
C_EXTENSIONS OFF are hard-coded target properties), and the NDK's kernel UAPI
headers - linux/swab.h, reached from <arpa/inet.h> - use `static inline`, which
is not a C90 keyword. The first build failed on every libgit2 file with
"unknown type name 'inline'" (NDK r29 and r30 alike). build.py adds clang's
-fgnu-keywords to libgit2's C flags: it makes the GNU keyword `inline`
available without changing the language standard or touching any source.
(libgit2's own src/libgit2/streams/mbedtls.c works round the same C90 limit
with `#define inline __inline__`.) Mbed TLS is built without it.

TOOLS (install separately, before the offline build)
----------------------------------------------------
Linux x64 (or macOS) with the Android NDK host toolchain. The shipped files were
built on Linux x64 (LMDE 7 / Debian 13).

  * Python 3.9+; Python 3.13 was used.
  * NDK 30.0.16248370 (r30), pinned in pins.json. Install the Android SDK
    command-line tools, then (adjust the sdkmanager path to the SDK install):
      sdkmanager --install 'ndk;30.0.16248370'
    build.py never installs anything and refuses any other NDK revision.
  * cmake >= 3.5.1 (libgit2's and Mbed TLS's cmake_minimum_required) and
    ninja >= 1.10. cmake 4.4.3 and ninja 1.13.2 were used (from the container
    image, below). On Debian:
      sudo apt-get install cmake ninja-build
    or in a Python venv:
      python3 -m venv ~/.venvs/codebrix-libgit2-android
      ~/.venvs/codebrix-libgit2-android/bin/python -m pip install \
          cmake==4.4.3 ninja==1.13.2
      export PATH="$HOME/.venvs/codebrix-libgit2-android/bin:$PATH"
  * Bare host only: an empty /system/etc/security/cacerts folder (see above).
  * adb from Android SDK platform-tools, only for a device or emulator run.

BUILD (from the repository root)
--------------------------------
The host that built the shipped files has no ninja, so they were built in the
Linux tool image (../linux/Containerfile.x86_64 on the digest-pinned manylinux
base in ../linux/pins.env; it supplies cmake 4.4.3, ninja 1.13.2 and Python
3.13 - build it once with ../linux/build.sh x64). With it cached:

  podman run --rm --network none \
    -v "$PWD:/repo:Z" \
    -v "$HOME/Android/Sdk/ndk/30.0.16248370:/ndk:ro" \
    localhost/codebrix-libgit2-build-x86_64:latest \
    /opt/python/cp313-cp313/bin/python \
    /repo/tools/libgit2-native-tools/android/build.py --ndk /ndk

The compiler and sysroot are the NDK's, mounted read-only; the container only
provides cmake, ninja and Python, and --network none proves nothing is
fetched. Both ABIs take about 20 seconds together.

On a host that has the tools:
  python3 tools/libgit2-native-tools/android/build.py \
    --ndk "$HOME/Android/Sdk/ndk/30.0.16248370"

Use --arch arm64 or --arch x64 to build just one, --jobs to control parallel
compilation (default: all cores). CODEBRIX_ANDROID_NDK can supply the NDK path
instead of --ndk. Do not use Python -O: assertions implement the gates, and
optimized Python is explicitly rejected.

WHAT build.py DOES, PER ABI
---------------------------
build.py copies ../mbedtls and ../libgit2 to
../output/build-scratch/android-<arch> (a FIXED path, removed afterwards) and
then:

  1. Mbed TLS:
       -G Ninja -DCMAKE_TOOLCHAIN_FILE=<NDK>/build/cmake/android.toolchain.cmake
       -DANDROID_ABI=<abi> -DANDROID_PLATFORM=33 -DCMAKE_BUILD_TYPE=Release
       -DCMAKE_POSITION_INDEPENDENT_CODE=ON -DENABLE_TESTING=OFF
       -DENABLE_PROGRAMS=OFF -DUSE_STATIC_MBEDTLS_LIBRARY=ON
       -DUSE_SHARED_MBEDTLS_LIBRARY=OFF -DGEN_FILES=OFF
       -DCMAKE_INSTALL_PREFIX=<scratch>/mbedtls-static
     build + install (headers and the three .a files only).
  2. libgit2, the same toolchain / ABI / API / Release / PIC options and:
       -DBUILD_SHARED_LIBS=ON -DBUILD_TESTS=OFF -DBUILD_CLI=OFF
       -DLIBGIT2_FILENAME=git2-5853918 -DUSE_SSH=OFF -DUSE_HTTPS=mbedTLS
       -DUSE_BUNDLED_ZLIB=ON -DREGEX_BACKEND=builtin
       -DCERT_LOCATION=/system/etc/security/cacerts
       -DMBEDTLS_INCLUDE_DIR=... -DMBEDTLS_LIBRARY=... -DMBEDX509_LIBRARY=...
       -DMBEDCRYPTO_LIBRARY=...
     (BUILD_CLI=OFF as upstream's Windows recipe - the CLI is an executable
     nobody ships; REGEX_BACKEND=builtin as the Linux builds - stated, not left
     to detection.)
  C flags     : -g, -ffile-prefix-map for both scratch sources, both build
                folders, the Mbed TLS install and the NDK,
                -fdebug-compilation-dir=build; libgit2 also -fgnu-keywords
  link flags  : -Wl,-z,max-page-size=16384 -Wl,-z,common-page-size=16384
                -Wl,--build-id=sha1 -Wl,--exclude-libs,ALL

then strips with llvm-strip --strip-unneeded, cross-compiles
../linux/smoke-test.c for the ABI, and runs verify.py on the result. The API
floor is 33, the CodeBrix Android floor (Sdl3, dav1d, Audio.Android).

Each ../output/android-<arch>/ gets libgit2-5853918.so,
unstripped/libgit2-5853918.so (with DWARF), LICENSE-Libgit2.txt (libgit2's
COPYING), LICENSE-Xdiff.txt (../LICENSE-Xdiff.txt), LICENSE-MbedTls.txt (Mbed
TLS's LICENSE), git2_features.h (the generated feature header), smoke-test,
cmake-summary.txt, BUILD-INFO.json (hashes, build-id, tool versions, every
option, the Mbed TLS archives' hashes, gate results) and SHA256SUMS.txt.

WHAT THE GATES CHECK (verify.py, on the stripped file, its twin and the header)
------------------------------------------------------------------------------
  * ELF64 little-endian shared object, right machine (AArch64 / x86-64)
  * Android ELF note API == 33
  * every PT_LOAD aligned to >= 16 KB, offset/address congruent; no
    writable+executable segment
  * SONAME exactly libgit2-5853918.so
  * NEEDED only libraries in pins.json "allowed_needed" (libc, libm, libdl,
    liblog) - libz.so is NOT allowed
  * no GLIBC_ version requirement; no TEXTREL, RPATH or RUNPATH
  * the required git_* exports (pins.json)
  * NO mbedtls_ / psa_ symbol in the dynamic table, exported or imported
  * git_libgit2_features() == 11, read from the function's own machine code
    (it compiles to `return <constant>;` - llvm-objdump shows the constant), so
    the HTTPS bit is proven from the shipped bytes without running them
  * the unstripped twin: same build-id, and its symbol table DEFINES the Mbed
    TLS functions libgit2 calls (mbedtls_ssl_handshake,
    mbedtls_x509_crt_parse_path, mbedtls_ctr_drbg_seed, mbedtls_sha256_starts,
    mbedtls_des_crypt_ecb ... - pins.json "required_mbedtls_symbols"): Mbed
    TLS is really inside, not expected from elsewhere
  * git2_features.h defines GIT_HTTPS, GIT_MBEDTLS, GIT_SHA256_MBEDTLS,
    GIT_SHA1_COLLISIONDETECT, GIT_THREADS, GIT_USE_NSEC, GIT_NTLM,
    GIT_HTTPPARSER_BUILTIN, GIT_REGEX_BUILTIN and NOT GIT_SSH / GIT_OPENSSL

To inspect another build:
  python3 verify.py <libgit2-5853918.so> --arch arm64 \
    --toolchain "$NDK/toolchains/llvm/prebuilt/linux-x86_64/bin"

RUNNING IT (emulator or device)
-------------------------------
build.py does not run anything Android. The shipped files WERE run once, on
2026-10-17, on the API 35 x86_64 Google APIs emulator image
(sdk_gphone64_x86_64, AE3A.240806.043, booted headless with -no-window
-read-only -no-snapshot): android-x64 natively, android-arm64 through the
image's ARM translation (libndk_translation). Both passed the full smoke test,
including GIT_OPT_SET_SSL_CERT_LOCATIONS on /system/etc/security/cacerts -
i.e. the statically linked Mbed TLS parsed the device's CA store. To repeat:

  adb -s SERIAL shell mkdir -p /data/local/tmp/lg2
  adb -s SERIAL push output/android-x64/libgit2-5853918.so \
                     output/android-x64/smoke-test /data/local/tmp/lg2/
  adb -s SERIAL shell 'cd /data/local/tmp/lg2 && chmod 755 smoke-test && \
      ./smoke-test ./libgit2-5853918.so 1.8.6 11 /system/etc/security/cacerts'

No network operation was tried there (no clone or fetch over HTTPS); the real
test of that is an app on a device.

REPRODUCIBILITY
---------------
Both libraries were built twice from scratch on 2026-10-17 and came out
byte-identical, stripped and unstripped. Hashes, sizes and tool versions:
../BUILD-PROVENANCE.txt and the .provenance.txt beside each shipped file in
../../../native_libraries/.

ADOPTION
--------
After the gates pass, copy each output library with LICENSE-Libgit2.txt,
LICENSE-Xdiff.txt and LICENSE-MbedTls.txt to
../../../native_libraries/android-<arch>/, and each unstripped twin to
../unstripped/android-<arch>/. Update ../unstripped/SHA256SUMS, the
.provenance.txt files and ../BUILD-PROVENANCE.txt in the same change. Never
commit a library without its unstripped twin from the SAME build.
================================================================================
