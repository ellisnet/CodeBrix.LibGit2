namespace CodeBrix.LibGit2; //was previously: LibGit2Sharp;

/// <summary>
/// Specifies the kind of operation that <see cref="IRepository.Reset(CodeBrix.LibGit2.ResetMode, Commit)"/> should perform.
/// </summary>
public enum ResetMode
{
    /// <summary>
    /// Moves the branch pointed to by HEAD to the specified commit object.
    /// </summary>
    Soft = 1,

    /// <summary>
    /// Moves the branch pointed to by HEAD to the specified commit object and resets the index
    /// to the tree recorded by the commit.
    /// </summary>
    Mixed,

    /// <summary>
    /// Moves the branch pointed to by HEAD to the specified commit object, resets the index
    /// to the tree recorded by the commit and updates the working directory to match the content
    /// of the index.
    /// </summary>
    Hard,
}
