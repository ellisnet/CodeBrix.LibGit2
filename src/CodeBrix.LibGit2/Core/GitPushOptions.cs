using System.Runtime.InteropServices;

namespace CodeBrix.LibGit2.Core; //was previously: LibGit2Sharp.Core;

[StructLayout(LayoutKind.Sequential)]
internal class GitPushOptions
{
    public int Version = 1;
    public int PackbuilderDegreeOfParallelism;
    public GitRemoteCallbacks RemoteCallbacks;
    public GitProxyOptions ProxyOptions;
    public RemoteRedirectMode FollowRedirects = RemoteRedirectMode.Initial;
    public GitStrArrayManaged CustomHeaders;
    public GitStrArrayManaged remote_push_options;
}
