using BiometricFingerprintsAttendanceSystem.Services.Api;
using BiometricFingerprintsAttendanceSystem.Services.Camera;
using BiometricFingerprintsAttendanceSystem.Services.Data;
using BiometricFingerprintsAttendanceSystem.Services.Db;
using BiometricFingerprintsAttendanceSystem.Services.Fingerprint;
using BiometricFingerprintsAttendanceSystem.Services.Security;
using Microsoft.Extensions.DependencyInjection;

namespace BiometricFingerprintsAttendanceSystem.Services;

public sealed class ServiceRegistry : IServiceRegistry
{
    private readonly IServiceProvider _provider;

    public ServiceRegistry(IServiceProvider provider)
    {
        _provider = provider;
        AppState = _provider.GetRequiredService<AppState>();
        ConnectionFactory = _provider.GetRequiredService<DbConnectionFactory>();
        Users = _provider.GetRequiredService<UserRepository>();
        Students = _provider.GetRequiredService<StudentRepository>();
        Attendance = _provider.GetRequiredService<AttendanceRepository>();
        Enrollment = _provider.GetRequiredService<EnrollmentRepository>();
        DemoFingerprints = _provider.GetRequiredService<DemoFingerprintRepository>();
        Camera = _provider.GetRequiredService<ICameraService>();
        Fingerprint = _provider.GetRequiredService<IFingerprintService>();
        Api = _provider.GetRequiredService<BiometricsApiClient>();
        DemoApi = _provider.GetService<DemoBiometricsApiClient>();
        Data = _provider.GetRequiredService<IDataService>();
        Audit = _provider.GetRequiredService<AuditLogService>();
        LoginAttempts = _provider.GetRequiredService<LoginAttemptRepository>();
        PasswordResets = _provider.GetRequiredService<PasswordResetRepository>();
        IsDemoMode = AppState.Config.EnableDemoMode;
    }

    public AppState AppState { get; }
    public DbConnectionFactory ConnectionFactory { get; }
    public UserRepository Users { get; }
    public StudentRepository Students { get; }
    public AttendanceRepository Attendance { get; }
    public EnrollmentRepository Enrollment { get; }
    public DemoFingerprintRepository DemoFingerprints { get; }
    public ICameraService Camera { get; }
    public IFingerprintService Fingerprint { get; }
    public BiometricsApiClient Api { get; }
    public DemoBiometricsApiClient? DemoApi { get; }
    public IDataService Data { get; }
    public AuditLogService Audit { get; }
    public LoginAttemptRepository LoginAttempts { get; }
    public PasswordResetRepository PasswordResets { get; }
    public bool IsDemoMode { get; }
    public IServiceProvider Provider => _provider;
}
