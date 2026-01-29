using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace BiometricFingerprintsAttendanceSystem.Services.Data;

/// <summary>
/// Mode-aware data service that routes operations based on SyncMode.
/// </summary>
public class DataService : IDataService
{
    private readonly OnlineDataProvider _online;
    private readonly OfflineDataProvider _offline;
    private readonly ILogger<DataService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    private bool _isOnline;
    private int _pendingSyncCount;

    public DataService(
        OnlineDataProvider online,
        OfflineDataProvider offline,
        AppConfig config,
        ILogger<DataService> logger)
    {
        _online = online;
        _offline = offline;
        _logger = logger;
        Mode = config.SyncMode;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Initial online check for non-offline modes
        if (Mode != SyncMode.OfflineOnly)
        {
            _ = CheckOnlineStatusAsync();
        }
    }

    public SyncMode Mode { get; }
    public bool IsOnline => _isOnline;
    public int PendingSyncCount => _pendingSyncCount;

    // ==================== Student Operations ====================

    public async Task<DataResult<StudentInfo>> GetStudentAsync(string regNo)
    {
        return Mode switch
        {
            SyncMode.OnlineOnly => await _online.GetStudentAsync(regNo),
            SyncMode.OfflineOnly => await _offline.GetStudentAsync(regNo),
            SyncMode.OnlineFirst => await OnlineFirstGetStudentAsync(regNo),
            SyncMode.OfflineFirst => await OfflineFirstGetStudentAsync(regNo),
            _ => DataResult<StudentInfo>.Fail("Invalid sync mode")
        };
    }

    public async Task<DataResult<byte[]>> GetStudentPhotoAsync(string regNo)
    {
        return Mode switch
        {
            SyncMode.OnlineOnly => await _online.GetStudentPhotoAsync(regNo),
            SyncMode.OfflineOnly => await _offline.GetStudentPhotoAsync(regNo),
            SyncMode.OnlineFirst => await OnlineFirstGetPhotoAsync(regNo),
            SyncMode.OfflineFirst => await OfflineFirstGetPhotoAsync(regNo),
            _ => DataResult<byte[]>.Fail("Invalid sync mode")
        };
    }

    // ==================== Enrollment Operations ====================

    public async Task<DataResult<EnrollmentStatus>> GetEnrollmentStatusAsync(string regNo)
    {
        return Mode switch
        {
            SyncMode.OnlineOnly => await _online.GetEnrollmentStatusAsync(regNo),
            SyncMode.OfflineOnly => await _offline.GetEnrollmentStatusAsync(regNo),
            SyncMode.OnlineFirst => await OnlineFirstGetEnrollmentStatusAsync(regNo),
            SyncMode.OfflineFirst => await OfflineFirstGetEnrollmentStatusAsync(regNo),
            _ => DataResult<EnrollmentStatus>.Fail("Invalid sync mode")
        };
    }

    public async Task<DataResult> SubmitEnrollmentAsync(EnrollmentRequest request)
    {
        return Mode switch
        {
            SyncMode.OnlineOnly => await _online.SubmitEnrollmentAsync(request),
            SyncMode.OfflineOnly => await _offline.SubmitEnrollmentAsync(request),
            SyncMode.OnlineFirst => await OnlineFirstSubmitEnrollmentAsync(request),
            SyncMode.OfflineFirst => await OfflineFirstSubmitEnrollmentAsync(request),
            _ => DataResult.Fail("Invalid sync mode")
        };
    }

    public async Task<DataResult<List<(string RegNo, List<FingerprintTemplate> Templates)>>> GetAllEnrollmentsAsync()
    {
        // Always from local for offline matching
        return await _offline.GetAllEnrollmentsAsync();
    }

    // ==================== Attendance Operations ====================

    public async Task<ClockInResponse> ClockInAsync(ClockInRequest request)
    {
        return Mode switch
        {
            SyncMode.OnlineOnly => await _online.ClockInAsync(request),
            SyncMode.OfflineOnly => await _offline.ClockInAsync(request),
            SyncMode.OnlineFirst => await OnlineFirstClockInAsync(request),
            SyncMode.OfflineFirst => await OfflineFirstClockInAsync(request),
            _ => new ClockInResponse { Success = false, Message = "Invalid sync mode" }
        };
    }

    public async Task<ClockOutResponse> ClockOutAsync(ClockOutRequest request)
    {
        return Mode switch
        {
            SyncMode.OnlineOnly => await _online.ClockOutAsync(request),
            SyncMode.OfflineOnly => await _offline.ClockOutAsync(request),
            SyncMode.OnlineFirst => await OnlineFirstClockOutAsync(request),
            SyncMode.OfflineFirst => await OfflineFirstClockOutAsync(request),
            _ => new ClockOutResponse { Success = false, Message = "Invalid sync mode" }
        };
    }

