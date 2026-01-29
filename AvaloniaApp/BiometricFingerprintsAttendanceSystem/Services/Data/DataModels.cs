namespace BiometricFingerprintsAttendanceSystem.Services.Data;

/// <summary>
/// Student information returned from lookup.
/// </summary>
public record StudentInfo
{
    public string RegNo { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string ClassName { get; init; } = string.Empty;
    public string? Department { get; init; }
    public string? Faculty { get; init; }
    public byte[]? PassportPhoto { get; init; }
    public string? PassportUrl { get; init; }
    public DateTime? RenewalDate { get; init; }
}

/// <summary>
/// Fingerprint template for enrollment.
/// </summary>
public record FingerprintTemplate
{
    public string Finger { get; init; } = string.Empty; // e.g., "LeftThumb", "RightIndex"
    public int FingerIndex { get; init; } // 1-10
    public byte[] TemplateData { get; init; } = [];
    public string? ImagePath { get; init; }
}

/// <summary>
/// Request to enroll a student's fingerprints.
/// </summary>
public record EnrollmentRequest
{
    public string RegNo { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string ClassName { get; init; } = string.Empty;
    public List<FingerprintTemplate> Templates { get; init; } = [];
    public DateTime EnrolledAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Request to clock in with fingerprint.
/// </summary>
public record ClockInRequest
{
    public byte[] FingerprintTemplate { get; init; } = [];
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string? DeviceId { get; init; }
}

/// <summary>
/// Response from clock-in operation.
/// </summary>
public record ClockInResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public StudentInfo? Student { get; init; }
    public DateTime? ClockInTime { get; init; }
    public bool AlreadyClockedIn { get; init; }
}

/// <summary>
/// Request to clock out with fingerprint.
/// </summary>
public record ClockOutRequest
{
    public byte[] FingerprintTemplate { get; init; } = [];
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string? DeviceId { get; init; }
}

/// <summary>
/// Response from clock-out operation.
/// </summary>
public record ClockOutResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public StudentInfo? Student { get; init; }
    public DateTime? ClockInTime { get; init; }
    public DateTime? ClockOutTime { get; init; }
    public TimeSpan? Duration { get; init; }
    public bool NotClockedIn { get; init; }
}

/// <summary>
/// Enrollment status for a student.
/// </summary>
public record EnrollmentStatus
{
    public bool IsEnrolled { get; init; }
    public int EnrolledFingerCount { get; init; }
    public DateTime? EnrolledAt { get; init; }
}

/// <summary>
/// Generic operation result.
/// </summary>
public record DataResult
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public string? ErrorCode { get; init; }

    public static DataResult Ok(string? message = null) => new() { Success = true, Message = message };
    public static DataResult Fail(string message, string? errorCode = null) => new() { Success = false, Message = message, ErrorCode = errorCode };
}

/// <summary>
/// Generic operation result with data.
/// </summary>
public record DataResult<T> : DataResult
{
    public T? Data { get; init; }

    public static DataResult<T> Ok(T data, string? message = null) => new() { Success = true, Data = data, Message = message };
    public new static DataResult<T> Fail(string message, string? errorCode = null) => new() { Success = false, Message = message, ErrorCode = errorCode };
}

/// <summary>
/// Pending sync record for offline-first mode.
/// </summary>
public record PendingSyncRecord
{
    public long Id { get; init; }
    public string OperationType { get; init; } = string.Empty; // "Enrollment", "ClockIn", "ClockOut"
    public string JsonPayload { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public int RetryCount { get; init; }
    public string? LastError { get; init; }
}
