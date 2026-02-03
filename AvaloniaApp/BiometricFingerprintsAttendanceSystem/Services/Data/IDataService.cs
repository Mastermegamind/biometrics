namespace BiometricFingerprintsAttendanceSystem.Services.Data;

/// <summary>
/// Unified data service interface for all enrollment and attendance operations.
/// Implementations handle the sync mode (Online/Offline/Hybrid).
/// </summary>
public interface IDataService
{
    /// <summary>
    /// Current sync mode.
    /// </summary>
    SyncMode Mode { get; }

    /// <summary>
    /// Whether the API is currently reachable.
    /// </summary>
    bool IsOnline { get; }

    /// <summary>
    /// Number of pending sync operations (for OfflineFirst mode).
    /// </summary>
    int PendingSyncCount { get; }

    // ==================== Student Operations ====================

    /// <summary>
    /// Look up a student by registration number.
    /// </summary>
    Task<DataResult<StudentInfo>> GetStudentAsync(string regNo);

    /// <summary>
    /// Get student's passport photo.
    /// </summary>
    Task<DataResult<byte[]>> GetStudentPhotoAsync(string regNo);

    // ==================== Enrollment Operations ====================

    /// <summary>
    /// Check if a student is enrolled.
    /// </summary>
    Task<DataResult<EnrollmentStatus>> GetEnrollmentStatusAsync(string regNo);

    /// <summary>
    /// Submit enrollment for a student.
    /// </summary>
    Task<DataResult> SubmitEnrollmentAsync(EnrollmentRequest request);

    /// <summary>
    /// Get enrolled fingerprint templates for a specific student.
    /// </summary>
    Task<DataResult<List<FingerprintTemplate>>> GetEnrollmentTemplatesAsync(string regNo);

    /// <summary>
    /// Get all enrolled fingerprint templates for local matching.
    /// Only used in offline modes.
    /// </summary>
    Task<DataResult<List<(string RegNo, List<FingerprintTemplate> Templates)>>> GetAllEnrollmentsAsync();

    // ==================== Attendance Operations ====================

    /// <summary>
    /// Clock in with fingerprint verification.
    /// </summary>
    Task<ClockInResponse> ClockInAsync(ClockInRequest request);

    /// <summary>
    /// Clock out with fingerprint verification.
    /// </summary>
    Task<ClockOutResponse> ClockOutAsync(ClockOutRequest request);

    /// <summary>
    /// Clock in with a pre-verified identity (client-side matching).
    /// </summary>
    Task<ClockInResponse> ClockInVerifiedAsync(VerifiedClockRequest request);

    /// <summary>
    /// Clock out with a pre-verified identity (client-side matching).
    /// </summary>
    Task<ClockOutResponse> ClockOutVerifiedAsync(VerifiedClockRequest request);

    /// <summary>
    /// Get attendance records for a date range.
    /// </summary>
    Task<DataResult<List<AttendanceRecord>>> GetAttendanceAsync(DateTime from, DateTime to, string? regNo = null);

    // ==================== Sync Operations ====================

    /// <summary>
    /// Force sync pending records to API (for OfflineFirst mode).
    /// </summary>
    Task<DataResult> SyncPendingAsync();

    /// <summary>
    /// Check API connectivity.
    /// </summary>
    Task<bool> CheckOnlineStatusAsync();
}

/// <summary>
/// Attendance record.
/// </summary>
public record AttendanceRecord
{
    public long Id { get; init; }
    public string RegNo { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string ClassName { get; init; } = string.Empty;
    public DateTime Date { get; init; }
    public DateTime? TimeIn { get; init; }
    public DateTime? TimeOut { get; init; }
    public TimeSpan? Duration => TimeOut.HasValue && TimeIn.HasValue ? TimeOut.Value - TimeIn.Value : null;
    public bool IsSynced { get; init; }
}
