using System;
using CodeBrix.LibGit2.Core;

namespace CodeBrix.LibGit2; //was previously: LibGit2Sharp;

/// <summary>
/// An exception thrown that corresponds to a libgit2 (native library) error.
/// </summary>
public abstract class NativeException : LibGit2SharpException
{
    /// <summary>
    /// Needed for mocking purposes.
    /// </summary>
    protected NativeException()
    { }

    internal NativeException(string message)
        : base(message)
    { }

    internal NativeException(string message, Exception innerException)
        : base(message, innerException)
    { }

    internal NativeException(string format, params object[] args)
        : base(format, args)
    {
    }

    internal NativeException(string message, GitErrorCategory category) : this(message)
    {
        Data.Add("libgit2.category", (int)category);
    }

    internal abstract GitErrorCode ErrorCode { get; }
}
