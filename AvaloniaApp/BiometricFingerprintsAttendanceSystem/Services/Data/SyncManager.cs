using Microsoft.Extensions.Logging;

namespace BiometricFingerprintsAttendanceSystem.Services.Data;

/// <summary>
/// Background service that periodically syncs pending records to the API.
/// Used primarily for OfflineFirst and OnlineFirst modes.
/// </summary>
public class SyncManager : IDisposable
{
    private readonly IDataService _dataService;
    private readonly ILogger<SyncManager> _logger;
    private readonly AppConfig _config;
    private readonly CancellationTokenSource _cts = new();
    private Task? _syncTask;
    private bool _disposed;

    public event EventHandler<SyncEventArgs>? SyncCompleted;
    public event EventHandler<SyncEventArgs>? SyncStarted;
    public event EventHandler<SyncErrorEventArgs>? SyncError;

    public SyncManager(IDataService dataService, AppConfig config, ILogger<SyncManager> logger)
    {
        _dataService = dataService;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Whether the sync manager is currently running.
    /// </summary>
    public bool IsRunning => _syncTask != null && !_syncTask.IsCompleted;

    /// <summary>
    /// Number of pending sync records.
    /// </summary>
    public int PendingCount => _dataService.PendingSyncCount;

    /// <summary>
    /// Current sync mode.
    /// </summary>
    public SyncMode Mode => _dataService.Mode;

    /// <summary>
    /// Whether the API is currently online.
    /// </summary>
    public bool IsOnline => _dataService.IsOnline;

    /// <summary>
    /// Start the background sync process.
    /// </summary>
    public void Start(TimeSpan? interval = null)
    {
        if (Mode == SyncMode.OnlineOnly || Mode == SyncMode.OfflineOnly)
        {
            _logger.LogInformation("Sync manager not needed for {Mode} mode", Mode);
            return;
        }

        if (IsRunning)
        {
            _logger.LogWarning("Sync manager is already running");
            return;
        }

        var syncInterval = interval ?? TimeSpan.FromMinutes(5);
        _syncTask = RunSyncLoopAsync(syncInterval, _cts.Token);
        _logger.LogInformation("Sync manager started with interval {Interval}", syncInterval);
    }

    /// <summary>
    /// Stop the background sync process.
    /// </summary>
    public async Task StopAsync()
    {
        if (!IsRunning) return;

        _cts.Cancel();

        if (_syncTask != null)
        {
            try
            {
                await _syncTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }

        _logger.LogInformation("Sync manager stopped");
    }

    /// <summary>
    /// Manually trigger a sync operation.
    /// </summary>
    public async Task<DataResult> SyncNowAsync()
    {
        if (Mode == SyncMode.OnlineOnly || Mode == SyncMode.OfflineOnly)
        {
            return DataResult.Ok("No sync needed for this mode");
        }

        return await PerformSyncAsync();
    }

    private async Task RunSyncLoopAsync(TimeSpan interval, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, ct);
                await PerformSyncAsync();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in sync loop");
                SyncError?.Invoke(this, new SyncErrorEventArgs(ex));
            }
        }
    }

    private async Task<DataResult> PerformSyncAsync()
    {
        if (_dataService.PendingSyncCount == 0)
        {
            return DataResult.Ok("No pending records");
        }

        SyncStarted?.Invoke(this, new SyncEventArgs(_dataService.PendingSyncCount));

        try
        {
            var result = await _dataService.SyncPendingAsync();

            SyncCompleted?.Invoke(this, new SyncEventArgs(
                _dataService.PendingSyncCount,
                result.Success,
                result.Message));

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync failed");
            SyncError?.Invoke(this, new SyncErrorEventArgs(ex));
            return DataResult.Fail(ex.Message);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _cts.Cancel();
        _cts.Dispose();
        _syncTask?.Dispose();
    }
}

/// <summary>
/// Event args for sync events.
/// </summary>
public class SyncEventArgs : EventArgs
{
    public int PendingCount { get; }
    public bool Success { get; }
    public string? Message { get; }

    public SyncEventArgs(int pendingCount, bool success = true, string? message = null)
    {
        PendingCount = pendingCount;
        Success = success;
        Message = message;
    }
}

/// <summary>
/// Event args for sync errors.
/// </summary>
public class SyncErrorEventArgs : EventArgs
{
    public Exception Exception { get; }

    public SyncErrorEventArgs(Exception exception)
    {
        Exception = exception;
    }
}
