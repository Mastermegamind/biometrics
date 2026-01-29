using BiometricFingerprintsAttendanceSystem.Services.Api;
using BiometricFingerprintsAttendanceSystem.Services.Camera;
using BiometricFingerprintsAttendanceSystem.Services.Db;
using BiometricFingerprintsAttendanceSystem.Services.Fingerprint;

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
    bool IsDemoMode { get; }
}
