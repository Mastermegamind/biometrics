using Microsoft.Extensions.Logging;

namespace BiometricFingerprintsAttendanceSystem.Services.Data;

/// <summary>
/// Background refresher to keep local enrollment templates warm in memory.
/// </summary>
public sealed class EnrollmentCacheRefresher : IDisposable
{
    private readonly OfflineDataProvider _offline;
    private readonly ILogger<EnrollmentCacheRefresher> _logger;
    private readonly CancellationTokenSource _cts = new();
    private Task? _task;
    private bool _disposed;

    public EnrollmentCacheRefresher(OfflineDataProvider offline, ILogger<EnrollmentCacheRefresher> logger)
    {
        _offline = offline;
        _logger = logger;
    }

    public bool IsRunning => _task != null && !_task.IsCompleted;

    public void Start(TimeSpan? interval = null)
    {
        if (IsRunning)
        {
            return;
        }

        var refreshInterval = interval ?? TimeSpan.FromMinutes(2);
        _task = RunAsync(refreshInterval, _cts.Token);
        _logger.LogInformation("Enrollment cache refresher started with interval {Interval}", refreshInterval);
    }

    public async Task StopAsync()
    {
        if (!IsRunning) return;
        _cts.Cancel();
        if (_task != null)
        {
            try
            {
                await _task;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }
    }

    private async Task RunAsync(TimeSpan interval, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, ct);
                await _offline.GetAllEnrollmentsAsync();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to refresh enrollment cache");
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cts.Cancel();
        _cts.Dispose();
        _task?.Dispose();
    }
}
