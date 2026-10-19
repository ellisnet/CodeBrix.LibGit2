using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CodeBrix.LibGit2.Handlers;
using CodeBrix.LibGit2.Tests.TestHelpers;
using Xunit;

namespace CodeBrix.LibGit2.Tests; //was previously: LibGit2Sharp.Tests;

public class NetworkTests : BaseFixture
{
    [NetworkTheory]
    [InlineData("http://github.com/libgit2/TestGitRepository")]
    [InlineData("https://github.com/libgit2/TestGitRepository")]
    public void CanListRemoteReferences(string url)
    {
        string remoteName = "testRemote";

        string repoPath = InitNewRepository();

        using (var repo = new Repository(repoPath))
        {
            Remote remote = repo.Network.Remotes.Add(remoteName, url);
            IList<Reference> references = repo.Network.ListReferences(remote).ToList();

            foreach (var reference in references)
            {
                // None of those references point to an existing
                // object in this brand new repository
                Assert.Null(reference.ResolveToDirectReference().Target);
            }

            List<Tuple<string, string>> actualRefs = references.
                Select(directRef => new Tuple<string, string>(directRef.CanonicalName, directRef.ResolveToDirectReference()
                    .TargetIdentifier)).ToList();

            Assert.Equal(TestRemoteRefs.ExpectedRemoteRefs.Count, actualRefs.Count);
            Assert.True(references.Single(reference => reference.CanonicalName == "HEAD") is SymbolicReference);
            for (int i = 0; i < TestRemoteRefs.ExpectedRemoteRefs.Count; i++)
            {
                Assert.Equal(TestRemoteRefs.ExpectedRemoteRefs[i].Item2, actualRefs[i].Item2);
                Assert.Equal(TestRemoteRefs.ExpectedRemoteRefs[i].Item1, actualRefs[i].Item1);
            }
        }
    }

    [NetworkTheory]
    [InlineData("http://github.com/libgit2/TestGitRepository")]
    [InlineData("https://github.com/libgit2/TestGitRepository")]
    public void CanListRemoteReferencesFromUrl(string url)
    {
        string repoPath = InitNewRepository();

        using (var repo = new Repository(repoPath))
        {
            IList<Reference> references = repo.Network.ListReferences(url).ToList();

            foreach (var reference in references)
            {
                // None of those references point to an existing
                // object in this brand new repository
                Assert.Null(reference.ResolveToDirectReference().Target);
            }

            List<Tuple<string, string>> actualRefs = references.
                Select(directRef => new Tuple<string, string>(directRef.CanonicalName, directRef.ResolveToDirectReference()
                    .TargetIdentifier)).ToList();

            Assert.Equal(TestRemoteRefs.ExpectedRemoteRefs.Count, actualRefs.Count);
            Assert.True(references.Single(reference => reference.CanonicalName == "HEAD") is SymbolicReference);
            for (int i = 0; i < TestRemoteRefs.ExpectedRemoteRefs.Count; i++)
            {
                Assert.Equal(TestRemoteRefs.ExpectedRemoteRefs[i].Item2, actualRefs[i].Item2);
                Assert.Equal(TestRemoteRefs.ExpectedRemoteRefs[i].Item1, actualRefs[i].Item1);
            }
        }
    }

    [NetworkFact]
    public void CanListRemoteReferenceObjects()
    {
        const string url = "http://github.com/libgit2/TestGitRepository";
        const string remoteName = "origin";

        var scd = BuildSelfCleaningDirectory();

        string clonedRepoPath = Repository.Clone(url, scd.DirectoryPath);

        using (var repo = new Repository(clonedRepoPath))
        {
            Remote remote = repo.Network.Remotes[remoteName];
            IEnumerable<Reference> references = repo.Network.ListReferences(remote).ToList();

            var actualRefs = new List<Tuple<string, string>>();

            foreach (Reference reference in references)
            {
                Assert.NotNull(reference.CanonicalName);

                var directReference = reference.ResolveToDirectReference();

                Assert.NotNull(directReference.Target);
                actualRefs.Add(new Tuple<string, string>(reference.CanonicalName, directReference.Target.Id.Sha));
            }

            Assert.Equal(TestRemoteRefs.ExpectedRemoteRefs.Count, actualRefs.Count);
            Assert.True(references.Single(reference => reference.CanonicalName == "HEAD") is SymbolicReference);
            for (int i = 0; i < TestRemoteRefs.ExpectedRemoteRefs.Count; i++)
            {
                Assert.Equal(TestRemoteRefs.ExpectedRemoteRefs[i].Item1, actualRefs[i].Item1);
                Assert.Equal(TestRemoteRefs.ExpectedRemoteRefs[i].Item2, actualRefs[i].Item2);
            }
        }
    }

