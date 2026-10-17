using System;

namespace CodeBrix.LibGit2.Core; //was previously: LibGit2Sharp.Core;

[Flags]
internal enum LookUpOptions
{
    None = 0,
    ThrowWhenNoGitObjectHasBeenFound = 1,
    DereferenceResultToCommit = 2,
    ThrowWhenCanNotBeDereferencedToACommit = 4,
}
