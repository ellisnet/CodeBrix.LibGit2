using System.Runtime.InteropServices;

namespace CodeBrix.LibGit2.Core; //was previously: LibGit2Sharp.Core;

[StructLayout(LayoutKind.Sequential)]
internal struct GitSubmoduleUpdateOptions
{
    public uint Version;

    public GitCheckoutOpts CheckoutOptions;

    public GitFetchOptions FetchOptions;

    public int AllowFetch;
}
