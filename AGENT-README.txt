================================================================================
AGENT-README: CodeBrix.LibGit2
A Guide for AI Coding Agents — CONSUMING the
CodeBrix.LibGit2.MitLicenseForever NuGet package
================================================================================

OVERVIEW
========
CodeBrix.LibGit2 is a Git library for .NET 10 or later. It reads and writes
Git repositories directly from C# - init, open, clone, status, stage, commit,
branch, tag, checkout, reset, merge, rebase, cherry-pick, revert, stash,
diff, blame, describe, fetch, pull, push, submodules, worktrees,
configuration and raw object-database access - with no Git installation
needed on the machine. The Git work is done by the libgit2 native library,
which ships INSIDE this package for Windows, macOS, Linux and Android.

CodeBrix.LibGit2 is ported from the LibGit2Sharp project, the long-standing
managed wrapper around libgit2. The type and member names are LibGit2Sharp's
(Repository, Commit, Signature, Commands, LibGit2SharpException ...); only the
namespaces differ. Every namespace is rooted at "CodeBrix.LibGit2":

    LibGit2Sharp            -> CodeBrix.LibGit2
    LibGit2Sharp.Handlers   -> CodeBrix.LibGit2.Handlers

Do NOT write `using LibGit2Sharp;` - that namespace does not exist in this
package. Code written against LibGit2Sharp usually compiles after the using
directives are changed.


INSTALLATION
============
PackageId:  CodeBrix.LibGit2.MitLicenseForever

    dotnet add package CodeBrix.LibGit2.MitLicenseForever

IMPORTANT: the NuGet package id is CodeBrix.LibGit2.MitLicenseForever (NOT
"CodeBrix.LibGit2" - that suffix exists only to make the package license
obvious forever). The assembly and primary namespace are CodeBrix.LibGit2.

NuGet dependencies: none. The package carries the managed assembly, its XML
documentation, and the libgit2 native library under runtimes/<rid>/native/
for these runtime identifiers:

    win-x64  win-arm64  osx-x64  osx-arm64
    linux-x64  linux-arm64  linux-riscv64  android-arm64  android-x64

NuGet and the .NET host pick the right binary automatically, for a normal
build, a RID-specific publish, or a self-contained publish. Other RIDs (for
example win-x86, linux-arm, linux-musl-x64 / Alpine) have NO native library
in this package and fail at the first call.

License: MIT for the managed code (SPDX: MIT). The native libgit2 binary is
GPLv2 with the libgit2 linking exception, which allows it to be linked into
and distributed with any application; its licence text travels beside every
binary as LICENSE-Libgit2.txt.

Requirements: .NET 10 or later.

