using System.Runtime.InteropServices;

namespace CodeBrix.LibGit2.Core; //was previously: LibGit2Sharp.Core;

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct git_time
{
    public long time;
    public int offset;
}
