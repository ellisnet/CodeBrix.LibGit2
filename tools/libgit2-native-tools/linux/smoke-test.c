/*
 * smoke-test.c - the load-and-run verification program for the libgit2 native library
 *
 * Part of CodeBrix.LibGit2's tools/libgit2-native-tools. One file, NO libgit2 headers, no build
 * system: every platform's gate compiles it with the plain C compiler and runs it against the
 * STAGED (stripped, about-to-be-shipped) library, so the bytes that are tested are the bytes
 * that ship. It lives in linux/ (where it runs on every build); the Android, Windows and macOS
 * scripts compile this same file from ../linux/smoke-test.c.
 *
 *     Linux / Android :  cc -std=c11 -O1 -o smoke-test smoke-test.c -ldl
 *     macOS           :  cc -std=c11 -O1 -o smoke-test smoke-test.c
 *     Windows (MSVC)  :  cl /nologo /O1 smoke-test.c
 *
 *     ./smoke-test <path-to-library> [expected-version] [expected-features] [ca-location]
 *
 *       expected-version   default "1.8.6"
 *       expected-features  the exact git_libgit2_features() bit mask the build must report,
 *                          default 15 (THREADS 1 | HTTPS 2 | SSH 4 | NSEC 8); "-" skips the check
 *       ca-location        optional: a CA bundle FILE or a CA certificate DIRECTORY. When given,
 *                          GIT_OPT_SET_SSL_CERT_LOCATIONS is called with it (as the file or the
 *                          path argument, whichever it is), which forces the TLS backend to
 *                          initialise and load it. For the Linux OpenSSL-Dynamic build that is the
 *                          moment libgit2 dlopen()s libssl, so this proves the run-time OpenSSL
 *                          loading works on the machine that runs it; for the Android mbedTLS build
 *                          it proves the statically linked mbedTLS parses the device's store
 *                          (/system/etc/security/cacerts).
 *
 * It loads the library the way .NET's DllImport does (dlopen / LoadLibrary, by path), resolves the
 * entry points it needs by name, and then:
 *   1. git_libgit2_version() must equal the expected version,
 *   2. git_libgit2_features() must equal the expected bit mask,
 *   3. git_libgit2_init() must return 1 (first initialisation),
 *   4. git_odb_hash() of the 6 bytes "hello\n" as a blob must be
 *      ce013625030ba8dba906f756967f9e9ca394464a (what `git hash-object` gives - exercises the
 *      bundled SHA-1 collision-detection code),
 *   5. git_repository_init() a new repository in a fresh temporary folder, write the same blob
 *      into it with git_blob_create_from_buffer() and check its id, then git_repository_open()
 *      that folder again,
 *   6. (only with ca-location) GIT_OPT_SET_SSL_CERT_LOCATIONS must succeed,
 *   7. git_libgit2_shutdown() must return 0.
 * Nothing touches the network. The temporary repository is removed again (POSIX only; on Windows
 * its path is printed).
 *
 * Exit code 0 means every step passed; anything else names the step that failed.
 *
 * Function types are declared by hand from the libgit2 1.8.6 headers (include/git2/common.h,
 * global.h, errors.h, odb.h, oid.h, repository.h, blob.h, types.h). With EXPERIMENTAL_SHA256 off
 * (libgit2's default, and every build here) a git_oid is 20 bytes; a larger buffer is passed
 * anyway. GIT_OPT_SET_SSL_CERT_LOCATIONS is the 13th git_libgit2_opt_t value, i.e. 12;
 * GIT_OBJECT_BLOB is 3. GIT_EXTERN functions use the platform's default C calling convention.
 */

#if !defined(_WIN32)
#  define _XOPEN_SOURCE 700
#endif

#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#if defined(_WIN32)
#  include <windows.h>
typedef HMODULE lib_handle;
static lib_handle lib_open(const char *path) { return LoadLibraryA(path); }
static void *lib_sym(lib_handle h, const char *name) { return (void *)GetProcAddress(h, name); }
static const char *lib_error(void) { static char buf[64]; snprintf(buf, sizeof buf, "Win32 error %lu", (unsigned long)GetLastError()); return buf; }
#else
#  include <dlfcn.h>
#  include <ftw.h>
#  include <sys/stat.h>
#  include <unistd.h>
typedef void *lib_handle;
static lib_handle lib_open(const char *path) { return dlopen(path, RTLD_NOW | RTLD_LOCAL); }
static void *lib_sym(lib_handle h, const char *name) { return dlsym(h, name); }
static const char *lib_error(void) { const char *e = dlerror(); return e ? e : "unknown"; }
#endif

#define GIT_OPT_SET_SSL_CERT_LOCATIONS 12
#define GIT_OBJECT_BLOB 3
#define HELLO_BLOB_ID "ce013625030ba8dba906f756967f9e9ca394464a"

