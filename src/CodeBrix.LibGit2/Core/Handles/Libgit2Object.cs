// This activates a lightweight mode which will help put under the light
// incorrectly released handles by outputting a warning message in the console.
//
// This should be activated when tests are being run on the CI server.
//
// Uncomment the line below or add a conditional symbol to activate this mode

//#define LEAKS_IDENTIFYING

// This activates a more thorough mode which will show the stack trace of the
// allocation code path for each handle that has been improperly released.
//
// This should be manually activated when some warnings have been raised as
// a result of LEAKS_IDENTIFYING mode activation.
//
// Uncomment the line below or add a conditional symbol to activate this mode

//#define LEAKS_TRACKING

using System;
#if LEAKS_TRACKING
using System.Diagnostics;
using System.Globalization;
#endif
using Microsoft.Win32.SafeHandles;

namespace CodeBrix.LibGit2.Core.Handles; //was previously: LibGit2Sharp.Core.Handles;

// The LeaksContainer class that upstream declared at the top of this file (under
// LEAKS_IDENTIFYING) now lives in Core/LeaksContainer.cs.

internal unsafe abstract class Libgit2Object : SafeHandleZeroOrMinusOneIsInvalid
{
#if LEAKS_TRACKING
    private readonly string trace;
    private readonly Guid id;
#endif

    internal unsafe Libgit2Object(void* ptr, bool owned)
        : this(new IntPtr(ptr), owned)
    {
    }

    internal unsafe Libgit2Object(IntPtr ptr, bool owned)
        : base(owned)
    {
        SetHandle(ptr);

#if LEAKS_TRACKING
        id = Guid.NewGuid();
        Trace.WriteLine(string.Format(CultureInfo.InvariantCulture, "Allocating {0} handle ({1})", GetType().Name, id));

        trace = new StackTrace(2, true).ToString();
#endif
    }

    internal IntPtr AsIntPtr() => DangerousGetHandle();

    protected override void Dispose(bool disposing)
    {
#if LEAKS_IDENTIFYING
        bool leaked = !disposing && DangerousGetHandle() != IntPtr.Zero;

        if (leaked)
        {
            LeaksContainer.Add(GetType().Name);
        }
#endif

        base.Dispose(disposing);

#if LEAKS_TRACKING
        if (!leaked)
        {
            Trace.WriteLine(string.Format(CultureInfo.InvariantCulture, "Disposing {0} handle ({1})", GetType().Name, id));
        }
        else
        {
            Trace.WriteLine(string.Format(CultureInfo.InvariantCulture, "Unexpected finalization of {0} handle ({1})", GetType().Name, id));
            Trace.WriteLine(trace);
            Trace.WriteLine("");
        }
#endif
    }
}