PLATFORM TRANSPORT NOTES (what HTTPS and SSH need at run time):
  Windows  HTTPS through the OS TLS stack (Schannel). ssh:// URLs run the
           `ssh` program found on the machine.
  macOS    HTTPS through the OS TLS stack (SecureTransport). ssh:// URLs run
           the system `ssh` program. The macOS binaries require macOS Sonoma
           or later; they do not load on Ventura or older.
  Linux    HTTPS through the machine's own OpenSSL (libssl + libcrypto),
           loaded at run time the first time HTTPS is used. If no OpenSSL is
           installed, HTTPS operations fail with "could not load ssl
           libraries"; local and file:// work is unaffected. ssh:// URLs run
           the system `ssh` program. The binaries target a low glibc
           baseline.
  Android  HTTPS is built into the library (statically linked TLS, using the
           device's /system/etc/security/cacerts store). NO SSH transport:
           ssh:// URLs fail on Android. Built for Android API level 33 and
           later.

For ssh:// URLs the ssh program is chosen like Git does: the GIT_SSH_COMMAND
environment variable, else GIT_SSH, else the core.sshCommand config value,
else plain `ssh`. Authentication is entirely the ssh program's business
(keys, ssh-agent, ~/.ssh/config); a CredentialsProvider callback is NOT
consulted for ssh:// URLs.


KEY NAMESPACES / USINGS
=======================
    using CodeBrix.LibGit2;            // Repository, Commands, Commit, Branch,
                                       //   Signature, options classes,
                                       //   exceptions, GlobalSettings -
                                       //   almost everything
    using CodeBrix.LibGit2.Handlers;   // callback delegate types:
                                       //   CredentialsHandler,
                                       //   ProgressHandler,
                                       //   TransferProgressHandler,
                                       //   CheckoutProgressHandler, ...

The option classes expose those delegates as properties, so lambdas assigned
to them compile without the Handlers using; add it only when you declare a
variable of a delegate type.

CodeBrix.LibGit2.Core holds the interop layer; nothing in it is public.

NAME CLASH: CodeBrix.LibGit2.Version (returned by GlobalSettings.Version)
collides with System.Version when `using System;` is present (CS0104). Use
`var`, or write CodeBrix.LibGit2.Version in full.


CORE API REFERENCE
==================

Repository (class, IDisposable; implements IRepository)
-------------------------------------------------------
Static members:
    string Repository.Init(string path)
    string Repository.Init(string path, bool isBare)
    string Repository.Init(string workingDirectoryPath, string gitDirectoryPath)
        Create a repository; returns the path of the .git directory (or of
        the bare repository), with a trailing separator.
    string Repository.Clone(string sourceUrl, string workdirPath)
    string Repository.Clone(string sourceUrl, string workdirPath,
                            CloneOptions options)
        Clone; returns the path of the new .git directory.
    bool   Repository.IsValid(string path)
    string Repository.Discover(string startingPath)
        Walk upwards from startingPath; returns the .git path or null.
    IEnumerable<Reference> Repository.ListRemoteReferences(string url)
        (+ overloads taking CredentialsHandler and/or ProxyOptions)

Constructors:
    new Repository(string path)
    new Repository(string path, RepositoryOptions options)
        path may be the working directory or the .git directory.
        RepositoryOptions: WorkingDirectoryPath, IndexPath, Identity.

Properties:
    Branch                Head           the branch HEAD points at (for a
                                         detached HEAD, a pseudo-branch
                                         named "(no branch)")
    RepositoryInformation Info           Path, WorkingDirectory (null when
                                         bare), IsBare, IsShallow,
                                         IsHeadDetached, IsHeadUnborn,
                                         CurrentOperation, Message
    IQueryableCommitLog   Commits        history from HEAD; QueryBy(...)
    BranchCollection      Branches
    TagCollection         Tags
    ReferenceCollection   Refs
    Index                 Index          throws BareRepositoryException on
                                         a bare repository
    Network               Network        Remotes, ListReferences, Fetch, Push
    Configuration         Config
    StashCollection       Stashes
    WorktreeCollection    Worktrees
    SubmoduleCollection   Submodules
    NoteCollection        Notes
    Diff                  Diff
    Rebase                Rebase
    ObjectDatabase        ObjectDatabase
    Ignore                Ignore

Instance methods (some are extension methods in RepositoryExtensions, which
is in the same namespace):
    Commit  Commit(string message, Signature author, Signature committer)
    Commit  Commit(string message, Signature author, Signature committer,
                   CommitOptions options)
    RepositoryStatus RetrieveStatus()
    RepositoryStatus RetrieveStatus(StatusOptions options)
    FileStatus       RetrieveStatus(string filePath)
    GitObject Lookup(string objectish)        // "HEAD~2", sha, "main:path"...
    GitObject Lookup(ObjectId id)
    T         Lookup<T>(string objectish)     where T : GitObject
    T         Lookup<T>(ObjectId id)          where T : GitObject
    Branch    CreateBranch(string branchName)
    Branch    CreateBranch(string branchName, Commit target)
    Branch    CreateBranch(string branchName, string committish)
    Tag       ApplyTag(string tagName)
    Tag       ApplyTag(string tagName, string objectish)
    Tag       ApplyTag(string tagName, Signature tagger, string message)
    Tag       ApplyTag(string tagName, string objectish, Signature tagger,
                       string message)
    void      Reset(ResetMode resetMode)                 // to HEAD
    void      Reset(ResetMode resetMode, string committish)
    void      Reset(ResetMode resetMode, Commit commit)
    void      Reset(ResetMode resetMode, Commit commit, CheckoutOptions opts)
    void      CheckoutPaths(string committishOrBranchSpec,
                            IEnumerable<string> paths)
    void      Checkout(Tree tree, IEnumerable<string> paths,
                       CheckoutOptions options)
    MergeResult Merge(Branch branch, Signature merger)
    MergeResult Merge(Commit commit, Signature merger)
    MergeResult Merge(string committish, Signature merger)
        (+ the same three with a trailing MergeOptions)
    MergeResult MergeFetchedRefs(Signature merger, MergeOptions options)
    CherryPickResult CherryPick(Commit commit, Signature committer)
    CherryPickResult CherryPick(Commit commit, Signature committer,
                                CherryPickOptions options)
    RevertResult Revert(Commit commit, Signature reverter)
    RevertResult Revert(Commit commit, Signature reverter,
                        RevertOptions options)
    BlameHunkCollection Blame(string path)
    BlameHunkCollection Blame(string path, BlameOptions options)
    string    Describe(Commit commit)
    string    Describe(Commit commit, DescribeOptions options)
    void      RevParse(string revision, out Reference reference,
                       out GitObject obj)
    void      RemoveUntrackedFiles()                     // like git clean
    void      Dispose()

Commands (static class) - porcelain operations
----------------------------------------------
    void   Commands.Stage(IRepository repo, string path)
    void   Commands.Stage(IRepository repo, IEnumerable<string> paths)
           (+ overloads with StageOptions: IncludeIgnored,
            ExplicitPathsOptions). "*" stages everything, like `git add -A`.
    void   Commands.Unstage(IRepository repo, string path)
           (+ IEnumerable<string> and ExplicitPathsOptions overloads)
    void   Commands.Move(IRepository repo, string sourcePath,
                         string destinationPath)
    void   Commands.Remove(IRepository repo, string path)
    void   Commands.Remove(IRepository repo, string path,
                           bool removeFromWorkingDirectory)
           (+ ExplicitPathsOptions and IEnumerable<string> overloads)
    Branch Commands.Checkout(IRepository repo, string committishOrBranchSpec)
    Branch Commands.Checkout(IRepository repo, Branch branch)
    Branch Commands.Checkout(IRepository repo, Commit commit) // detaches HEAD
           (+ each with a trailing CheckoutOptions)
    void   Commands.Fetch(Repository repo, string remote,
                          IEnumerable<string> refspecs, FetchOptions options,
                          string logMessage)
    MergeResult Commands.Pull(Repository repo, Signature merger,
                              PullOptions options)
Fetch and Pull take the concrete Repository, not IRepository.

Commit log
----------
    repo.Commits                         IQueryableCommitLog (history of HEAD)
    repo.Commits.QueryBy(CommitFilter filter)        -> ICommitLog
    repo.Commits.QueryBy(string path)                -> IEnumerable<LogEntry>
    repo.Commits.QueryBy(string path, CommitFilter filter)
    branch.Commits                       ICommitLog of that branch
    CommitFilter: IncludeReachableFrom (object: Commit, Branch, Reference,
                  sha string, or a list of them; default "HEAD"),
                  ExcludeReachableFrom, SortBy (CommitSortStrategies: None,
                  Topological, Time, Reverse - a [Flags] enum; default
                  Time), FirstParentOnly
    LogEntry:     Path, Commit

Git objects
-----------
    GitObject   Id (ObjectId), Sha, IsMissing, Peel<T>()
    Commit      Message, MessageShort, Author, Committer, Tree, Parents,
                Notes, Encoding, this[string relativePath] -> TreeEntry
    Tree        Count, this[string relativePath] -> TreeEntry, enumerable
                of TreeEntry
    TreeEntry   Name, Path, Mode, Target (GitObject), TargetType
    Blob        Size, IsBinary, GetContentText(), GetContentText(Encoding),
                GetContentStream(), (+ FilteringOptions overloads)
    TagAnnotation  Name, Message, Target, Tagger
    ObjectId    Sha, ToString(int prefixLength), RawId, StartsWith(string),
                ObjectId.TryParse(string, out ObjectId), ObjectId.Zero,
                new ObjectId(string sha)

Branches - repo.Branches (BranchCollection, enumerable of Branch)
-----------------------------------------------------------------
    Branch this[string name]             // "feature" or "origin/feature";
                                         //   null when absent
    Branch Add(string name, string committish)
    Branch Add(string name, Commit commit)
           (+ bool allowOverwrite overloads)
    void   Remove(string name) / Remove(string name, bool isRemote)
    void   Remove(Branch branch)
    Branch Rename(string currentName, string newName)
    Branch Rename(Branch branch, string newName)
           (+ bool allowOverwrite overloads)
    Branch Update(Branch branch, params Action<BranchUpdater>[] actions)
           BranchUpdater: Remote, UpstreamBranch, TrackedBranch
    Branch: FriendlyName, CanonicalName, Tip (Commit, null when unborn),
            Commits, IsRemote, IsTracking, TrackedBranch, TrackingDetails
            (AheadBy, BehindBy, CommonAncestor), IsCurrentRepositoryHead,
            RemoteName, UpstreamBranchCanonicalName

Tags - repo.Tags (TagCollection, enumerable of Tag)
---------------------------------------------------
    Tag this[string name]
    Tag Add(string name, string objectish)                     // lightweight
    Tag Add(string name, GitObject target)                     // lightweight
    Tag Add(string name, string objectish, Signature tagger, string message)
    Tag Add(string name, GitObject target, Signature tagger, string message)
        (+ bool allowOverwrite overloads)                      // annotated
    void Remove(string name) / Remove(Tag tag)
    Tag: FriendlyName, CanonicalName, Target, PeeledTarget, IsAnnotated,
         Annotation (TagAnnotation, null for lightweight)

References - repo.Refs (ReferenceCollection, enumerable of Reference)
---------------------------------------------------------------------
    Reference this[string name]          // canonical name, e.g.
                                         //   "refs/heads/main"
    Reference Head
    Add(...), Remove(...), Rename(...), UpdateTarget(...)  - direct
        (ObjectId) and symbolic (Reference) forms, optional log message and
        allowOverwrite
    IEnumerable<Reference> FromGlob(string pattern)
    IEnumerable<Reference> ReachableFrom(IEnumerable<Commit> targets)
    ReflogCollection Log(string canonicalName) / Log(Reference reference)
    void RewriteHistory(RewriteHistoryOptions options,
                        params Commit[] commitsToRewrite)
    Reference: CanonicalName, TargetIdentifier, IsLocalBranch,
               IsRemoteTrackingBranch, IsTag, IsNote,
               ResolveToDirectReference()
               (DirectReference / SymbolicReference subclasses)

Index - repo.Index (enumerable of IndexEntry)
---------------------------------------------
    int Count, bool IsFullyMerged, IndexEntry this[string path]
    void Add(string pathInTheWorkdir)
    void Add(Blob blob, string indexEntryPath, Mode indexEntryMode)
    void Remove(string indexEntryPath)
    void Replace(Tree source) / Replace(Commit commit)
         (+ paths / ExplicitPathsOptions overloads)
    void Clear(), void Write(), Tree WriteToTree()
    ConflictCollection Conflicts         // Conflict: Ancestor, Ours, Theirs
Index.Add/Remove change the in-memory index; call Write() to persist them.
Commands.Stage / Unstage / Remove write the index for you.

Status - repo.RetrieveStatus() -> RepositoryStatus (enumerable of StatusEntry)
-----------------------------------------------------------------------------
    IsDirty, Added, Staged, Removed, Missing, Modified, Untracked, Ignored,
    RenamedInIndex, RenamedInWorkDir, Unaltered, this[string path]
    StatusEntry: FilePath, State (FileStatus [Flags]: NewInIndex,
                 ModifiedInIndex, DeletedFromIndex, RenamedInIndex,
                 NewInWorkdir, ModifiedInWorkdir, DeletedFromWorkdir,
                 RenamedInWorkdir, Ignored, Conflicted, Unaltered, ...),
                 HeadToIndexRenameDetails, IndexToWorkDirRenameDetails
    StatusOptions: Show, IncludeUntracked, IncludeIgnored, IncludeUnaltered,
                   RecurseUntrackedDirs, DetectRenamesInIndex,
                   DetectRenamesInWorkDir, PathSpec, ExcludeSubmodules

Diff - repo.Diff
----------------
    T Compare<T>(Tree oldTree, Tree newTree)             where T : IDiffResult
    T Compare<T>(Tree oldTree, Tree newTree, IEnumerable<string> paths)
    T Compare<T>(Tree oldTree, DiffTargets diffTargets)  // tree vs index
                                                         //   and/or workdir
    T Compare<T>()                                       // index vs workdir
    T Compare<T>(IEnumerable<string> paths, bool includeUntracked)
        (+ CompareOptions / ExplicitPathsOptions overloads)
    ContentChanges Compare(Blob oldBlob, Blob newBlob)
    T is one of:
        TreeChanges  Added, Deleted, Modified, Renamed, Copied, TypeChanged,
                     Conflicted, Unmodified, Count; enumerable of
                     TreeEntryChanges (Path, OldPath, Status (ChangeKind),
                     Mode, OldMode, Oid, OldOid, Exists, OldExists)
        Patch        Content (unified diff text), LinesAdded, LinesDeleted,
                     this[string path] -> PatchEntryChanges
        PatchStats   TotalLinesAdded, TotalLinesDeleted,
                     this[string path] -> ContentChangeStats
    TreeChanges, Patch and PatchStats are IDisposable - dispose them.
    DiffTargets: Index, WorkingDirectory ([Flags], combine with |).
    Pass null as oldTree to diff against an empty tree (initial commit).

Network - repo.Network
----------------------
    RemoteCollection Remotes
        Remote this[string name]         // null when absent
        Remote Add(string name, string url)
        Remote Add(string name, string url, string fetchRefSpec)
        void   Remove(string name)
        Remote Rename(string name, string newName)
        void   Update(string remote, params Action<RemoteUpdater>[] actions)
        Remote: Name, Url, PushUrl, FetchRefSpecs, PushRefSpecs, RefSpecs,
                TagFetchMode, AutomaticallyPruneOnFetch (IDisposable)
        RefSpec: Specification, Source, Destination, Direction, ForceUpdate
    IEnumerable<Reference> ListReferences(Remote remote)
    IEnumerable<Reference> ListReferences(string url)
        (+ CredentialsHandler / ProxyOptions overloads)
    void Fetch(string url, IEnumerable<string> refspecs)
        (+ FetchOptions / logMessage overloads)
    void Push(Branch branch)
    void Push(Branch branch, PushOptions pushOptions)
    void Push(IEnumerable<Branch> branches, PushOptions pushOptions)
    void Push(Remote remote, string pushRefSpec)
    void Push(Remote remote, IEnumerable<string> pushRefSpecs,
              PushOptions pushOptions)
        (+ further overloads)
    Push(Branch) needs the branch to track an upstream - set it first with
    repo.Branches.Update(branch, b => b.Remote = "origin",
                         b => b.UpstreamBranch = branch.CanonicalName).

Configuration - repo.Config (IDisposable when built by hand)
------------------------------------------------------------
    T    GetValueOrDefault<T>(string key)
    T    GetValueOrDefault<T>(string key, T defaultValue)
         (+ ConfigurationLevel and key-parts overloads)
    ConfigurationEntry<T> Get<T>(string key)    // null when absent
    void Set<T>(string key, T value)
    void Set<T>(string key, T value, ConfigurationLevel level)
    void Add(string key, string value), bool Unset(string key),
    bool UnsetAll(string key)
    IEnumerable<ConfigurationEntry<string>> Find(string regexp)
    Signature BuildSignature(DateTimeOffset now)  // from user.name /
                                                  //   user.email; null if
                                                  //   not configured
    ConfigurationLevel: Local, Global, Xdg, System, ProgramData, Worktree
    Configuration.BuildFrom(string repositoryConfigurationFileLocation)
        (+ overloads) - a Configuration without a Repository

Stashes - repo.Stashes (StashCollection, enumerable of Stash)
-------------------------------------------------------------
    Stash Add(Signature stasher)
    Stash Add(Signature stasher, string message)
    Stash Add(Signature stasher, string message, StashModifiers options)
        returns null when there is nothing to stash.
        StashModifiers: Default, KeepIndex, IncludeUntracked, IncludeIgnored
    Stash this[int index]                // 0 = most recent
    StashApplyStatus Apply(int index) / Apply(int index, StashApplyOptions)
    StashApplyStatus Pop(int index)   / Pop(int index, StashApplyOptions)
    void Remove(int index)
    Stash: Message, WorkTree, Base, Index, Untracked (all Commit)

Worktrees - repo.Worktrees (WorktreeCollection, enumerable of Worktree)
-----------------------------------------------------------------------
    Worktree this[string name]
    Worktree Add(string committishOrBranchSpec, string name, string path,
                 bool isLocked)
    Worktree Add(string name, string path, bool isLocked)
    bool Prune(Worktree worktree) / Prune(Worktree worktree, bool ifLocked)
    Worktree: Name, IsLocked, LockReason, Lock(string reason), Unlock(),
              WorktreeRepository (a new Repository - dispose it)

Submodules - repo.Submodules (SubmoduleCollection, enumerable of Submodule)
--------------------------------------------------------------------------
    Submodule this[string name]
    void Init(string name, bool overwrite)
    void Update(string name, SubmoduleUpdateOptions options)
    Submodule: Name, Path, Url, HeadCommitId, IndexCommitId, WorkDirCommitId,
               RetrieveStatus(), IgnoreRule, UpdateRule,
               FetchRecurseSubmodulesRule
    To clone with submodules set CloneOptions.RecurseSubmodules = true.

Notes - repo.Notes (NoteCollection, enumerable of Note)
-------------------------------------------------------
    Note Add(ObjectId targetId, string message, Signature author,
             Signature committer, string @namespace)
    void Remove(ObjectId targetId, Signature author, Signature committer,
                string @namespace)
    IEnumerable<Note> this[ObjectId id], DefaultNamespace, Namespaces
    Note: Message, Namespace, TargetObjectId, BlobId

Rebase - repo.Rebase
--------------------
    RebaseResult Start(Branch branch, Branch upstream, Branch onto,
                       Identity committer, RebaseOptions options)
    RebaseResult Continue(Identity committer, RebaseOptions options)
    void Abort() / Abort(RebaseOptions options)
    long GetCurrentStepIndex(), long GetTotalStepCount(),
    RebaseStepInfo GetCurrentStepInfo(), GetStepInfo(long stepIndex)
    RebaseResult: Status (RebaseStatus), CompletedStepCount, TotalStepCount,
                  CurrentStepInfo
    Rebase takes an Identity (name + email), not a Signature.

ObjectDatabase - repo.ObjectDatabase
------------------------------------
    bool Contains(ObjectId id), GitObjectMetadata RetrieveObjectMetadata(id)
    Blob CreateBlob(string path) / CreateBlob(Stream stream, ...)
    Tree CreateTree(TreeDefinition definition) / CreateTree(Index index)
    Commit CreateCommit(Signature author, Signature committer,
                        string message, Tree tree,
                        IEnumerable<Commit> parents, bool prettifyMessage)
    ObjectId CreateCommitWithSignature(string commitContent,
                                       string signature)
    TagAnnotation CreateTagAnnotation(string name, GitObject target,
                                      Signature tagger, string message)
    Commit FindMergeBase(Commit first, Commit second)
    bool   CanMergeWithoutConflict(Commit one, Commit another)
    MergeTreeResult MergeCommits(Commit ours, Commit theirs,
                                 MergeTreeOptions options)  // no workdir
    HistoryDivergence CalculateHistoryDivergence(Commit one, Commit another)
    string ShortenObjectId(GitObject gitObject)
    void Archive(Commit commit, string archivePath)            // tar
    PackBuilderResults Pack(PackBuilderOptions options)
    void AddBackend(OdbBackend backend, int priority)
    ObjectId Write<T>(byte[] data) where T : GitObject
    TreeDefinition.From(commit / tree), .Add(...), .Remove(...) builds trees
    in memory for CreateTree.

Signature and Identity
----------------------
    new Signature(string name, string email, DateTimeOffset when)
    new Signature(Identity identity, DateTimeOffset when)
    Signature: Name, Email, When
    new Identity(string name, string email)
    Commits, merges, tags, stashes, notes, pulls, reverts and cherry-picks
    all need an explicit Signature. The library never invents one; read it
    from configuration with repo.Config.BuildSignature(DateTimeOffset.Now)
    (null if user.name / user.email are not set) or construct it.

GlobalSettings (static)
-----------------------
    Version Version                      // InformationalVersion,
                                         //   Features (BuiltInFeatures:
                                         //   Threads, Https, Ssh, NSec),
                                         //   LibGit2CommitSha
    string NativeLibraryPath             // see NATIVE LIBRARY LOCATION
    LogConfiguration LogConfiguration    // new LogConfiguration(LogLevel,
                                         //   LogHandler) - native trace log
    IEnumerable<string> GetConfigSearchPaths(ConfigurationLevel level)
    void SetConfigSearchPaths(ConfigurationLevel level,
                              params string[] paths)
    FilterRegistration RegisterFilter(Filter filter[, int priority]),
    void DeregisterFilter(FilterRegistration registration)
    SmartSubtransportRegistration<T> RegisterSmartSubtransport<T>(
        string scheme)                   // custom transports
    void SetUserAgent(string userAgent), string GetUserAgent()
    void SetOwnerValidation(bool enabled), bool GetOwnerValidation()
    void SetStrictHashVerification(bool), SetEnableCaching(bool),
         SetEnableOfsDelta(bool), SetEnableStrictObjectCreation(bool),
         SetExtensions(params string[]), string[] GetExtensions()

NATIVE LIBRARY LOCATION: normally nothing is needed - the package's
runtimes/<rid>/native/ asset is found by the .NET host. To load the native
library from a folder of your own, set GlobalSettings.NativeLibraryPath to
that FOLDER before any other call into the library (setting it afterwards
throws LibGit2SharpException). The resolver looks in that folder for the
native file exactly as shipped in the package's runtimes/<rid>/native/
folder: the .dll on Windows, the lib<name>.so on Linux and Android, the
lib<name>.dylib on macOS (a copy without the "lib" prefix is accepted too).
So copy the package's binary into your folder unchanged. If the file is not
found there, the normal .NET probing is tried next.

Exceptions
----------
    LibGit2SharpException                base of everything this library
                                         throws (derives from Exception)
      NativeException (abstract)         errors reported by libgit2
        AmbiguousSpecificationException  BareRepositoryException
        CheckoutConflictException        InvalidSpecificationException
        LockedFileException              NameConflictException
        NonFastForwardException          NotFoundException
          MergeFetchHeadNotFoundException
        PeelException                    UnmergedIndexEntriesException
        UserCancelledException           (a callback returned false)
      EmptyCommitException               nothing to commit
      EntryExistsException               RecurseSubmodulesException
      RemoveFromIndexException           RepositoryNotFoundException
      UnbornBranchException              UnmatchedPathException
The base class keeps its upstream name: catch LibGit2SharpException, there
is no "LibGit2Exception". The cancellation exception is spelled
UserCancelledException (double l).

Callbacks and handlers (namespace CodeBrix.LibGit2.Handlers)
------------------------------------------------------------
    Credentials CredentialsHandler(string url, string usernameFromUrl,
                                   SupportedCredentialTypes types)
    bool TransferProgressHandler(TransferProgress progress)
         TransferProgress: TotalObjects, ReceivedObjects, IndexedObjects,
                           ReceivedBytes
    bool ProgressHandler(string serverProgressOutput)
    bool UpdateTipsHandler(string referenceName, ObjectId oldId,
                           ObjectId newId)
    bool CertificateCheckHandler(Certificate certificate, bool valid,
                                 string host)
    void CheckoutProgressHandler(string path, int completedSteps,
                                 int totalSteps)
    bool CheckoutNotifyHandler(string path, CheckoutNotifyFlags flags)
    bool PushTransferProgressHandler(int current, int total, long bytes)
    void PushStatusErrorHandler(PushStatusError pushStatusErrors)
    bool PrePushHandler(IEnumerable<PushUpdate> updates)
    void LogHandler(LogLevel level, string message)
    (+ PackBuilderProgressHandler, StashApplyProgressHandler,
       RebaseStepStartingHandler, RebaseStepCompletedHandler,
       RepositoryOperationStarting, RepositoryOperationCompleted,
       RemoteRenameFailureHandler, UnmatchedPathHandler)
    A handler returning bool aborts the operation when it returns false.
    Credentials types: UsernamePasswordCredentials { Username, Password },
    SecureUsernamePasswordCredentials (SecureString password),
    DefaultCredentials (the logged-in user, via NTLM / SPNEGO).

Options classes (all plain property bags with a parameterless constructor)
-------------------------------------------------------------------------
    CloneOptions     IsBare, Checkout, BranchName, RecurseSubmodules,
                     OnCheckoutProgress, FetchOptions (read-only property,
                     already created - set its members)
    FetchOptions     TagFetchMode, Prune, Depth (0 = full history),
                     CustomHeaders, CredentialsProvider, CertificateCheck,
                     OnProgress, OnTransferProgress, OnUpdateTips,
                     ProxyOptions
    PushOptions      CredentialsProvider, CertificateCheck,
                     OnPushStatusError, OnPushTransferProgress,
                     OnPackBuilderProgress, OnNegotiationCompletedBeforePush,
                     PackbuilderDegreeOfParallelism, CustomHeaders,
                     ProxyOptions
    PullOptions      FetchOptions, MergeOptions
    CommitOptions    AmendPreviousCommit, AllowEmptyCommit, PrettifyMessage,
                     CommentaryChar
    CheckoutOptions  CheckoutModifiers (None, Force), CheckoutNotifyFlags,
                     OnCheckoutNotify, OnCheckoutProgress
    MergeOptions     FastForwardStrategy (Default, NoFastForward,
                     FastForwardOnly), FindRenames, FailOnConflict,
                     MergeFileFavor, IgnoreWhitespaceChange, + checkout
                     options
    MergeResult      Status (MergeStatus: UpToDate, FastForward,
                     NonFastForward, Conflicts), Commit
    ResetMode        Soft, Mixed, Hard
    DescribeOptions  Strategy (DescribeStrategy: Default, Tags, All),
                     UseCommitIdAsFallback, MinimumCommitIdAbbreviatedSize,
                     AlwaysRenderLongFormat, OnlyFollowFirstParent
    Also: StatusOptions, StageOptions, ExplicitPathsOptions, CompareOptions,
          BlameOptions, RebaseOptions, CherryPickOptions, RevertOptions,
          StashApplyOptions, SubmoduleUpdateOptions, MergeTreeOptions,
          RepositoryOptions, ProxyOptions, FilteringOptions.


================================================================================

COMPLETE EXAMPLES
=================
Every example below was compiled and run against this package's assembly.
The usings shown are the complete set each example needs.

Example 1: Open a repository and walk its history
--------------------------------------------------
    using System;
    using System.Linq;
    using CodeBrix.LibGit2;

    using (var repo = new Repository("/path/to/repo"))
    {
        var filter = new CommitFilter
        {
            IncludeReachableFrom = repo.Head,
            SortBy = CommitSortStrategies.Topological
                     | CommitSortStrategies.Time
        };

        foreach (Commit c in repo.Commits.QueryBy(filter).Take(20))
        {
            Console.WriteLine($"{c.Id.ToString(7)} " +
                              $"{c.Author.When:yyyy-MM-dd} " +
                              $"{c.Author.Name}: {c.MessageShort}");
        }

        // history of one file
        foreach (LogEntry entry in repo.Commits.QueryBy("hello.txt"))
        {
            Console.WriteLine($"{entry.Commit.Id.ToString(7)} " +
                              $"touched {entry.Path}");
        }
    }


Example 2: Create a repository, check status, stage and commit
---------------------------------------------------------------
    using System;
    using System.IO;
    using CodeBrix.LibGit2;

    string gitDir = Repository.Init("/path/to/new-repo");

    using (var repo = new Repository(gitDir))
    {
        string wd = repo.Info.WorkingDirectory;
        File.WriteAllText(Path.Combine(wd, "a.txt"), "one\n");
        File.WriteAllText(Path.Combine(wd, "b.txt"), "two\n");

        RepositoryStatus status = repo.RetrieveStatus();
        foreach (StatusEntry e in status.Untracked)
        {
            Console.WriteLine($"untracked: {e.FilePath}");
        }

        Commands.Stage(repo, "*");

        var sig = new Signature("A. U. Thor", "thor@example.com",
                                DateTimeOffset.Now);
        Commit first = repo.Commit("First commit", sig, sig);

        File.AppendAllText(Path.Combine(wd, "a.txt"), "more\n");
        Commands.Stage(repo, "a.txt");
        Commit second = repo.Commit("Second commit", sig, sig);

        Console.WriteLine($"{first.Id.ToString(7)} -> " +
                          $"{second.Id.ToString(7)}, " +
                          $"dirty: {repo.RetrieveStatus().IsDirty}");
    }


Example 3: Branch, checkout and merge
-------------------------------------
    using System;
    using System.IO;
    using CodeBrix.LibGit2;

    using (var repo = new Repository("/path/to/repo"))
    {
        var sig = new Signature("A. U. Thor", "thor@example.com",
                                DateTimeOffset.Now);
        string mainName = repo.Head.FriendlyName;

        Branch feature = repo.CreateBranch("feature");
        Commands.Checkout(repo, feature);

        File.WriteAllText(
            Path.Combine(repo.Info.WorkingDirectory, "feature.txt"),
            "feature work\n");
        Commands.Stage(repo, "feature.txt");
        repo.Commit("Feature work", sig, sig);

        Commands.Checkout(repo, repo.Branches[mainName]);
        MergeResult result = repo.Merge(repo.Branches["feature"], sig,
                                        new MergeOptions());

        if (result.Status == MergeStatus.Conflicts)
        {
            foreach (Conflict conflict in repo.Index.Conflicts)
            {
                Console.WriteLine($"conflict: {conflict.Ours?.Path}");
            }
        }
        else
        {
            Console.WriteLine($"merge: {result.Status}, " +
                              $"HEAD {repo.Head.Tip.Id.ToString(7)}");
        }

        repo.Branches.Remove("feature");
    }


Example 4: Clone with progress and credentials
----------------------------------------------
    using System;
    using CodeBrix.LibGit2;

    var options = new CloneOptions { RecurseSubmodules = false };

    options.FetchOptions.OnTransferProgress = progress =>
    {
        Console.WriteLine($"{progress.ReceivedObjects}/" +
                          $"{progress.TotalObjects} objects");
        return true;                       // false cancels the clone
    };

    // called only if the server asks for credentials (https)
    options.FetchOptions.CredentialsProvider = (url, usernameFromUrl, types) =>
        new UsernamePasswordCredentials
        {
            Username = "my-user",
            Password = Environment.GetEnvironmentVariable("GIT_TOKEN") ?? ""
        };

    string gitDir = Repository.Clone(
        "https://example.com/owner/project.git", "/path/to/clone", options);
    Console.WriteLine($"cloned into {gitDir}");

For a shallow clone set options.FetchOptions.Depth = 1. For a bare clone set
options.IsBare = true. To check out a branch other than the remote's default,
set options.BranchName.


Example 5: Add a remote, push, fetch and pull
---------------------------------------------
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeBrix.LibGit2;

    using (var repo = new Repository("/path/to/repo"))
    {
        Remote origin = repo.Network.Remotes["origin"]
            ?? repo.Network.Remotes.Add("origin",
                   "https://example.com/owner/project.git");

        // Push(Branch) needs an upstream; set it once
        Branch local = repo.Head;
        repo.Branches.Update(local,
            b => b.Remote = origin.Name,
            b => b.UpstreamBranch = local.CanonicalName);

        var pushOptions = new PushOptions
        {
            CredentialsProvider = (url, user, types) =>
                new UsernamePasswordCredentials
                {
                    Username = "my-user",
                    Password = Environment.GetEnvironmentVariable(
                                   "GIT_TOKEN") ?? ""
                }
        };
        repo.Network.Push(repo.Head, pushOptions);

        var fetchOptions = new FetchOptions { TagFetchMode = TagFetchMode.All };
        IEnumerable<string> refSpecs =
            origin.FetchRefSpecs.Select(r => r.Specification);
        Commands.Fetch(repo, origin.Name, refSpecs, fetchOptions, null);

        var sig = new Signature("A. U. Thor", "thor@example.com",
                                DateTimeOffset.Now);
        MergeResult pulled = Commands.Pull(repo, sig, new PullOptions());
        Console.WriteLine($"pull: {pulled.Status}");

        foreach (Reference r in repo.Network.ListReferences(origin))
        {
            Console.WriteLine($"remote ref {r.CanonicalName}");
        }
    }


Example 6: Diff two commits and the working directory
-----------------------------------------------------
    using System;
    using System.Linq;
    using CodeBrix.LibGit2;

    using (var repo = new Repository("/path/to/repo"))
    {
        Commit newer = repo.Head.Tip;
        Commit older = newer.Parents.First();

        using (TreeChanges changes =
                   repo.Diff.Compare<TreeChanges>(older.Tree, newer.Tree))
        {
            foreach (TreeEntryChanges change in changes)
            {
                Console.WriteLine($"{change.Status} {change.Path}");
            }
        }

        using (Patch patch = repo.Diff.Compare<Patch>(older.Tree, newer.Tree))
        {
            Console.WriteLine($"+{patch.LinesAdded} -{patch.LinesDeleted}");
            Console.WriteLine(patch.Content);       // unified diff text
        }

        // HEAD against index + working directory (like `git diff HEAD`)
        using (var wd = repo.Diff.Compare<TreeChanges>(repo.Head.Tip.Tree,
                   DiffTargets.Index | DiffTargets.WorkingDirectory))
        {
            Console.WriteLine($"{wd.Count} files changed since HEAD");
        }
    }


Example 7: Tags, stash, reset, blame, describe, configuration
--------------------------------------------------------------
    using System;
    using CodeBrix.LibGit2;

    using (var repo = new Repository("/path/to/repo"))
    {
        var sig = new Signature("A. U. Thor", "thor@example.com",
                                DateTimeOffset.Now);

        Tag lightweight = repo.Tags.Add("nightly", repo.Head.Tip);
        Tag annotated = repo.ApplyTag("release", repo.Head.Tip.Sha, sig,
                                      "Release");
        Console.WriteLine($"{lightweight.FriendlyName} " +
                          $"{annotated.IsAnnotated}");

        Stash stash = repo.Stashes.Add(sig, "work in progress",
                                       StashModifiers.IncludeUntracked);
        if (stash != null)                     // null = nothing to stash
        {
            repo.Stashes.Pop(0);
        }

        repo.Reset(ResetMode.Hard);            // discard working changes

        foreach (BlameHunk hunk in repo.Blame("hello.txt"))
        {
            Console.WriteLine($"{hunk.FinalCommit.Id.ToString(7)} lines " +
                              $"{hunk.FinalStartLineNumber}+{hunk.LineCount}");
        }

        string described = repo.Describe(repo.Head.Tip,
            new DescribeOptions { Strategy = DescribeStrategy.Tags });
        Console.WriteLine($"describe: {described}");

        repo.Config.Set("user.name", "A. U. Thor");
        repo.Config.Set("user.email", "thor@example.com");
        Signature fromConfig = repo.Config.BuildSignature(DateTimeOffset.Now);
        string name = repo.Config.GetValueOrDefault<string>("user.name");

        GitObject parent = repo.Lookup("HEAD~1");
        Commit tip = repo.Lookup<Commit>(repo.Head.Tip.Id);
        Console.WriteLine($"{fromConfig.Name} {name} {parent.Sha} " +
                          $"{tip.MessageShort}");
    }


Example 8: Handle "not a repository" and other errors
-----------------------------------------------------
    using System;
    using CodeBrix.LibGit2;

    string path = "/path/to/maybe-a-repo";

    if (!Repository.IsValid(path))
    {
        string found = Repository.Discover(path);    // search parent folders
        Console.WriteLine(found ?? "not inside a repository");
    }

    try
    {
        using var repo = new Repository(path);
        // ...
    }
    catch (RepositoryNotFoundException ex)
    {
        Console.WriteLine($"no repository: {ex.Message}");
    }
    catch (LibGit2SharpException ex)                 // every other git error
    {
        Console.WriteLine($"git error: {ex.Message}");
    }


Example 9: Check which native features are available
----------------------------------------------------
    using System;
    using CodeBrix.LibGit2;

    var v = GlobalSettings.Version;     // CodeBrix.LibGit2.Version
    Console.WriteLine(v);               // version (arch - features)
    bool https = v.Features.HasFlag(BuiltInFeatures.Https);
    bool ssh = v.Features.HasFlag(BuiltInFeatures.Ssh);  // false on Android
    Console.WriteLine($"https={https} ssh={ssh}");


================================================================================

MINIMUM VIABLE PROJECT TEMPLATE
===============================
A console application that prints the last ten commits of a repository.

GitLog.csproj:
    <Project Sdk="Microsoft.NET.Sdk">
      <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net10.0</TargetFramework>
        <Nullable>disable</Nullable>
      </PropertyGroup>
      <ItemGroup>
        <PackageReference Include="CodeBrix.LibGit2.MitLicenseForever" />
      </ItemGroup>
    </Project>

(Add the package with `dotnet add package CodeBrix.LibGit2.MitLicenseForever`
so the current version is written into the csproj. No other package is
needed on any supported platform.)

Program.cs:
    using System;
    using System.Linq;
    using CodeBrix.LibGit2;

    string path = args.Length > 0 ? args[0] : ".";

    string gitDir = Repository.Discover(path);
    if (gitDir == null)
    {
        Console.Error.WriteLine($"not inside a Git repository: {path}");
        return 1;
    }

    try
    {
        using var repo = new Repository(gitDir);
        if (repo.Info.IsHeadUnborn)
        {
            Console.WriteLine("no commits yet");
            return 0;
        }

        foreach (Commit c in repo.Commits.Take(10))
        {
            Console.WriteLine($"{c.Id.ToString(7)} {c.Author.Name}: " +
                              c.MessageShort);
        }
        return 0;
    }
    catch (LibGit2SharpException ex)
    {
        Console.Error.WriteLine($"git error: {ex.Message}");
        return 2;
    }


================================================================================

PERFORMANCE TIPS
================

1.  Dispose every Repository (using / using var). It owns native handles,
    caches and open pack files; until it is disposed those files stay open,
    and on Windows the repository folder cannot be deleted.

2.  Open a Repository once and reuse it for a batch of operations instead
    of constructing one per call - opening reads config, refs and the index.
    Do NOT use one instance from several threads at the same time; libgit2
    objects are not safe for simultaneous use. Give each thread its own
    Repository, or serialize access with your own lock.

3.  Look objects up directly. repo.Lookup<Commit>(sha),
    repo.Branches["name"], repo.Tags["name"] and repo.Refs["refs/..."] are
    single native calls; enumerating a collection and filtering with LINQ
    loads every entry.

4.  Put the filtering into CommitFilter instead of LINQ over the whole log:
    IncludeReachableFrom / ExcludeReachableFrom limit the walk natively
    (ExcludeReachableFrom = "origin/main" gives "commits not yet pushed"),
    FirstParentOnly skips merged-in history. The default SortBy is Time;
    use CommitSortStrategies.None when the order does not matter. Combine
    with Take(n) - the log is lazy.

5.  Dispose TreeChanges, Patch and PatchStats. Ask for the narrowest diff
    result: TreeChanges (no content) is much cheaper than Patch (full
    text). Pass a paths list to Compare<T> when you only care about some
    files.

6.  For status of one file use repo.RetrieveStatus(filePath) rather than
    scanning the full RepositoryStatus. Pass StatusOptions with
    IncludeUntracked = false / RecurseUntrackedDirs = false on very large
    working trees when untracked files do not matter.

7.  For large clones, FetchOptions.Depth = 1 (shallow) and
    CloneOptions.Checkout = false (no files written to the working tree)
    cut time and disk use.

8.  Read blob content with Blob.GetContentStream() for big files instead of
    GetContentText(), which builds a whole string.


================================================================================

COMMON PITFALLS TO AVOID
========================

1.  DO NOT confuse the NuGet package id with the namespace.
      Package:   CodeBrix.LibGit2.MitLicenseForever
      Namespace: CodeBrix.LibGit2
    And do not write `using LibGit2Sharp;` - that namespace does not exist
    here. The exception base class, however, IS still called
    LibGit2SharpException.

2.  NOT DISPOSING. `new Repository(path)` without using/Dispose leaks
    native memory and file handles; tests that delete temp folders fail on
    Windows. The same goes for TreeChanges, Patch, PatchStats, Remote and
    Configuration objects you build yourself.

3.  PATH SEPARATORS AND ROOTS. Paths that come back from status and diff
    (StatusEntry.FilePath, TreeEntryChanges.Path) are relative to the
    working directory and use '/' on every OS. Look entries up with '/'
    (status["src/a.cs"]) and build full paths with
    Path.Combine(repo.Info.WorkingDirectory, entry.FilePath). Paths passed
    to Commands.Stage / Unstage / Remove may be relative or absolute.
    repo.Info.Path and repo.Info.WorkingDirectory END WITH a separator, and
    Repository.Init / Clone return the .git directory, not the working
    directory.

4.  BARE REPOSITORIES have no working directory and no index:
    repo.Info.WorkingDirectory is null, repo.Index throws
    BareRepositoryException, and Commands.Stage / Checkout / status cannot
    be used. Build commits with ObjectDatabase.CreateTree(TreeDefinition) +
    ObjectDatabase.CreateCommit and move the branch with
    repo.Refs.UpdateTarget. Check repo.Info.IsBare first.

5.  DETACHED AND UNBORN HEAD. After Commands.Checkout(repo, someCommit)
    HEAD is detached: repo.Info.IsHeadDetached is true and repo.Head is a
    pseudo-branch named "(no branch)" - do not push it or use its name as a
    branch. In a fresh repository with no commits repo.Info.IsHeadUnborn is
    true and repo.Head.Tip is null; guard before dereferencing Tip.

6.  IDENTITY IS REQUIRED. Every commit, merge, tag annotation, stash,
    pull, revert and cherry-pick takes an explicit Signature; there is no
    implicit "current user". repo.Config.BuildSignature(DateTimeOffset.Now)
    returns null when user.name / user.email are not configured (common on
    build servers) - check for null or construct a Signature yourself.
    Rebase takes an Identity instead of a Signature.

7.  EMPTY COMMITS. A commit whose tree is identical to its parent's throws
    EmptyCommitException ("No changes; nothing to commit."). Stage
    something first, or pass new CommitOptions { AllowEmptyCommit = true }.
    (The very first commit of a repository is not checked.)

8.  DEFAULT BRANCH NAME. Repository.Init creates whatever init.defaultBranch
    says, which is "master" when nothing is configured. Never hard-code
    "main"; read repo.Head.FriendlyName.

9.  PUSH NEEDS AN UPSTREAM. Network.Push(Branch) throws
    LibGit2SharpException when the branch does not track a remote branch.
    Set it with repo.Branches.Update(branch, b => b.Remote = "origin",
    b => b.UpstreamBranch = branch.CanonicalName), or use
    Push(Remote, "refs/heads/x:refs/heads/x"). A non-fast-forward push
    throws NonFastForwardException; other per-reference rejections from the
    server are reported through PushOptions.OnPushStatusError - set it.

10. NETWORK IN TESTS. Clone, Fetch, Pull, Push and ListReferences against
    real hosts need network access and credentials. In unit tests, clone
    from a local path or file:// URL (a bare repository created with
    Repository.Init(path, true) works as a "server") so the tests never
    leave the machine.

