using System;

namespace CodeBrix.LibGit2.Core; //was previously: LibGit2Sharp.Core;

[Flags]
internal enum GitReferenceType
{
    Invalid = 0,
    Oid = 1,
    Symbolic = 2,
    Packed = 4,
    Peel = 8,
    ListAll = Oid | Symbolic | Packed
}
