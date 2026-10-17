using System;
using System.Collections.Generic;
using System.Text;

namespace CodeBrix.LibGit2; //was previously: LibGit2Sharp;

/// <summary>
/// Information about a smart subtransport registration.
/// </summary>
public abstract class SmartSubtransportRegistrationData
{
    /// <summary>
    /// The URI scheme for this transport, for example "http" or "ssh".
    /// </summary>
    public string Scheme { get; internal set; }

    internal IntPtr RegistrationPointer { get; set; }

    internal IntPtr FunctionPointer { get; set; }
}
