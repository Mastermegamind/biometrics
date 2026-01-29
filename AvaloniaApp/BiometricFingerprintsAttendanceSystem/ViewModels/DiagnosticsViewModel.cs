using Avalonia.Media;
using BiometricFingerprintsAttendanceSystem.Services;
using BiometricFingerprintsAttendanceSystem.Services.Data;
using BiometricFingerprintsAttendanceSystem.Services.Fingerprint;
using Microsoft.Extensions.DependencyInjection;

namespace BiometricFingerprintsAttendanceSystem.ViewModels;

public sealed class DiagnosticsViewModel : ViewModelBase
{
    private readonly IServiceRegistry _services;
    private readonly SyncManager _syncManager;
    private string _apiStatus = "Checking…";
    private string _dbStatus = "Checking…";
    private string _fingerprintStatus = "Checking…";
    private string _syncStatus = "—";
    private string _pendingSync = "0";
    private string _lastChecked = "—";
    private IBrush _apiBrush = Brushes.Orange;
    private IBrush _dbBrush = Brushes.Orange;
    private IBrush _fingerprintBrush = Brushes.Orange;

    public DiagnosticsViewModel(IServiceRegistry services)
    {
        _services = services;
        _syncManager = services.Provider.GetRequiredService<SyncManager>();
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        _ = RefreshAsync();
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

    public string SyncStatus
    {
        get => _syncStatus;
        private set => SetField(ref _syncStatus, value);
    }

    public string PendingSync
    {
        get => _pendingSync;
        private set => SetField(ref _pendingSync, value);
    }

    public string LastChecked
    {
        get => _lastChecked;
        private set => SetField(ref _lastChecked, value);
    }

    public IBrush ApiBrush
    {
        get => _apiBrush;
        private set => SetField(ref _apiBrush, value);
    }

    public IBrush DbBrush
    {
        get => _dbBrush;
        private set => SetField(ref _dbBrush, value);
    }

    public IBrush FingerprintBrush
    {
        get => _fingerprintBrush;
        private set => SetField(ref _fingerprintBrush, value);
    }

    public AsyncRelayCommand RefreshCommand { get; }

    private async Task RefreshAsync()
    {
        await Task.WhenAll(UpdateApiStatusAsync(), UpdateDbStatusAsync());
        UpdateFingerprintStatus();
        UpdateSyncStatus();
        LastChecked = DateTime.Now.ToString("HH:mm:ss");
    }

    private async Task UpdateApiStatusAsync()
    {
        ApiStatus = "Checking…";
        ApiBrush = Brushes.Orange;

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var ok = await _services.Api.PingAsync(cts.Token);
        ApiStatus = ok ? "Online" : "Offline";
        ApiBrush = ok ? Brushes.LimeGreen : Brushes.IndianRed;
    }

    private async Task UpdateDbStatusAsync()
    {
        DbStatus = "Checking…";
        DbBrush = Brushes.Orange;

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await using var conn = _services.ConnectionFactory.Create();
            await conn.OpenAsync(cts.Token);
            DbStatus = "Online";
            DbBrush = Brushes.LimeGreen;
        }
        catch
        {
            DbStatus = "Offline";
            DbBrush = Brushes.IndianRed;
        }
    }

    private void UpdateFingerprintStatus()
    {
        var device = _services.AppState.Config.FingerprintDevice?.Trim();
        if (string.IsNullOrWhiteSpace(device) || string.Equals(device, "None", StringComparison.OrdinalIgnoreCase))
        {
            FingerprintStatus = "Not configured";
            FingerprintBrush = Brushes.Gray;
            return;
        }

        if (_services.Fingerprint is NotSupportedFingerprintService)
        {
            FingerprintStatus = $"{device} (SDK missing)";
            FingerprintBrush = Brushes.IndianRed;
            return;
        }

        FingerprintStatus = $"{device} (Ready)";
        FingerprintBrush = Brushes.LimeGreen;
    }

    private void UpdateSyncStatus()
    {
        PendingSync = _syncManager.PendingCount.ToString();
        SyncStatus = _syncManager.IsRunning ? "Running" : "Stopped";
    }
}
