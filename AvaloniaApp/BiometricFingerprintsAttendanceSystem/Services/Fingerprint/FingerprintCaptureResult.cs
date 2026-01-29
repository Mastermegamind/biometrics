namespace BiometricFingerprintsAttendanceSystem.Services.Fingerprint;

/// <summary>
/// Result of a fingerprint capture operation.
/// </summary>
public sealed class FingerprintCaptureResult
{
    /// <summary>
    /// Whether the capture was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The captured fingerprint sample data (raw image data).
    /// </summary>
    public byte[]? SampleData { get; init; }

    /// <summary>
    /// The extracted fingerprint template (for storage/matching).
    /// </summary>
    public byte[]? TemplateData { get; init; }

    /// <summary>
    /// The quality score of the captured sample (0-100).
    /// </summary>
    public int Quality { get; init; }

    /// <summary>
    /// The capture status for feedback.
    /// </summary>
    public FingerprintCaptureStatus Status { get; init; }

    /// <summary>
    /// Error message if capture failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    // Back-compat aliases for older view-model expectations.
    public string? Message => ErrorMessage;
    public byte[]? ImageData => SampleData;

    /// <summary>
    /// Creates a successful capture result.
    /// </summary>
    public static FingerprintCaptureResult Successful(byte[] sampleData, byte[]? templateData, int quality) => new()
    {
        Success = true,
        SampleData = sampleData,
        TemplateData = templateData,
        Quality = quality,
        Status = FingerprintCaptureStatus.Success
    };

    /// <summary>
    /// Creates a failed capture result.
    /// </summary>
    public static FingerprintCaptureResult Failed(FingerprintCaptureStatus status, string? errorMessage = null) => new()
    {
        Success = false,
        Status = status,
        ErrorMessage = errorMessage ?? GetDefaultErrorMessage(status)
    };

    private static string GetDefaultErrorMessage(FingerprintCaptureStatus status) => status switch
    {
        FingerprintCaptureStatus.NoFinger => "No finger detected on the sensor.",
        FingerprintCaptureStatus.TooFast => "Finger moved too quickly. Please try again slowly.",
        FingerprintCaptureStatus.TooSlow => "Finger moved too slowly. Please try again.",
        FingerprintCaptureStatus.PoorQuality => "Image quality is too low. Please clean your finger and try again.",
        FingerprintCaptureStatus.TooDry => "Finger is too dry. Please moisturize slightly and try again.",
        FingerprintCaptureStatus.TooWet => "Finger is too wet. Please dry your finger and try again.",
        FingerprintCaptureStatus.Partial => "Only partial fingerprint captured. Please place finger fully on sensor.",
        FingerprintCaptureStatus.Cancelled => "Capture was cancelled.",
        FingerprintCaptureStatus.DeviceError => "Fingerprint device error occurred.",
        FingerprintCaptureStatus.Timeout => "Capture timed out. Please try again.",
        _ => "An unknown error occurred during capture."
    };
}
