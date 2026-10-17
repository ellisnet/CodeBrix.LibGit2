using System;
using System.Text.RegularExpressions;
using CodeBrix.LibGit2.Tests.TestHelpers;
using Xunit;

namespace CodeBrix.LibGit2.Tests; //was previously: LibGit2Sharp.Tests;

public class GlobalSettingsTests : BaseFixture
{
    [Fact]
    public void CanGetMinimumCompiledInFeatures()
    {
        BuiltInFeatures features = GlobalSettings.Version.Features;

        Assert.True(features.HasFlag(BuiltInFeatures.Threads));
        Assert.True(features.HasFlag(BuiltInFeatures.Https));
    }

    [Fact]
    public void CanRetrieveValidVersionString()
    {
        // Version string format is:
        //      1.x.y.z[+{source revision}] (arch - features)
        // Example output:
        //      "1.0.290.900+10f73066af898b6a130b414cbed0554ca7ea714b (x64 - Threads, Https, Ssh, NSec)"
        //was previously: Major.Minor.Patch[-previewTag]+libgit2-{libgit2_abbrev_hash}.{LibGit2Sharp_hash} (MinVer build metadata, not used here)

        string versionInfo = GlobalSettings.Version.ToString();

        // The GlobalSettings.Version returned string should contain :
        //      version: '1.0.290.900' CodeBrix.LibGit2 date-stamped version number.
        //      sourceRevision: '10f73066af898b6a130b414cbed0554ca7ea714b' source revision, when the build knows it.
        //      arch: 'x86' or 'x64' libgit2 target.
        //      git2Features: 'Threads, Ssh' libgit2 features compiled with.
        string regex = @"^(?<version>1\.\d+\.\d+\.\d+)(\+(?<sourceRevision>[a-f0-9]{40}))? \((?<arch>\w+) - (?<git2Features>(?:\w*(?:, )*\w+)*)\)$";

        Assert.NotNull(versionInfo);

        Match regexResult = Regex.Match(versionInfo, regex);

        Assert.True(regexResult.Success, "The following version string format is enforced:" +
                                         "1.x.y.z[+{source revision}] (arch - features). " +
                                         "But found \"" + versionInfo + "\" instead.");
    }

    [Fact]
    public void TryingToResetNativeLibraryPathAfterLoadedThrows()
    {
        // Do something that loads the native library
        var features = GlobalSettings.Version.Features;

        Assert.Throws<LibGit2SharpException>(() => { GlobalSettings.NativeLibraryPath = "C:/Foo"; });
    }

    [Fact]
    public void SetExtensions()
    {
        var extensions = GlobalSettings.GetExtensions();

        // Assert that "noop" is supported by default
        Assert.Equal(new[] { "noop", "objectformat", "worktreeconfig" }, extensions);

        // Disable "noop" extensions
        GlobalSettings.SetExtensions("!noop");
        extensions = GlobalSettings.GetExtensions();
        Assert.Equal(new[] { "objectformat", "worktreeconfig" }, extensions);

        // Enable two new extensions (it will reset the configuration and "noop" will be enabled)
        GlobalSettings.SetExtensions("partialclone", "newext");
        extensions = GlobalSettings.GetExtensions();
        Assert.Equal(new[] { "newext", "noop", "objectformat", "partialclone", "worktreeconfig" }, extensions);
    }

    [Fact]
    public void OwnerValidation()
    {
        // Assert that owner validation is enabled by default
        Assert.True(GlobalSettings.GetOwnerValidation());

        // Disable owner validation
        GlobalSettings.SetOwnerValidation(false);
        Assert.False(GlobalSettings.GetOwnerValidation());

        // Enable it again
        GlobalSettings.SetOwnerValidation(true);
        Assert.True(GlobalSettings.GetOwnerValidation());
    }
}
