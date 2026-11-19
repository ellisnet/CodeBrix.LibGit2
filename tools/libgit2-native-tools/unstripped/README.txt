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

  <rid>/libgit2-49e408b.so   the pre-strip ELF (Linux, Android), with DWARF
                             debug info (every build compiles with -g; strip
                             removes it)

The twins are stored RAW, not compressed: 8-12 MB each, well under GitHub's
per-file limit.

Verify any file two ways:

  1. sha256 - `sha256sum -c SHA256SUMS` from this folder; the same value is the
     "SHA256 unstripped" line of that RID's entry in ../BUILD-PROVENANCE.txt and
     of ../../../native_libraries/<rid>/libgit2-49e408b.so.provenance.txt.
  2. the GNU build-id equals the shipped binary's -
        readelf -n <rid>/libgit2-49e408b.so | grep 'Build ID'
        readelf -n ../../../native_libraries/<rid>/libgit2-49e408b.so \
            | grep 'Build ID'
     (readelf may also print "Gap in build notes" and exit 1 - harmless, see
     ../linux/README.txt, TROUBLESHOOTING. For Android, the NDK's
     llvm-readelf -n works as well as binutils readelf -n.)

STORED SO FAR - libgit2 1.9.7 (build-ids, each verified equal to its shipped
twin's):

  linux-x64      aa47cc3c6f6eb0a328854a4c68aa01c7ee055c8e   (verified 2026-11-18)
  linux-arm64    64fb5464a41577349093d7c95ea9293d2ffaea1d   (verified 2026-11-18)
  linux-riscv64  41ab7faa013a5e14fe4925ddb68daf3946b56126   (verified 2026-11-18)
  android-arm64  8c3b7075711abd509316e3ed8120adc095afed50   (verified 2026-11-18)
  android-x64    6be173816ba4b17be6a88a2349f6ad56e2e723d5   (verified 2026-11-18)

The 1.8.6 twins (libgit2-5853918.so) were removed with their binaries on
2026-11-18; they are in git history.

PENDING - nothing to store yet
--------------------------------------------------------------------------------
The win-* and osx-* RIDs have no binary for libgit2 1.9.7 yet (see
../BUILD-PROVENANCE.txt). When they are built with the scripts in ../windows
and ../macos, any debug-symbol twin (a .pdb, should a Windows build make one;
the dylibs ship unstripped) lands here under the same rule.

THE RULE
--------------------------------------------------------------------------------
Whenever a newly built binary is adopted into native_libraries/<rid>/, its
unstripped mate from the same build lands here in the same change, and
SHA256SUMS is updated. A binary here that no longer matches the shipped one's
build-id is stale and must be replaced, never kept alongside.
================================================================================
