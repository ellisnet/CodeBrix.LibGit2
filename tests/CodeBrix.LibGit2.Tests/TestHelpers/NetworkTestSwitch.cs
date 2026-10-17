using System;

namespace CodeBrix.LibGit2.Tests.TestHelpers;

/// <summary>
/// The opt-in switch shared by <see cref="NetworkFactAttribute"/> and <see cref="NetworkTheoryAttribute"/>.
/// </summary>
public static class NetworkTestSwitch
{
    /// <summary>The environment variable that enables network tests when set to 1.</summary>
    public const string EnvironmentVariable = "LIBGIT2_RUN_NETWORK_TESTS";

    /// <summary>The skip message reported for a network test when the switch is off.</summary>
    public const string SkipReason = "Reaches the network; set LIBGIT2_RUN_NETWORK_TESTS=1 to run.";

    /// <summary>True when the environment variable is set to exactly 1.</summary>
    public static bool Enabled => Environment.GetEnvironmentVariable(EnvironmentVariable) == "1";
}
