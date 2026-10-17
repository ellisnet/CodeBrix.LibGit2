using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace CodeBrix.LibGit2.Tests.TestHelpers;

/// <summary>
/// A <see cref="FactAttribute"/> for a test that reaches the network (clones, fetches, pushes or
/// lists references on a remote host). The test is skipped unless the environment variable
/// LIBGIT2_RUN_NETWORK_TESTS is set to 1, so a default test run never leaves the machine.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class NetworkFactAttribute : FactAttribute
{
    /// <summary>
    /// Marks the test as network-dependent, skipping it when network tests are not enabled.
    /// </summary>
    public NetworkFactAttribute(
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
