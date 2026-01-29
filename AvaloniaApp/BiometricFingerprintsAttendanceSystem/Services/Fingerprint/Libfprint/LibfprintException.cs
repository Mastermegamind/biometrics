using System.Runtime.InteropServices;

namespace BiometricFingerprintsAttendanceSystem.Services.Fingerprint.Libfprint;

/// <summary>
/// Exception thrown when libfprint operations fail.
/// </summary>
public sealed class LibfprintException : Exception
{
    public FpDeviceError ErrorCode { get; }

    public LibfprintException(string message) : base(message)
    {
        ErrorCode = FpDeviceError.General;
    }

    public LibfprintException(string message, FpDeviceError errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }

    public LibfprintException(string message, Exception innerException) : base(message, innerException)
    {
        ErrorCode = FpDeviceError.General;
    }

    /// <summary>
    /// Creates an exception from a GError pointer.
    /// </summary>
    internal static LibfprintException FromGError(IntPtr errorPtr)
    {
        if (errorPtr == IntPtr.Zero)
        {
            return new LibfprintException("Unknown libfprint error");
        }

        try
        {
            // GError structure: { GQuark domain; gint code; gchar *message; }
            var error = Marshal.PtrToStructure<GError>(errorPtr);
            // Avoid dereferencing error.Message here; some drivers return invalid pointers.
            var domainNamePtr = LibfprintNative.g_quark_to_string(error.Domain);
            var domainValue = domainNamePtr == IntPtr.Zero
                ? $"0x{error.Domain:X}"
                : (Marshal.PtrToStringUTF8(domainNamePtr) ?? $"0x{error.Domain:X}");
            var message = $"libfprint error (domain={domainValue}, code={error.Code})";

            return new LibfprintException(message, (FpDeviceError)error.Code);
        }
        finally
        {
            LibfprintNative.g_error_free(errorPtr);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct GError
    {
        public readonly uint Domain;
        public readonly int Code;
        public readonly IntPtr Message;
    }

    /// <summary>
    /// Maps error code to FingerprintCaptureStatus.
    /// </summary>
    public FingerprintCaptureStatus ToCaptureStatus() => ErrorCode switch
    {
        FpDeviceError.Retry => FingerprintCaptureStatus.PoorQuality,
        FpDeviceError.RetryTooShort => FingerprintCaptureStatus.TooFast,
        FpDeviceError.RetryCenter => FingerprintCaptureStatus.Partial,
        FpDeviceError.RetryRemoveAndRetry => FingerprintCaptureStatus.NoFinger,
        FpDeviceError.Busy => FingerprintCaptureStatus.DeviceError,
        FpDeviceError.NotSupported => FingerprintCaptureStatus.DeviceError,
        _ => FingerprintCaptureStatus.Unknown
    };
}
