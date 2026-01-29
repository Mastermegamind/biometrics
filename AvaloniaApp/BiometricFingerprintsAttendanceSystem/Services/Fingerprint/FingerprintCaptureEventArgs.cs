namespace BiometricFingerprintsAttendanceSystem.Services.Fingerprint;

/// <summary>
/// Event arguments for fingerprint capture events.
/// </summary>
public sealed class FingerprintCaptureEventArgs : EventArgs
{
    /// <summary>
    /// Whether the capture was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The captured fingerprint sample data (raw image or encoded).
    /// </summary>
    public byte[]? SampleData { get; init; }

    /// <summary>
    /// The fingerprint template extracted from the sample (if available).
    /// </summary>
    public byte[]? TemplateData { get; init; }

    /// <summary>
    /// The quality score of the captured sample (0-100).
    /// </summary>
    public int Quality { get; init; }

    /// <summary>
    /// Error message if the capture failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// The capture status/feedback for UI display.
    /// </summary>
    public FingerprintCaptureStatus Status { get; init; }

    /// <summary>
    /// The finger position if known.
    /// </summary>
    public FingerPosition? Finger { get; init; }
}

/// <summary>
/// Status codes for fingerprint capture operations.
/// </summary>
public enum FingerprintCaptureStatus
{
    /// <summary>
    /// Capture completed successfully.
    /// </summary>
    Success,

    /// <summary>
    /// No finger detected on sensor.
    /// </summary>
    NoFinger,

    /// <summary>
    /// Finger moved too quickly during capture.
    /// </summary>
    TooFast,

    /// <summary>
    /// Finger moved too slowly during capture (for swipe sensors).
    /// </summary>
    TooSlow,

    /// <summary>
    /// Captured image quality is too low.
    /// </summary>
    PoorQuality,

    /// <summary>
    /// Finger is too dry for proper capture.
    /// </summary>
    TooDry,

    /// <summary>
    /// Finger is too wet for proper capture.
    /// </summary>
    TooWet,

    /// <summary>
    /// Only partial fingerprint was captured.
    /// </summary>
    Partial,

    /// <summary>
    /// Capture was cancelled.
    /// </summary>
    Cancelled,

    /// <summary>
    /// Device error occurred during capture.
    /// </summary>
    DeviceError,

    /// <summary>
    /// Capture timed out.
    /// </summary>
    Timeout,

    /// <summary>
    /// Unknown error occurred.
    /// </summary>
    Unknown
}
