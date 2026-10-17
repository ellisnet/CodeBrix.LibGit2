Objects.tt - reference copy of the T4 template behind Core/Handles/Objects.cs
================================================================================

What it is
  Upstream LibGit2Sharp generates src/CodeBrix.LibGit2/Core/Handles/Objects.cs
  (the SafeHandle subclasses - TreeEntryHandle, ReferenceHandle, RepositoryHandle
  and the rest - one per libgit2 native object type) from a T4 text template.
  Objects.tt in this folder is that template, with its namespace text updated to
  CodeBrix.LibGit2.Core.Handles and its output reshaped to the file-scoped
  namespace form used by the hand-maintained Objects.cs.

Why it lives here and not beside Objects.cs
  The package is packed from the solution in Visual Studio. If Objects.tt were a
  project item, Visual Studio would attach the TextTemplatingFileGenerator custom
  tool to it and could regenerate Objects.cs on save or build, silently replacing
  the maintained file. Keeping the template outside the project tree means it is
  not a project item at all: nothing runs it, and the csproj carries no T4
  metadata (no Generator attribute, no DependentUpon/AutoGen on Objects.cs, no
  T4 <Service> entry).

  Objects.cs is therefore an ORDINARY source file. Edit it directly; this
  template is kept for the record and for regeneration by hand.

Regenerating by hand (only when adding or removing a handle type)
  1. Edit the two arrays at the top of Objects.tt (cNames = the libgit2 C type,
     csNames = the managed handle class; same order, same length). Each C type
     needs a matching NativeMethods.<c_name>_free entry point and an opaque
     struct of that name in Core/.
  2. Generate to a scratch location, never over the source file:
       dotnet tool install --global dotnet-t4      (one time)
       t4 -o /some/scratch/Objects.cs tools/handles-template/Objects.tt
     or open the .tt in Visual Studio OUTSIDE the solution and use
     "Run Custom Tool", saving the output somewhere outside src/.
  3. Diff the scratch output against src/CodeBrix.LibGit2/Core/Handles/Objects.cs
     and carry the changes across by hand (leading blank lines or whitespace in
     the T4 output are not significant).
  4. Build Debug and Release; both must stay at 0 warnings.
