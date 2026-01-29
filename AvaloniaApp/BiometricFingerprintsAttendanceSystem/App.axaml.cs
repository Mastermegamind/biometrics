using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using BiometricFingerprintsAttendanceSystem.Services;
using BiometricFingerprintsAttendanceSystem.Services.Api;
using BiometricFingerprintsAttendanceSystem.Services.Camera;
using BiometricFingerprintsAttendanceSystem.Services.Config;
using BiometricFingerprintsAttendanceSystem.Services.Data;
using BiometricFingerprintsAttendanceSystem.Services.Db;
using BiometricFingerprintsAttendanceSystem.Services.Fingerprint;
using BiometricFingerprintsAttendanceSystem.Services.Logging;
using BiometricFingerprintsAttendanceSystem.Services.Security;
using BiometricFingerprintsAttendanceSystem.ViewModels;
using BiometricFingerprintsAttendanceSystem.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BiometricFingerprintsAttendanceSystem;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var splash = new SplashWindow();
            desktop.MainWindow = splash;
            splash.Show();

            await Task.Delay(1500);

            var serviceProvider = ConfigureServices();
            var services = new ServiceRegistry(serviceProvider);
            serviceProvider.GetRequiredService<SyncManager>().Start();
            serviceProvider.GetRequiredService<EnrollmentCacheRefresher>().Start();

            var mainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(services)
            };

            desktop.MainWindow = mainWindow;
            mainWindow.Show();
            splash.Close();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        var config = AppConfig.Load();

        // Configuration & State
        services.AddSingleton(config);
        services.AddSingleton<AppState>();
        services.AddSingleton(new ApiSettings(
            config.ApiBaseUrl,
            config.ApiKey,
            config.ApiKeyHeader,
            config.StudentLookupPath,
            config.EnrollmentStatusPath,
            config.EnrollmentSubmitPath,
            config.IdentifyPath));
        services.AddMemoryCache();

        // Database
        services.AddSingleton<DbConnectionFactory>();
        services.AddSingleton<UserRepository>();
        services.AddSingleton<StudentRepository>();
        services.AddSingleton<AttendanceRepository>();
        services.AddSingleton<EnrollmentRepository>();
        services.AddSingleton<DemoFingerprintRepository>();

        // Services
        services.AddSingleton<ICameraService, OpenCvCameraService>();
        services.AddSingleton<IFingerprintService>(sp => ResolveFingerprintService(config));
        services.AddSingleton<BiometricsApiClient>();

        if (config.EnableDemoMode)
        {
            services.AddSingleton<DemoBiometricsApiClient>(sp =>
            {
                var apiClient = sp.GetRequiredService<BiometricsApiClient>();
                return new DemoBiometricsApiClient(apiClient, config.DemoStudentRegNo, config.DemoStudentName, config.DemoStudentClass);
            });
        }

        // Data Services (Online/Offline/Hybrid modes)
        services.AddSingleton<OnlineDataProvider>();
        services.AddSingleton<OfflineDataProvider>();
        services.AddSingleton<IDataService, DataService>();
        services.AddSingleton<AuditLogService>();
        services.AddSingleton<LoginAttemptRepository>();
        services.AddSingleton<PasswordResetRepository>();
        services.AddSingleton<EnrollmentCacheRefresher>();
        services.AddSingleton<AppConfigService>();

        // ViewModels
        services.AddTransient<LoginViewModel>();
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<RegisterViewModel>();
        services.AddTransient<VerificationViewModel>();
        services.AddTransient<TimeOutViewModel>();
        services.AddTransient<UserHomeViewModel>();
        services.AddTransient<AdminHomeViewModel>();
        services.AddTransient<AdminRegistrationViewModel>();
        services.AddTransient<AdminPasswordResetViewModel>();
        services.AddTransient<DiagnosticsViewModel>();
        services.AddTransient<AdminSettingsViewModel>();
        services.AddTransient<EnrollmentViewModel>();
        services.AddTransient<DemoViewModel>();
        services.AddTransient<AttendanceReportViewModel>();

        // Live Mode ViewModels
        services.AddTransient<LiveEnrollmentViewModel>();
        services.AddTransient<LiveClockInViewModel>();
        services.AddTransient<LiveClockOutViewModel>();

        // Sync Manager
        services.AddSingleton<SyncManager>();

        // Logging
        services.AddLogging(logging =>
        {
            logging.AddConsole();
            logging.AddProvider(new JsonFileLoggerProvider(Path.Combine(AppContext.BaseDirectory, "logs", "app.log")));
            logging.SetMinimumLevel(LogLevel.Information);
        });

        return services.BuildServiceProvider();
    }

    private static IFingerprintService ResolveFingerprintService(AppConfig config)
    {
        if (!TryParseDevice(config.FingerprintDevice, out var device))
        {
            return new NotSupportedFingerprintService();
        }

        if (device == FingerprintDeviceType.Auto)
        {
            return ResolveAutoDetectedService(config);
        }

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
            if (config.EnableFingerprintSdks)
            {
                return new DigitalPersonaFingerprintService();
            }
            return new WindowsBiometricFingerprintService();
        }

        if (OperatingSystem.IsLinux())
        {
            return new LinuxFprintdService();
        }

        return new NotSupportedFingerprintService();
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
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
            FingerprintDeviceType.DigitalPersona4500 => new LinuxFprintdService(),
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

        if (Enum.TryParse<FingerprintDeviceType>(value, ignoreCase: true, out device))
        {
            return true;
        }

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
