using System;
using CodeBrix.LibGit2.Core;

namespace CodeBrix.LibGit2; //was previously: LibGit2Sharp;

/// <summary>
/// The exception that is thrown attempting to reference a resource that does not exist.
/// </summary>
public class NotFoundException : NativeException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CodeBrix.LibGit2.NotFoundException"/> class.
    /// </summary>
    public NotFoundException()
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeBrix.LibGit2.NotFoundException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">A message that describes the error.</param>
    public NotFoundException(string message)
        : base(message)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeBrix.LibGit2.NotFoundException"/> class with a specified error message.
    /// </summary>
    /// <param name="format">A composite format string for use in <see cref="string.Format(IFormatProvider, string, object[])"/>.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    public NotFoundException(string format, params object[] args)
        : base(format, args)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeBrix.LibGit2.NotFoundException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception. If the <paramref name="innerException"/> parameter is not a null reference, the current exception is raised in a catch block that handles the inner exception.</param>
    public NotFoundException(string message, Exception innerException)
        : base(message, innerException)
    { }

    internal NotFoundException(string message, GitErrorCategory category)
        : base(message, category)
    { }

    internal override GitErrorCode ErrorCode
    {
        get
        {
            return GitErrorCode.NotFound;
        }
    }
}
