================================================================================
tools/libgit2-native-tools/linux - building libgit2-5853918.so for linux-x64,
                                   linux-arm64 and linux-riscv64
================================================================================

>>> STATUS 2026-10-17: see ../BUILD-PROVENANCE.txt for which RIDs are built,
    gated and adopted, with their hashes. Written and first run on the x86_64
    LMDE 7 laptop with podman 5.4.2: linux-x64 natively, linux-arm64 and
    linux-riscv64 under qemu-user emulation - the route CodeBrix.Sdl3's
    sdl3-native-tools took. All three passed the gate on their first run. <<<

WHAT THIS IS
--------------------------------------------------------------------------------
Everything needed to build the three Linux libgit2 native libraries this
package ships, from the libgit2 snapshot vendored in ../libgit2/. Each build is
verified before it is allowed to be adopted.

The build reaches nothing outside this repository. It literally cannot: the
compile runs in a container started with `--network none`, and the first thing
container-build.sh prints is the proof that the container has no network
interface but loopback. The source it compiles and the smoke test it runs are
files in this repository.

NOTHING HERE INSTALLS ANYTHING ON YOUR MACHINE. Every script checks what it
needs, and if something is missing it names it, prints the command that
installs it, and stops. Installing is your decision.


================================================================================
PREREQUISITES  (the complete list - one package, two if emulating)
================================================================================
  1. A container engine - podman (preferred) or docker.

       sudo apt install podman

     Verify:       podman --version

  2. ONLY IF building a foreign architecture on this host (arm64 or riscv64 on
     an x86_64 machine): qemu user-mode emulation and its binfmt registrations.

       sudo apt install qemu-user-static binfmt-support

     Verify:       ls /proc/sys/fs/binfmt_misc | grep qemu
     Alternative that installs nothing permanently:
       sudo podman run --rm --privileged \
            docker.io/multiarch/qemu-user-static --reset -p yes

  3. Disk: about 2 GB per architecture for the base image plus its derived
     image, and about 150 MB of build tree per run (inside the container's
     /tmp, discarded when it exits).

  NOT REQUIRED ON THE HOST, AND DELIBERATELY SO: cmake, ninja, gcc, any
  cross-compiler, OpenSSL. They live inside the container images, at pinned
  versions, so the build does not vary with whatever the workstation happens
  to have installed this year.


================================================================================
THE IMAGES, AND WHY THEY ARE PINNED BY DIGEST
================================================================================
  RID             base image                           glibc floor  compiler
  --------------  -----------------------------------  -----------  ---------
  linux-x64       quay.io/pypa/manylinux_2_28_x86_64    2.28         gcc 14.2
  linux-arm64     quay.io/pypa/manylinux_2_28_aarch64   2.28         gcc 14.2
  linux-riscv64   quay.io/pypa/manylinux_2_39_riscv64   2.39         gcc 14.3

WHY A CONTAINER, EVEN ON A NATIVE MACHINE. glibc symbol versioning is
forward-only: a binary compiled against the glibc on a current desktop distro
refuses to load on anything older. Building on the workstation would quietly
restrict the package to the newest distributions, and the failure would only
appear on a user's machine. The manylinux images are old userlands with modern
compilers, which is exactly the tool for this. The glibc number in the image
name IS the compatibility ceiling the gate enforces; the floor actually
referenced is lower (2.25 for x64 / arm64, 2.38 for riscv64 - recorded in
../BUILD-PROVENANCE.txt).

riscv64 has no older manylinux than 2_39, so its ceiling is glibc 2.39 -
Debian 13 / Ubuntu 24.04 and newer. In practice every riscv64 distribution
anyone runs is newer than that.

WHY DIGESTS. pins.env records a dated tag AND a sha256 digest for each image,
and the Containerfiles resolve the digest. A tag can be moved; a digest cannot.
The three digests are the same ones CodeBrix.Sdl3, CodeBrix.VideoPlayback.Dav1d,
CodeBrix.Ollama and CodeBrix.Audio pin, so every CodeBrix Linux native comes
from one base userland.

