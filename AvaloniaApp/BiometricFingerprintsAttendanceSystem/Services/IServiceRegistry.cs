using BiometricFingerprintsAttendanceSystem.Services.Api;
using BiometricFingerprintsAttendanceSystem.Services.Camera;
using BiometricFingerprintsAttendanceSystem.Services.Data;
using BiometricFingerprintsAttendanceSystem.Services.Db;
using BiometricFingerprintsAttendanceSystem.Services.Fingerprint;
using BiometricFingerprintsAttendanceSystem.Services.Security;

namespace BiometricFingerprintsAttendanceSystem.Services;

public interface IServiceRegistry
{
    AppState AppState { get; }
    DbConnectionFactory ConnectionFactory { get; }
    UserRepository Users { get; }
    StudentRepository Students { get; }
    AttendanceRepository Attendance { get; }
    EnrollmentRepository Enrollment { get; }
    DemoFingerprintRepository DemoFingerprints { get; }
    ICameraService Camera { get; }
    IFingerprintService Fingerprint { get; }
    BiometricsApiClient Api { get; }
    DemoBiometricsApiClient? DemoApi { get; }
    OnlineDataProvider OnlineData { get; }
    OnlineTemplateMatcher OnlineMatcher { get; }
    IDataService Data { get; }
    AuditLogService Audit { get; }
    LoginAttemptRepository LoginAttempts { get; }
    PasswordResetRepository PasswordResets { get; }
    bool IsDemoMode { get; }
    IServiceProvider Provider { get; }
}