    [NetworkFact]
    public void CanListRemoteReferencesWithCredentials()
    {
        InconclusiveIf(() => string.IsNullOrEmpty(Constants.PrivateRepoUrl),
            "Populate Constants.PrivateRepo* to run this test");

        string remoteName = "origin";

        string repoPath = InitNewRepository();

        using (var repo = new Repository(repoPath))
        {
            Remote remote = repo.Network.Remotes.Add(remoteName, Constants.PrivateRepoUrl);

            var references = repo.Network.ListReferences(remote, Constants.PrivateRepoCredentials);

            foreach (var reference in references)
            {
                Assert.NotNull(reference);
            }
        }
    }

    [NetworkTheory]
    [InlineData(FastForwardStrategy.Default)]
    [InlineData(FastForwardStrategy.NoFastForward)]
    public void CanPull(FastForwardStrategy fastForwardStrategy)
    {
        string url = "https://github.com/libgit2/TestGitRepository";

        var scd = BuildSelfCleaningDirectory();
        string clonedRepoPath = Repository.Clone(url, scd.DirectoryPath);

        using (var repo = new Repository(clonedRepoPath))
        {
            repo.Reset(ResetMode.Hard, "HEAD~1");

            Assert.False(repo.RetrieveStatus().Any());
            Assert.Equal(repo.Lookup<Commit>("refs/remotes/origin/master~1"), repo.Head.Tip);

            PullOptions pullOptions = new PullOptions()
            {
                MergeOptions = new MergeOptions()
                {
                    FastForwardStrategy = fastForwardStrategy
                }
            };

            MergeResult mergeResult = Commands.Pull(repo, Constants.Signature, pullOptions);

            if (fastForwardStrategy == FastForwardStrategy.Default || fastForwardStrategy == FastForwardStrategy.FastForwardOnly)
            {
                Assert.Equal(MergeStatus.FastForward, mergeResult.Status);
                Assert.Equal(mergeResult.Commit, repo.Branches["refs/remotes/origin/master"].Tip);
                Assert.Equal(repo.Head.Tip, repo.Branches["refs/remotes/origin/master"].Tip);
            }
            else
            {
                Assert.Equal(MergeStatus.NonFastForward, mergeResult.Status);
            }
        }
    }

    [NetworkFact]
    public void CanPullIntoEmptyRepo()
    {
        string url = "https://github.com/libgit2/TestGitRepository";
        string remoteName = "origin";
        string repoPath = InitNewRepository();

        using (var repo = new Repository(repoPath))
        {
            // Set up remote
            repo.Network.Remotes.Add(remoteName, url);

            // Set up tracking information
            repo.Branches.Update(repo.Head,
                b => b.Remote = remoteName,
                b => b.UpstreamBranch = "refs/heads/master");

            // Pull!
            MergeResult mergeResult = Commands.Pull(repo, Constants.Signature, new PullOptions());

            Assert.Equal(MergeStatus.FastForward, mergeResult.Status);
            Assert.Equal(mergeResult.Commit, repo.Branches["refs/remotes/origin/master"].Tip);
            Assert.Equal(repo.Head.Tip, repo.Branches["refs/remotes/origin/master"].Tip);
        }
    }

