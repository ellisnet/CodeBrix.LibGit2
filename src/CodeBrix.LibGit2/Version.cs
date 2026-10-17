using System.Globalization;
using System.Reflection;
using CodeBrix.LibGit2.Core;

namespace CodeBrix.LibGit2; //was previously: LibGit2Sharp;

/// <summary>
/// Gets the current CodeBrix.LibGit2 version.
/// </summary>
public class Version
{
    /// <summary>
    /// Needed for mocking purposes.
    /// </summary>
    protected Version()
    { }

    internal static Version Build()
    {
        return new Version();
    }

    /// <summary>
    /// Returns version of the CodeBrix.LibGit2 library.
    /// </summary>
    public virtual string InformationalVersion
    {
        get
        {
            var attribute = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            return attribute.InformationalVersion;
        }
    }

    /// <summary>
    /// Returns all the optional features that were compiled into
    /// libgit2.
    /// </summary>
    /// <returns>A <see cref="BuiltInFeatures"/> enumeration.</returns>
    public virtual BuiltInFeatures Features
    {
        get { return Proxy.git_libgit2_features(); }
    }

    /// <summary>
    /// Returns the SHA hash for the libgit2 library.
    /// </summary>
    public virtual string LibGit2CommitSha => RetrieveAbbrevShaFrom(AssemblyCommitIds.LibGit2CommitSha);

    /// <summary>
    /// Returns the SHA hash for the CodeBrix.LibGit2 library.
    /// </summary>
    public virtual string LibGit2SharpCommitSha => RetrieveAbbrevShaFrom(AssemblyCommitIds.LibGit2SharpCommitSha);

    private string RetrieveAbbrevShaFrom(string sha)
    {
        var index = sha.Length > 7 ? 7 : sha.Length;
        return sha.Substring(0, index);
    }

    /// <summary>
    /// Returns a string representing the CodeBrix.LibGit2 version.
    /// </summary>
    /// <remarks>
    ///   The format is <c>{InformationalVersion} (arch - features)</c>, where InformationalVersion is the
    ///   date-stamped package version <c>1.x.y.z</c>, optionally followed by <c>+{source revision}</c>,
    ///   arch is the process architecture (<c>x86</c>, <c>x64</c>, <c>arm64</c> ...) and features are the
    ///   <see cref="BuiltInFeatures"/> compiled into the native libgit2 library.
    /// </remarks>
    /// <returns>The version string, for example <c>1.0.290.900+10f7306... (x64 - Threads, Https, Ssh, NSec)</c>.</returns>
    public override string ToString()
    {
        return RetrieveVersion();
    }

    private string RetrieveVersion()
    {
        string features = Features.ToString();

        return string.Format(CultureInfo.InvariantCulture,
                             "{0} ({1} - {2})",
                             InformationalVersion,
                             Platform.ProcessorArchitecture,
                             features);
    }
}
