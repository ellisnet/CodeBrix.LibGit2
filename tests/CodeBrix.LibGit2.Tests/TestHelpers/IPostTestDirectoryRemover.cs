namespace CodeBrix.LibGit2.Tests.TestHelpers; //was previously: LibGit2Sharp.Tests.TestHelpers;

public interface IPostTestDirectoryRemover
{
    void Register(string directoryPath);
}
