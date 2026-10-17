using System;
using CodeBrix.LibGit2.Core;

namespace CodeBrix.LibGit2; //was previously: LibGit2Sharp;

/// <summary>
/// The exception that is thrown when a checkout cannot be performed
/// because of a conflicting change staged in the index, or unstaged
/// in the working directory.
/// </summary>
public class CheckoutConflictException : NativeException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CodeBrix.LibGit2.CheckoutConflictException"/> class.
    /// </summary>
    public CheckoutConflictException()
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeBrix.LibGit2.CheckoutConflictException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">A message that describes the error.</param>
    public CheckoutConflictException(string message)
        : base(message)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeBrix.LibGit2.CheckoutConflictException"/> class with a specified error message.
    /// </summary>
    /// <param name="format">A composite format string for use in <see cref="string.Format(IFormatProvider, string, object[])"/>.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    public CheckoutConflictException(string format, params object[] args)
        : base(format, args)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeBrix.LibGit2.CheckoutConflictException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception. If the <paramref name="innerException"/> parameter is not a null reference, the current exception is raised in a catch block that handles the inner exception.</param>
    public CheckoutConflictException(string message, Exception innerException)
        : base(message, innerException)
    { }

    internal CheckoutConflictException(string message, GitErrorCategory category)
        : base(message, category)
    { }

    internal override GitErrorCode ErrorCode
    {
        get
        {
            return GitErrorCode.Conflict;
        }
    }
}