THE IMAGES ARE THE "INSTALLED TOOLS" EXCEPTION TO THE RULE. Pulling a base image
and building the derived image are the only things the Linux route fetches from
outside the repository - and they are a compiler and two build tools, not
libgit2 source. For the day the images are gone, see THE BARE-HOST ROUTE at the
end of this file.


================================================================================
THE DERIVED IMAGE - THE "INSTALL THE TOOLS" STEP
================================================================================
Containerfile.<arch> adds to the base image, and nothing else:
  * cmake and ninja at the versions pinned in pins.env (pip wheels from the
    image's own CPython);
  * `file`, for the gate (binutils - readelf, nm, objdump, strings, strip -
    comes with the image's gcc toolset).

    Containerfile.x86_64    -> codebrix-libgit2-build-x86_64
    Containerfile.aarch64   -> codebrix-libgit2-build-aarch64
    Containerfile.riscv64   -> codebrix-libgit2-build-riscv64

NO -devel PACKAGES. Unlike SDL, libgit2 needs no system headers at all here:
every dependency is bundled (zlib, PCRE2, llhttp, xdiff, ntlmclient, SHA-1
collision detection all come from ../libgit2/deps and ../libgit2/src), and
USE_HTTPS=OpenSSL-Dynamic declares the few OpenSSL types and prototypes it uses
itself (src/libgit2/streams/openssl_dynamic.h, src/util/hash/openssl.h,
deps/ntlmclient/crypt_openssl.h) and dlopen()s libssl / libcrypto at RUN time.
openssl-devel would only let CMake's find_package(OpenSSL) succeed, and with
OpenSSL-Dynamic nothing uses the result - so it is not installed.

Each Containerfile ends by PROVING its toolchain: it prints the compiler,
linker, glibc, cmake and ninja versions, checks the gate tools are present, and
compiles and runs a C program that dlopen()s libc.

To force an image rebuild:      FORCE_IMAGE_REBUILD=1 ./build.sh


================================================================================
USAGE
================================================================================
    cd tools/libgit2-native-tools/linux
    ./build.sh                  # all three RIDs
    ./build.sh x64              # or arm64 / riscv64

  Environment variables:
    CONTAINER_ENGINE=docker ./build.sh          force an engine
    FORCE_IMAGE_REBUILD=1 ./build.sh            rebuild the derived image first

  Timings: recorded per RID in ../BUILD-PROVENANCE.txt (2026-10-17, 24-core
  x86_64 laptop). linux-x64 builds in about ten seconds once the image exists;
  arm64 and riscv64 under qemu-user emulation take a few minutes each, and
  their first image build (dnf + pip under emulation) longer.

  DO NOT RUN TWO COPIES OF build.sh AT ONCE. Both rewrite ../output/SHA256SUMS
  at the end.

  Output, git-ignored (see ../output/README.txt):
    ../output/<rid>/libgit2-5853918.so             stripped - the file the
                                                   package ships
    ../output/<rid>/unstripped/libgit2-5853918.so  the pre-strip twin (debug
                                                   info), for crash triage
    ../output/<rid>/LICENSE-Libgit2.txt            libgit2's COPYING, verbatim
    ../output/<rid>/LICENSE-Xdiff.txt              ../LICENSE-Xdiff.txt
    ../output/<rid>/BUILD-INFO.txt                 toolchain, pins, feature
                                                   summary, sizes, sha256,
                                                   build-id, glibc floor,
                                                   smoke test
    ../output/<rid>/cmake-summary.txt              libgit2's feature summary
    ../output/<rid>/cmake-configure.log            the whole configure log
    ../output/<rid>/exports.txt                    every exported name
    ../output/<rid>/smoke-test                     the gate program that ran
    ../output/<rid>/SHA256SUMS.txt                 that RID's files
    ../output/SHA256SUMS                           every library, one line each


================================================================================
THE BUILD, IN ONE PARAGRAPH
================================================================================
container-build.sh copies ../libgit2 to scratch (there is no patch step) and
configures libgit2's own CMakeLists.txt with LIBGIT2_CMAKE_OPTIONS from
pins.env - upstream LibGit2Sharp.NativeBinaries' build.libgit2.sh Linux flags,
verbatim:

    -DCMAKE_BUILD_TYPE=Release -DBUILD_TESTS=OFF -DUSE_SSH=exec
    -DLIBGIT2_FILENAME=git2-5853918 -DUSE_HTTPS=OpenSSL-Dynamic
    -DUSE_BUNDLED_ZLIB=ON

plus exactly these additions:

    -G Ninja                 the generator; no effect on the output
    -DREGEX_BACKEND=builtin  REQUIRED BY THE IMAGE: the manylinux images carry
                             pcre-devel (for their CPython builds), and libgit2
                             picks a system PCRE whenever find_package(PCRE)
                             succeeds - which would link libpcre.so.1 into the
                             library and swap the regex engine. "builtin" is
                             the bundled PCRE2, which is what upstream's image
                             (no pcre-devel) selects on its own. Same result as
                             upstream, stated instead of left to the image.
    CMAKE_C_FLAGS            -g (debug info for the unstripped twin; no effect
                             on code generation) and -ffile-prefix-map for the
                             scratch source, build and /work paths
    linker                   -Wl,--build-id=sha1

CMake's own Release flags (-O3 -DNDEBUG for gcc) are used unchanged. The
result, libgit2-5853918.so.1.8.6, is collected as ONE regular file named
libgit2-5853918.so - the name upstream ships - with its SONAME
libgit2-5853918.so.1.8 kept as built, and stripped with --strip-unneeded.
(libgit2 also builds its `git2` CLI by default on Linux, as upstream's build
does; it is not shipped.)

WHAT LIBGIT2 PICKS WITH THOSE FLAGS (its own CMake summary, every RID, checked
by gate item 8):

    threadsafe    threadsafe support (pthreads)
    nanoseconds   nanosecond precision file mtimes and ctimes
    HTTPS         using OpenSSL-Dynamic
    SHA1          using CollisionDetection (bundled sha1dc)
    SHA256        using OpenSSL-Dynamic
    http-parser   using bundled parser (llhttp)
    regex         using bundled PCRE (PCRE2)
    xdiff         xdiff support (bundled)
    SSH           using OpenSSH exec support (runs the system `ssh`)
    zlib          using bundled zlib
    ntlmclient    NTLM authentication support for Unix
    futimens      futimens support
  disabled: experimental SHA256 API, GSSAPI, iconv, the debug allocators


================================================================================
THE VERIFICATION GATE
================================================================================
A build that fails ANY of these exits non-zero and must not be adopted.

  1. ELF class and machine - ELF64 and x86-64 / AArch64 / RISC-V to match the
     RID (readelf -h). Catches the wrong file under the wrong RID.
  2. SONAME - exactly libgit2-5853918.so.1.8.
  3. No RPATH or RUNPATH (the library lands in somebody else's application
     folder), and no TEXTREL.
  4. Required exports - the REQUIRED_SYMBOLS in pins.env (git_libgit2_init,
     git_repository_open, git_clone, git_remote_connect, git_remote_push,
     git_commit_create ...); the total export count is recorded (944, the same
     as every upstream 2.0.324 binary).
  5. Dependencies - NEEDED must be a subset of ALLOWED_DEPS in pins.env
     (libc / libm / libpthread / libdl / librt and the dynamic loader: glibc
     only). libssl, libcrypto, libz, libpcre, libssh2 and friends are named
     explicitly (FORBIDDEN_DEP_PATTERN) and fail the gate if they ever appear:
     OpenSSL must be dlopen()ed, zlib / PCRE2 / llhttp bundled. `ldd -r` must
     report no undefined symbols.
  6. glibc floor - the highest GLIBC_x.y symbol version referenced, CHECKED
     against pins.env: <= 2.28 for x64 and arm64, <= 2.39 for riscv64.
  7. Build-id - the stripped file and its unstripped twin carry the same GNU
     build-id (the link between a crash dump and the twin's symbols).
  8. Feature summary - every line in REQUIRED_FEATURES (pins.env) must be in
     libgit2's own CMake summary. Catches a backend that changed under us.
  9. OpenSSL-Dynamic compiled in - the libssl sonames it probes at run time
     (libssl.so.1.1, .so.3, .so.10, .so.1.0.0) are strings in the binary.
 10. Smoke test - linux/smoke-test.c, compiled in the container, dlopen()s the
     STAGED (stripped) library the way .NET does, resolves 13 entry points,
     checks git_libgit2_version() == 1.8.6 and git_libgit2_features() == 15
     (THREADS | HTTPS | SSH | NSEC), git_libgit2_init(), hashes a blob with
     git_odb_hash() (the bundled SHA-1), creates a repository in a temporary
     folder, writes the blob into it and opens it again, then calls
     GIT_OPT_SET_SSL_CERT_LOCATIONS with the image's CA bundle - which is the
     moment OpenSSL-Dynamic dlopen()s the image's libssl (1.1 on AlmaLinux 8,
     3 on Rocky 10) - and git_libgit2_shutdown(). Under qemu this really
     executes the foreign-architecture code.


================================================================================
TROUBLESHOOTING
================================================================================
"neither podman nor docker found"
    Install one (see PREREQUISITES). The script will not install it for you.

"exec format error" / every command in the container dies immediately
    You are building a foreign architecture and the binfmt handler for it is not
    registered. See PREREQUISITES item 2 - or build on the native machine.

Image pull fails / the tag no longer exists
    quay.io/pypa retires old dated tags. Pick a current tag from
    https://quay.io/organization/pypa, put it in pins.env WITH its digest, and
    say in the commit message which glibc floor that changes. If quay.io itself
    is gone, see THE BARE-HOST ROUTE.

The gate fails "FORBIDDEN dependencies linked: libpcre.so.1"
    REGEX_BACKEND=builtin was dropped from pins.env, or a new libgit2 changed
    how it picks a regex engine. Put it back. The same goes for libz (check
    USE_BUNDLED_ZLIB=ON) and libssl / libcrypto (check
    USE_HTTPS=OpenSSL-Dynamic, never plain OpenSSL).

The gate fails "CMake feature summary lacks: ..."
    libgit2 picked a different backend than pins.env expects. Read
    output/<rid>/cmake-summary.txt, decide whether the change is wanted, and
    if it is, change REQUIRED_FEATURES in the same commit and say why.

"readelf: Warning: Gap in build notes detected"
    Harmless: the Red Hat toolchain's annobin plugin leaves build-attribute
    notes, and readelf warns (and exits 1) about them. container-build.sh
    tolerates it (CodeBrix.Sdl3's first-run gate fix of 2026-10-01).

The smoke test fails at SET_SSL_CERT_LOCATIONS
    libgit2 could not dlopen() a libssl from the image - its list is
    libssl.so.1.1, .1.0.0, .10, .3 (src/libgit2/streams/openssl_dynamic.c).
    The CA-bundle and libssl lines above the smoke-test output say what the
    image has.

"pins.env was not found" after a clone
    The repository-root .gitignore has a blanket '*.env' rule, and it also
    ignores directory names that occur inside the vendored trees. Both are
    handled by the "!*" re-include at the top of ../.gitignore - do not delete
    that line. Check with:
        git check-ignore -v tools/libgit2-native-tools/linux/pins.env

Podman "permission denied" writing ../output/
    Rootless podman maps your user into the container and the :Z mount flag
    handles SELinux relabelling. On a system with an unusual security policy,
    try --userns=keep-id.


================================================================================
THE BARE-HOST ROUTE (no container - for the day the images are gone)
================================================================================
The rule this folder exists for is that the libraries can be rebuilt from this
repository alone, years from now. The container images are the one input that
comes from outside, so here is how to build WITHOUT them. The cost is the glibc
floor: a bare-host build's floor is the glibc of the machine that built it,
which must then be recorded in ../BUILD-PROVENANCE.txt as the compatibility
floor of that binary.

On a Debian-based machine of the target architecture, install the compiler and
the two build tools - libgit2 needs no -dev packages here:

    sudo apt install build-essential cmake ninja-build file

then run container-build.sh itself outside a container (it reads every pin
from pins.env; only these four come from the environment):

    cd tools/libgit2-native-tools
    sudo ln -sfn "$PWD" /work        # container-build.sh reads /work
    TARGET_RID=linux-x64 GLIBC_MAX=<this machine's glibc, e.g. 2.41> \
    DERIVED_IMAGE=bare-host \
    BASE_IMAGE_REF="$(. /etc/os-release; echo $PRETTY_NAME)" \
    bash linux/container-build.sh

It will warn that the host has network interfaces - the build still fetches
nothing - and the glibc-floor check passes or fails against the GLIBC_MAX you
give it. The smoke test's TLS step uses /etc/ssl/certs/ca-certificates.crt
and whichever libssl the machine has.


================================================================================
ADOPTING A BUILT BINARY INTO THE PACKAGE
================================================================================
  1. Read ../output/<rid>/BUILD-INFO.txt and satisfy yourself the build is the
     one you think it is: libgit2 commit, image digest, glibc floor, feature
     summary, the smoke-test lines.

  2. Copy the library and its licence files into the package's native tree:

       cp ../output/<rid>/libgit2-5853918.so  ../../../native_libraries/<rid>/
       cp ../output/<rid>/LICENSE-Libgit2.txt ../../../native_libraries/<rid>/
       cp ../output/<rid>/LICENSE-Xdiff.txt   ../../../native_libraries/<rid>/

     <rid> is linux-x64, linux-arm64 or linux-riscv64. The file must keep the
     name libgit2-5853918.so - the managed wrapper loads exactly that name.
     The licence copies are not optional: libgit2's licence and the bundled
     dependencies' notices travel with the binary, and the package packs them
     into runtimes/<rid>/native/ beside it.

  3. Update ../../../native_libraries/<rid>/libgit2-5853918.so.provenance.txt
     from BUILD-INFO.txt (it is not packed; it is the in-repo record beside the
     binary).

  4. Store the unstripped twin in the SAME change:

       mkdir -p ../unstripped/<rid>
       cp ../output/<rid>/unstripped/libgit2-5853918.so ../unstripped/<rid>/
       ( cd ../unstripped && sha256sum <rid>/libgit2-5853918.so ) # replace
                                                 # that RID's line in SHA256SUMS

     See ../unstripped/README.txt for the rule and the build-id check.

  5. Record the build in ../BUILD-PROVENANCE.txt - copy the values straight out
     of BUILD-INFO.txt.

  6. Run the managed test suite before publishing.


================================================================================
FILES
================================================================================
  README.txt              this document
  pins.env                every version / digest / option pin (edit here
                          only); the Windows and macOS scripts read its
                          platform-neutral keys too
  build.sh                host entry point: images, emulation check,
                          orchestration
  container-build.sh      the build and the gate; runs inside the container
  Containerfile.x86_64    derived build image for linux-x64 (also the tool
                          image of the Android route)
  Containerfile.aarch64   derived build image for linux-arm64
  Containerfile.riscv64   derived build image for linux-riscv64
  smoke-test.c            the dlopen verification program, shared by every
                          platform's gate
  ../output/              build results (git-ignored)
================================================================================
