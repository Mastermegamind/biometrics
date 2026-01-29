namespace BiometricFingerprintsAttendanceSystem.Services.Fingerprint;

/// <summary>
/// Cross-platform fingerprint service interface supporting enrollment, verification, and device management.
/// Implementations: Windows (DigitalPersona SDK, WBF), Linux (fprintd/libfprint).
/// </summary>
public interface IFingerprintService : IDisposable
{
    /// <summary>
    /// Gets whether the fingerprint device is available and ready.
    /// </summary>
    bool IsDeviceAvailable { get; }

    /// <summary>
    /// Gets the current device status.
    /// </summary>
    FingerprintDeviceStatus DeviceStatus { get; }

    /// <summary>
    /// Gets information about the connected fingerprint device.
    /// </summary>
    FingerprintDeviceInfo? DeviceInfo { get; }

    /// <summary>
    /// Raised when a fingerprint is captured from the device.
    /// </summary>
    event EventHandler<FingerprintCaptureEventArgs>? FingerprintCaptured;

    /// <summary>
    /// Raised when the device status changes.
    /// </summary>
    event EventHandler<FingerprintDeviceStatus>? DeviceStatusChanged;

    /// <summary>
    /// Initializes the fingerprint device and starts listening for events.
    /// </summary>
    Task<bool> InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts capturing fingerprint samples. Captured samples are raised via FingerprintCaptured event.
    /// </summary>
    Task StartCaptureAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops capturing fingerprint samples.
    /// </summary>
    Task StopCaptureAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Captures a single fingerprint and returns the template data.
    /// </summary>
    Task<FingerprintCaptureResult> CaptureAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a fingerprint template from raw sample data.
    /// </summary>
    Task<byte[]?> CreateTemplateAsync(byte[] sampleData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies a fingerprint sample against a stored template.
    /// </summary>
    /// <param name="sample">The captured fingerprint sample.</param>
    /// <param name="template">The stored template to verify against.</param>
    /// <returns>Verification result with match score.</returns>
    Task<FingerprintVerifyResult> VerifyAsync(byte[] sample, byte[] template, CancellationToken cancellationToken = default);

    /// <summary>
    /// Identifies a fingerprint sample against multiple stored templates.
    /// </summary>
    /// <param name="sample">The captured fingerprint sample.</param>
    /// <param name="templates">Dictionary of identifier to template data.</param>
    /// <returns>Match result with the identified person or null if no match.</returns>
    Task<FingerprintMatchResult?> IdentifyAsync(byte[] sample, IReadOnlyDictionary<string, byte[]> templates, CancellationToken cancellationToken = default);

    /// <summary>
    /// Legacy match method for backward compatibility.
    /// </summary>
    Task<FingerprintMatchResult?> MatchAsync(byte[] sample, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enrolls a fingerprint for a user (Linux fprintd specific - stores in system).
    /// </summary>
    /// <param name="username">The system username to enroll.</param>
    /// <param name="finger">The finger to enroll.</param>
    Task<FingerprintEnrollResult> EnrollAsync(string username, FingerPosition finger, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies a user's fingerprint against system-stored prints (Linux fprintd specific).
    /// </summary>
    /// <param name="username">The system username to verify.</param>
    Task<bool> VerifyUserAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists enrolled fingers for a user (Linux fprintd specific).
    /// </summary>
    Task<IReadOnlyList<FingerPosition>> ListEnrolledFingersAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes enrolled fingerprints for a user (Linux fprintd specific).
    /// </summary>
    Task<bool> DeleteEnrolledFingersAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the quality score of a captured fingerprint sample (0-100).
    /// </summary>
    int GetSampleQuality(byte[] sample);
}