    [Fact]
    public void PullWithoutMergeBranchThrows()
    {
        var scd = BuildSelfCleaningDirectory();
        string clonedRepoPath = Repository.Clone(StandardTestRepoPath, scd.DirectoryPath);

        using (var repo = new Repository(clonedRepoPath))
        {
            Branch branch = repo.Branches["master"];

            // Update the Upstream merge branch
            repo.Branches.Update(branch,
                b => b.UpstreamBranch = "refs/heads/another_master");

            bool didPullThrow = false;
            MergeFetchHeadNotFoundException thrownException = null;

            try
            {
                Commands.Pull(repo, Constants.Signature, new PullOptions());
            }
            catch (MergeFetchHeadNotFoundException ex)
            {
                didPullThrow = true;
                thrownException = ex;
            }

            Assert.True(didPullThrow, "Pull did not throw.");
            Assert.True(thrownException.Message.Contains("refs/heads/another_master"), "Exception message did not contain expected reference.");
        }
    }

    [NetworkFact]
    public void CanMergeFetchedRefs()
    {
        string url = "https://github.com/libgit2/TestGitRepository";

        var scd = BuildSelfCleaningDirectory();
        string clonedRepoPath = Repository.Clone(url, scd.DirectoryPath);

        using (var repo = new Repository(clonedRepoPath))
        {
            repo.Reset(ResetMode.Hard, "HEAD~1");

            Assert.False(repo.RetrieveStatus().Any());
            Assert.Equal(repo.Lookup<Commit>("refs/remotes/origin/master~1"), repo.Head.Tip);

            Commands.Fetch(repo, repo.Head.RemoteName, Array.Empty<string>(), null, null);

            MergeOptions mergeOptions = new MergeOptions()
            {
                FastForwardStrategy = FastForwardStrategy.NoFastForward
            };

            MergeResult mergeResult = repo.MergeFetchedRefs(Constants.Signature, mergeOptions);
            Assert.Equal(MergeStatus.NonFastForward, mergeResult.Status);
        }
    }

    [NetworkFact]
    public void CanPruneRefs()
    {
        string url = "https://github.com/libgit2/TestGitRepository";

        var scd = BuildSelfCleaningDirectory();
        string clonedRepoPath = Repository.Clone(url, scd.DirectoryPath);

        var scd2 = BuildSelfCleaningDirectory();
        string clonedRepoPath2 = Repository.Clone(url, scd2.DirectoryPath);

        using (var repo = new Repository(clonedRepoPath))
        {
            repo.Network.Remotes.Add("pruner", clonedRepoPath2);
            Commands.Fetch(repo, "pruner", Array.Empty<string>(), null, null);
            Assert.NotNull(repo.Refs["refs/remotes/pruner/master"]);

            // Remove the branch from the source repository
            using (var repo2 = new Repository(clonedRepoPath2))
            {
                repo2.Refs.Remove("refs/heads/master");
            }

            // and by default we don't prune it
            Commands.Fetch(repo, "pruner", Array.Empty<string>(), null, null);
            Assert.NotNull(repo.Refs["refs/remotes/pruner/master"]);

            // but we do when asked by the user
            Commands.Fetch(repo, "pruner", Array.Empty<string>(), new FetchOptions { Prune = true }, null);
            Assert.Null(repo.Refs["refs/remotes/pruner/master"]);
        }
    }

    [Fact]
    public void list_references_from_remote_with_null_credentials_handler_connects_anonymously()
    {
        // Arrange
        string remotePath = SandboxBareTestRepo();
        string repoPath = InitNewRepository();

        using (var repo = new Repository(repoPath))
        {
            Remote remote = repo.Network.Remotes.Add("origin", remotePath);
            CredentialsHandler credentialsProvider = null;

            // Act
            IList<Reference> references = repo.Network.ListReferences(remote, credentialsProvider).ToList();

            // Assert
            AssertReferencesMatchRepository(remotePath, references);
        }
    }

    [Fact]
    public void list_references_from_remote_with_null_credentials_handler_and_proxy_options_connects_anonymously()
    {
        // Arrange
        string remotePath = SandboxBareTestRepo();
        string repoPath = InitNewRepository();

        using (var repo = new Repository(repoPath))
        {
            Remote remote = repo.Network.Remotes.Add("origin", remotePath);
            CredentialsHandler credentialsProvider = null;

            // Act
            IList<Reference> references = repo.Network.ListReferences(remote, credentialsProvider, new ProxyOptions()).ToList();

            // Assert
            AssertReferencesMatchRepository(remotePath, references);
        }
    }

