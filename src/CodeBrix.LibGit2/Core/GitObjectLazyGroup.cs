using System;
using System.Diagnostics.CodeAnalysis;
using CodeBrix.LibGit2.Core.Handles;

namespace CodeBrix.LibGit2.Core; //was previously: LibGit2Sharp.Core;

internal class GitObjectLazyGroup : LazyGroup<ObjectHandle>
{
    private readonly ObjectId id;

    public GitObjectLazyGroup(Repository repo, ObjectId id)
        : base(repo)
    {
        this.id = id;
    }

    protected override void EvaluateInternal(Action<ObjectHandle> evaluator)
    {
        using (var osw = new ObjectSafeWrapper(id, repo.Handle))
        {
            evaluator(osw.ObjectPtr);
        }
    }

    public static ILazy<TResult> Singleton<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TResult>(Repository repo, ObjectId id, Func<ObjectHandle, TResult> resultSelector, bool throwIfMissing = false)

    {
        return Singleton(() =>
        {
            using (var osw = new ObjectSafeWrapper(id, repo.Handle, throwIfMissing: throwIfMissing))
            {
                return resultSelector(osw.ObjectPtr);
            }
        });
    }
}
