namespace BiometricFingerprintsAttendanceSystem.Services.Fingerprint;

/// <summary>
/// Result of a fingerprint enrollment operation.
/// </summary>
public sealed class FingerprintEnrollResult
{
    /// <summary>
    /// Whether the enrollment was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The finger that was enrolled.
    /// </summary>
    public FingerPosition Finger { get; init; }

    /// <summary>
    /// The enrolled template data (for application-managed storage).
    /// Null if enrollment is system-managed (e.g., fprintd).
    /// </summary>
    public byte[]? TemplateData { get; init; }

    /// <summary>
    /// The current stage of enrollment (for multi-capture enrollment).
    /// </summary>
    public int CurrentStage { get; init; }

    /// <summary>
    /// Total stages required for enrollment.
    /// </summary>
    public int TotalStages { get; init; }

    /// <summary>
    /// Whether enrollment is complete.
    /// </summary>
    public bool IsComplete { get; init; }

    /// <summary>
    /// Error message if enrollment failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Enrollment status for UI feedback.
    /// </summary>
    public FingerprintEnrollStatus Status { get; init; }

    /// <summary>
    /// Creates a successful enrollment result.
    /// </summary>
    public static FingerprintEnrollResult Successful(FingerPosition finger, byte[]? template = null) => new()
    {
        Success = true,
        Finger = finger,
        TemplateData = template,
        IsComplete = true,
        Status = FingerprintEnrollStatus.Completed
    };

    /// <summary>
    /// Creates a stage-complete result (for multi-capture enrollment).
    /// </summary>
    public static FingerprintEnrollResult StageComplete(FingerPosition finger, int currentStage, int totalStages) => new()
    {
        Success = true,
        Finger = finger,
        CurrentStage = currentStage,
        TotalStages = totalStages,
        IsComplete = currentStage >= totalStages,
        Status = currentStage >= totalStages ? FingerprintEnrollStatus.Completed : FingerprintEnrollStatus.StageComplete
    };

    /// <summary>
    /// Creates a failed enrollment result.
    /// </summary>
    public static FingerprintEnrollResult Failed(FingerprintEnrollStatus status, string? errorMessage = null) => new()
    {
        Success = false,
        Status = status,
        ErrorMessage = errorMessage ?? GetDefaultErrorMessage(status)
    };

    private static string GetDefaultErrorMessage(FingerprintEnrollStatus status) => status switch
    {
        FingerprintEnrollStatus.AlreadyEnrolled => "This finger is already enrolled.",
        FingerprintEnrollStatus.DataFull => "Storage is full. Delete existing fingerprints first.",
        FingerprintEnrollStatus.Cancelled => "Enrollment was cancelled.",
        FingerprintEnrollStatus.DeviceError => "Fingerprint device error occurred.",
        FingerprintEnrollStatus.PoorQuality => "Fingerprint quality too low for enrollment.",
        FingerprintEnrollStatus.Timeout => "Enrollment timed out.",
        FingerprintEnrollStatus.PermissionDenied => "Permission denied. Administrator access required.",
        _ => "An unknown error occurred during enrollment."
    };
}

/// <summary>
/// Status codes for fingerprint enrollment operations.
/// </summary>
public enum FingerprintEnrollStatus
{
    /// <summary>
    /// Enrollment completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Current enrollment stage completed, more captures needed.
    /// </summary>
    StageComplete,

    /// <summary>
    /// Waiting for finger to be placed on sensor.
    /// </summary>
    WaitingForFinger,

    /// <summary>
    /// Processing captured fingerprint.
    /// </summary>
    Processing,

    /// <summary>
    /// The finger is already enrolled.
    /// </summary>
    AlreadyEnrolled,

    /// <summary>
    /// Fingerprint storage is full.
    /// </summary>
    DataFull,

    /// <summary>
    /// Enrollment was cancelled.
    /// </summary>
    Cancelled,

    /// <summary>
    /// Device error occurred.
    /// </summary>
    DeviceError,

    /// <summary>
    /// Fingerprint quality too poor for enrollment.
    /// </summary>
    PoorQuality,

    /// <summary>
    /// Enrollment timed out.
    /// </summary>
    Timeout,

    /// <summary>
    /// Permission denied (e.g., not running as root on Linux).
    /// </summary>
    PermissionDenied,

    /// <summary>
    /// Unknown error.
    /// </summary>
    Unknown
}
