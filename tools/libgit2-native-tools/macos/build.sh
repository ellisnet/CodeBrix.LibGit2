#!/usr/bin/env bash
# ==============================================================================================
# build.sh - build libgit2-5853918.dylib for osx-arm64 and/or osx-x64, from this repository
# ==============================================================================================
#
#   NEVER YET RUN. Written on Linux on 2026-10-17, adapted from upstream
#   libgit2sharp.nativebinaries' build.libgit2.sh, Darwin branch (a verbatim copy is in
#   ../reference-upstream/build.libgit2.sh). Expect to fix something on the first real run; fix
#   it IN THE SCRIPT and record it in ../BUILD-PROVENANCE.txt, then rewrite this header and
#   README.txt's status block with what the run established.
#
#   ./build.sh arm64          build osx-arm64
#   ./build.sh x64            build osx-x64
#   ./build.sh all            both (the default)
#
# Options (environment variables):
#   DEPLOYMENT_TARGET=12.0    pass -DCMAKE_OSX_DEPLOYMENT_TARGET. UNSET BY DEFAULT, exactly as
#                             upstream: the floor is then whatever this Mac's Xcode defaults to
#                             (upstream's 2.0.324 dylibs say minos 14.0 - their macos-14 runner).
#                             See README.txt, THE MACOS FLOOR, before setting it.
#
# WHAT IT KEEPS FROM UPSTREAM, EXACTLY - the cmake arguments:
#     -DCMAKE_BUILD_TYPE=Release -DBUILD_TESTS=OFF -DUSE_SSH=exec
#     -DLIBGIT2_FILENAME=git2-5853918 -DCMAKE_OSX_ARCHITECTURES=arm64|x86_64
#     -DUSE_HTTPS=ON -DUSE_BUNDLED_ZLIB=ON
#   (USE_HTTPS=ON on a Mac auto-selects SecureTransport), CMake's default generator, then
#   `cmake --build .`. Upstream does not strip the dylib and neither does this.
#
# WHAT IT CHANGES, AND WHY
#   * the source is ../libgit2 (the vendored, unmodified snapshot) COPIED to a scratch folder
#     and built there - upstream built inside its libgit2 submodule;
#   * the pins come from ../linux/pins.env, the one place every platform reads them from;
#   * output goes to ../output/osx-<arch>/ with LICENSE-Libgit2.txt, LICENSE-Xdiff.txt,
#     BUILD-INFO.txt and SHA256SUMS.txt beside the dylib;
#   * a verification gate runs before anything is called a result (README.txt, THE GATE).
#
# Both slices build on either kind of Mac (CMAKE_OSX_ARCHITECTURES cross-compiles). The smoke
# test runs a slice only where the Mac can execute it: natively, or osx-x64 under Rosetta 2 on
# Apple Silicon. It installs nothing; anything missing is named and the script stops.
# ==============================================================================================

set -euo pipefail

trap 'rc=$?; echo "ERROR: build.sh failed (exit $rc) at line $LINENO: $BASH_COMMAND" >&2' ERR

first_line() { sed -n '1p'; }

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TOOLS_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
OUTPUT_DIR="$TOOLS_DIR/output"
SCRATCH_ROOT="${TMPDIR:-/tmp}/codebrix-libgit2-build-macos"

# shellcheck source=../linux/pins.env
. "$TOOLS_DIR/linux/pins.env"
DYLIB="lib$LIBGIT2_FILENAME.dylib"

# Load commands the dylib may link: the system only. Anything else would have to be installed
# on the user's Mac.
ALLOWED_DYLIBS="/System/Library/Frameworks/CoreFoundation.framework/Versions/A/CoreFoundation
/System/Library/Frameworks/Security.framework/Versions/A/Security
/usr/lib/libiconv.2.dylib
/usr/lib/libSystem.B.dylib
/usr/lib/libz.1.dylib"
EXPECTED_FEATURES=15     # THREADS | HTTPS | SSH | NSEC - what upstream's 2.0.324 dylibs return

# ----------------------------------------------------------------------------------------------
# Prerequisites - checked, never installed
# ----------------------------------------------------------------------------------------------
[ "$(uname -s)" = "Darwin" ] || { echo "ERROR: this script runs on macOS (it needs Xcode's SDK)." >&2; exit 1; }
for tool in cmake xcrun otool nm lipo codesign shasum; do
    command -v "$tool" > /dev/null 2>&1 || {
        cat >&2 <<EOF
ERROR: $tool is not on PATH. This script installs nothing. Install:
    xcode-select --install          # compiler, SDK, otool, nm, lipo, codesign
    brew install cmake              # or the CMake.app from cmake.org, with its CLI on PATH
EOF
        exit 1; }
