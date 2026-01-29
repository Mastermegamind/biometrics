namespace BiometricFingerprintsAttendanceSystem.Services.Fingerprint;

/// <summary>
/// Contains information about the connected fingerprint device.
/// </summary>
public sealed class FingerprintDeviceInfo
{
    /// <summary>
    /// The device manufacturer/vendor name.
    /// </summary>
    public string Vendor { get; init; } = string.Empty;

    /// <summary>
    /// The device product name/model.
    /// </summary>
    public string ProductName { get; init; } = string.Empty;

    /// <summary>
    /// The USB Vendor ID (if applicable).
    /// </summary>
    public string? VendorId { get; init; }

    /// <summary>
    /// The USB Product ID (if applicable).
    /// </summary>
    public string? ProductId { get; init; }

    /// <summary>
    /// The device serial number (if available).
    /// </summary>
    public string? SerialNumber { get; init; }

    /// <summary>
    /// The device driver or backend being used.
    /// </summary>
    public string Driver { get; init; } = string.Empty;

    /// <summary>
    /// The type of fingerprint device.
    /// </summary>
    public FingerprintDeviceType DeviceType { get; init; }

    /// <summary>
    /// Whether the device supports enrollment.
    /// </summary>
    public bool SupportsEnrollment { get; init; }

    /// <summary>
    /// Whether the device supports identification (1:N matching).
    /// </summary>
    public bool SupportsIdentification { get; init; }

    /// <summary>
    /// Whether the device supports verification (1:1 matching).
    /// </summary>
    public bool SupportsVerification { get; init; } = true;

    /// <summary>
    /// The image width in pixels (if device provides images).
    /// </summary>
    public int ImageWidth { get; init; }

    /// <summary>
    /// The image height in pixels (if device provides images).
    /// </summary>
    public int ImageHeight { get; init; }

    /// <summary>
    /// The image resolution in DPI.
    /// </summary>
    public int ImageDpi { get; init; }

    public override string ToString() => $"{Vendor} {ProductName} ({Driver})";
}
