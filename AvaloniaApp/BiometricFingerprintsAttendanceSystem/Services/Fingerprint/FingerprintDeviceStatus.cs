namespace BiometricFingerprintsAttendanceSystem.Services.Fingerprint;

/// <summary>
/// Represents the current status of a fingerprint device.
/// </summary>
public enum FingerprintDeviceStatus
{
    /// <summary>
    /// Device status is unknown or not yet initialized.
    /// </summary>
    Unknown,

    /// <summary>
    /// No fingerprint device is connected.
    /// </summary>
    Disconnected,

    /// <summary>
    /// Device is connected but not yet initialized.
    /// </summary>
    Connected,

    /// <summary>
    /// Device is ready to capture fingerprints.
    /// </summary>
    Ready,

    /// <summary>
    /// Device is currently capturing a fingerprint.
    /// </summary>
    Capturing,

    /// <summary>
    /// Device encountered an error.
    /// </summary>
    Error,

    /// <summary>
    /// Device is busy with another operation.
    /// </summary>
    Busy,

    /// <summary>
    /// Device requires a finger to be placed on the sensor.
    /// </summary>
    WaitingForFinger,

    /// <summary>
    /// Device is processing captured data.
    /// </summary>
    Processing
}