11. HTTPS ON LINUX NEEDS OPENSSL ON THE MACHINE. The Linux binaries load
    the system libssl/libcrypto at run time. Minimal container images
    often lack them: HTTPS then fails with "could not load ssl libraries"
    while local work succeeds. Install the distribution's OpenSSL runtime
    library package in the image.

12. ANDROID HAS NO SSH. ssh:// and git@host:path URLs fail on Android; use
    https:// there. On every other platform ssh:// works only if an `ssh`
    program is installed and can authenticate without a prompt (keys or
    ssh-agent) - CredentialsProvider is not used for SSH.

13. GlobalSettings.NativeLibraryPath must be set before the first call into
    the library; setting it after the native library has loaded throws.
    The folder must hold the binary under its shipped name - see NATIVE
    LIBRARY LOCATION.

14. UNSUPPORTED RUNTIMES. 32-bit Windows, 32-bit ARM Linux and musl-based
    Linux (Alpine) have no native binary in this package; the first call
    throws a TypeInitializationException wrapping a DllNotFoundException.

15. Version NAME CLASH. `Version v = GlobalSettings.Version;` is ambiguous
    with System.Version (CS0104). Use var or CodeBrix.LibGit2.Version.

16. BLAME LINE NUMBERS are zero-based: BlameHunk.FinalStartLineNumber is 0
    for the first line of the file.

