using System.IO;
using System.Linq;
using CodeBrix.LibGit2.Tests.TestHelpers;
using SilverAssertions;
using Xunit;

namespace CodeBrix.LibGit2.Tests; //was previously: LibGit2Sharp.Tests;

public class CleanTests : BaseFixture
{
    [Fact]
    public void CanCleanWorkingDirectory()
    {
        string path = SandboxStandardTestRepo();
        using (var repo = new Repository(path))
        {
            // Verify that there are the expected number of entries and untracked files
            Assert.Equal(6, repo.RetrieveStatus().Count());
            Assert.Single(repo.RetrieveStatus().Untracked);

            repo.RemoveUntrackedFiles();

            // Verify that there are the expected number of entries and 0 untracked files
            Assert.Equal(5, repo.RetrieveStatus().Count());
            Assert.Empty(repo.RetrieveStatus().Untracked);
        }
    }

    [Fact]
    public void RemoveUntrackedFiles_does_not_recreate_a_deleted_staged_file()
    {
        //Arrange
        string path = SandboxStandardTestRepo();
        using (var repo = new Repository(path))
        {
            const string relativePath = "staged-then-deleted.txt";
            string fullPath = Touch(repo.Info.WorkingDirectory, relativePath, "content\n");
            Commands.Stage(repo, relativePath);
            File.Delete(fullPath);

            //Act
            repo.RemoveUntrackedFiles();

            //Assert
            File.Exists(fullPath).Should().BeFalse();
            repo.RetrieveStatus(relativePath).Should().Be(FileStatus.NewInIndex | FileStatus.DeletedFromWorkdir);
        }
    }

    [Fact]
    public void CannotCleanABareRepository()
    {
        string path = SandboxBareTestRepo();
        using (var repo = new Repository(path))
        {
            Assert.Throws<BareRepositoryException>(() => repo.RemoveUntrackedFiles());
        }
    }
}
