namespace CodeBrix.LibGit2.Core; //was previously: LibGit2Sharp.Core;

internal interface ILazy<T>
{
    T Value { get; }
}