17. INTERACTIVE REBASE is not available: only pick steps are applied;
    Squash, Fixup, Reword and Edit steps throw LibGit2SharpException.


================================================================================

WHAT THIS PACKAGE DOES NOT DO
=============================
Do NOT reach for this library for:

  - Running the git command-line program or reproducing every git command.
    It is a library over libgit2; there is no `gc`, `fsck`, `bisect`,
    `filter-repo`, `send-email` or interactive `add -p`.
  - Git hooks. pre-commit, commit-msg, post-checkout and the rest are NOT
    executed by any operation.
  - Git LFS. LFS pointer files are ordinary blobs here; nothing downloads
    or uploads LFS content. (A custom Filter registered through
    GlobalSettings.RegisterFilter is the extension point if you build it.)
  - Credential helpers and stored passwords. git-credential-manager,
    credential.helper and keychains are not consulted for HTTPS; supply
    credentials through CredentialsProvider.
  - SSH key handling in-process. SSH is delegated to the system ssh
    program, and there is no SSH transport at all on Android.
  - Commit signing with GPG or SSH keys. It can WRITE a commit you have
    signed yourself (ObjectDatabase.CreateCommitWithSignature) and READ a
    signature (Commit.ExtractSignature), but it does not call gpg.
  - Interactive rebase (squash / fixup / reword / edit).
  - Hosting-service APIs (pull requests, issues, releases) - it speaks
    the Git protocol only.
  - Platforms outside the nine shipped runtime identifiers (see
    INSTALLATION), and iOS.

