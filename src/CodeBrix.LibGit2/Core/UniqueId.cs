namespace CodeBrix.LibGit2.Core;

internal static class UniqueId
{
    // Only used as the MarshalCookie key that the custom string marshalers are cached under,
    // so any value that is fixed for the life of the assembly works; a constant keeps builds reproducible.
    public const string UniqueIdentifier = "e1728188-0c72-4aa0-b885-29df93bc697b";
}
