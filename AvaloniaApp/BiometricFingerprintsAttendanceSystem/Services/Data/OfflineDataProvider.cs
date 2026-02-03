using BiometricFingerprintsAttendanceSystem.Services.Db;
using BiometricFingerprintsAttendanceSystem.Services.Fingerprint;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace BiometricFingerprintsAttendanceSystem.Services.Data;

/// <summary>
/// Handles all local database operations for offline mode.
/// </summary>
public class OfflineDataProvider
{
    private readonly DbConnectionFactory _db;
    private readonly IFingerprintService _fingerprint;
    private readonly ILogger<OfflineDataProvider> _logger;
    private readonly IMemoryCache _cache;
    private readonly HttpClient _http = new();
    private readonly int _minMatchScore;
    private readonly double _maxFalseAcceptRate;
    private const string EnrollmentCacheKey = "offline_enrollments_cache";

    public OfflineDataProvider(
        DbConnectionFactory db,
        IFingerprintService fingerprint,
        ILogger<OfflineDataProvider> logger,
        IMemoryCache cache,
        AppConfig config)
    {
        _db = db;
        _fingerprint = fingerprint;
        _logger = logger;
        _cache = cache;
        _minMatchScore = config.MinMatchScore;
        _maxFalseAcceptRate = config.MaxFalseAcceptRate;
    }