CodeBrix.LibGit2 IS for: creating, cloning and opening repositories;
reading history, trees, blobs and diffs; staging and committing; branching,
tagging, merging, rebasing, stashing, resetting and checking out; fetching,
pulling and pushing over HTTPS, SSH (not on Android) and local paths; and
working with configuration, submodules, worktrees, notes and the object
database - all in-process, with no Git installation.


================================================================================

WORKING EXAMPLES ON GITHUB
==========================
The test project is the executable documentation for this package. Browse it
at:

    https://github.com/ellisnet/CodeBrix.LibGit2/tree/main/tests/CodeBrix.LibGit2.Tests

Feature-to-test-file map (all under
https://github.com/ellisnet/CodeBrix.LibGit2/blob/main/tests/CodeBrix.LibGit2.Tests/):

  Opening, init, discovery, repository information:
    RepositoryTests.cs, RepositoryOptionsTests.cs
  Cloning (progress, credentials, shallow, bare, custom headers):
    CloneTests.cs
  Fetch, pull, listing remote references:
    FetchTests.cs, NetworkTests.cs
  Push:
    PushTests.cs
  Remotes and refspecs:
    RemoteTests.cs, RefSpecTests.cs
  Commits and the commit log, per-file history:
    CommitTests.cs, CommitAncestorTests.cs, LogTests.cs,
    FileHistoryTests.cs
  Staging, unstaging, removing, moving:
    StageTests.cs, UnstageTests.cs, RemoveTests.cs, IndexTests.cs
  Status, ignore rules, attributes, clean:
    StatusTests.cs, IgnoreTests.cs, AttributesTests.cs, CleanTests.cs
  Branches:
    BranchTests.cs
  Tags:
    TagTests.cs
  References and reflog:
    ReferenceTests.cs, ReflogTests.cs
  Checkout:
    CheckoutTests.cs
  Reset:
    ResetHeadTests.cs, ResetIndexTests.cs
  Merge and conflicts:
    MergeTests.cs, ConflictTests.cs, CurrentOperationTests.cs
  Rebase, cherry-pick, revert:
    RebaseTests.cs, CherryPickTests.cs, RevertTests.cs
  Stash:
    StashTests.cs
  Diff (tree to tree, tree to index/workdir, blobs, patches, stats):
    DiffTreeToTreeTests.cs, DiffTreeToTargetTests.cs,
    DiffWorkdirToIndexTests.cs, DiffBlobToBlobTests.cs,
    PatchEntryChangesTests.cs, PatchStatsTests.cs
  Blame and describe:
    BlameTests.cs, DescribeTests.cs
  Blobs, trees, building trees in memory:
    BlobTests.cs, TreeTests.cs, TreeDefinitionTests.cs
  Object database, pack building, custom backends, archives:
    ObjectDatabaseTests.cs, PackBuilderTests.cs, OdbBackendTests.cs,
    ArchiveTests.cs, ArchiveTarTests.cs
  Configuration:
    ConfigurationTests.cs
  Submodules and worktrees:
    SubmoduleTests.cs, WorktreeTests.cs
  Notes:
    NoteTests.cs
  History rewriting:
    FilterBranchTests.cs
  Content filters:
    FilterTests.cs, FilterSubstitutionCipherTests.cs
  Signatures, object ids, equality:
    SignatureTests.cs, ObjectIdTests.cs, EqualityTests.cs
  GlobalSettings, version, native library path:
    GlobalSettingsTests.cs
  Mocking the API (public members are virtual):
    MockingTests.cs

To read a file's source directly, fetch the raw URL, for example:
    https://raw.githubusercontent.com/ellisnet/CodeBrix.LibGit2/main/tests/CodeBrix.LibGit2.Tests/CommitTests.cs


================================================================================

QUICK REFERENCE CARD
====================

--- Install ---
dotnet add package CodeBrix.LibGit2.MitLicenseForever

--- Namespaces ---
using CodeBrix.LibGit2;            // everything
using CodeBrix.LibGit2.Handlers;   // delegate types, only when you name them

--- Open / create ---
Open:               using var repo = new Repository(path);
Init:               string gitDir = Repository.Init(path);
Init bare:          Repository.Init(path, true)
Clone:              string gitDir = Repository.Clone(url, path[, options]);
Is a repo?:         Repository.IsValid(path)
Find upwards:       Repository.Discover(path)          // null if none

--- Inspect ---
Current branch:     repo.Head.FriendlyName
Tip commit:         repo.Head.Tip                       // null if unborn
History:            repo.Commits / repo.Commits.QueryBy(new CommitFilter {...})
File history:       repo.Commits.QueryBy("path/file.txt")
Lookup:             repo.Lookup<Commit>("HEAD~1")
Status:             repo.RetrieveStatus()  .IsDirty .Modified .Untracked
Info:               repo.Info.WorkingDirectory .IsBare .IsHeadDetached

--- Change ---
Stage:              Commands.Stage(repo, "file.txt")   // "*" = everything
Unstage:            Commands.Unstage(repo, "file.txt")
Remove / move:      Commands.Remove(repo, "f") / Commands.Move(repo, "a", "b")
Signature:          new Signature("Name", "email", DateTimeOffset.Now)
Commit:             repo.Commit("msg", author, committer)
Amend:              repo.Commit("msg", a, c,
                        new CommitOptions { AmendPreviousCommit = true })
Reset:              repo.Reset(ResetMode.Hard[, commit])
Clean:              repo.RemoveUntrackedFiles()

--- Branches and tags ---
Create branch:      repo.CreateBranch("name")
Switch:             Commands.Checkout(repo, repo.Branches["name"])
Delete branch:      repo.Branches.Remove("name")
Rename branch:      repo.Branches.Rename(branch, "new-name")
Track upstream:     repo.Branches.Update(b, u => u.Remote = "origin",
                        u => u.UpstreamBranch = b.CanonicalName)
Lightweight tag:    repo.Tags.Add("name", commit)
Annotated tag:      repo.ApplyTag("name", sha, tagger, "message")

--- Merge / rebase / stash ---
Merge:              repo.Merge(repo.Branches["x"], sig[, new MergeOptions()])
Cherry-pick:        repo.CherryPick(commit, sig)
Revert:             repo.Revert(commit, sig)
Rebase:             repo.Rebase.Start(branch, upstream, onto, identity,
                        new RebaseOptions())
Stash / pop:        repo.Stashes.Add(sig, "msg") / repo.Stashes.Pop(0)

--- Diff / blame ---
Tree to tree:       repo.Diff.Compare<TreeChanges>(oldTree, newTree)
Patch text:         repo.Diff.Compare<Patch>(oldTree, newTree).Content
Against workdir:    repo.Diff.Compare<TreeChanges>(tree,
                        DiffTargets.Index | DiffTargets.WorkingDirectory)
Blame:              repo.Blame("path")
Describe:           repo.Describe(commit)

--- Remote ---
Add remote:         repo.Network.Remotes.Add("origin", url)
Fetch:              Commands.Fetch(repo, "origin", refspecs, fetchOptions, null)
Pull:               Commands.Pull(repo, sig, new PullOptions())
Push:               repo.Network.Push(repo.Head[, pushOptions])
List remote refs:   repo.Network.ListReferences(remote)
Credentials:        options.CredentialsProvider = (url, user, types) =>
                        new UsernamePasswordCredentials
                        { Username = u, Password = p };

--- Config ---
Read:               repo.Config.GetValueOrDefault<string>("user.name")
Write:              repo.Config.Set("user.name", "Name")
Signature:          repo.Config.BuildSignature(DateTimeOffset.Now) // or null

--- Diagnostics ---
Version/features:   GlobalSettings.Version  .Features (Threads|Https|Ssh|NSec)
Trace log:          GlobalSettings.LogConfiguration =
                        new LogConfiguration(LogLevel.Trace, (l, m) => ...)
Errors:             catch (LibGit2SharpException)

--- Cleanup ---
Dispose:            repo.Dispose()   // or a 'using' declaration

Target: .NET 10 or later
License: MIT (native libgit2: GPLv2 with linking exception)


================================================================================
END OF AGENT-README
================================================================================
