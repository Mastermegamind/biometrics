using BiometricFingerprintsAttendanceSystem.Services.Api;
using BiometricFingerprintsAttendanceSystem.Services.Camera;
using BiometricFingerprintsAttendanceSystem.Services.Db;
using BiometricFingerprintsAttendanceSystem.Services.Fingerprint;
using System;
using System.Runtime.Versioning;

namespace BiometricFingerprintsAttendanceSystem.Services;

public sealed class ServiceRegistry : IServiceRegistry
{
    public ServiceRegistry(AppState state)
    {
        AppState = state;
        IsDemoMode = state.Config.EnableDemoMode;
        ConnectionFactory = new DbConnectionFactory(state.Config.ConnectionString);

        // Initialize UserRepository with demo credentials if demo mode is enabled
        Users = new UserRepository(
            ConnectionFactory,
            state.Config.EnableDemoMode,
            state.Config.DemoAdminEmail,
            state.Config.DemoAdminPassword);

        Students = new StudentRepository(ConnectionFactory);
        Attendance = new AttendanceRepository(ConnectionFactory);
        Enrollment = new EnrollmentRepository(ConnectionFactory);
        DemoFingerprints = new DemoFingerprintRepository(ConnectionFactory);
        Camera = new OpenCvCameraService();

        Api = new BiometricsApiClient(new ApiSettings(
            state.Config.ApiBaseUrl,
            state.Config.ApiKey,
            state.Config.ApiKeyHeader,
            state.Config.StudentLookupPath,
            state.Config.EnrollmentStatusPath,
            state.Config.EnrollmentSubmitPath,
            state.Config.IdentifyPath));

        // Initialize DemoApi wrapper if demo mode is enabled
        DemoApi = state.Config.EnableDemoMode
            ? new DemoBiometricsApiClient(
                Api,
                state.Config.DemoStudentRegNo,
                state.Config.DemoStudentName,
                state.Config.DemoStudentClass)
            : null;

        Fingerprint = ResolveFingerprintService(state.Config);
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
    public bool IsDemoMode { get; }

    private static IFingerprintService ResolveFingerprintService(AppConfig config)
    {
        if (!TryParseDevice(config.FingerprintDevice, out var device))
        {
            return new NotSupportedFingerprintService();
        }

        // Handle auto-detection based on platform
        if (device == FingerprintDeviceType.Auto)
        {
            return ResolveAutoDetectedService(config);
        }

        // Platform-specific service resolution
        if (OperatingSystem.IsWindows())
        {
            return ResolveWindowsService(device);
        }

        if (OperatingSystem.IsLinux())
        {
            return ResolveLinuxService(device);
        }

        return new NotSupportedFingerprintService();
    }

    private static IFingerprintService ResolveAutoDetectedService(AppConfig config)
    {
        if (OperatingSystem.IsWindows())
        {
            // Windows: Try DigitalPersona SDK first, then WBF fallback
            if (config.EnableFingerprintSdks)
            {
                var dpService = new DigitalPersonaFingerprintService();
                // Check if SDK is available by attempting initialization
                return dpService;
            }

            // Fall back to Windows Biometric Framework
            return new WindowsBiometricFingerprintService();
        }

        if (OperatingSystem.IsLinux())
        {
            // Linux: Use fprintd for all devices
            return new LinuxFprintdService();
        }

        return new NotSupportedFingerprintService();
    }

    [SupportedOSPlatform("windows")]
    private static IFingerprintService ResolveWindowsService(FingerprintDeviceType device)
    {
        return device switch
        {
            FingerprintDeviceType.DigitalPersona4500 => new DigitalPersonaFingerprintService(),
            FingerprintDeviceType.MantraMfs100 => new MantraFingerprintService(),
            FingerprintDeviceType.WindowsBiometric => new WindowsBiometricFingerprintService(),
            FingerprintDeviceType.None => new NotSupportedFingerprintService(),
            _ => new NotSupportedFingerprintService()
        };
    }

    private static IFingerprintService ResolveLinuxService(FingerprintDeviceType device)
    {
        // All Linux fingerprint devices use fprintd/libfprint
        // The specific device type is for informational purposes
        return device switch
        {
            FingerprintDeviceType.Fprintd => new LinuxFprintdService(),
            FingerprintDeviceType.ValiditySensors => new LinuxFprintdService(),
            FingerprintDeviceType.Goodix => new LinuxFprintdService(),
            FingerprintDeviceType.Synaptics => new LinuxFprintdService(),
            FingerprintDeviceType.Elan => new LinuxFprintdService(),
            FingerprintDeviceType.FocalTech => new LinuxFprintdService(),
            FingerprintDeviceType.LighTuning => new LinuxFprintdService(),
            FingerprintDeviceType.Upek => new LinuxFprintdService(),
            FingerprintDeviceType.DigitalPersona4500 => new LinuxFprintdService(), // DP works on Linux via fprintd
            FingerprintDeviceType.None => new NotSupportedFingerprintService(),
            _ => new NotSupportedFingerprintService()
        };
    }

    private static bool TryParseDevice(string? value, out FingerprintDeviceType device)
    {
        device = FingerprintDeviceType.None;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        // Try enum parsing first (handles all enum values)
        if (Enum.TryParse<FingerprintDeviceType>(value, ignoreCase: true, out device))
        {
            return true;
        }

        // Handle common aliases
        var normalizedValue = value.Trim().ToLowerInvariant();
        device = normalizedValue switch
        {
            "digitalpersona" or "dp4500" or "uareu" => FingerprintDeviceType.DigitalPersona4500,
            "mantra" or "mfs100" => FingerprintDeviceType.MantraMfs100,
            "wbf" or "windowshello" or "hello" => FingerprintDeviceType.WindowsBiometric,
            "libfprint" or "fprint" => FingerprintDeviceType.Fprintd,
            "validity" => FingerprintDeviceType.ValiditySensors,
            "authentec" => FingerprintDeviceType.Upek,
            "detect" or "autodetect" => FingerprintDeviceType.Auto,
            _ => FingerprintDeviceType.None
        };

        return device != FingerprintDeviceType.None || string.Equals(value, "None", StringComparison.OrdinalIgnoreCase);
    }
}