typedef struct { char *message; int klass; } git_error_view;
typedef struct { unsigned char id[64]; } git_oid_buffer;   /* 20 bytes used; generous on purpose */

typedef int                    (*fn_version)(int *, int *, int *);
typedef int                    (*fn_features)(void);
typedef int                    (*fn_init)(void);
typedef int                    (*fn_shutdown)(void);
typedef int                    (*fn_opts)(int, ...);
typedef const git_error_view * (*fn_error_last)(void);
typedef int                    (*fn_odb_hash)(git_oid_buffer *, const void *, size_t, int);
typedef char *                 (*fn_oid_tostr_s)(const git_oid_buffer *);
typedef int                    (*fn_repository_init)(void **, const char *, unsigned int);
typedef int                    (*fn_repository_open)(void **, const char *);
typedef void                   (*fn_repository_free)(void *);
typedef const char *           (*fn_repository_path)(void *);
typedef int                    (*fn_blob_create_from_buffer)(git_oid_buffer *, void *, const void *, size_t);

static lib_handle lib;
static int resolved = 0;

static void *need(const char *name)
{
    void *p = lib_sym(lib, name);
    if (!p) {
        fprintf(stderr, "FAIL: symbol %s not found in the library\n", name);
        exit(10);
    }
    resolved++;
    return p;
}

static fn_error_last p_error_last;

static const char *last_error(void)
{
    const git_error_view *e = p_error_last ? p_error_last() : NULL;
    return (e && e->message) ? e->message : "(no libgit2 error message)";
}

#if !defined(_WIN32)
static int remove_entry(const char *path, const struct stat *sb, int flag, struct FTW *ftw)
{
    (void)sb; (void)flag; (void)ftw;
    return remove(path);
}
#endif

