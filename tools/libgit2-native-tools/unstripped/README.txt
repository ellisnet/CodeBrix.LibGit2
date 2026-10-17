================================================================================
tools/libgit2-native-tools/unstripped - the durable home for pre-strip binaries
================================================================================

UNLIKE output/, THIS FOLDER IS COMMITTED (the CodeBrix.Sdl3 policy). It exists
so the unstripped mate of every libgit2 binary BUILT HERE and shipped in the
package survives the machine it was built on (../output/ is git-ignored and
disposable). These files are needed for crash triage: a stripped release
binary in a crash dump can only be symbolised from its unstripped twin.
Nothing here is shipped, and nothing here is an input to any build or pack
step.

One folder per runtime identifier, mirroring ../../../native_libraries/<rid>/:

  <rid>/libgit2-5853918.so   the pre-strip ELF (Linux, Android), with DWARF
                             debug info (every build compiles with -g; strip
                             removes it)

The twins are stored RAW, not compressed: 8-12 MB each, well under GitHub's
per-file limit.

Verify any file two ways:

  1. sha256 - `sha256sum -c SHA256SUMS` from this folder; the same value is the
     "SHA256 unstripped" line of that RID's entry in ../BUILD-PROVENANCE.txt and
     of ../../../native_libraries/<rid>/libgit2-5853918.so.provenance.txt.
  2. the GNU build-id equals the shipped binary's -
        readelf -n <rid>/libgit2-5853918.so | grep 'Build ID'
        readelf -n ../../../native_libraries/<rid>/libgit2-5853918.so \
            | grep 'Build ID'
     (readelf may also print "Gap in build notes" and exit 1 - harmless, see
     ../linux/README.txt, TROUBLESHOOTING. For Android, the NDK's
     llvm-readelf -n works as well as binutils readelf -n.)

STORED SO FAR (build-ids, each verified equal to its shipped twin's):

  linux-x64      9c26c49eac90f1653051618e72ba2d466ebd9437   (verified 2026-10-17)
  linux-arm64    5ba009a1b89b585462a5069d0c2bcd8573bcd4a8   (verified 2026-10-17)
  linux-riscv64  c70d8602964379e562fe0fa61aae2b9c8016a6bc   (verified 2026-10-17)
  android-arm64  f269df3147ab348bde8e15563c4f8d4490e5a278   (verified 2026-10-17)
  android-x64    c0757c46cded48c7985f2b858bb06f87a3b9b0f5   (verified 2026-10-17)

ADOPTED, NOT BUILT HERE - nothing to store
--------------------------------------------------------------------------------
The win-* and osx-* binaries are adopted from LibGit2Sharp.NativeBinaries
2.0.324 (see ../BUILD-PROVENANCE.txt). Upstream published no .pdb or dSYM
(the dylibs ship unstripped anyway), so there is nothing to keep here. When
those RIDs are rebuilt with the scripts in ../windows and ../macos, any
debug-symbol twin lands here under the same rule.

THE RULE
--------------------------------------------------------------------------------
Whenever a newly built binary is adopted into native_libraries/<rid>/, its
unstripped mate from the same build lands here in the same change, and
SHA256SUMS is updated. A binary here that no longer matches the shipped one's
build-id is stale and must be replaced, never kept alongside.
================================================================================