done
[ -f "$TOOLS_DIR/$LIBGIT2_DIR/include/git2/version.h" ] || {
    echo "ERROR: the vendored libgit2 source is missing from $TOOLS_DIR/$LIBGIT2_DIR - restore it from git." >&2; exit 1; }
HEADER_VERSION="$(sed -n 's/^#define LIBGIT2_VERSION *"\(.*\)"$/\1/p' "$TOOLS_DIR/$LIBGIT2_DIR/include/git2/version.h")"
[ "$HEADER_VERSION" = "$LIBGIT2_VERSION" ] || { echo "ERROR: version.h says $HEADER_VERSION, pins.env says $LIBGIT2_VERSION" >&2; exit 1; }

build_slice() {
    local arch="$1" rid osxarch scratch build out started
    case "$arch" in
        arm64) rid=osx-arm64; osxarch=arm64 ;;
        x64)   rid=osx-x64;   osxarch=x86_64 ;;
    esac
    scratch="$SCRATCH_ROOT/$rid/libgit2"
    build="$SCRATCH_ROOT/$rid/build"
    out="$OUTPUT_DIR/$rid"
    started="$SECONDS"

    echo
    echo "=============================================================================="
    echo " BUILD $rid  (libgit2 $LIBGIT2_VERSION, commit $LIBGIT2_COMMIT)"
    echo "=============================================================================="
    rm -rf "$SCRATCH_ROOT/$rid"
    mkdir -p "$SCRATCH_ROOT/$rid"
    cp -a "$TOOLS_DIR/$LIBGIT2_DIR" "$scratch"

    local -a opts=(-DCMAKE_BUILD_TYPE=Release -DBUILD_TESTS=OFF -DUSE_SSH=exec
                   "-DLIBGIT2_FILENAME=$LIBGIT2_FILENAME" "-DCMAKE_OSX_ARCHITECTURES=$osxarch"
                   -DUSE_HTTPS=ON -DUSE_BUNDLED_ZLIB=ON)
    [ -n "${DEPLOYMENT_TARGET:-}" ] && opts+=("-DCMAKE_OSX_DEPLOYMENT_TARGET=$DEPLOYMENT_TARGET")
    echo "  cmake ${opts[*]}"
    cmake -S "$scratch" -B "$build" "${opts[@]}" > "$SCRATCH_ROOT/$rid/configure.log" 2>&1 \
        || { tail -60 "$SCRATCH_ROOT/$rid/configure.log" >&2; echo "ERROR: cmake configure failed" >&2; exit 1; }
    local summary
    summary="$(sed -n '/^-- Enabled features:/,/^-- Configuring done/p' "$SCRATCH_ROOT/$rid/configure.log" | sed '$d' | sed 's/^-- //')"
    printf '%s\n' "$summary" | sed 's/^/    /'
    cmake --build "$build" -j "$(sysctl -n hw.ncpu)" > "$SCRATCH_ROOT/$rid/build.log" 2>&1 \
        || { grep -E -B2 -A8 'error:' "$SCRATCH_ROOT/$rid/build.log" | head -80 >&2; echo "ERROR: build failed" >&2; exit 1; }

    # CMake writes libgit2-5853918.1.8.6.dylib plus symlinks; upstream copies
    # libgit2-5853918.dylib (cp follows the link), and so does this.
    rm -rf "$out"; mkdir -p "$out"
    cp "$build/$DYLIB" "$out/$DYLIB"
    cp "$TOOLS_DIR/$LIBGIT2_DIR/COPYING" "$out/$LICENSE_FILE_NAME"
    cp "$TOOLS_DIR/LICENSE-Xdiff.txt" "$out/LICENSE-Xdiff.txt"
    local lib="$out/$DYLIB"

    # ---- THE GATE --------------------------------------------------------------------------
    echo "--- verifying ---"
    local failed=0
    fail() { echo "  [FAIL] $1"; failed=1; }
    pass() { echo "  [ok] $1"; }

    local archs; archs="$(lipo -archs "$lib")"
    [ "$archs" = "$osxarch" ] && pass "thin $osxarch Mach-O" || fail "lipo -archs says '$archs', expected '$osxarch'"

    local idname; idname="$(otool -D "$lib" | sed -n 2p)"
    pass "install name: $idname (as libgit2 builds it; recorded)"

    local deps unexpected=""
    deps="$(otool -L "$lib" | sed -n '3,$p' | awk '{print $1}')"
    while IFS= read -r d; do
        [ -n "$d" ] || continue
        printf '%s\n' "$ALLOWED_DYLIBS" | grep -F -x -- "$d" > /dev/null || unexpected="$unexpected $d"
    done <<< "$deps"
    [ -z "$unexpected" ] && pass "links system libraries only: $(printf '%s ' $deps)" || fail "unexpected dylibs:$unexpected"
    if otool -l "$lib" | grep -q LC_RPATH; then fail "LC_RPATH present"; else pass "no LC_RPATH"; fi

    local exports missing=""
    exports="$(nm -gU "$lib" | awk '{print $3}' | sed 's/^_//' | sort -u)"
    for sym in $REQUIRED_SYMBOLS; do
        printf '%s\n' "$exports" | grep -x "$sym" > /dev/null || missing="$missing $sym"
    done
    [ -z "$missing" ] && pass "$(printf '%s\n' "$exports" | grep -c '^git_') git_* exports, all required ones present" || fail "missing exports:$missing"

    local minos; minos="$(otool -l "$lib" | awk '/LC_BUILD_VERSION/{f=1} f&&/minos/{print $2; exit}')"
    pass "minimum macOS (LC_BUILD_VERSION minos): ${minos:-unknown}"

    local want
    for want in "HTTPS, using SecureTransport" "SSH, using OpenSSH exec support" "SHA1, using CollisionDetection" "zlib, using bundled zlib"; do
        printf '%s\n' "$summary" | grep -F -x -- " * $want" > /dev/null && pass "feature: $want" || fail "feature summary lacks: $want"
    done

    cc -std=c11 -O1 -arch "$osxarch" -o "$out/smoke-test" "$TOOLS_DIR/linux/smoke-test.c"
    local smoke="(not run - this Mac cannot execute $osxarch)"
    if arch "-$osxarch" /usr/bin/true 2> /dev/null; then
        smoke="$(arch "-$osxarch" "$out/smoke-test" "$lib" "$LIBGIT2_VERSION" "$EXPECTED_FEATURES" 2>&1)" \
            && pass "smoke test PASSED" || fail "smoke test failed"
        printf '%s\n' "$smoke" | sed 's/^/    /'
    else
        echo "  [skip] smoke test compiled; this Mac cannot run $osxarch"
    fi

    [ "$failed" -eq 0 ] || { echo "VERIFICATION FAILED for $rid - output/$rid must not be adopted." >&2; exit 1; }

    # ---- RECORD ----------------------------------------------------------------------------
    local sha size
    sha="$(shasum -a 256 "$lib" | cut -d' ' -f1)"
    size="$(stat -f %z "$lib")"
    cat > "$out/BUILD-INFO.txt" <<EOF
