#!/usr/bin/env python3
"""Static gates for an Android libgit2-5853918.so (no device needed; nothing is loaded on the build host).

    python3 verify.py <libgit2-5853918.so> --arch arm64|x64 \
        --toolchain <NDK>/toolchains/llvm/prebuilt/<host>/bin [--unstripped <twin>] [--features-header <git2_features.h>]

Checks: ELF64 little-endian shared object for the right machine; Android ABI note == the pinned API
(33); every PT_LOAD aligned to >= 16 KB with offset/address congruent; no writable+executable segment;
SONAME exactly libgit2-5853918.so; NEEDED only Android system libraries (pins.json "allowed_needed" -
libz.so in particular is NOT allowed, zlib is bundled); no GLIBC_ version requirement; no TEXTREL /
RPATH / RUNPATH; the required git_* exports; NO mbedtls_ / psa_ symbol exported or imported (mbedTLS is
linked in statically and kept private with --exclude-libs); the git_libgit2_features() bit mask read
from the machine code of that function == pins.json "expected_features" (THREADS | HTTPS | NSEC = 11:
the HTTPS bit proves the TLS backend was compiled in); a GNU build-id is present.

With --unstripped (build.py always passes it): the twin has the same build-id, and its symbol table
DEFINES every pins.json "required_mbedtls_symbols" entry - the proof that mbedTLS itself, not just
libgit2's calls into it, is inside the library.
With --features-header (build.py always passes it): the git2_features.h libgit2's CMake generated has
every "required_features_defines" and none of the "forbidden_features_defines".
"""
import argparse
import hashlib
import json
from pathlib import Path
import re
import struct
import subprocess

if not __debug__:
    raise SystemExit("Run the Android build/verification tools without Python -O; their gates must stay enabled.")

TOOLS = Path(__file__).resolve().parents[1]
PINS = json.loads((TOOLS / "android/pins.json").read_text())
# arch -> (NDK triple cpu, ELF e_machine, Android ABI name)
ARCHITECTURES = {"arm64": ("aarch64", 183, "arm64-v8a"), "x64": ("x86_64", 62, "x86_64")}
FEATURE_NAMES = {1: "THREADS", 2: "HTTPS", 4: "SSH", 8: "NSEC"}


def output(*args):
    return subprocess.check_output([str(arg) for arg in args], text=True)


