using BiometricFingerprintsAttendanceSystem.Services.Db;
using BiometricFingerprintsAttendanceSystem.Services.Fingerprint;
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

    public OfflineDataProvider(
        DbConnectionFactory db,
        IFingerprintService fingerprint,
        ILogger<OfflineDataProvider> logger)
    {
        _db = db;
        _fingerprint = fingerprint;
        _logger = logger;
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
                SELECT matricno, name, faculty, department, passport
                FROM students
                WHERE matricno = @regNo";
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
                Faculty = reader.IsDBNull(2) ? null : reader.GetString(2),
                Department = reader.IsDBNull(3) ? null : reader.GetString(3),
                PassportPhoto = reader.IsDBNull(4) ? null : (byte[])reader["passport"]
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
            cmd.CommandText = "SELECT passport FROM students WHERE matricno = @regNo";
            cmd.Parameters.AddWithValue("@regNo", regNo);

            var result = await cmd.ExecuteScalarAsync();
            if (result == null || result == DBNull.Value)
            {
                return DataResult<byte[]>.Fail("Photo not found");
            }

            return DataResult<byte[]>.Ok((byte[])result);
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
                SELECT fingermask, created_at
                FROM new_enrollment
                WHERE matricno = @regNo";
            cmd.Parameters.AddWithValue("@regNo", regNo);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return DataResult<EnrollmentStatus>.Ok(new EnrollmentStatus { IsEnrolled = false });
            }

            var fingermask = reader.IsDBNull(0) ? "" : reader.GetString(0);
            var createdAt = reader.IsDBNull(1) ? (DateTime?)null : reader.GetDateTime(1);

            // Count enrolled fingers from fingermask
            var fingerCount = string.IsNullOrEmpty(fingermask) ? 0 : fingermask.Count(c => c == '1');

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

            // Check if already enrolled
            await using var checkCmd = conn.CreateCommand();
            checkCmd.CommandText = "SELECT id FROM new_enrollment WHERE matricno = @regNo";
            checkCmd.Parameters.AddWithValue("@regNo", request.RegNo);
            var existingId = await checkCmd.ExecuteScalarAsync();

            // Build fingermask (1 = enrolled, 0 = not enrolled)
            var fingermask = new char[10];
            Array.Fill(fingermask, '0');
            foreach (var template in request.Templates)
            {
                if (template.FingerIndex >= 1 && template.FingerIndex <= 10)
                {
                    fingermask[template.FingerIndex - 1] = '1';
                }
            }

            if (existingId != null)
            {
                // Update existing enrollment
                await using var updateCmd = conn.CreateCommand();
                var sql = new System.Text.StringBuilder("UPDATE new_enrollment SET fingermask = @fingermask");

                foreach (var template in request.Templates)
                {
                    sql.Append($", fingerdata{template.FingerIndex} = @finger{template.FingerIndex}");
                }
                sql.Append(" WHERE matricno = @regNo");

                updateCmd.CommandText = sql.ToString();
                updateCmd.Parameters.AddWithValue("@fingermask", new string(fingermask));
                updateCmd.Parameters.AddWithValue("@regNo", request.RegNo);

                foreach (var template in request.Templates)
                {
                    updateCmd.Parameters.AddWithValue($"@finger{template.FingerIndex}", template.TemplateData);
                }

                await updateCmd.ExecuteNonQueryAsync();
            }
            else
            {
                // Insert new enrollment
                await using var insertCmd = conn.CreateCommand();
                var columns = new List<string> { "matricno", "fingermask" };
                var values = new List<string> { "@regNo", "@fingermask" };

                foreach (var template in request.Templates)
                {
                    columns.Add($"fingerdata{template.FingerIndex}");
                    values.Add($"@finger{template.FingerIndex}");
                }

                insertCmd.CommandText = $"INSERT INTO new_enrollment ({string.Join(", ", columns)}) VALUES ({string.Join(", ", values)})";
                insertCmd.Parameters.AddWithValue("@regNo", request.RegNo);
                insertCmd.Parameters.AddWithValue("@fingermask", new string(fingermask));

                foreach (var template in request.Templates)
                {
                    insertCmd.Parameters.AddWithValue($"@finger{template.FingerIndex}", template.TemplateData);
                }

                await insertCmd.ExecuteNonQueryAsync();
            }

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
    /// Get all enrolled fingerprints for local matching.
    /// </summary>
    public async Task<DataResult<List<(string RegNo, List<FingerprintTemplate> Templates)>>> GetAllEnrollmentsAsync()
    {
        try
        {
            await using var conn = await _db.CreateConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT matricno, fingermask,
                       fingerdata1, fingerdata2, fingerdata3, fingerdata4, fingerdata5,
                       fingerdata6, fingerdata7, fingerdata8, fingerdata9, fingerdata10
                FROM new_enrollment";

            var results = new List<(string RegNo, List<FingerprintTemplate> Templates)>();

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var regNo = reader.GetString(0) ?? string.Empty;
                var fingermask = reader.IsDBNull(1) ? "0000000000" : reader.GetString(1);
                var templates = new List<FingerprintTemplate>();

                for (int i = 0; i < 10; i++)
                {
                    if (fingermask.Length > i && fingermask[i] == '1')
                    {
                        var colIndex = 2 + i; // fingerdata1 is at index 2
                        if (!reader.IsDBNull(colIndex))
                        {
                            var data = (byte[])reader[colIndex];
                            templates.Add(new FingerprintTemplate
                            {
                                FingerIndex = i + 1,
                                Finger = GetFingerName(i + 1),
                                TemplateData = data
                            });
                        }
                    }
                }

                if (templates.Count > 0 && !string.IsNullOrWhiteSpace(regNo))
                {
                    results.Add((regNo, templates));
                }
            }

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
                    if (verifyResult.IsMatch)
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
                WHERE matricno = @regNo AND date = @date AND timeout IS NULL";
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
            var now = DateTime.Now;
            await using var insertCmd = conn.CreateCommand();
            insertCmd.CommandText = @"
                INSERT INTO attendance (matricno, name, date, day, timein)
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
                    if (verifyResult.IsMatch)
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
                WHERE matricno = @regNo AND date = @date AND timeout IS NULL
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
            var now = DateTime.Now;
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
                SELECT id, matricno, name, date, timein, timeout
                FROM attendance
                WHERE date BETWEEN @from AND @to";

            if (!string.IsNullOrEmpty(regNo))
            {
                sql += " AND matricno = @regNo";
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
            cmd.Parameters.AddWithValue("@created", DateTime.UtcNow);

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
}
