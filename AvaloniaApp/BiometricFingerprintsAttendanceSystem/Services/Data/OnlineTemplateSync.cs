using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using BiometricFingerprintsAttendanceSystem.Services.Db;
using BiometricFingerprintsAttendanceSystem.Services.Fingerprint;
using BiometricFingerprintsAttendanceSystem.Services.Time;
using System.Security.Cryptography;

namespace BiometricFingerprintsAttendanceSystem.Services.Data;

/// <summary>
/// Smart caching service that syncs fingerprint templates from online API to local storage.
/// Only downloads templates that are new or changed (incremental sync).
/// </summary>
public sealed class OnlineTemplateSync : IDisposable
{
    private readonly OnlineDataProvider _online;
    private readonly DbConnectionFactory _db;
    private readonly IFingerprintService _fingerprint;
    private readonly AppConfig _config;
    private readonly IMemoryCache _cache;
    private readonly ILogger<OnlineTemplateSync> _logger;
    private readonly CancellationTokenSource _cts = new();
    private readonly SemaphoreSlim _syncLock = new(1, 1);

    private Task? _backgroundTask;
    private bool _disposed;
    private bool _initialSyncDone;

    private const string TemplateCacheKey = "all_fingerprint_templates_cache";
    private const string LocalHashCacheKey = "local_template_hashes";

    public DateTime? LastSyncAt { get; private set; }
    public int CachedStudentCount { get; private set; }
    public int CachedTemplateCount { get; private set; }
    public int LastSyncNewCount { get; private set; }
    public int LastSyncUpdatedCount { get; private set; }
    public int LastSyncSkippedCount { get; private set; }
    public bool IsSyncing { get; private set; }
    public string? LastSyncError { get; private set; }