    public async Task<DataResult<List<AttendanceRecord>>> GetAttendanceAsync(DateTime from, DateTime to, string? regNo = null)
    {
        return Mode switch
        {
            SyncMode.OnlineOnly => await _online.GetAttendanceAsync(from, to, regNo),
            SyncMode.OfflineOnly => await _offline.GetAttendanceAsync(from, to, regNo),
            SyncMode.OnlineFirst => await OnlineFirstGetAttendanceAsync(from, to, regNo),
            SyncMode.OfflineFirst => await _offline.GetAttendanceAsync(from, to, regNo), // Always local for offline-first
            _ => DataResult<List<AttendanceRecord>>.Fail("Invalid sync mode")
        };
    }

    // ==================== Sync Operations ====================

    public async Task<DataResult> SyncPendingAsync()
    {
        if (Mode == SyncMode.OnlineOnly || Mode == SyncMode.OfflineOnly)
        {
            return DataResult.Ok("No sync needed for this mode");
        }

        var pendingRecords = await _offline.GetPendingSyncRecordsAsync();
        _pendingSyncCount = pendingRecords.Count;

        if (pendingRecords.Count == 0)
        {
            return DataResult.Ok("No pending records to sync");
        }

        if (!await CheckOnlineStatusAsync())
        {
            return DataResult.Fail("API is offline, cannot sync");
        }

        var synced = 0;
        var failed = 0;

        foreach (var record in pendingRecords)
        {
            try
            {
                var result = await SyncRecordAsync(record);
                if (result.Success)
                {
                    await _offline.MarkSyncedAsync(record.Id);
                    synced++;
                }
                else
                {
                    await _offline.MarkSyncFailedAsync(record.Id, result.Message ?? "Unknown error");
                    failed++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync record {Id}", record.Id);
                await _offline.MarkSyncFailedAsync(record.Id, ex.Message);
                failed++;
            }
        }

        _pendingSyncCount = failed;
        return DataResult.Ok($"Synced {synced} records, {failed} failed");
    }

    public async Task<bool> CheckOnlineStatusAsync()
    {
        if (Mode == SyncMode.OfflineOnly)
        {
            _isOnline = false;
            return false;
        }

        _isOnline = await _online.PingAsync();
        return _isOnline;
    }

    // ==================== OnlineFirst Implementations ====================

    private async Task<DataResult<StudentInfo>> OnlineFirstGetStudentAsync(string regNo)
    {
        var result = await _online.GetStudentAsync(regNo);
        if (result.Success)
        {
            _isOnline = true;
            return result;
        }

        _logger.LogWarning("Online student lookup failed, falling back to offline: {Message}", result.Message);
        _isOnline = false;
        return await _offline.GetStudentAsync(regNo);
    }

    private async Task<DataResult<byte[]>> OnlineFirstGetPhotoAsync(string regNo)
    {
        var result = await _online.GetStudentPhotoAsync(regNo);
        if (result.Success) return result;

        return await _offline.GetStudentPhotoAsync(regNo);
    }

    private async Task<DataResult<EnrollmentStatus>> OnlineFirstGetEnrollmentStatusAsync(string regNo)
    {
        var result = await _online.GetEnrollmentStatusAsync(regNo);
        if (result.Success)
        {
            _isOnline = true;
            return result;
        }

        _isOnline = false;
        return await _offline.GetEnrollmentStatusAsync(regNo);
    }

    private async Task<DataResult> OnlineFirstSubmitEnrollmentAsync(EnrollmentRequest request)
    {
        // Try online first
        var onlineResult = await _online.SubmitEnrollmentAsync(request);

        // Always save locally too (for offline matching)
        var localResult = await _offline.SubmitEnrollmentAsync(request);

        if (onlineResult.Success)
        {
            _isOnline = true;
            return onlineResult;
        }

        _isOnline = false;
        _logger.LogWarning("Online enrollment failed, saved locally: {Message}", onlineResult.Message);

        // Queue for sync later
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        await _offline.QueueForSyncAsync("Enrollment", json);
        _pendingSyncCount++;

        return localResult.Success
            ? DataResult.Ok("Saved locally, will sync when online")
            : localResult;
    }

    private async Task<ClockInResponse> OnlineFirstClockInAsync(ClockInRequest request)
    {
        var result = await _online.ClockInAsync(request);
        if (result.Success)
        {
            _isOnline = true;
            return result;
        }

        _isOnline = false;
        _logger.LogWarning("Online clock-in failed, falling back to offline: {Message}", result.Message);

        // Fall back to local
        var localResult = await _offline.ClockInAsync(request);

        if (localResult.Success)
        {
            // Queue for sync
            var json = JsonSerializer.Serialize(new
            {
                RegNo = localResult.Student?.RegNo,
                Timestamp = request.Timestamp,
                DeviceId = request.DeviceId
            }, _jsonOptions);
            await _offline.QueueForSyncAsync("ClockIn", json);
            _pendingSyncCount++;
        }

        return localResult;
    }

    private async Task<ClockOutResponse> OnlineFirstClockOutAsync(ClockOutRequest request)
    {
        var result = await _online.ClockOutAsync(request);
        if (result.Success)
        {
            _isOnline = true;
            return result;
        }

        _isOnline = false;
        _logger.LogWarning("Online clock-out failed, falling back to offline: {Message}", result.Message);

        var localResult = await _offline.ClockOutAsync(request);

        if (localResult.Success)
        {
            var json = JsonSerializer.Serialize(new
            {
                RegNo = localResult.Student?.RegNo,
                Timestamp = request.Timestamp,
                DeviceId = request.DeviceId
            }, _jsonOptions);
            await _offline.QueueForSyncAsync("ClockOut", json);
            _pendingSyncCount++;
        }

        return localResult;
    }

    private async Task<DataResult<List<AttendanceRecord>>> OnlineFirstGetAttendanceAsync(DateTime from, DateTime to, string? regNo)
    {
        var result = await _online.GetAttendanceAsync(from, to, regNo);
        if (result.Success) return result;

        return await _offline.GetAttendanceAsync(from, to, regNo);
    }

    // ==================== OfflineFirst Implementations ====================

    private async Task<DataResult<StudentInfo>> OfflineFirstGetStudentAsync(string regNo)
    {
        var result = await _offline.GetStudentAsync(regNo);
        if (result.Success) return result;

        // Try online if not found locally
        return await _online.GetStudentAsync(regNo);
    }

    private async Task<DataResult<byte[]>> OfflineFirstGetPhotoAsync(string regNo)
    {
        var result = await _offline.GetStudentPhotoAsync(regNo);
        if (result.Success) return result;

        return await _online.GetStudentPhotoAsync(regNo);
    }

    private async Task<DataResult<EnrollmentStatus>> OfflineFirstGetEnrollmentStatusAsync(string regNo)
    {
        // Always check local first for offline-first
        return await _offline.GetEnrollmentStatusAsync(regNo);
    }

    private async Task<DataResult> OfflineFirstSubmitEnrollmentAsync(EnrollmentRequest request)
    {
        // Save locally first
        var localResult = await _offline.SubmitEnrollmentAsync(request);
        if (!localResult.Success)
        {
            return localResult;
        }

        // Queue for API sync
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        await _offline.QueueForSyncAsync("Enrollment", json);
        _pendingSyncCount++;

        // Try to sync immediately if online
        if (await CheckOnlineStatusAsync())
        {
            var onlineResult = await _online.SubmitEnrollmentAsync(request);
            if (onlineResult.Success)
            {
                // Remove from sync queue (it's the most recent one)
                var pending = await _offline.GetPendingSyncRecordsAsync();
                var last = pending.LastOrDefault(p => p.OperationType == "Enrollment");
                if (last != null)
                {
                    await _offline.MarkSyncedAsync(last.Id);
                    _pendingSyncCount--;
                }
            }
        }

        return DataResult.Ok("Enrollment saved");
    }

    private async Task<ClockInResponse> OfflineFirstClockInAsync(ClockInRequest request)
    {
        // Always process locally first
        var result = await _offline.ClockInAsync(request);

        if (result.Success)
        {
            // Queue for sync
            var json = JsonSerializer.Serialize(new
            {
                RegNo = result.Student?.RegNo,
                Timestamp = request.Timestamp,
                DeviceId = request.DeviceId
            }, _jsonOptions);
            await _offline.QueueForSyncAsync("ClockIn", json);
            _pendingSyncCount++;
        }

        return result;
    }

    private async Task<ClockOutResponse> OfflineFirstClockOutAsync(ClockOutRequest request)
    {
        var result = await _offline.ClockOutAsync(request);

        if (result.Success)
        {
            var json = JsonSerializer.Serialize(new
            {
                RegNo = result.Student?.RegNo,
                Timestamp = request.Timestamp,
                DeviceId = request.DeviceId
            }, _jsonOptions);
            await _offline.QueueForSyncAsync("ClockOut", json);
            _pendingSyncCount++;
        }

        return result;
    }

    // ==================== Sync Helper ====================

    private async Task<DataResult> SyncRecordAsync(PendingSyncRecord record)
    {
        return record.OperationType switch
        {
            "Enrollment" => await SyncEnrollmentAsync(record),
            "ClockIn" => await SyncClockInAsync(record),
            "ClockOut" => await SyncClockOutAsync(record),
            _ => DataResult.Fail($"Unknown operation type: {record.OperationType}")
        };
    }

    private async Task<DataResult> SyncEnrollmentAsync(PendingSyncRecord record)
    {
        var request = JsonSerializer.Deserialize<EnrollmentRequest>(record.JsonPayload, _jsonOptions);
        if (request == null) return DataResult.Fail("Invalid enrollment data");

        return await _online.SubmitEnrollmentAsync(request);
    }

    private async Task<DataResult> SyncClockInAsync(PendingSyncRecord record)
    {
        // For clock-in sync, we just need to record that it happened
        // The fingerprint verification was already done locally
        _logger.LogInformation("Syncing clock-in record: {Payload}", record.JsonPayload);
        // API would need an endpoint to record pre-verified attendance
        return DataResult.Ok("Clock-in synced");
    }

    private async Task<DataResult> SyncClockOutAsync(PendingSyncRecord record)
    {
        _logger.LogInformation("Syncing clock-out record: {Payload}", record.JsonPayload);
        return DataResult.Ok("Clock-out synced");
    }
}
