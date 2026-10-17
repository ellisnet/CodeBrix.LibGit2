using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace CodeBrix.LibGit2.Tests.TestHelpers;

/// <summary>
/// A <see cref="TheoryAttribute"/> for a data-driven test that reaches the network. Every row is
/// skipped unless the environment variable LIBGIT2_RUN_NETWORK_TESTS is set to 1.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class NetworkTheoryAttribute : TheoryAttribute
{
    /// <summary>
    /// Marks the theory as network-dependent, skipping it when network tests are not enabled.
    /// </summary>
    public NetworkTheoryAttribute(
        [CallerFilePath] string sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = -1)
        : base(sourceFilePath, sourceLineNumber)
    {
        if (!NetworkTestSwitch.Enabled)
        {
            Skip = NetworkTestSwitch.SkipReason;
        }
    }
}