    /// <summary>
    /// Get student from local database.
    /// </summary>
    public async Task<DataResult<StudentInfo>> GetStudentAsync(string regNo)
    {
        try
        {
            await using var conn = await _db.CreateConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT regno, name, class_name, faculty, department, passport_filename, passport_url, renewal_date,
                       is_enrolled, fingers_enrolled, enrolled_at
                FROM students
                WHERE regno = @regNo OR matricno = @regNo";
            cmd.Parameters.AddWithValue("@regNo", regNo);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return DataResult<StudentInfo>.Fail("Student not found locally", "NOT_FOUND");
            }

            var student = new StudentInfo
            {
                RegNo = reader.GetString(0),
                Name = reader.GetString(1),
                ClassName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                Faculty = reader.IsDBNull(3) ? null : reader.GetString(3),
                Department = reader.IsDBNull(4) ? null : reader.GetString(4),
                PassportPhoto = TryLoadPassportFromDisk(reader.IsDBNull(5) ? null : reader.GetString(5)),
                PassportUrl = reader.IsDBNull(6) ? null : reader.GetString(6),
                RenewalDate = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                IsEnrolled = reader.IsDBNull(8) ? null : reader.GetBoolean(8),
                EnrolledFingerCount = reader.IsDBNull(9) ? null : reader.GetInt32(9),
                EnrolledAt = reader.IsDBNull(10) ? null : reader.GetDateTime(10)
            };

            return DataResult<StudentInfo>.Ok(student);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get student {RegNo} from local DB", regNo);
            return DataResult<StudentInfo>.Fail(ex.Message, "DB_ERROR");
        }
    }

    /// <summary>
    /// Get student photo from local database.
    /// </summary>
    public async Task<DataResult<byte[]>> GetStudentPhotoAsync(string regNo)
    {
        try
        {
            await using var conn = await _db.CreateConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT passport_filename FROM students WHERE regno = @regNo OR matricno = @regNo";
            cmd.Parameters.AddWithValue("@regNo", regNo);

            var result = await cmd.ExecuteScalarAsync();
            if (result == null || result == DBNull.Value)
            {
                return DataResult<byte[]>.Fail("Photo not found");
            }

            var filename = result.ToString();
            var bytes = TryLoadPassportFromDisk(filename);
            if (bytes == null || bytes.Length == 0)
            {
                return DataResult<byte[]>.Fail("Photo file not found");
            }

            return DataResult<byte[]>.Ok(bytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get photo for {RegNo}", regNo);
            return DataResult<byte[]>.Fail(ex.Message);
        }
    }

    /// <summary>
    /// Get enrollment status from local database.
    /// </summary>
    public async Task<DataResult<EnrollmentStatus>> GetEnrollmentStatusAsync(string regNo)
    {
        try
        {
            await using var conn = await _db.CreateConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT COUNT(*), MIN(captured_at)
                FROM fingerprint_enrollments
                WHERE regNo = @regNo";
            cmd.Parameters.AddWithValue("@regNo", regNo);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return DataResult<EnrollmentStatus>.Ok(new EnrollmentStatus { IsEnrolled = false });
            }

            var fingerCount = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
            var createdAt = reader.IsDBNull(1) ? (DateTime?)null : reader.GetDateTime(1);

            return DataResult<EnrollmentStatus>.Ok(new EnrollmentStatus
            {
                IsEnrolled = fingerCount >= 2,
                EnrolledFingerCount = fingerCount,
                EnrolledAt = createdAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get enrollment status for {RegNo}", regNo);
            return DataResult<EnrollmentStatus>.Fail(ex.Message);
        }
    }

    /// <summary>
    /// Submit enrollment to local database.
    /// </summary>
    public async Task<DataResult> SubmitEnrollmentAsync(EnrollmentRequest request)
    {
        try
        {
            await using var conn = await _db.CreateConnectionAsync();

            var enrolledCount = 0;
            foreach (var template in request.Templates)
            {
                if (template.FingerIndex < 1 || template.FingerIndex > 10)
                {
                    continue;
                }

                await using var upsertCmd = conn.CreateCommand();
                upsertCmd.CommandText = @"
                    INSERT INTO fingerprint_enrollments
                        (regno, finger_index, finger_name, template, template_data, template_hash, image_preview, captured_at)
                    VALUES
                        (@regNo, @fingerIndex, @fingerName, @template, @templateData, @templateHash, @imagePreview, @capturedAt)
                    ON DUPLICATE KEY UPDATE
                        finger_name = VALUES(finger_name),
                        template = VALUES(template),
                        template_data = VALUES(template_data),
                        template_hash = VALUES(template_hash),
                        image_preview = VALUES(image_preview),
                        captured_at = VALUES(captured_at)";

                upsertCmd.Parameters.AddWithValue("@regNo", request.RegNo);
                upsertCmd.Parameters.AddWithValue("@fingerIndex", template.FingerIndex);
                upsertCmd.Parameters.AddWithValue("@fingerName", NormalizeFingerName(template.Finger, template.FingerIndex));
                upsertCmd.Parameters.AddWithValue("@template", template.TemplateData);
                upsertCmd.Parameters.AddWithValue("@templateData", Convert.ToBase64String(template.TemplateData));
                upsertCmd.Parameters.AddWithValue("@templateHash", ComputeTemplateHash(template.TemplateData));
                upsertCmd.Parameters.AddWithValue("@imagePreview", (object?)SaveFingerprintPreviewToDisk(request.RegNo, template.ImagePath) ?? DBNull.Value);
                upsertCmd.Parameters.AddWithValue("@capturedAt", request.EnrolledAt);

                await upsertCmd.ExecuteNonQueryAsync();
                enrolledCount++;
            }

            await UpdateEnrollmentStatusAsync(request.RegNo, new EnrollmentStatus
            {
                IsEnrolled = enrolledCount >= 2,
                EnrolledFingerCount = enrolledCount,
                EnrolledAt = request.EnrolledAt
            });

            _cache.Remove(EnrollmentCacheKey);
            _logger.LogInformation("Enrollment saved locally for {RegNo}", request.RegNo);
            return DataResult.Ok("Enrollment saved locally");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save enrollment for {RegNo}", request.RegNo);
            return DataResult.Fail(ex.Message, "DB_ERROR");
        }
    }

    /// <summary>
    /// Get enrolled fingerprint templates for a specific student from local database.
    /// </summary>
    public async Task<DataResult<List<FingerprintTemplate>>> GetEnrollmentTemplatesAsync(string regNo)
    {
        try
        {
            await using var conn = await _db.CreateConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT finger_index, finger_name, template
                FROM fingerprint_enrollments
                WHERE regno = @regno
                ORDER BY finger_index";
            cmd.Parameters.AddWithValue("@regno", regNo);

            var results = new List<FingerprintTemplate>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var fingerIndex = reader.GetInt32(0);
                var fingerName = reader.IsDBNull(1) ? GetFingerName(fingerIndex) : reader.GetString(1);
                var data = reader.IsDBNull(2) ? null : (byte[])reader[2];

                if (data == null || data.Length == 0)
                {
                    continue;
                }

                results.Add(new FingerprintTemplate
                {
                    FingerIndex = fingerIndex,
                    Finger = fingerName,
                    TemplateData = data
                });
            }

            return DataResult<List<FingerprintTemplate>>.Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get enrollment templates for {RegNo}", regNo);
            return DataResult<List<FingerprintTemplate>>.Fail(ex.Message, "DB_ERROR");
        }
    }

    /// <summary>
    /// Get all enrolled fingerprints for local matching.
    /// </summary>
    public async Task<DataResult<List<(string RegNo, List<FingerprintTemplate> Templates)>>> GetAllEnrollmentsAsync()
    {
        try
        {
            if (_cache.TryGetValue(EnrollmentCacheKey, out List<(string RegNo, List<FingerprintTemplate> Templates)>? cached) &&
                cached is not null)
            {
                return DataResult<List<(string RegNo, List<FingerprintTemplate> Templates)>>.Ok(cached);
            }

            await using var conn = await _db.CreateConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT regno, finger_index, finger_name, template
                FROM fingerprint_enrollments
                ORDER BY regno, finger_index";

            var results = new List<(string RegNo, List<FingerprintTemplate> Templates)>();

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var regNo = reader.GetString(0) ?? string.Empty;
                var fingerIndex = reader.GetInt32(1);
                var fingerName = reader.IsDBNull(2) ? GetFingerName(fingerIndex) : reader.GetString(2);
                var data = reader.IsDBNull(3) ? null : (byte[])reader[3];

                if (string.IsNullOrWhiteSpace(regNo) || data == null || data.Length == 0)
                {
                    continue;
                }

                var existing = results.FirstOrDefault(r => r.RegNo == regNo);
                if (existing.RegNo == null)
                {
                    existing = (regNo, new List<FingerprintTemplate>());
                    results.Add(existing);
                }

                existing.Templates.Add(new FingerprintTemplate
                {
                    FingerIndex = fingerIndex,
                    Finger = fingerName,
                    TemplateData = data
                });
            }

            _cache.Set(EnrollmentCacheKey, results, TimeSpan.FromMinutes(2));
            return DataResult<List<(string RegNo, List<FingerprintTemplate> Templates)>>.Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all enrollments");
            return DataResult<List<(string RegNo, List<FingerprintTemplate> Templates)>>.Fail(ex.Message);
        }
    }

    /// <summary>
    /// Clock in with local fingerprint matching.
    /// </summary>
    public async Task<ClockInResponse> ClockInAsync(ClockInRequest request)
    {
        try
        {
            // Get all enrolled fingerprints
            var enrollmentsResult = await GetAllEnrollmentsAsync();
            if (!enrollmentsResult.Success || enrollmentsResult.Data == null)
            {
                return new ClockInResponse { Success = false, Message = "Failed to load enrollments" };
            }

            // Match fingerprint against all enrolled templates
            string? matchedRegNo = null;
            foreach (var (regNo, templates) in enrollmentsResult.Data)
            {
                foreach (var template in templates)
                {
                    var verifyResult = await _fingerprint.VerifyAsync(request.FingerprintTemplate, template.TemplateData);
                    _logger.LogInformation(
                        "Clock-in verify attempt RegNo={RegNo} Finger={Finger} Score={Score} FAR={FAR}",
                        regNo, template.Finger, verifyResult.MatchScore, verifyResult.FalseAcceptRate);

                    if (verifyResult.IsMatch &&
                        verifyResult.MatchScore >= _minMatchScore &&
                        (verifyResult.FalseAcceptRate <= 0 || verifyResult.FalseAcceptRate <= _maxFalseAcceptRate))
                    {
                        matchedRegNo = regNo;
                        break;
                    }
                }
                if (matchedRegNo != null) break;
            }

            if (matchedRegNo == null)
            {
                return new ClockInResponse { Success = false, Message = "Fingerprint not recognized" };
            }

            // Get student info
            var studentResult = await GetStudentAsync(matchedRegNo);
            var student = studentResult.Data ?? new StudentInfo { RegNo = matchedRegNo, Name = matchedRegNo };

            // Check if already clocked in today
            var today = DateTime.Today;
            await using var conn = await _db.CreateConnectionAsync();
            await using var checkCmd = conn.CreateCommand();
            checkCmd.CommandText = @"
                SELECT id FROM attendance
                WHERE regno = @regNo AND date = @date AND timeout IS NULL";
            checkCmd.Parameters.AddWithValue("@regNo", matchedRegNo);
            checkCmd.Parameters.AddWithValue("@date", today.ToString("yyyy-MM-dd"));

            var existingRecord = await checkCmd.ExecuteScalarAsync();
            if (existingRecord != null)
            {
                return new ClockInResponse
                {
                    Success = false,
                    Message = "Already clocked in today",
                    AlreadyClockedIn = true,
                    Student = student
                };
            }

            // Record clock-in
            var now = LagosTime.Now;
            await using var insertCmd = conn.CreateCommand();
            insertCmd.CommandText = @"
                INSERT INTO attendance (regno, name, date, day, timein)
                VALUES (@regNo, @name, @date, @day, @timein)";
            insertCmd.Parameters.AddWithValue("@regNo", matchedRegNo);
            insertCmd.Parameters.AddWithValue("@name", student.Name);
            insertCmd.Parameters.AddWithValue("@date", today.ToString("yyyy-MM-dd"));
            insertCmd.Parameters.AddWithValue("@day", today.DayOfWeek.ToString());
            insertCmd.Parameters.AddWithValue("@timein", now.ToString("HH:mm:ss"));

            await insertCmd.ExecuteNonQueryAsync();

            _logger.LogInformation("Clock-in recorded locally for {RegNo} at {Time}", matchedRegNo, now);

            return new ClockInResponse
            {
                Success = true,
                Message = "Clock-in successful",
                Student = student,
                ClockInTime = now
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Local clock-in failed");
            return new ClockInResponse { Success = false, Message = ex.Message };
        }
    }

    /// <summary>
    /// Clock out with local fingerprint matching.
    /// </summary>
    public async Task<ClockOutResponse> ClockOutAsync(ClockOutRequest request)
    {
        try
        {
            // Get all enrolled fingerprints
            var enrollmentsResult = await GetAllEnrollmentsAsync();
            if (!enrollmentsResult.Success || enrollmentsResult.Data == null)
            {
                return new ClockOutResponse { Success = false, Message = "Failed to load enrollments" };
            }

            // Match fingerprint
            string? matchedRegNo = null;
            foreach (var (regNo, templates) in enrollmentsResult.Data)
            {
                foreach (var template in templates)
                {
                    var verifyResult = await _fingerprint.VerifyAsync(request.FingerprintTemplate, template.TemplateData);
                    _logger.LogInformation(
                        "Clock-out verify attempt RegNo={RegNo} Finger={Finger} Score={Score} FAR={FAR}",
                        regNo, template.Finger, verifyResult.MatchScore, verifyResult.FalseAcceptRate);

                    if (verifyResult.IsMatch &&
                        verifyResult.MatchScore >= _minMatchScore &&
                        (verifyResult.FalseAcceptRate <= 0 || verifyResult.FalseAcceptRate <= _maxFalseAcceptRate))
                    {
                        matchedRegNo = regNo;
                        break;
                    }
                }
                if (matchedRegNo != null) break;
            }

            if (matchedRegNo == null)
            {
                return new ClockOutResponse { Success = false, Message = "Fingerprint not recognized" };
            }

            // Get student info
            var studentResult = await GetStudentAsync(matchedRegNo);
            var student = studentResult.Data ?? new StudentInfo { RegNo = matchedRegNo, Name = matchedRegNo };

            // Find today's clock-in record
            var today = DateTime.Today;
            await using var conn = await _db.CreateConnectionAsync();
            await using var findCmd = conn.CreateCommand();
            findCmd.CommandText = @"
                SELECT id, timein FROM attendance
                WHERE regno = @regNo AND date = @date AND timeout IS NULL
                ORDER BY id DESC LIMIT 1";
            findCmd.Parameters.AddWithValue("@regNo", matchedRegNo);
            findCmd.Parameters.AddWithValue("@date", today.ToString("yyyy-MM-dd"));

            await using var reader = await findCmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return new ClockOutResponse
                {
                    Success = false,
                    Message = "No clock-in record found for today",
                    NotClockedIn = true,
                    Student = student
                };
            }

            var recordId = reader.GetInt64(0);
            var timeInStr = reader.GetString(1);
            await reader.CloseAsync();

            // Update with clock-out time
            var now = LagosTime.Now;
            await using var updateCmd = conn.CreateCommand();
            updateCmd.CommandText = "UPDATE attendance SET timeout = @timeout WHERE id = @id";
            updateCmd.Parameters.AddWithValue("@timeout", now.ToString("HH:mm:ss"));
            updateCmd.Parameters.AddWithValue("@id", recordId);

            await updateCmd.ExecuteNonQueryAsync();

            // Calculate duration
            var timeIn = DateTime.Parse($"{today:yyyy-MM-dd} {timeInStr}");
            var duration = now - timeIn;

            _logger.LogInformation("Clock-out recorded locally for {RegNo} at {Time}", matchedRegNo, now);

            return new ClockOutResponse
            {
                Success = true,
                Message = "Clock-out successful",
                Student = student,
                ClockInTime = timeIn,
                ClockOutTime = now,
                Duration = duration
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Local clock-out failed");
            return new ClockOutResponse { Success = false, Message = ex.Message };
        }
    }

    /// <summary>
    /// Get attendance records from local database.
    /// </summary>
    public async Task<DataResult<List<AttendanceRecord>>> GetAttendanceAsync(DateTime from, DateTime to, string? regNo = null)
    {
        try
        {
            await using var conn = await _db.CreateConnectionAsync();
            await using var cmd = conn.CreateCommand();

            var sql = @"
                SELECT id, regno, name, date, timein, timeout
                FROM attendance
                WHERE date BETWEEN @from AND @to";

            if (!string.IsNullOrEmpty(regNo))
            {
                sql += " AND regno = @regNo";
            }
            sql += " ORDER BY date DESC, timein DESC";

            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@from", from.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@to", to.ToString("yyyy-MM-dd"));
            if (!string.IsNullOrEmpty(regNo))
            {
                cmd.Parameters.AddWithValue("@regNo", regNo);
            }

            var records = new List<AttendanceRecord>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var dateStr = reader.GetString(3);
                var timeInStr = reader.IsDBNull(4) ? null : reader.GetString(4);
                var timeOutStr = reader.IsDBNull(5) ? null : reader.GetString(5);

                DateTime? timeIn = null;
                DateTime? timeOut = null;

                if (DateTime.TryParse(dateStr, out var date))
                {
                    if (!string.IsNullOrEmpty(timeInStr))
                        timeIn = DateTime.Parse($"{dateStr} {timeInStr}");
                    if (!string.IsNullOrEmpty(timeOutStr))
                        timeOut = DateTime.Parse($"{dateStr} {timeOutStr}");
                }

                records.Add(new AttendanceRecord
                {
                    Id = reader.GetInt64(0),
                    RegNo = reader.GetString(1),
                    Name = reader.GetString(2),
                    Date = date,
                    TimeIn = timeIn,
                    TimeOut = timeOut,
                    IsSynced = false // Local records may not be synced
                });
            }

            return DataResult<List<AttendanceRecord>>.Ok(records);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get attendance records");
            return DataResult<List<AttendanceRecord>>.Fail(ex.Message);
        }
    }

    /// <summary>
    /// Save a record to the pending sync queue.
    /// </summary>
    public async Task<DataResult> QueueForSyncAsync(string operationType, string jsonPayload)
    {
        try
        {
            await using var conn = await _db.CreateConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO pending_sync (operation_type, json_payload, created_at)
                VALUES (@type, @payload, @created)";
            cmd.Parameters.AddWithValue("@type", operationType);
            cmd.Parameters.AddWithValue("@payload", jsonPayload);
            cmd.Parameters.AddWithValue("@created", LagosTime.Now);

            await cmd.ExecuteNonQueryAsync();
            return DataResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue sync record");
            return DataResult.Fail(ex.Message);
        }
    }

    /// <summary>
    /// Get pending sync records.
    /// </summary>
    public async Task<List<PendingSyncRecord>> GetPendingSyncRecordsAsync()
    {
        try
        {
            await using var conn = await _db.CreateConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT id, operation_type, json_payload, created_at, retry_count, last_error
                FROM pending_sync
                WHERE retry_count < 5
                ORDER BY created_at ASC";

            var records = new List<PendingSyncRecord>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                records.Add(new PendingSyncRecord
                {
                    Id = reader.GetInt64(0),
                    OperationType = reader.GetString(1),
                    JsonPayload = reader.GetString(2),
                    CreatedAt = reader.GetDateTime(3),
                    RetryCount = reader.GetInt32(4),
                    LastError = reader.IsDBNull(5) ? null : reader.GetString(5)
                });
            }

            return records;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get pending sync records");
            return [];
        }
    }

    /// <summary>
    /// Mark a sync record as completed.
    /// </summary>
    public async Task MarkSyncedAsync(long id)
    {
        try
        {
            await using var conn = await _db.CreateConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM pending_sync WHERE id = @id";
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark sync record {Id} as completed", id);
        }
    }

    /// <summary>
    /// Mark a sync record as failed.
    /// </summary>
    public async Task MarkSyncFailedAsync(long id, string error)
    {
        try
        {
            await using var conn = await _db.CreateConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE pending_sync
                SET retry_count = retry_count + 1, last_error = @error
                WHERE id = @id";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@error", error);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark sync record {Id} as failed", id);
        }
    }

    private static string GetFingerName(int index) => index switch
    {
        1 => "RightThumb",
        2 => "RightIndex",
        3 => "RightMiddle",
        4 => "RightRing",
        5 => "RightLittle",
        6 => "LeftThumb",
        7 => "LeftIndex",
        8 => "LeftMiddle",
        9 => "LeftRing",
        10 => "LeftLittle",
        _ => $"Finger{index}"
    };

    private static string NormalizeFingerName(string? finger, int fingerIndex)
    {
        if (!string.IsNullOrWhiteSpace(finger))
        {
            var compact = finger.Replace(" ", "", StringComparison.OrdinalIgnoreCase);
            var pos = compact switch
            {
                "RightThumb" => FingerPosition.RightThumb,
                "RightIndex" or "RightIndexFinger" => FingerPosition.RightIndexFinger,
                "RightMiddle" or "RightMiddleFinger" => FingerPosition.RightMiddleFinger,
                "RightRing" or "RightRingFinger" => FingerPosition.RightRingFinger,
                "RightLittle" or "RightLittleFinger" => FingerPosition.RightLittleFinger,
                "LeftThumb" => FingerPosition.LeftThumb,
                "LeftIndex" or "LeftIndexFinger" => FingerPosition.LeftIndexFinger,
                "LeftMiddle" or "LeftMiddleFinger" => FingerPosition.LeftMiddleFinger,
                "LeftRing" or "LeftRingFinger" => FingerPosition.LeftRingFinger,
                "LeftLittle" or "LeftLittleFinger" => FingerPosition.LeftLittleFinger,
                _ => FingerPosition.Unknown
            };

            if (pos != FingerPosition.Unknown)
            {
                return pos.ToFprintdName();
            }

            if (finger.Contains('-', StringComparison.Ordinal))
            {
                return finger.ToLowerInvariant();
            }
        }

        if (fingerIndex >= 1 && fingerIndex <= 10)
        {
            return ((FingerPosition)fingerIndex).ToFprintdName();
        }

        return "any";
    }

    public async Task UpsertStudentSnapshotAsync(StudentInfo student, EnrollmentStatus? status)
    {
        try
        {
            await using var conn = await _db.CreateConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO students
                    (regno, matricno, name, class_name, faculty, department, passport_filename, passport_url, renewal_date,
                     is_enrolled, fingers_enrolled, enrolled_at, updated_at)
                VALUES
                    (@regno, @matricno, @name, @className, @faculty, @department, @passportFilename, @passportUrl, @renewalDate,
                     @isEnrolled, @fingersEnrolled, @enrolledAt, CURRENT_TIMESTAMP)
                ON DUPLICATE KEY UPDATE
                    name = VALUES(name),
                    class_name = VALUES(class_name),
                    faculty = COALESCE(VALUES(faculty), faculty),
                    department = COALESCE(VALUES(department), department),
                    passport_filename = COALESCE(VALUES(passport_filename), passport_filename),
                    passport_url = VALUES(passport_url),
                    renewal_date = VALUES(renewal_date),
                    is_enrolled = COALESCE(VALUES(is_enrolled), is_enrolled),
                    fingers_enrolled = COALESCE(VALUES(fingers_enrolled), fingers_enrolled),
                    enrolled_at = COALESCE(VALUES(enrolled_at), enrolled_at),
                    updated_at = CURRENT_TIMESTAMP";

            var passportFilename = await DownloadPassportToDiskAsync(student.RegNo, student.PassportUrl);
            cmd.Parameters.AddWithValue("@regno", student.RegNo);
            cmd.Parameters.AddWithValue("@matricno", DBNull.Value);
            cmd.Parameters.AddWithValue("@name", student.Name);
            cmd.Parameters.AddWithValue("@className", student.ClassName);
            cmd.Parameters.AddWithValue("@faculty", (object?)student.Faculty ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@department", (object?)student.Department ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@passportFilename", (object?)passportFilename ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@passportUrl", (object?)student.PassportUrl ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@renewalDate", student.RenewalDate.HasValue ? student.RenewalDate.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@isEnrolled", status?.IsEnrolled);
            cmd.Parameters.AddWithValue("@fingersEnrolled", status?.EnrolledFingerCount);
            cmd.Parameters.AddWithValue("@enrolledAt", status?.EnrolledAt.HasValue == true ? status.EnrolledAt.Value : DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cache student snapshot for {RegNo}", student.RegNo);
        }
    }

    /// <summary>
    /// Clock out with a pre-verified identity (no additional matching).
    /// Uses the synced local disk cache as the source of truth for validation.
    /// </summary>
    public async Task<ClockOutResponse> ClockOutVerifiedAsync(VerifiedClockRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.RegNo))
            {
                return new ClockOutResponse { Success = false, Message = "Invalid RegNo" };
            }

            var now = request.Timestamp == default ? LagosTime.Now : request.Timestamp;
            var date = now.Date;

            // Get student info
            var studentResult = await GetStudentAsync(request.RegNo);
            var student = studentResult.Data ?? new StudentInfo { RegNo = request.RegNo, Name = request.RegNo };

            // Find today's clock-in record
            await using var conn = await _db.CreateConnectionAsync();
            await using var findCmd = conn.CreateCommand();
            findCmd.CommandText = @"
                SELECT id, timein FROM attendance
                WHERE regno = @regNo AND date = @date AND timeout IS NULL
                ORDER BY id DESC LIMIT 1";
            findCmd.Parameters.AddWithValue("@regNo", request.RegNo);
            findCmd.Parameters.AddWithValue("@date", date.ToString("yyyy-MM-dd"));

            await using var reader = await findCmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return new ClockOutResponse
                {
                    Success = false,
                    Message = "No clock-in record found for today",
                    NotClockedIn = true,
                    Student = student
                };
            }

            var recordId = reader.GetInt64(0);
            var timeInStr = reader.GetString(1);
            await reader.CloseAsync();

            // Update with clock-out time
            await using var updateCmd = conn.CreateCommand();
            updateCmd.CommandText = "UPDATE attendance SET timeout = @timeout WHERE id = @id";
            updateCmd.Parameters.AddWithValue("@timeout", now.ToString("HH:mm:ss"));
            updateCmd.Parameters.AddWithValue("@id", recordId);

            await updateCmd.ExecuteNonQueryAsync();

            // Calculate duration
            var timeIn = DateTime.Parse($"{date:yyyy-MM-dd} {timeInStr}");
            var duration = now - timeIn;

            _logger.LogInformation("Clock-out recorded locally (verified) for {RegNo} at {Time}", request.RegNo, now);

            return new ClockOutResponse
            {
                Success = true,
                Message = "Clock-out successful",
                Student = student,
                ClockInTime = timeIn,
                ClockOutTime = now,
                Duration = duration
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Local verified clock-out failed");
            return new ClockOutResponse { Success = false, Message = ex.Message };
        }
    }

    /// <summary>
    /// Clock in with a pre-verified identity (no additional matching).
    /// Uses the synced local disk cache as the source of truth for validation.
    /// </summary>
    public async Task<ClockInResponse> ClockInVerifiedAsync(VerifiedClockRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.RegNo))
            {
                return new ClockInResponse { Success = false, Message = "Invalid RegNo" };
            }

            var now = request.Timestamp == default ? LagosTime.Now : request.Timestamp;
            var date = now.Date;

            // Get student info
            var studentResult = await GetStudentAsync(request.RegNo);
            var student = studentResult.Data ?? new StudentInfo { RegNo = request.RegNo, Name = request.RegNo };

            // Check if already clocked in today
            await using var conn = await _db.CreateConnectionAsync();
            await using var checkCmd = conn.CreateCommand();
            checkCmd.CommandText = @"
                SELECT id FROM attendance
                WHERE regno = @regNo AND date = @date AND timeout IS NULL";
            checkCmd.Parameters.AddWithValue("@regNo", request.RegNo);
            checkCmd.Parameters.AddWithValue("@date", date.ToString("yyyy-MM-dd"));

            var existingRecord = await checkCmd.ExecuteScalarAsync();
            if (existingRecord != null)
            {
                return new ClockInResponse
                {
                    Success = false,
                    Message = "Already clocked in today",
                    AlreadyClockedIn = true,
                    Student = student
                };
            }

            // Record clock-in
            await using var insertCmd = conn.CreateCommand();
            insertCmd.CommandText = @"
                INSERT INTO attendance (regno, name, date, day, timein)
                VALUES (@regNo, @name, @date, @day, @timein)";
            insertCmd.Parameters.AddWithValue("@regNo", request.RegNo);
            insertCmd.Parameters.AddWithValue("@name", student.Name);
            insertCmd.Parameters.AddWithValue("@date", date.ToString("yyyy-MM-dd"));
            insertCmd.Parameters.AddWithValue("@day", date.DayOfWeek.ToString());
            insertCmd.Parameters.AddWithValue("@timein", now.ToString("HH:mm:ss"));

            await insertCmd.ExecuteNonQueryAsync();

            _logger.LogInformation("Clock-in recorded locally (verified) for {RegNo} at {Time}", request.RegNo, now);

            return new ClockInResponse
            {
                Success = true,
                Message = "Clock-in successful",
                Student = student,
                ClockInTime = now
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Local verified clock-in failed");
            return new ClockInResponse { Success = false, Message = ex.Message };
        }
    }

    public async Task UpdateEnrollmentStatusAsync(string regNo, EnrollmentStatus status)
    {
        try
        {
            await using var conn = await _db.CreateConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE students
                SET is_enrolled = @isEnrolled,
                    fingers_enrolled = @fingersEnrolled,
                    enrolled_at = @enrolledAt,
                    updated_at = CURRENT_TIMESTAMP
                WHERE regno = @regno OR matricno = @regno";
            cmd.Parameters.AddWithValue("@regno", regNo);
            cmd.Parameters.AddWithValue("@isEnrolled", status.IsEnrolled);
            cmd.Parameters.AddWithValue("@fingersEnrolled", status.EnrolledFingerCount);
            cmd.Parameters.AddWithValue("@enrolledAt", status.EnrolledAt.HasValue ? status.EnrolledAt.Value : DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update enrollment status for {RegNo}", regNo);
        }
    }

    private static string GetPassportStorageDir()
    {
        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(baseDir, "MdaBiometrics", "passports");
        Directory.CreateDirectory(dir);
        return dir;
    }

    private async Task<string?> DownloadPassportToDiskAsync(string regNo, string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        try
        {
            var fileName = BuildPassportFileName(regNo, url);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return null;
            }
            var dir = GetPassportStorageDir();
            var path = Path.Combine(dir, fileName);

            if (File.Exists(path))
            {
                return fileName;
            }

            var bytes = await _http.GetByteArrayAsync(url);
            if (bytes.Length == 0)
            {
                return null;
            }

            await File.WriteAllBytesAsync(path, bytes);
            return fileName;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to download passport for {RegNo}", regNo);
            return null;
        }
    }

    private static string? BuildPassportFileName(string regNo, string url)
    {
        try
        {
            var uri = new Uri(url);
            var name = Path.GetFileName(uri.LocalPath);
            if (!string.IsNullOrWhiteSpace(name))
            {
                return SanitizeFileName($"{SanitizeFileName(regNo)}_{name}");
            }
        }
        catch
        {
            // Ignore URL parsing errors
        }

        return $"passport_{SanitizeFileName(regNo)}_{LagosTime.Now:yyyyMMddHHmmss}.jpg";
    }

    private static byte[]? TryLoadPassportFromDisk(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        var path = Path.Combine(GetPassportStorageDir(), fileName);
        return File.Exists(path) ? File.ReadAllBytes(path) : null;
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
        return cleaned.Replace(' ', '_');
    }

    private static string ComputeTemplateHash(byte[] data)
    {
        if (data.Length == 0)
        {
            return string.Empty;
        }
        var hash = SHA256.HashData(data);
        return Convert.ToHexString(hash);
    }

    private static string GetFingerprintPreviewDir()
    {
        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(baseDir, "MdaBiometrics", "fingerprints");
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static string? SaveFingerprintPreviewToDisk(string regNo, string? sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
        {
            return null;
        }

        try
        {
            var fileName = $"fp_{SanitizeFileName(regNo)}_{LagosTime.Now:yyyyMMddHHmmssfff}.png";
            var dir = GetFingerprintPreviewDir();
            var destPath = Path.Combine(dir, fileName);
            File.Copy(sourcePath, destPath, overwrite: true);
            return fileName;
        }
        catch
        {
            return null;
        }
    }
}

