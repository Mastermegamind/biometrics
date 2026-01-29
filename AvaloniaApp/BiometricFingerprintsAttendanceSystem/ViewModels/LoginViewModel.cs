using Avalonia.Media;
using Avalonia.Threading;
using BiometricFingerprintsAttendanceSystem.Services;
using BiometricFingerprintsAttendanceSystem.Services.Fingerprint;

namespace BiometricFingerprintsAttendanceSystem.ViewModels;

public sealed class LoginViewModel : ViewModelBase
{
    private readonly IServiceRegistry _services;
    private readonly INavigationService _navigation;
    private readonly DispatcherTimer _monitorTimer;
    private bool _isMonitoring;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _statusMessage = string.Empty;
    private bool _isAdminLoginOpen;
    private string _apiStatus = "Checking…";
    private string _dbStatus = "Checking…";
    private string _fingerprintStatus = "Checking…";
    private string _lastChecked = "—";
    private IBrush _apiStatusBrush = Brushes.Orange;
    private IBrush _dbStatusBrush = Brushes.Orange;
    private IBrush _fingerprintStatusBrush = Brushes.Orange;

    public LoginViewModel(IServiceRegistry services, INavigationService navigation)
    {
        _services = services;
        _navigation = navigation;

        OpenLoginCommand = new RelayCommand(OpenLogin);
        CloseLoginCommand = new RelayCommand(CloseLogin);
        ClockInCommand = new RelayCommand(() => _navigation.NavigateToKey("LiveClockIn"));
        ClockOutCommand = new RelayCommand(() => _navigation.NavigateToKey("LiveClockOut"));
        LoginCommand = new AsyncRelayCommand(LoginAsync, CanLogin);

        _monitorTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        _monitorTimer.Tick += async (_, _) => await RefreshMonitorAsync();
        _monitorTimer.Start();
        _ = RefreshMonitorAsync();
    }

    public string Username
    {
        get => _username;
        set
        {
            if (SetField(ref _username, value))
            {
                LoginCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            if (SetField(ref _password, value))
            {
                LoginCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsAdminLoginOpen
    {
        get => _isAdminLoginOpen;
        private set => SetField(ref _isAdminLoginOpen, value);
    }

    public string ApiStatus
    {
        get => _apiStatus;
        private set => SetField(ref _apiStatus, value);
    }

    public string DbStatus
    {
        get => _dbStatus;
        private set => SetField(ref _dbStatus, value);
    }

    public string FingerprintStatus
    {
        get => _fingerprintStatus;
        private set => SetField(ref _fingerprintStatus, value);
    }

    public string LastChecked
    {
        get => _lastChecked;
        private set => SetField(ref _lastChecked, value);
    }

    public IBrush ApiStatusBrush
    {
        get => _apiStatusBrush;
        private set => SetField(ref _apiStatusBrush, value);
    }

    public IBrush DbStatusBrush
    {
        get => _dbStatusBrush;
        private set => SetField(ref _dbStatusBrush, value);
    }

    public IBrush FingerprintStatusBrush
    {
        get => _fingerprintStatusBrush;
        private set => SetField(ref _fingerprintStatusBrush, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    public RelayCommand OpenLoginCommand { get; }
    public RelayCommand CloseLoginCommand { get; }
    public RelayCommand ClockInCommand { get; }
    public RelayCommand ClockOutCommand { get; }
    public AsyncRelayCommand LoginCommand { get; }

    private bool CanLogin()
    {
        return !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
    }

    private void OpenLogin()
    {
        StatusMessage = string.Empty;
        IsAdminLoginOpen = true;
    }

    private void CloseLogin()
    {
        StatusMessage = string.Empty;
        IsAdminLoginOpen = false;
    }

    private async Task LoginAsync()
    {
        StatusMessage = "Signing in...";
        var userType = await _services.Users.GetUserTypeAsync(Username.Trim(), Password);
        if (string.IsNullOrWhiteSpace(userType))
        {
            StatusMessage = "Invalid username or password.";
            return;
        }

        if (!string.Equals(userType, "Administrator", StringComparison.OrdinalIgnoreCase))
        {
            StatusMessage = "Admin access only.";
            return;
        }

        StatusMessage = string.Empty;
        _services.AppState.CurrentUserType = userType;
        IsAdminLoginOpen = false;
        Username = string.Empty;
        Password = string.Empty;
        _navigation.NavigateToKey("Admin");
    }

    private async Task RefreshMonitorAsync()
    {
        if (_isMonitoring)
        {
            return;
        }

        _isMonitoring = true;
        try
        {
            await Task.WhenAll(UpdateApiStatusAsync(), UpdateDbStatusAsync());
            UpdateFingerprintStatus();
            LastChecked = DateTime.Now.ToString("HH:mm:ss");
        }
        finally
        {
            _isMonitoring = false;
        }
    }

    private async Task UpdateApiStatusAsync()
    {
        ApiStatus = "Checking…";
        ApiStatusBrush = Brushes.Orange;

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var ok = await _services.Api.PingAsync(cts.Token);
        ApiStatus = ok ? "Online" : "Offline";
        ApiStatusBrush = ok ? Brushes.LimeGreen : Brushes.IndianRed;
    }

    private async Task UpdateDbStatusAsync()
    {
        DbStatus = "Checking…";
        DbStatusBrush = Brushes.Orange;

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await using var conn = _services.ConnectionFactory.Create();
            await conn.OpenAsync(cts.Token);
            DbStatus = "Online";
            DbStatusBrush = Brushes.LimeGreen;
        }
        catch
        {
            DbStatus = "Offline";
            DbStatusBrush = Brushes.IndianRed;
        }
    }

    private void UpdateFingerprintStatus()
    {
        var device = _services.AppState.Config.FingerprintDevice?.Trim();
        if (string.IsNullOrWhiteSpace(device) || string.Equals(device, "None", StringComparison.OrdinalIgnoreCase))
        {
            FingerprintStatus = "Not configured";
            FingerprintStatusBrush = Brushes.Gray;
            return;
        }

        if (_services.Fingerprint is NotSupportedFingerprintService)
        {
            FingerprintStatus = $"{device} (SDK missing)";
            FingerprintStatusBrush = Brushes.IndianRed;
            return;
        }

        FingerprintStatus = $"{device} (Ready)";
        FingerprintStatusBrush = Brushes.LimeGreen;
    }
}
