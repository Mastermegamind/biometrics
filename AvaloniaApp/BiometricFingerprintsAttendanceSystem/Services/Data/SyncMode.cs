namespace BiometricFingerprintsAttendanceSystem.Services.Data;

/// <summary>
/// Defines the data synchronization mode for the application.
/// </summary>
public enum SyncMode
{
    /// <summary>
    /// API is primary, local database is fallback.
    /// Best for: Good internet with offline backup capability.
    /// </summary>
    OnlineFirst,

    /// <summary>
    /// Local database is primary, syncs to API when available.
    /// Best for: Poor/intermittent connectivity environments.
    /// </summary>
    OfflineFirst,

    /// <summary>
    /// API only, no local storage.
    /// Best for: Cloud-only deployments, thin clients.
    /// </summary>
    OnlineOnly,

    /// <summary>
    /// Local database only, no API calls.
    /// Best for: Air-gapped networks, standalone deployments.
    /// </summary>
    OfflineOnly
}
