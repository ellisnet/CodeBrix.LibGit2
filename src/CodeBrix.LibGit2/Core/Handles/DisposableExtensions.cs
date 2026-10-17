using System;

namespace CodeBrix.LibGit2.Core.Handles; //was previously: LibGit2Sharp.Core.Handles;

internal static class DisposableExtensions
{
    public static void SafeDispose(this IDisposable disposable)
    {
        if (disposable == null)
        {
            return;
        }

        disposable.Dispose();
    }
}
