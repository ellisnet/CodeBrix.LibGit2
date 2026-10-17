using System;
using System.Runtime.InteropServices;

namespace CodeBrix.LibGit2.Core; //was previously: LibGit2Sharp.Core;

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct GitError
{
    public char* Message;
    public GitErrorCategory Category;
}
