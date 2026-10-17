#!/usr/bin/env python3
"""Build libgit2-5853918.so for android-arm64 (arm64-v8a) and android-x64 (x86_64) from the vendored
libgit2 source, with HTTPS through mbedTLS built first, as static libraries, from the vendored mbedtls/.

Never installs or downloads anything. Requires the pinned NDK (pins.json), cmake and ninja on PATH, and
Python 3.9+. See README.txt for the tools and for the offline container route used for the shipped files.

    python3 build.py --ndk ~/Android/Sdk/ndk/30.0.16248370 [--arch all|arm64|x64] [--jobs N]
"""
import argparse
from datetime import datetime, timezone
import json
import os
from pathlib import Path
import platform
import re
import shutil
import subprocess
import time

from verify import ARCHITECTURES, PINS, TOOLS, output, sha256, verify

LIBRARY = PINS["library"]
LICENSES = ("LICENSE-Libgit2.txt", "LICENSE-Xdiff.txt", "LICENSE-MbedTls.txt")


def run(*args, **kwargs):
    print("+", " ".join(str(arg) for arg in args), flush=True)
    subprocess.run([str(arg) for arg in args], check=True, **kwargs)


def configure(source, build_dir, options):
    """Run a cmake configure, echo it, and return its output (the feature summary is in it)."""
    print("+ cmake -S", source, "-B", build_dir, *options, flush=True)
    result = subprocess.run(["cmake", "-S", str(source), "-B", str(build_dir), *options],
                            text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    print(result.stdout, flush=True)
    if result.returncode:
        raise SystemExit(f"cmake configure failed in {source}")
    return result.stdout


def version_tuple(text):
    return tuple(int(part) for part in re.search(r"(\d+(?:\.\d+)+)", text)[1].split("."))


def in_container():
    return Path("/run/.containerenv").exists() or Path("/.dockerenv").exists()


def check_tools(ndk):
    revision = re.search(r"Pkg.Revision\s*=\s*(\S+)", (ndk / "source.properties").read_text())[1]
    if revision != PINS["ndk"]:
        raise SystemExit(f"NDK {PINS['ndk']} is pinned; found {revision} at {ndk}")
    for tool in ("cmake", "ninja"):
        if shutil.which(tool) is None:
            raise SystemExit(f"{tool} is not on PATH - install it (README.txt, TOOLS); this script installs nothing")
    cmake = output("cmake", "--version").splitlines()[0].strip()
    ninja = output("ninja", "--version").strip()
    assert version_tuple(cmake) >= version_tuple(PINS["cmake_minimum"]), f"cmake too old: {cmake}"
    assert version_tuple(ninja) >= version_tuple(PINS["ninja_minimum"]), f"ninja too old: {ninja}"
    header = (TOOLS / "libgit2/include/git2/version.h").read_text()
    assert f'#define LIBGIT2_VERSION        "{PINS["libgit2_version"]}"' in header, "libgit2/ is not the pinned version"
    build_info = (TOOLS / "mbedtls/include/mbedtls/build_info.h").read_text()
    assert f'#define MBEDTLS_VERSION_STRING         "{PINS["mbedtls_version"]}"' in build_info, \
        "mbedtls/ is not the pinned version"
    # libgit2's cmake/SelectHTTPSBackend.cmake insists that CERT_LOCATION exists ON THE BUILD MACHINE
    # (it was written for native builds). The value is compiled in as GIT_DEFAULT_CERT_LOCATION and is
    # only ever opened on the DEVICE. Inside the throwaway build container an empty folder of that name
    # is created; on a bare host the user is told to create it. Nothing is read from it.
    cert = Path(PINS["cert_location"])
    if not cert.is_dir():
        if in_container():
            cert.mkdir(parents=True)
            print(f"[container] created the empty folder {cert} so libgit2's CMake accepts CERT_LOCATION", flush=True)
        else:
            raise SystemExit(f"libgit2's CMake requires CERT_LOCATION={cert} to exist on the build machine. "
                             f"Create it (empty is fine), e.g.  sudo mkdir -p {cert}  - or use the container route "
                             f"in README.txt, which needs no change to this machine.")
    return revision, cmake, ninja


def build(arch, ndk, jobs, tools):
    revision, cmake, ninja = tools
    host = {"Linux": "linux-x86_64", "Darwin": "darwin-x86_64"}.get(platform.system())
    if host is None:
        raise SystemExit("Run on Linux or macOS (on Windows, use a Linux build machine or WSL).")
    toolchain = ndk / "toolchains/llvm/prebuilt" / host / "bin"
    cpu, _, abi = ARCHITECTURES[arch]
    api = PINS["api"]
    compiler = toolchain / f"{cpu}-linux-android{api}-clang"
    dest = TOOLS / "output" / ("android-" + arch)
    if dest.exists():
        shutil.rmtree(dest)
    (dest / "unstripped").mkdir(parents=True)
    started = time.monotonic()
    # A FIXED scratch path (not a random temp name), so the recorded flags are the same on every
    # run; the prefix maps below keep it out of the binaries either way.
    scratch = TOOLS / "output" / "build-scratch" / ("android-" + arch)
    if scratch.exists():
        shutil.rmtree(scratch)
    scratch.mkdir(parents=True)
    try:
        # Keep the vendored upstream snapshots pristine: both are built from scratch copies.
        mbed_src, mbed_build, mbed_prefix = scratch / "mbedtls", scratch / "build-mbedtls", scratch / "mbedtls-static"
        source, build_dir = scratch / "libgit2", scratch / "build"
        shutil.copytree(TOOLS / "mbedtls", mbed_src, symlinks=True)
        shutil.copytree(TOOLS / "libgit2", source, symlinks=True)
        # Fixed names for every absolute path the compiler sees, so neither the scratch folder nor
        # the NDK's install location lands in the debug information of the unstripped twin.
        prefix_maps = [f"-ffile-prefix-map={mbed_src}=mbedtls", f"-ffile-prefix-map={mbed_build}=build-mbedtls",
                       f"-ffile-prefix-map={mbed_prefix}=mbedtls-static",
                       f"-ffile-prefix-map={source}=libgit2", f"-ffile-prefix-map={build_dir}=build",
                       f"-ffile-prefix-map={ndk}=ndk", "-fdebug-compilation-dir=build"]
        compile_flags = " ".join(["-g", *prefix_maps])
        common = ["-G", "Ninja",
                  f"-DCMAKE_TOOLCHAIN_FILE={ndk}/build/cmake/android.toolchain.cmake",
                  f"-DANDROID_ABI={abi}", f"-DANDROID_PLATFORM={api}",
                  "-DCMAKE_BUILD_TYPE=Release", "-DCMAKE_POSITION_INDEPENDENT_CODE=ON"]

        # --- 1. mbedTLS, static, for this ABI -------------------------------------------------
        mbed_options = common + ["-DENABLE_TESTING=OFF", "-DENABLE_PROGRAMS=OFF",
                                 "-DUSE_STATIC_MBEDTLS_LIBRARY=ON", "-DUSE_SHARED_MBEDTLS_LIBRARY=OFF",
                                 "-DGEN_FILES=OFF", f"-DCMAKE_INSTALL_PREFIX={mbed_prefix}",
                                 f"-DCMAKE_C_FLAGS={compile_flags}"]
        mbed_log = configure(mbed_src, mbed_build, mbed_options)
        run("cmake", "--build", mbed_build, "--parallel", jobs)
        run("cmake", "--install", mbed_build)
        mbed_libs = {name: mbed_prefix / "lib" / f"lib{name}.a" for name in ("mbedtls", "mbedx509", "mbedcrypto")}
        for name, path in mbed_libs.items():
            assert path.is_file(), f"mbedTLS did not produce {path}"
        assert not list((mbed_prefix / "lib").glob("*.so*")), "mbedTLS must be static only"

        # --- 2. libgit2, shared, linking that mbedTLS in -----------------------------------------
        link_flags = ("-Wl,-z,max-page-size=16384 -Wl,-z,common-page-size=16384 -Wl,--build-id=sha1 "
                      "-Wl,--exclude-libs,ALL")
        options = common + ["-DBUILD_SHARED_LIBS=ON", "-DBUILD_TESTS=OFF", "-DBUILD_CLI=OFF",
                            f"-DLIBGIT2_FILENAME={PINS['libgit2_filename']}",
                            "-DUSE_SSH=OFF", "-DUSE_HTTPS=mbedTLS", "-DUSE_BUNDLED_ZLIB=ON",
                            "-DREGEX_BACKEND=builtin",
                            f"-DCERT_LOCATION={PINS['cert_location']}",
                            f"-DMBEDTLS_INCLUDE_DIR={mbed_prefix / 'include'}",
                            f"-DMBEDTLS_LIBRARY={mbed_libs['mbedtls']}",
                            f"-DMBEDX509_LIBRARY={mbed_libs['mbedx509']}",
                            f"-DMBEDCRYPTO_LIBRARY={mbed_libs['mbedcrypto']}"]
        # -fgnu-keywords: libgit2 1.8.6 compiles its own targets as strict ISO C90 (C_STANDARD 90,
        # C_EXTENSIONS OFF, hard-coded target properties), and the NDK's kernel UAPI headers
        # (linux/swab.h, reached from <arpa/inet.h>) use `static inline`, which is not a C90
        # keyword - every file fails with "unknown type name 'inline'" (r29 and r30 alike). clang's
        # -fgnu-keywords makes `inline` (a GNU keyword) available again without changing the
        # language standard or any source; libgit2's own src/libgit2/streams/mbedtls.c works round
        # the same C90 limit with `#define inline __inline__`. First-run fix, 2026-10-17.
        libgit2_c_flags = compile_flags + " -fgnu-keywords"
        flag_options = [f"-DCMAKE_C_FLAGS={libgit2_c_flags}", f"-DCMAKE_SHARED_LINKER_FLAGS={link_flags}"]
        log = configure(source, build_dir, options + flag_options)
        summary = log[log.find("-- Enabled features:"):log.find("-- Configuring done")]
        summary = "\n".join(line[3:] if line.startswith("-- ") else line for line in summary.splitlines()).strip()
        (dest / "cmake-summary.txt").write_text(summary + "\n")
        run("cmake", "--build", build_dir, "--parallel", jobs)
        # UNLIKE Linux there is no libgit2-5853918.so.1.8.6 + symlinks: CMake's Android platform
        # module does not version shared libraries (CMAKE_PLATFORM_NO_VERSIONED_SONAME), so the
        # build writes one regular file, libgit2-5853918.so, whose SONAME is that same plain name -
        # the Android convention. (libgit2's SONAME option must stay ON, its default, all the same:
        # libgit2 only applies LIBGIT2_FILENAME inside that option - with it OFF the file is called
        # liblibgit2package.so. Found on the first run, 2026-10-17.)
        library = build_dir / LIBRARY
        assert library.is_file() and not library.is_symlink(), f"Expected a regular file {library}"
        shutil.copy2(library, dest / "unstripped" / LIBRARY)
        shutil.copy2(library, dest / LIBRARY)
        run(toolchain / "llvm-strip", "--strip-unneeded", dest / LIBRARY)
        features_header = build_dir / "src/util/git2_features.h"
        shutil.copy2(features_header, dest / "git2_features.h")
        # The smoke test is cross-compiled for a device run (README.txt); build.py does not run it.
        run(compiler, "-std=c11", "-O1", "-Wl,-z,max-page-size=16384", TOOLS / "linux/smoke-test.c",
            "-ldl", "-o", dest / "smoke-test")
        shutil.copy2(source / "COPYING", dest / "LICENSE-Libgit2.txt")
        shutil.copy2(TOOLS / "LICENSE-Xdiff.txt", dest / "LICENSE-Xdiff.txt")
        shutil.copy2(mbed_src / "LICENSE", dest / "LICENSE-MbedTls.txt")
        report = verify(dest / LIBRARY, arch, toolchain, unstripped=dest / "unstripped" / LIBRARY,
                        features_header=features_header)
        report.update({"built_utc": datetime.now(timezone.utc).replace(microsecond=0).isoformat(),
                       "build_seconds": round(time.monotonic() - started),
                       "ndk": revision, "clang": output(compiler, "--version").splitlines()[0],
                       "cmake": cmake, "ninja": ninja,
                       "libgit2_commit": PINS["libgit2_commit"], "libgit2_version": PINS["libgit2_version"],
                       "mbedtls_tag": PINS["mbedtls_tag"], "mbedtls_commit": PINS["mbedtls_commit"],
                       "mbedtls_cmake_options": mbed_options,
                       "mbedtls_static_libraries": {name: {"bytes": path.stat().st_size, "sha256": sha256(path)}
                                                    for name, path in mbed_libs.items()},
                       "cmake_options": options + flag_options,
                       "cmake_feature_summary": summary.splitlines(),
                       "default_cert_location": PINS["cert_location"],
                       "strip": "llvm-strip --strip-unneeded", "patches": [],
                       "static_gates": "passed (verify.py: stripped file + unstripped twin + git2_features.h)",
                       "runtime_tests": "not run by build.py - see BUILD-PROVENANCE.txt for any device run"})
        (dest / "BUILD-INFO.json").write_text(json.dumps(report, indent=2) + "\n")
        names = (LIBRARY, f"unstripped/{LIBRARY}", *LICENSES, "smoke-test", "git2_features.h")
        (dest / "SHA256SUMS.txt").write_text("".join(f"{sha256(dest / name)}  {name}\n" for name in names))
        if mbed_log.count("CMake Warning"):
            print(f"note: mbedTLS configure printed {mbed_log.count('CMake Warning')} CMake warning(s)", flush=True)
        print(json.dumps(report, indent=2), flush=True)
    finally:
        shutil.rmtree(scratch, ignore_errors=True)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--arch", choices=["all", *ARCHITECTURES], default="all")
    parser.add_argument("--ndk", type=Path, default=os.environ.get("CODEBRIX_ANDROID_NDK"),
                        required=not os.environ.get("CODEBRIX_ANDROID_NDK"))
    parser.add_argument("--jobs", type=int, default=os.cpu_count() or 4)
    args = parser.parse_args()
    if args.jobs < 1:
        parser.error("--jobs must be positive")
    ndk = args.ndk.resolve()
    tools = check_tools(ndk)
    for arch in ARCHITECTURES if args.arch == "all" else [args.arch]:
        build(arch, ndk, args.jobs, tools)