    [Fact]
    public void list_references_from_url_with_null_credentials_handler_connects_anonymously()
    {
        // Arrange
        string remotePath = SandboxBareTestRepo();
        string url = new Uri(remotePath).AbsoluteUri;
        string repoPath = InitNewRepository();

        using (var repo = new Repository(repoPath))
        {
            CredentialsHandler credentialsProvider = null;

            // Act
            IList<Reference> references = repo.Network.ListReferences(url, credentialsProvider).ToList();

            // Assert
            AssertReferencesMatchRepository(remotePath, references);
        }
    }

    [Fact]
    public void list_references_from_url_with_null_credentials_handler_and_proxy_options_connects_anonymously()
    {
        // Arrange
        string remotePath = SandboxBareTestRepo();
        string url = new Uri(remotePath).AbsoluteUri;
        string repoPath = InitNewRepository();

        using (var repo = new Repository(repoPath))
        {
            CredentialsHandler credentialsProvider = null;

            // Act
            IList<Reference> references = repo.Network.ListReferences(url, credentialsProvider, new ProxyOptions()).ToList();

            // Assert
            AssertReferencesMatchRepository(remotePath, references);
        }
    }

    [Fact]
    public async Task list_references_from_url_with_credentials_handler_forwards_proxy_options()
    {
        // Arrange: a loopback listener stands in for the git server; it records the first request
        // line it receives and drops the connection. Nothing leaves the machine. For a plain
        // http:// url libgit2 sends a request through a proxy in absolute form
        // ("GET http://host:port/path HTTP/1.1") and without one in origin form ("GET /path ..."),
        // so the request line shows whether the caller's ProxyOptions reached libgit2. (libgit2
        // only opens a connection to the proxy itself for https:// urls, so the unreachable proxy
        // url below is never dialled.)
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var serverListener = new TcpListener(IPAddress.Loopback, 0);
        serverListener.Start();

        try
        {
            int serverPort = ((IPEndPoint)serverListener.LocalEndpoint).Port;
            string url = $"http://127.0.0.1:{serverPort}/repo.git";
            var proxyOptions = new ProxyOptions
            {
                ProxyType = ProxyType.Specified,
                Url = "http://127.0.0.1:1"
            };
            CredentialsHandler credentialsProvider = (_, _, _) => new DefaultCredentials();
            Task<string> firstRequestLine = ReadFirstRequestLineAsync(serverListener, cancellationToken);
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                // Act
                Assert.ThrowsAny<LibGit2SharpException>(
                    () => repo.Network.ListReferences(url, credentialsProvider, proxyOptions).ToList());
            }

            string requestLine = await firstRequestLine.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);

            // Assert
            Assert.StartsWith($"GET http://127.0.0.1:{serverPort}/repo.git/info/refs", requestLine, StringComparison.Ordinal);
        }
        finally
        {
            serverListener.Stop();
        }
    }

    private static async Task<string> ReadFirstRequestLineAsync(TcpListener listener, CancellationToken cancellationToken)
    {
        using (TcpClient client = await listener.AcceptTcpClientAsync(cancellationToken))
        using (var reader = new StreamReader(client.GetStream(), Encoding.ASCII))
        {
            return await reader.ReadLineAsync(cancellationToken);
        }
    }

    private static void AssertReferencesMatchRepository(string repositoryPath, IList<Reference> references)
    {
        Assert.NotEmpty(references);
        Assert.True(references.Single(reference => reference.CanonicalName == "HEAD") is SymbolicReference);

        using (var expectedRepo = new Repository(repositoryPath))
        {
            var expected = expectedRepo.Refs
                .Select(reference => new Tuple<string, string>(reference.CanonicalName, reference.ResolveToDirectReference().TargetIdentifier))
                .OrderBy(pair => pair.Item1, StringComparer.Ordinal)
                .ToList();
            var actual = references
                .Where(reference => reference.CanonicalName != "HEAD" && !reference.CanonicalName.EndsWith("^{}", StringComparison.Ordinal))
                .Select(reference => new Tuple<string, string>(reference.CanonicalName, reference.ResolveToDirectReference().TargetIdentifier))
                .OrderBy(pair => pair.Item1, StringComparer.Ordinal)
                .ToList();

            Assert.Equal(expected, actual);
        }
    }
}