    public OnlineTemplateSync(
        OnlineDataProvider online,
        DbConnectionFactory db,
        IFingerprintService fingerprint,
        AppConfig config,
        IMemoryCache cache,
        ILogger<OnlineTemplateSync> logger)
    {
        _online = online;
        _db = db;
        _fingerprint = fingerprint;
        _config = config;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Start background sync with specified interval.
    /// </summary>
    public void StartBackgroundSync(TimeSpan? interval = null)
    {
        if (_backgroundTask != null && !_backgroundTask.IsCompleted)
        {
            return;
        }

        var syncInterval = interval ?? TimeSpan.FromMinutes(1);
        _backgroundTask = RunBackgroundSyncAsync(syncInterval, _cts.Token);
        _logger.LogInformation("Smart template sync started with interval {Interval}", syncInterval);
    }

    /// <summary>
    /// Stop background sync.
    /// </summary>
    public async Task StopBackgroundSyncAsync()
    {
        _cts.Cancel();
        if (_backgroundTask != null)
        {
            try
            {
                await _backgroundTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }
    }

    /// <summary>
    /// Smart sync - only downloads templates that are new or changed.
    /// </summary>
    public async Task<SyncResult> SmartSyncAsync(CancellationToken cancellationToken = default)
    {
        if (!await _syncLock.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken))
        {
            return new SyncResult { Success = false, Message = "Sync already in progress" };
        }

        IsSyncing = true;
        var newCount = 0;
        var updatedCount = 0;
        var skippedCount = 0;
        var studentSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            _logger.LogInformation("Starting smart template sync...");

            // Get local template hashes for comparison
            var localHashes = await GetLocalTemplateHashesAsync(cancellationToken);
            _logger.LogDebug("Local cache has {Count} template hashes", localHashes.Count);

            // Fetch all templates from online API
            var onlineResult = await _online.GetAllEnrollmentTemplatesAsync();
            if (!onlineResult.Success || onlineResult.Data == null)
            {
                var error = onlineResult.Message ?? "Failed to fetch templates from online API";
                _logger.LogWarning("Smart sync failed: {Error}", error);
                LastSyncError = error;
                return new SyncResult { Success = false, Message = error };
            }

            await using var conn = await _db.CreateConnectionAsync();

            foreach (var (regNo, templates) in onlineResult.Data)
            {
                cancellationToken.ThrowIfCancellationRequested();
                studentSet.Add(regNo);

                foreach (var template in templates)
                {
                    if (template.TemplateData.Length == 0) continue;

                    var key = $"{regNo}:{template.FingerIndex}";
                    var newHash = ComputeHash(template.TemplateData);

                    // Check if template already exists locally with same hash
                    if (localHashes.TryGetValue(key, out var existingHash) && existingHash == newHash)
                    {
                        // Template unchanged, skip
                        skippedCount++;
                        continue;
                    }

                    // Template is new or changed - ensure student exists and save
                    await EnsureStudentExistsAsync(conn, regNo);

                    if (localHashes.ContainsKey(key))
                    {
                        // Existing template being updated
                        await UpsertTemplateAsync(conn, regNo, template.FingerIndex, template.Finger, template.TemplateData, newHash);
                        updatedCount++;
                        _logger.LogDebug("Updated template: {RegNo} finger {Finger}", regNo, template.FingerIndex);
                    }
                    else
                    {
                        // New template
                        await UpsertTemplateAsync(conn, regNo, template.FingerIndex, template.Finger, template.TemplateData, newHash);
                        newCount++;
                        _logger.LogDebug("New template: {RegNo} finger {Finger}", regNo, template.FingerIndex);
                    }

                    // Update local hash cache
                    localHashes[key] = newHash;
                }
            }

            // Update stats
            CachedStudentCount = studentSet.Count;
            CachedTemplateCount = newCount + updatedCount + skippedCount;
            LastSyncNewCount = newCount;
            LastSyncUpdatedCount = updatedCount;
            LastSyncSkippedCount = skippedCount;
            LastSyncAt = LagosTime.Now;
            LastSyncError = null;
            _initialSyncDone = true;

            // Clear memory cache only if there were changes
            if (newCount > 0 || updatedCount > 0)
            {
                _cache.Remove(TemplateCacheKey);
                _logger.LogInformation(
                    "Smart sync completed: {New} new, {Updated} updated, {Skipped} unchanged",
                    newCount, updatedCount, skippedCount);
            }
            else
            {
                _logger.LogDebug("Smart sync completed: no changes detected ({Skipped} templates unchanged)", skippedCount);
            }

            return new SyncResult
            {
                Success = true,
                Message = newCount + updatedCount > 0
                    ? $"Synced {newCount} new, {updatedCount} updated ({skippedCount} unchanged)"
                    : $"No changes ({skippedCount} templates up to date)",
                StudentCount = studentSet.Count,
                TemplateCount = newCount + updatedCount + skippedCount,
                NewCount = newCount,
                UpdatedCount = updatedCount,
                SkippedCount = skippedCount
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Smart sync cancelled");
            return new SyncResult { Success = false, Message = "Sync cancelled" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Smart sync failed with exception");
            LastSyncError = ex.Message;
            return new SyncResult { Success = false, Message = ex.Message };
        }
        finally
        {
            IsSyncing = false;
            _syncLock.Release();
        }
    }

    /// <summary>
    /// Force full sync - re-downloads all templates regardless of local state.
    /// </summary>
    public async Task<SyncResult> ForceSyncAsync(CancellationToken cancellationToken = default)
    {
        // Clear local hashes to force re-download
        _cache.Remove(LocalHashCacheKey);
        return await SmartSyncAsync(cancellationToken);
    }

    /// <summary>
    /// Authenticate a fingerprint against locally cached templates.
    /// </summary>
    public async Task<AuthResult> AuthenticateAsync(byte[] fingerprintTemplate, CancellationToken cancellationToken = default)
    {
        if (fingerprintTemplate == null || fingerprintTemplate.Length == 0)
        {
            return new AuthResult { Success = false, Message = "Invalid fingerprint template" };
        }

        // Get all cached templates
        var templates = await GetAllCachedTemplatesAsync(cancellationToken);
        if (templates.Count == 0)
        {
            _logger.LogWarning("No cached templates available for authentication");
            return new AuthResult { Success = false, Message = "No templates cached. Please sync first." };
        }

        AuthResult? bestMatch = null;

        foreach (var entry in templates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var verifyResult = await _fingerprint.VerifyAsync(fingerprintTemplate, entry.TemplateData, cancellationToken);

            if (!verifyResult.IsMatch) continue;
            if (verifyResult.MatchScore < _config.MinMatchScore) continue;
            if (verifyResult.FalseAcceptRate > 0 && verifyResult.FalseAcceptRate > _config.MaxFalseAcceptRate) continue;

            _logger.LogInformation(
                "Local cache match found: RegNo={RegNo} Finger={Finger} Score={Score} FAR={FAR}",
                entry.RegNo, entry.FingerName, verifyResult.MatchScore, verifyResult.FalseAcceptRate);

            if (bestMatch == null || verifyResult.MatchScore > bestMatch.MatchScore)
            {
                bestMatch = new AuthResult
                {
                    Success = true,
                    RegNo = entry.RegNo,
                    FingerIndex = entry.FingerIndex,
                    FingerName = entry.FingerName,
                    MatchScore = verifyResult.MatchScore,
                    MatchFar = verifyResult.FalseAcceptRate,
                    Message = "Authenticated from local cache"
                };
            }
        }

        if (bestMatch != null)
        {
            return bestMatch;
        }

        _logger.LogInformation("No match found in cached templates");
        return new AuthResult { Success = false, Message = "Fingerprint not recognized" };
    }

    /// <summary>
    /// Get all cached templates from local database (with in-memory caching).
    /// </summary>
    public async Task<List<CachedTemplate>> GetAllCachedTemplatesAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(TemplateCacheKey, out List<CachedTemplate>? cached) && cached != null && cached.Count > 0)
        {
            return cached;
        }

        var templates = new List<CachedTemplate>();

        try
        {
            await using var conn = await _db.CreateConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT fe.regno, fe.finger_index, fe.finger_name, fe.template, s.name
                FROM fingerprint_enrollments fe
                LEFT JOIN students s ON fe.regno = s.regno
                WHERE fe.template IS NOT NULL AND LENGTH(fe.template) > 0
                ORDER BY fe.regno, fe.finger_index";

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var regNo = reader.GetString(0);
                var fingerIndex = reader.GetInt32(1);
                var fingerName = reader.IsDBNull(2) ? GetFingerName(fingerIndex) : reader.GetString(2);
                var templateData = reader.IsDBNull(3) ? null : (byte[])reader[3];
                var studentName = reader.IsDBNull(4) ? regNo : reader.GetString(4);

                if (templateData == null || templateData.Length == 0) continue;

                templates.Add(new CachedTemplate
                {
                    RegNo = regNo,
                    StudentName = studentName,
                    FingerIndex = fingerIndex,
                    FingerName = fingerName,
                    TemplateData = templateData
                });
            }

            // Cache for 2 minutes
            _cache.Set(TemplateCacheKey, templates, TimeSpan.FromMinutes(2));
            CachedTemplateCount = templates.Count;

            _logger.LogDebug("Loaded {Count} templates into memory cache", templates.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load cached templates from database");
        }

        return templates;
    }