libgit2 native library - build information
==============================================================================
RID              : $rid
Built            : $(date -u '+%Y-%m-%d %H:%M:%S UTC')
Build duration   : $(( SECONDS - started ))s
Built by         : tools/libgit2-native-tools/macos/build.sh
Build host       : $(sw_vers -productName) $(sw_vers -productVersion), $(uname -m)
Xcode            : $(xcodebuild -version 2>/dev/null | tr '\n' ' ')
SDK              : $(xcrun --show-sdk-version)
Compiler         : $(cc --version | first_line)
cmake            : $(cmake --version | first_line)

libgit2          : $LIBGIT2_VERSION, commit $LIBGIT2_COMMIT
Source           : tools/libgit2-native-tools/libgit2/ (vendored, unmodified; copied to scratch)
cmake options    : ${opts[*]}

Feature summary (libgit2's own CMake summary)
------------------------------------------------------------------------------
$summary

Result
------------------------------------------------------------------------------
File             : $DYLIB  (not stripped, as upstream)
Size             : $size bytes
SHA256           : $sha
Architecture     : $archs
Install name     : $idname
Minimum macOS    : ${minos:-unknown}
Links            : $(printf '%s ' $deps)
Exports          : $(printf '%s\n' "$exports" | grep -c .)
Gate             : PASSED

Smoke test
------------------------------------------------------------------------------
$smoke
EOF
    ( cd "$out" && shasum -a 256 "$DYLIB" "$LICENSE_FILE_NAME" LICENSE-Xdiff.txt smoke-test > SHA256SUMS.txt )
    echo "  $lib"
    echo "  sha256 $sha"
}

case "${1:-all}" in
    arm64|x64) build_slice "$1" ;;
    all)       build_slice arm64; build_slice x64 ;;
    *) echo "usage: $0 [all|arm64|x64]" >&2; exit 2 ;;
esac
echo "Done. Adopt with ADOPTING A BUILT BINARY in README.txt."
