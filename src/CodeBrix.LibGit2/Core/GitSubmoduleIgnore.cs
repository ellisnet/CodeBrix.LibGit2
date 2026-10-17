namespace CodeBrix.LibGit2.Core; //was previously: LibGit2Sharp.Core;

internal enum GitSubmoduleIgnore
{
    Unspecified = -1,
    None = 1,
    Untracked = 2,
    Dirty = 3,
    All = 4,
}