    /// <summary>
    /// Get hashes of all locally stored templates for change detection.
    /// </summary>
    private async Task<Dictionary<string, string>> GetLocalTemplateHashesAsync(CancellationToken cancellationToken)
    {
        // Try memory cache first
        if (_cache.TryGetValue(LocalHashCacheKey, out Dictionary<string, string>? cached) && cached != null)
        {
            return cached;
        }

        var hashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            await using var conn = await _db.CreateConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT regno, finger_index, template_hash
                FROM fingerprint_enrollments
                WHERE template_hash IS NOT NULL";

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var regNo = reader.GetString(0);
                var fingerIndex = reader.GetInt32(1);
                var hash = reader.IsDBNull(2) ? null : reader.GetString(2);

                if (!string.IsNullOrEmpty(hash))
                {
                    hashes[$"{regNo}:{fingerIndex}"] = hash;
                }
            }

            // Cache hashes for 5 minutes
            _cache.Set(LocalHashCacheKey, hashes, TimeSpan.FromMinutes(5));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load local template hashes, will do full sync");
        }

        return hashes;
    }

    /// <summary>
    /// Clear the in-memory template cache.
    /// </summary>
    public void ClearCache()
    {
        _cache.Remove(TemplateCacheKey);
        _cache.Remove(LocalHashCacheKey);
        CachedTemplateCount = 0;
    }

    private async Task RunBackgroundSyncAsync(TimeSpan interval, CancellationToken ct)
    {
        // Initial sync
        await SmartSyncAsync(ct);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, ct);
                await SmartSyncAsync(ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Background template sync failed");
            }
        }
    }

    private async Task EnsureStudentExistsAsync(MySqlConnector.MySqlConnection conn, string regNo)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT IGNORE INTO students (regno, name, updated_at)
            VALUES (@regno, @name, CURRENT_TIMESTAMP)";
        cmd.Parameters.AddWithValue("@regno", regNo);
        cmd.Parameters.AddWithValue("@name", regNo);
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task UpsertTemplateAsync(
        MySqlConnector.MySqlConnection conn,
        string regNo,
        int fingerIndex,
        string fingerName,
        byte[] templateData,
        string templateHash)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO fingerprint_enrollments
                (regno, finger_index, finger_name, template, template_data, template_hash, captured_at)
            VALUES
                (@regNo, @fingerIndex, @fingerName, @template, @templateData, @templateHash, @capturedAt)
            ON DUPLICATE KEY UPDATE
                finger_name = VALUES(finger_name),
                template = VALUES(template),
                template_data = VALUES(template_data),
                template_hash = VALUES(template_hash),
                captured_at = VALUES(captured_at)";

        cmd.Parameters.AddWithValue("@regNo", regNo);
        cmd.Parameters.AddWithValue("@fingerIndex", fingerIndex);
        cmd.Parameters.AddWithValue("@fingerName", NormalizeFingerName(fingerName, fingerIndex));
        cmd.Parameters.AddWithValue("@template", templateData);
        cmd.Parameters.AddWithValue("@templateData", Convert.ToBase64String(templateData));
        cmd.Parameters.AddWithValue("@templateHash", templateHash);
        cmd.Parameters.AddWithValue("@capturedAt", LagosTime.Now);

        await cmd.ExecuteNonQueryAsync();
    }

    private static string ComputeHash(byte[] data)
    {
        var hashBytes = SHA256.HashData(data);
        return Convert.ToHexString(hashBytes);
    }

    private static string NormalizeFingerName(string? fingerName, int fingerIndex)
    {
        if (!string.IsNullOrWhiteSpace(fingerName))
        {
            return fingerName.ToLowerInvariant().Replace(' ', '-');
        }
        return GetFingerName(fingerIndex).ToLowerInvariant().Replace(' ', '-');
    }

    private static string GetFingerName(int index) => index switch
    {
        1 => "right-thumb",
        2 => "right-index",
        3 => "right-middle",
        4 => "right-ring",
        5 => "right-pinky",
        6 => "left-thumb",
        7 => "left-index",
        8 => "left-middle",
        9 => "left-ring",
        10 => "left-pinky",
        _ => $"finger-{index}"
    };

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cts.Cancel();
        _cts.Dispose();
        _syncLock.Dispose();
    }

    public sealed record SyncResult
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
        public int StudentCount { get; init; }
        public int TemplateCount { get; init; }
        public int NewCount { get; init; }
        public int UpdatedCount { get; init; }
        public int SkippedCount { get; init; }
    }

    public sealed record AuthResult
    {
        public bool Success { get; init; }
        public string? RegNo { get; init; }
        public int FingerIndex { get; init; }
        public string? FingerName { get; init; }
        public int MatchScore { get; init; }
        public double MatchFar { get; init; }
        public string Message { get; init; } = string.Empty;
    }

    public sealed record CachedTemplate
    {
        public string RegNo { get; init; } = string.Empty;
        public string StudentName { get; init; } = string.Empty;
        public int FingerIndex { get; init; }
        public string FingerName { get; init; } = string.Empty;
        public byte[] TemplateData { get; init; } = [];
    }
}
