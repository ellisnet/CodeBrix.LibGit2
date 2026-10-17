using System;

namespace CodeBrix.LibGit2.Core; //was previously: LibGit2Sharp.Core;

[Flags]
internal enum GitCertificateSshType
{
    MD5 = (1 << 0),
    SHA1 = (1 << 1),
}
