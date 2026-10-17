tools/libgit2-native-tools/output - build results (git-ignored except this file)
================================================================================
Every build script in tools/libgit2-native-tools writes here, one folder per
runtime identifier:

  <rid>/libgit2-5853918.so | git2-5853918.dll | libgit2-5853918.dylib
                                    the library - the file the package ships
  <rid>/unstripped/                 the pre-strip twin of the same build (Linux,
                                    Android; a .pdb on Windows if one is made)
  <rid>/LICENSE-Libgit2.txt         libgit2's COPYING, verbatim
  <rid>/LICENSE-Xdiff.txt           the bundled xdiff's notices
                                    (a copy of ../LICENSE-Xdiff.txt)
  <rid>/LICENSE-MbedTls.txt         Mbed TLS's LICENSE (Android only)
  <rid>/BUILD-INFO.txt / .json      toolchain, pins, feature summary, sizes,
                                    sha256, build-id, floors, smoke-test output
  <rid>/cmake-summary.txt           libgit2's own feature summary
  <rid>/cmake-configure.log         the whole configure log (Linux)
  <rid>/exports.txt                 every exported name (Linux)
  <rid>/git2_features.h             the generated feature header (Android)
  <rid>/smoke-test                  the gate program that was built
  <rid>/SHA256SUMS.txt              checksums of that RID's files
  SHA256SUMS                        every built Linux library, one line each
  build-scratch/                    android/build.py's scratch tree, removed
                                    after each build

Everything here is DISPOSABLE. Nothing in it is committed, and nothing here is
an input to any build or pack step. The copies that are meant to last are the
ones adopted into ../../../native_libraries/<rid>/ and the unstripped twins in
../unstripped/<rid>/ - see ../README.txt.
