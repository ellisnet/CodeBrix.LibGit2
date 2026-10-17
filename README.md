# CodeBrix.LibGit2

A Git repository library for .NET, built on the libgit2 native Git implementation.
CodeBrix.LibGit2 is provided as a .NET 10 library and associated `CodeBrix.LibGit2.MitLicenseForever` NuGet package.

CodeBrix.LibGit2 supports applications and assemblies that target Microsoft .NET version 10.0 and later.
Microsoft .NET version 10.0 is a Long-Term Supported (LTS) version of .NET, and was released on Nov 11, 2025; and will be actively supported by Microsoft until Nov 14, 2028.
Please update your C#/.NET code and projects to the latest LTS version of Microsoft .NET.

## Installation

```
dotnet add package CodeBrix.LibGit2.MitLicenseForever
```

Note that the NuGet package ID and the namespace are different - there is no package named plain `CodeBrix.LibGit2`:

* NuGet package ID: `CodeBrix.LibGit2.MitLicenseForever`
* Assembly and primary namespace: `CodeBrix.LibGit2` - i.e. `using CodeBrix.LibGit2;`

XML documentation (IntelliSense) ships alongside the assembly.

CodeBrix.LibGit2 has no dependencies other than .NET. The native Git library for each supported platform is inside the package, so no Git installation is needed on the target machine.

## CodeBrix.LibGit2 supports:

* Creating, opening, discovering and cloning repositories, including bare repositories, shallow clones and linked worktrees
* Walking and filtering commit history, and per-file history
* Staging, unstaging, removing and moving files, and committing (including amend)
* Working-directory status, with ignore rules and `.gitattributes`
* Branches, tags (lightweight and annotated), references, reflogs and notes
* Checkout, reset (soft, mixed, hard), clean, stash, cherry-pick and revert
* Merge (fast-forward or merge commit, with conflict reporting) and rebase
* Diffs between trees, the index and the working directory, as change lists, patches and line statistics
* Blame and describe
* Remotes: fetch, pull, push and listing remote references over HTTPS, SSH and local paths, with credential, certificate-check and progress callbacks
* Submodules and worktrees
* Repository, global and system configuration
* Direct object-database access: blobs, trees, commits, tag annotations, pack building, archives and custom backends
* Content filters, custom transports and native trace logging
* Native libraries for nine runtime identifiers, selected automatically by NuGet

## Native libraries

The native Git library is packed under `runtimes/<rid>/native/` for each of these runtime identifiers, next to a copy of its licence:

`win-x64`, `win-arm64`, `osx-x64`, `osx-arm64`, `linux-x64`, `linux-arm64`, `linux-riscv64`, `android-arm64`, `android-x64`

Building or publishing an application for one of those runtime identifiers picks up the matching binary; no extra package is needed. How network transports work on each platform:

* **Windows** - HTTPS uses the operating system's own TLS stack (Schannel). `ssh://` URLs are handled by running the `ssh` program found on the machine.
* **macOS** - HTTPS uses the operating system's own TLS stack (SecureTransport). `ssh://` URLs are handled by running the system `ssh` program. The macOS libraries require macOS Sonoma or later.
* **Linux** - HTTPS uses the OpenSSL libraries already installed on the machine (`libssl` and `libcrypto`), which are loaded the first time HTTPS is used; without them, HTTPS operations fail while everything else keeps working. `ssh://` URLs are handled by running the system `ssh` program. The binaries are built for a low glibc baseline, so the same file runs on current and older distributions.
* **Android** - HTTPS support is built into the library itself; nothing extra is needed on the device. There is no SSH transport on Android.

Local repositories and `file://` URLs need no network support at all.

To load the native library from a folder of your own instead, set `GlobalSettings.NativeLibraryPath` to that folder, holding the binary under its shipped name, before making any other call into the library.

## Sample Code

### Open a Repository and Walk Its History

```csharp
using System;
using System.Linq;
using CodeBrix.LibGit2;

using (var repo = new Repository("/path/to/repo"))
{
    Console.WriteLine($"On branch {repo.Head.FriendlyName}");

    foreach (Commit commit in repo.Commits.Take(10))
    {
        Console.WriteLine($"{commit.Id.ToString(7)}  {commit.Author.Name}  {commit.MessageShort}");
    }
}
```

### Clone a Repository

```csharp
using System;
using CodeBrix.LibGit2;

string gitDir = Repository.Clone("https://github.com/ellisnet/CodeBrix.LibGit2.git", "/path/to/clone");

using (var repo = new Repository(gitDir))
{
    Console.WriteLine($"Cloned {repo.Head.FriendlyName} at {repo.Head.Tip.Sha}");
}
```

`CloneOptions` adds a branch name, a bare clone, progress reporting and a credentials callback for private repositories.

### Create a Repository, Stage a File and Commit It

```csharp
using System;
using System.IO;
using CodeBrix.LibGit2;

string repoPath = Repository.Init("/path/to/my-new-repo");

using (var repo = new Repository(repoPath))
{
    File.WriteAllText(Path.Combine(repo.Info.WorkingDirectory, "hello.txt"), "Hello, Git!\n");

    Commands.Stage(repo, "hello.txt");

    var author = new Signature("A. U. Thor", "thor@example.com", DateTimeOffset.Now);
    Commit commit = repo.Commit("Add hello.txt", author, author);

    Console.WriteLine($"Committed {commit.Sha} on {repo.Head.FriendlyName}");
}
```

Always dispose a `Repository` (as above with `using`) - it holds native resources and open file handles.

## Documentation

The NuGet package includes `AGENT-README.txt`, a complete API reference and usage guide written for AI coding agents - point your agent at that file when it is writing code against this library.

Additional sample code and usage examples are available in the `CodeBrix.LibGit2.Tests` project:
https://github.com/ellisnet/CodeBrix.LibGit2/tree/main/tests/CodeBrix.LibGit2.Tests

## License

CodeBrix.LibGit2 is licensed under the MIT License - see the
[LICENSE](https://github.com/ellisnet/CodeBrix.LibGit2/blob/main/LICENSE) file.

For licensing and provenance information about the open source code included in
this package, see [THIRD-PARTY-NOTICES.txt](https://github.com/ellisnet/CodeBrix.LibGit2/blob/main/THIRD-PARTY-NOTICES.txt).
