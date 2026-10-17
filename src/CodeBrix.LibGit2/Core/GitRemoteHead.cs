using System;
using System.Runtime.InteropServices;

namespace CodeBrix.LibGit2.Core; //was previously: LibGit2Sharp.Core;

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct git_remote_head
{
    public int Local;
    public git_oid Oid;
    public git_oid Loid;
    public char* Name;
    public char* SymrefTarget;
}
