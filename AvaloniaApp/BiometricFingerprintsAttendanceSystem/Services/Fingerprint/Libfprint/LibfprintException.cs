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
            var code = Marshal.ReadInt32(errorPtr, IntPtr.Size);
            var messagePtr = Marshal.ReadIntPtr(errorPtr, IntPtr.Size + sizeof(int));
            var message = Marshal.PtrToStringUTF8(messagePtr) ?? "Unknown error";

            return new LibfprintException(message, (FpDeviceError)code);
        }
        finally
        {
            LibfprintNative.g_error_free(errorPtr);
        }
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