int main(int argc, char **argv)
{
    const char *expected_version = "1.8.6";
    const char *expected_features = "15";
    const char *ca_file = NULL;
    char version[32];
    int major = 0, minor = 0, rev = 0, features, rc;
    char tmpdir[1024];

    if (argc < 2) {
        fprintf(stderr, "usage: %s <library> [expected-version] [expected-features|-] [ca-location]\n", argv[0]);
        return 2;
    }
    if (argc > 2 && argv[2][0]) expected_version = argv[2];
    if (argc > 3 && argv[3][0]) expected_features = argv[3];
    if (argc > 4 && argv[4][0]) ca_file = argv[4];

    lib = lib_open(argv[1]);
    if (!lib) {
        fprintf(stderr, "FAIL: could not load %s: %s\n", argv[1], lib_error());
        return 11;
    }
    printf("loaded           : %s\n", argv[1]);

    fn_version                 p_version     = (fn_version)need("git_libgit2_version");
    fn_features                p_features    = (fn_features)need("git_libgit2_features");
    fn_init                    p_init        = (fn_init)need("git_libgit2_init");
    fn_shutdown                p_shutdown    = (fn_shutdown)need("git_libgit2_shutdown");
    fn_opts                    p_opts        = (fn_opts)need("git_libgit2_opts");
    p_error_last                             = (fn_error_last)need("git_error_last");
    fn_odb_hash                p_odb_hash    = (fn_odb_hash)need("git_odb_hash");
    fn_oid_tostr_s             p_oid_tostr_s = (fn_oid_tostr_s)need("git_oid_tostr_s");
    fn_repository_init         p_repo_init   = (fn_repository_init)need("git_repository_init");
    fn_repository_open         p_repo_open   = (fn_repository_open)need("git_repository_open");
    fn_repository_free         p_repo_free   = (fn_repository_free)need("git_repository_free");
    fn_repository_path         p_repo_path   = (fn_repository_path)need("git_repository_path");
    fn_blob_create_from_buffer p_blob_create = (fn_blob_create_from_buffer)need("git_blob_create_from_buffer");
    printf("entry points     : %d resolved\n", resolved);

    /* 1. version */
    p_version(&major, &minor, &rev);
    snprintf(version, sizeof version, "%d.%d.%d", major, minor, rev);
    printf("git_libgit2_version : %s (expected %s)\n", version, expected_version);
    if (strcmp(version, expected_version) != 0) {
        fprintf(stderr, "FAIL: version mismatch\n");
        return 20;
    }

    /* 2. features */
    features = p_features();
    printf("git_libgit2_features: %d =%s%s%s%s\n", features,
           (features & 1) ? " THREADS" : "", (features & 2) ? " HTTPS" : "",
           (features & 4) ? " SSH" : "", (features & 8) ? " NSEC" : "");
    if (strcmp(expected_features, "-") != 0 && features != atoi(expected_features)) {
        fprintf(stderr, "FAIL: features %d, expected %s\n", features, expected_features);
        return 21;
    }

    /* 3. init */
    rc = p_init();
    printf("git_libgit2_init    : %d\n", rc);
    if (rc != 1) {
        fprintf(stderr, "FAIL: git_libgit2_init returned %d: %s\n", rc, last_error());
        return 22;
    }

    /* 4. hash a blob - no repository involved */
    {
        git_oid_buffer oid;
        memset(&oid, 0, sizeof oid);
        if (p_odb_hash(&oid, "hello\n", 6, GIT_OBJECT_BLOB) != 0) {
            fprintf(stderr, "FAIL: git_odb_hash: %s\n", last_error());
            return 23;
        }
        printf("git_odb_hash        : %s\n", p_oid_tostr_s(&oid));
        if (strcmp(p_oid_tostr_s(&oid), HELLO_BLOB_ID) != 0) {
            fprintf(stderr, "FAIL: blob id mismatch, expected %s\n", HELLO_BLOB_ID);
            return 24;
        }
    }

    /* 5. a real repository on disk */
#if defined(_WIN32)
    {
        char base[MAX_PATH];
        DWORD n = GetTempPathA(sizeof base, base);
        if (n == 0 || n >= sizeof base) { fprintf(stderr, "FAIL: GetTempPath\n"); return 25; }
        snprintf(tmpdir, sizeof tmpdir, "%scodebrix-libgit2-smoke-%lu", base, (unsigned long)GetCurrentProcessId());
    }
#else
    {
        const char *base = getenv("TMPDIR");
        if (!base || !base[0]) base = "/tmp";
        snprintf(tmpdir, sizeof tmpdir, "%s/codebrix-libgit2-smoke-XXXXXX", base);
        if (!mkdtemp(tmpdir)) { perror("FAIL: mkdtemp"); return 25; }
    }
#endif
    {
        void *repo = NULL, *again = NULL;
        git_oid_buffer oid;
        memset(&oid, 0, sizeof oid);
        if (p_repo_init(&repo, tmpdir, 0) != 0) {
            fprintf(stderr, "FAIL: git_repository_init(%s): %s\n", tmpdir, last_error());
            return 26;
        }
        printf("git_repository_init : %s\n", p_repo_path(repo));
        if (p_blob_create(&oid, repo, "hello\n", 6) != 0) {
            fprintf(stderr, "FAIL: git_blob_create_from_buffer: %s\n", last_error());
            return 27;
        }
        printf("blob written        : %s\n", p_oid_tostr_s(&oid));
        if (strcmp(p_oid_tostr_s(&oid), HELLO_BLOB_ID) != 0) {
            fprintf(stderr, "FAIL: written blob id mismatch\n");
            return 28;
        }
        p_repo_free(repo);
        if (p_repo_open(&again, tmpdir) != 0) {
            fprintf(stderr, "FAIL: git_repository_open(%s): %s\n", tmpdir, last_error());
            return 29;
        }
        printf("git_repository_open : ok\n");
        p_repo_free(again);
    }
#if !defined(_WIN32)
    nftw(tmpdir, remove_entry, 16, FTW_DEPTH | FTW_PHYS);
    printf("temporary repository: removed\n");
#else
    printf("temporary repository: left at %s\n", tmpdir);
#endif

    /* 6. TLS backend initialisation (optional) */
    if (ca_file) {
        int is_dir = 0;
#if defined(_WIN32)
        DWORD attr = GetFileAttributesA(ca_file);
        is_dir = (attr != INVALID_FILE_ATTRIBUTES) && (attr & FILE_ATTRIBUTE_DIRECTORY);
#else
        struct stat st;
        is_dir = (stat(ca_file, &st) == 0) && S_ISDIR(st.st_mode);
#endif
        if (is_dir)
            rc = p_opts(GIT_OPT_SET_SSL_CERT_LOCATIONS, (const char *)NULL, ca_file);
        else
            rc = p_opts(GIT_OPT_SET_SSL_CERT_LOCATIONS, ca_file, (const char *)NULL);
        printf("SET_SSL_CERT_LOCATIONS(%s %s): %d\n", is_dir ? "path" : "file", ca_file, rc);
        if (rc != 0) {
            fprintf(stderr, "FAIL: TLS backend did not initialise: %s\n", last_error());
            return 30;
        }
    } else {
        printf("SET_SSL_CERT_LOCATIONS: skipped (no ca-location argument)\n");
    }

    /* 7. shutdown */
    rc = p_shutdown();
    printf("git_libgit2_shutdown: %d\n", rc);
    if (rc != 0) {
        fprintf(stderr, "FAIL: git_libgit2_shutdown returned %d\n", rc);
        return 31;
    }

    printf("SMOKE TEST PASSED\n");
    return 0;
}
