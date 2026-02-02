using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
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
    private readonly IMemoryCache _cache;
    private readonly JsonSerializerOptions _jsonOptions;

    private bool _isOnline;
    private int _pendingSyncCount;

    public DataService(
        OnlineDataProvider online,
        OfflineDataProvider offline,
        AppConfig config,
        ILogger<DataService> logger,
        IMemoryCache cache)
    {
        _online = online;
        _offline = offline;
        _logger = logger;
        _cache = cache;
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
        var cacheKey = $"student:{regNo}";
        if (_cache.TryGetValue(cacheKey, out StudentInfo? cachedStudent) && cachedStudent is not null)
        {
            return DataResult<StudentInfo>.Ok(cachedStudent);
        }
        var result = Mode switch
        {
            SyncMode.OnlineOnly => await _online.GetStudentAsync(regNo),
            SyncMode.OfflineOnly => await _offline.GetStudentAsync(regNo),
            SyncMode.OnlineFirst => await OnlineFirstGetStudentAsync(regNo),
            SyncMode.OfflineFirst => await OfflineFirstGetStudentAsync(regNo),
            _ => DataResult<StudentInfo>.Fail("Invalid sync mode")
        };
        if (result.Success && result.Data is not null)
        {
            _cache.Set(cacheKey, result.Data, TimeSpan.FromMinutes(5));
            if (Mode != SyncMode.OfflineOnly)
            {
                await _offline.UpsertStudentSnapshotAsync(result.Data, null);
            }
        }
        return result;
    }

    public async Task<DataResult<byte[]>> GetStudentPhotoAsync(string regNo)
    {
        var cacheKey = $"student_photo:{regNo}";
        if (_cache.TryGetValue(cacheKey, out byte[]? cachedPhoto) && cachedPhoto is not null)
        {
            return DataResult<byte[]>.Ok(cachedPhoto);
        }
        var result = Mode switch
        {
            SyncMode.OnlineOnly => await _online.GetStudentPhotoAsync(regNo),
            SyncMode.OfflineOnly => await _offline.GetStudentPhotoAsync(regNo),
            SyncMode.OnlineFirst => await OnlineFirstGetPhotoAsync(regNo),
            SyncMode.OfflineFirst => await OfflineFirstGetPhotoAsync(regNo),
            _ => DataResult<byte[]>.Fail("Invalid sync mode")
        };
        if (result.Success && result.Data is not null)
        {
            _cache.Set(cacheKey, result.Data, TimeSpan.FromMinutes(10));
        }
        return result;
    }

    // ==================== Enrollment Operations ====================

    public async Task<DataResult<EnrollmentStatus>> GetEnrollmentStatusAsync(string regNo)
    {
        var cacheKey = $"enrollment_status:{regNo}";
        if (_cache.TryGetValue(cacheKey, out EnrollmentStatus? cachedStatus) && cachedStatus is not null)
        {
            return DataResult<EnrollmentStatus>.Ok(cachedStatus);
        }
        var result = Mode switch
        {
            SyncMode.OnlineOnly => await _online.GetEnrollmentStatusAsync(regNo),
            SyncMode.OfflineOnly => await _offline.GetEnrollmentStatusAsync(regNo),
            SyncMode.OnlineFirst => await OnlineFirstGetEnrollmentStatusAsync(regNo),
            SyncMode.OfflineFirst => await OfflineFirstGetEnrollmentStatusAsync(regNo),
            _ => DataResult<EnrollmentStatus>.Fail("Invalid sync mode")
        };
        if (result.Success && result.Data is not null)
        {
            _cache.Set(cacheKey, result.Data, TimeSpan.FromMinutes(2));
            if (Mode != SyncMode.OfflineOnly)
            {
                await _offline.UpdateEnrollmentStatusAsync(regNo, result.Data);
            }
        }
        return result;
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

    public async Task<DataResult<List<FingerprintTemplate>>> GetEnrollmentTemplatesAsync(string regNo)
    {
        return Mode switch
        {
            SyncMode.OnlineOnly => await _online.GetEnrollmentTemplatesAsync(regNo),
            SyncMode.OfflineOnly => await _offline.GetEnrollmentTemplatesAsync(regNo),
            SyncMode.OnlineFirst => await OnlineFirstGetEnrollmentTemplatesAsync(regNo),
            SyncMode.OfflineFirst => await OfflineFirstGetEnrollmentTemplatesAsync(regNo),
            _ => DataResult<List<FingerprintTemplate>>.Fail("Invalid sync mode")
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
            if (result.Data is not null)
            {
                await _offline.UpsertStudentSnapshotAsync(result.Data, null);
            }
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
            if (result.Data is not null)
            {
                await _offline.UpdateEnrollmentStatusAsync(regNo, result.Data);
            }
            return result;
        }

        _isOnline = false;
        return await _offline.GetEnrollmentStatusAsync(regNo);
    }

    private async Task<DataResult<List<FingerprintTemplate>>> OnlineFirstGetEnrollmentTemplatesAsync(string regNo)
    {
        var result = await _online.GetEnrollmentTemplatesAsync(regNo);
        if (result.Success)
        {
            _isOnline = true;
            return result;
        }

        _isOnline = false;
        return await _offline.GetEnrollmentTemplatesAsync(regNo);
    }

    private async Task<DataResult> OnlineFirstSubmitEnrollmentAsync(EnrollmentRequest request)
    {
        // Try online first
        var onlineResult = await _online.SubmitEnrollmentAsync(request);
        if (onlineResult.Success)
        {
            _logger.LogInformation("Enrollment sent to API for {RegNo}", request.RegNo);
        }
        else
        {
            _logger.LogWarning("Enrollment API submit failed for {RegNo}: {Message}", request.RegNo, onlineResult.Message);
        }

        // Always save locally too (for offline matching)
        var localResult = await _offline.SubmitEnrollmentAsync(request);
        if (localResult.Success)
        {
            _logger.LogInformation("Enrollment saved locally for {RegNo}", request.RegNo);
        }
        else
        {
            _logger.LogWarning("Enrollment local save failed for {RegNo}: {Message}", request.RegNo, localResult.Message);
        }

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
        var onlineResult = await _online.GetStudentAsync(regNo);
        if (onlineResult.Success && onlineResult.Data is not null)
        {
            await _offline.UpsertStudentSnapshotAsync(onlineResult.Data, null);
        }
        return onlineResult;
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
        var local = await _offline.GetEnrollmentStatusAsync(regNo);
        if (local.Success) return local;

        var online = await _online.GetEnrollmentStatusAsync(regNo);
        if (online.Success && online.Data is not null)
        {
            await _offline.UpdateEnrollmentStatusAsync(regNo, online.Data);
        }
        return online;
    }

    private async Task<DataResult<List<FingerprintTemplate>>> OfflineFirstGetEnrollmentTemplatesAsync(string regNo)
    {
        var local = await _offline.GetEnrollmentTemplatesAsync(regNo);
        if (local.Success) return local;

        var online = await _online.GetEnrollmentTemplatesAsync(regNo);
        if (online.Success)
        {
            _isOnline = true;
        }
        return online;
    }

    private async Task<DataResult> OfflineFirstSubmitEnrollmentAsync(EnrollmentRequest request)
    {
        // Save locally first
        var localResult = await _offline.SubmitEnrollmentAsync(request);
        if (!localResult.Success)
        {
            return localResult;
        }
        _logger.LogInformation("Enrollment saved locally for {RegNo}", request.RegNo);

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
                _logger.LogInformation("Enrollment sent to API for {RegNo}", request.RegNo);
                // Remove from sync queue (it's the most recent one)
                var pending = await _offline.GetPendingSyncRecordsAsync();
                var last = pending.LastOrDefault(p => p.OperationType == "Enrollment");
                if (last != null)
                {
                    await _offline.MarkSyncedAsync(last.Id);
                    _pendingSyncCount--;
                }
            }
            else
            {
                _logger.LogWarning("Enrollment API submit failed for {RegNo}: {Message}", request.RegNo, onlineResult.Message);
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
        _logger.LogInformation("Syncing clock-in record: {Payload}", record.JsonPayload);
        try
        {
            using var doc = JsonDocument.Parse(record.JsonPayload);
            var root = doc.RootElement;
            var regNo = ReadStringProperty(root, "RegNo", "regNo");
            var deviceId = ReadStringProperty(root, "DeviceId", "deviceId");
            var timestamp = ReadDateTimeProperty(root, "Timestamp", "timestamp") ?? DateTime.Now;

            if (string.IsNullOrWhiteSpace(regNo))
            {
                return DataResult.Fail("Missing regNo in sync payload");
            }

            var response = await _online.ClockInVerifiedAsync(new VerifiedClockRequest
            {
                RegNo = regNo,
                Timestamp = timestamp,
                DeviceId = deviceId
            });

            return response.Success
                ? DataResult.Ok("Clock-in synced")
                : DataResult.Fail(response.Message ?? "Clock-in sync failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync clock-in record");
            return DataResult.Fail(ex.Message);
        }
    }

    private async Task<DataResult> SyncClockOutAsync(PendingSyncRecord record)
    {
        _logger.LogInformation("Syncing clock-out record: {Payload}", record.JsonPayload);
        try
        {
            using var doc = JsonDocument.Parse(record.JsonPayload);
            var root = doc.RootElement;
            var regNo = ReadStringProperty(root, "RegNo", "regNo");
            var deviceId = ReadStringProperty(root, "DeviceId", "deviceId");
            var timestamp = ReadDateTimeProperty(root, "Timestamp", "timestamp") ?? DateTime.Now;

            if (string.IsNullOrWhiteSpace(regNo))
            {
                return DataResult.Fail("Missing regNo in sync payload");
            }

            var response = await _online.ClockOutVerifiedAsync(new VerifiedClockRequest
            {
                RegNo = regNo,
                Timestamp = timestamp,
                DeviceId = deviceId
            });

            return response.Success
                ? DataResult.Ok("Clock-out synced")
                : DataResult.Fail(response.Message ?? "Clock-out sync failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync clock-out record");
            return DataResult.Fail(ex.Message);
        }
    }

    private static string? ReadStringProperty(JsonElement root, string camel, string lower)
    {
        if (root.TryGetProperty(camel, out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            return prop.GetString();
        }
        if (root.TryGetProperty(lower, out prop) && prop.ValueKind == JsonValueKind.String)
        {
            return prop.GetString();
        }
        return null;
    }

    private static DateTime? ReadDateTimeProperty(JsonElement root, string camel, string lower)
    {
        if (root.TryGetProperty(camel, out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            if (DateTime.TryParse(prop.GetString(), out var dt))
            {
                return dt;
            }
        }
        if (root.TryGetProperty(lower, out prop) && prop.ValueKind == JsonValueKind.String)
        {
            if (DateTime.TryParse(prop.GetString(), out var dt))
            {
                return dt;
            }
        }
        return null;
    }
}
