using BiometricFingerprintsAttendanceSystem.Services;
using BiometricFingerprintsAttendanceSystem.Services.Config;
using Microsoft.Extensions.DependencyInjection;

namespace BiometricFingerprintsAttendanceSystem.ViewModels;

public sealed class AdminSettingsViewModel : ViewModelBase
{
    private readonly IServiceRegistry _services;
    private readonly AppConfigService _configService;
    private string _apiBaseUrl;
    private string _syncMode;
    private string _fingerprintDevice;
    private bool _demoEnabled;
    private string _statusMessage = string.Empty;

    public AdminSettingsViewModel(IServiceRegistry services)
    {
        _services = services;
        _configService = services.Provider.GetRequiredService<AppConfigService>();

        var config = services.AppState.Config;
        _apiBaseUrl = config.ApiBaseUrl;
        _syncMode = config.SyncMode.ToString();
        _fingerprintDevice = config.FingerprintDevice;
        _demoEnabled = config.EnableDemoMode;

        SaveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
    }

    public string ApiBaseUrl
    {
        get => _apiBaseUrl;
        set
        {
            if (SetField(ref _apiBaseUrl, value))
            {
                SaveCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string SyncMode
    {
        get => _syncMode;
        set
        {
            if (SetField(ref _syncMode, value))
            {
                SaveCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string FingerprintDevice
    {
        get => _fingerprintDevice;
        set
        {
            if (SetField(ref _fingerprintDevice, value))
            {
                SaveCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool DemoEnabled
    {
        get => _demoEnabled;
        set
        {
            if (SetField(ref _demoEnabled, value))
            {
                SaveCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    public AsyncRelayCommand SaveCommand { get; }

    private bool CanSave()
    {
        return !string.IsNullOrWhiteSpace(ApiBaseUrl)
            && !string.IsNullOrWhiteSpace(SyncMode)
            && !string.IsNullOrWhiteSpace(FingerprintDevice);
    }

    private async Task SaveAsync()
    {
        var saved = await _configService.SaveSettingsAsync(ApiBaseUrl.Trim(), SyncMode.Trim(), FingerprintDevice.Trim(), DemoEnabled);
        if (!saved)
        {
            StatusMessage = "Failed to save settings.";
            await _services.Audit.LogAsync(_services.AppState.CurrentUsername, "SettingsSave", null, "Failed", "Settings save failed");
            return;
        }

        StatusMessage = "Settings saved. Restart required.";
        await _services.Audit.LogAsync(_services.AppState.CurrentUsername, "SettingsSave", null, "Success", "Settings saved");
    }
}
