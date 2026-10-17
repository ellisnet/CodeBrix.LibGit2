using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using CodeBrix.LibGit2.Core;

namespace CodeBrix.LibGit2; //was previously: LibGit2Sharp;

/// <summary>
/// Conains a X509 certificate
/// </summary>
public class CertificateX509 : Certificate
{
    /// <summary>
    /// For mocking purposes
    /// </summary>
    protected CertificateX509()
    { }

    /// <summary>
    /// The certificate.
    /// </summary>
    public virtual X509Certificate Certificate { get; private set; }

    internal unsafe CertificateX509(git_certificate_x509* cert)
    {
        int len = checked((int)cert->len.ToUInt32());
        byte[] data = new byte[len];
        Marshal.Copy(new IntPtr(cert->data), data, 0, len);
        Certificate = X509CertificateLoader.LoadCertificate(data); //was previously: new X509Certificate(data) (SYSLIB0057 on net10)
    }

    internal CertificateX509(X509Certificate cert)
    {
        Certificate = cert;
    }

    internal unsafe IntPtr ToPointers(out IntPtr dataPtr)
    {
        var certData = Certificate.Export(X509ContentType.Cert);
        dataPtr = Marshal.AllocHGlobal(certData.Length);
        Marshal.Copy(certData, 0, dataPtr, certData.Length);
        var gitCert = new git_certificate_x509()
        {
            cert_type = GitCertificateType.X509,
            data = (byte*)dataPtr.ToPointer(),
            len = (UIntPtr)certData.Length,
        };

        var ptr = Marshal.AllocHGlobal(Marshal.SizeOf(gitCert));
        Marshal.StructureToPtr(gitCert, ptr, false);

        return ptr;
    }
}