def sha256(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def feature_names(mask):
    return " ".join(name for bit, name in FEATURE_NAMES.items() if mask & bit) or "none"


def features_from_code(library, arch, toolchain):
    """git_libgit2_features() is `return <constant>;` - read the constant out of its machine code."""
    text = output(toolchain / "llvm-objdump", "-d", "--no-show-raw-insn",
                  "--disassemble-symbols=git_libgit2_features", library)
    body = text[text.find("<git_libgit2_features>:"):]
    if arch == "x64":
        found = re.findall(r"\bmov[l]?\s+\$(0x[0-9a-f]+),\s*%eax", body)
    else:
        found = re.findall(r"\bmov\s+w0,\s*#(0x[0-9a-f]+|\d+)\b", body)
    assert len(found) == 1, f"Could not read the constant git_libgit2_features() returns:\n{body[:600]}"
    return int(found[0], 0)


def verify(library, arch, toolchain, expected_api=None, unstripped=None, features_header=None):
    expected_api = PINS["api"] if expected_api is None else expected_api
    library = Path(library)
    data = library.read_bytes()
    assert data[:6] == b"\x7fELF\x02\x01", "Expected little-endian ELF64"
    assert struct.unpack_from("<H", data, 16)[0] == 3, "Expected a shared object (ET_DYN)"
    assert struct.unpack_from("<H", data, 18)[0] == ARCHITECTURES[arch][1], "Wrong architecture"
    phoff = struct.unpack_from("<Q", data, 32)[0]
    phsize, phnum = struct.unpack_from("<HH", data, 54)
    alignments = []
    android_api = None
    ndk_note = None
    for index in range(phnum):
        kind, flags, offset, address, _, size, _, alignment = struct.unpack_from(
            "<IIQQQQQQ", data, phoff + index * phsize)
        if kind == 1:  # PT_LOAD
            assert alignment >= 16384 and alignment & (alignment - 1) == 0, f"Not 16 KB aligned: {alignment}"
            assert offset % 16384 == address % 16384, "Invalid load alignment"
            assert flags & 3 != 3, "Writable executable segment"
            alignments.append(alignment)
        if kind == 4:  # PT_NOTE
            cursor = offset
            while cursor + 12 <= offset + size:
                namesz, descsz, note_type = struct.unpack_from("<III", data, cursor)
                cursor += 12
                name = data[cursor:cursor + namesz].rstrip(b"\0")
                cursor += (namesz + 3) & ~3
                if name == b"Android" and note_type == 1 and descsz >= 4:
                    android_api = struct.unpack_from("<I", data, cursor)[0]
                    if descsz >= 4 + 64 + 64:
                        ndk_note = data[cursor + 4:cursor + 4 + 64].rstrip(b"\0").decode() + " / " + \
                            data[cursor + 68:cursor + 68 + 64].rstrip(b"\0").decode()
                cursor += (descsz + 3) & ~3
    assert alignments, "No load segments"
    assert android_api == expected_api, f"Expected API {expected_api}, got {android_api}"
    readelf = toolchain / "llvm-readelf"
    dynamic = output(readelf, "--dynamic", library)
    dependencies = re.findall(r"\(NEEDED\).*?\[(.*?)\]", dynamic)
    assert dependencies and set(dependencies) <= set(PINS["allowed_needed"]), \
        f"Unexpected NEEDED: {sorted(set(dependencies) - set(PINS['allowed_needed']))}"
    assert "GLIBC_" not in output(readelf, "--version-info", library), "Desktop glibc dependency"
    assert not re.search(r"\((TEXTREL|RPATH|RUNPATH)\)", dynamic), "Text relocations or build-host paths"
    assert not re.search(r"\(FLAGS\).*TEXTREL", dynamic), "DF_TEXTREL set"
    soname = re.search(r"\(SONAME\).*?\[(.*?)\]", dynamic)
    assert soname and soname[1] == PINS["soname"], f"Wrong SONAME {soname[1] if soname else None} (want {PINS['soname']})"
    nm = toolchain / "llvm-nm"
    exports = set()
    for line in output(nm, "-D", "--defined-only", library).splitlines():
        parts = line.split()
        if len(parts) >= 3 and parts[1] in "TtWwVvDdBbRrIi":
            exports.add(parts[2].split("@")[0])
    imports = {line.split()[-1].split("@")[0] for line in output(nm, "-D", "--undefined-only", library).splitlines() if line.strip()}
    required = set(PINS["required_exports"])
    assert required <= exports, f"Missing exports: {sorted(required - exports)}"
    leaked = sorted(name for name in exports | imports if name.startswith(("mbedtls_", "psa_", "p256_", "everest_")))
    assert not leaked, f"mbedTLS symbols in the DYNAMIC table (must be linked in and private): {leaked[:10]}"
    git_exports = sorted(name for name in exports if name.startswith("git_"))
    features = features_from_code(library, arch, toolchain)
    assert features == PINS["expected_features"], \
        f"git_libgit2_features() returns {features} ({feature_names(features)}), expected {PINS['expected_features']}"
    notes = output(readelf, "--notes", library)
    build_id = re.search(r"Build ID: (\w+)", notes)
    assert build_id, "Missing build ID"
    report = {"rid": "android-" + arch, "abi": ARCHITECTURES[arch][2], "file": library.name,
              "api": android_api, "ndk_note": ndk_note, "load_alignments": alignments,
              "soname": soname[1], "dependencies": dependencies,
              "exports_total": len(exports), "exports_git": len(git_exports),
              "required_exports": sorted(required),
              "mbedtls_in_dynamic_table": "none (private)",
              "features_from_code": features, "features": feature_names(features),
              "build_id": build_id[1], "sha256": sha256(library), "bytes": len(data)}
    if unstripped is not None:
        unstripped = Path(unstripped)
        twin_id = re.search(r"Build ID: (\w+)", output(readelf, "--notes", unstripped))
        assert twin_id and twin_id[1] == build_id[1], "Stripped and unstripped build-ids differ"
        defined = set()
        for line in output(nm, "--defined-only", unstripped).splitlines():
            parts = line.split()
            if len(parts) >= 3:
                defined.add(parts[2])
        want = set(PINS["required_mbedtls_symbols"])
        assert want <= defined, f"mbedTLS not linked in - missing from the twin's symbol table: {sorted(want - defined)}"
        mbedtls_defined = sorted(name for name in defined if name.startswith("mbedtls_"))
        psa_defined = sorted(name for name in defined if name.startswith("psa_"))
        thirdparty = sorted(name for name in defined if name.startswith(("p256_", "Hacl_", "mbedtls_everest")))
        report.update({"unstripped_sha256": sha256(unstripped), "unstripped_bytes": unstripped.stat().st_size,
                       "mbedtls_functions_linked_in": len(mbedtls_defined),
                       "psa_functions_linked_in": len(psa_defined),
                       "mbedtls_3rdparty_linked_in": thirdparty or "none (everest, p256-m off in the default config)",
                       "required_mbedtls_symbols": sorted(want)})
    if features_header is not None:
        text = Path(features_header).read_text()
        defined = set(re.findall(r"^#define (GIT_\w+)", text, flags=re.M))
        missing = sorted(set(PINS["required_features_defines"]) - defined)
        forbidden = sorted(set(PINS["forbidden_features_defines"]) & defined)
        assert not missing, f"git2_features.h lacks: {missing}"
        assert not forbidden, f"git2_features.h has forbidden: {forbidden}"
        report["git2_features_h"] = sorted(defined)
    return report


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("library", type=Path)
    parser.add_argument("--arch", required=True, choices=ARCHITECTURES)
    parser.add_argument("--toolchain", type=Path, required=True, help="NDK prebuilt bin directory")
    parser.add_argument("--unstripped", type=Path, default=None, help="the pre-strip twin of the same build")
    parser.add_argument("--features-header", type=Path, default=None, help="git2_features.h from the build tree")
    parser.add_argument("--expect-api", type=int, default=None,
                        help="only for inspecting someone else's build; the default is the pinned API")
    args = parser.parse_args()
    print(json.dumps(verify(args.library, args.arch, args.toolchain, args.expect_api,
                            args.unstripped, args.features_header), indent=2))
