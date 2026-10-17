#if LEAKS_IDENTIFYING
using System.Collections.Generic;
using System.Linq;

namespace CodeBrix.LibGit2.Core; //was previously: LibGit2Sharp.Core;

// Split out of Core/Handles/Libgit2Object.cs, where upstream declared it in a second
// namespace block. Compiled only when the LEAKS_IDENTIFYING symbol is defined.

/// <summary>
/// Holds leaked handle type names reported by <see cref="Core.Handles.Libgit2Object"/>
/// </summary>
public static class LeaksContainer
{
    private static readonly HashSet<string> _typeNames = new HashSet<string>();
    private static readonly object _lockpad = new object();

    /// <summary>
    /// Report a new leaked handle type name
    /// </summary>
    /// <param name="typeName">Short name of the leaked handle type.</param>
    public static void Add(string typeName)
    {
        lock (_lockpad)
        {
            _typeNames.Add(typeName);
        }
    }

    /// <summary>
    /// Removes all previously reported leaks.
    /// </summary>
    public static void Clear()
    {
        lock (_lockpad)
        {
            _typeNames.Clear();
        }
    }

    /// <summary>
    /// Returns all reported leaked handle type names.
    /// </summary>
    public static IEnumerable<string> TypeNames
    {
        get
        {
            string[] result = null;
            lock (_lockpad)
            {
                result = _typeNames.ToArray();
            }
            return result;
        }
    }
}
#endif
