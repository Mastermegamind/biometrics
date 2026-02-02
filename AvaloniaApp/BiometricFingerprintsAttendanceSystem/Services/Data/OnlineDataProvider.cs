using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using BiometricFingerprintsAttendanceSystem.Services.Net;
using BiometricFingerprintsAttendanceSystem.Services.Fingerprint;

namespace BiometricFingerprintsAttendanceSystem.Services.Data;

/// <summary>
/// Handles all API calls for online operations.
/// </summary>
public class OnlineDataProvider
{
    private readonly HttpClient _http;
    private readonly AppConfig _config;
    private readonly ILogger<OnlineDataProvider> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public OnlineDataProvider(AppConfig config, ILogger<OnlineDataProvider> logger)
    {
        _config = config;
        _logger = logger;
        var baseAddress = new Uri(config.ApiBaseUrl ?? "http://localhost:5000");
        var timeout = TimeSpan.FromSeconds(config.ApiTimeoutSeconds > 0 ? config.ApiTimeoutSeconds : 30);
        _http = HttpClientFactory.CreateWithBaseAddress(
            baseAddress,
            timeout,
            string.IsNullOrWhiteSpace(config.ApiKeyHeader) ? "X-API-Key" : config.ApiKeyHeader,
            config.ApiKey);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // API key is configured via HttpClientFactory
    }

    /// <summary>
    /// Check if API is reachable.
    /// </summary>
    public async Task<bool> PingAsync()
    {
        try
        {
            var path = "/api/health";
            LogRequest("GET", path, null);
            var response = await _http.GetAsync(path);
            LogResponse(path, response, null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "API ping failed");
            return false;
        }
    }

    /// <summary>
    /// Get student by registration number.
    /// </summary>
    public async Task<DataResult<StudentInfo>> GetStudentAsync(string regNo)
    {
        try
        {
            var path = BuildApiRegNoPath(_config.StudentLookupPath, regNo);
            LogRequest("GET", path, new { regNo });
            var response = await _http.GetAsync(path);
            var body = await SafeReadResponseBodyAsync(response);
            LogResponse(path, response, body);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return DataResult<StudentInfo>.Fail("Student not found", "NOT_FOUND");

                return DataResult<StudentInfo>.Fail($"API error: {response.StatusCode}");
            }

            var envelope = JsonSerializer.Deserialize<ApiStudentEnvelope>(body, _jsonOptions);
            var apiResponse = envelope?.Student ?? JsonSerializer.Deserialize<ApiStudentResponse>(body, _jsonOptions);

            if (apiResponse == null)
                return DataResult<StudentInfo>.Fail("Invalid API response");

            var student = new StudentInfo
            {
                RegNo = apiResponse.RegNo ?? apiResponse.MatricNo ?? regNo,
                Name = apiResponse.Name ?? apiResponse.FullName ?? "Unknown",
                ClassName = apiResponse.ClassName ?? apiResponse.Class ?? apiResponse.Classes?.FirstOrDefault() ?? "",
                Department = apiResponse.Department,
                Faculty = apiResponse.Faculty,
                PassportUrl = apiResponse.PassportUrl ?? apiResponse.Passport,
                RenewalDate = apiResponse.RenewalDate
            };

            return DataResult<StudentInfo>.Ok(student);
        }
        catch (TaskCanceledException)
        {
            return DataResult<StudentInfo>.Fail("Request timed out", "TIMEOUT");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to get student {RegNo}", regNo);
            return DataResult<StudentInfo>.Fail("Network error", "NETWORK_ERROR");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting student {RegNo}", regNo);
            return DataResult<StudentInfo>.Fail(ex.Message, "UNKNOWN_ERROR");
        }
    }

    /// <summary>
    /// Get student photo.
    /// </summary>
    public async Task<DataResult<byte[]>> GetStudentPhotoAsync(string regNo)
    {
        try
        {
            var path = BuildApiRegNoPath("students/photo", regNo);
            LogRequest("GET", path, new { regNo });
            var response = await _http.GetAsync(path);
            LogResponse(path, response, null);

            if (!response.IsSuccessStatusCode)
                return DataResult<byte[]>.Fail("Photo not found");

            var photoBytes = await response.Content.ReadAsByteArrayAsync();
            _logger.LogInformation("API response body bytes for {Path}: {ByteCount}", path, photoBytes.Length);
            return DataResult<byte[]>.Ok(photoBytes);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get photo for {RegNo}", regNo);
            return DataResult<byte[]>.Fail(ex.Message);
        }
    }

    /// <summary>
    /// Get enrollment status.
    /// </summary>
    public async Task<DataResult<EnrollmentStatus>> GetEnrollmentStatusAsync(string regNo)
    {
        try
        {
            var path = BuildApiRegNoPath(_config.EnrollmentStatusPath, regNo);
            LogRequest("GET", path, new { regNo });
            var response = await _http.GetAsync(path);
            var body = await SafeReadResponseBodyAsync(response);
            LogResponse(path, response, body);

            if (!response.IsSuccessStatusCode)
            {
                return DataResult<EnrollmentStatus>.Ok(new EnrollmentStatus { IsEnrolled = false });
            }

            var apiResponse = JsonSerializer.Deserialize<ApiEnrollmentStatusResponse>(body, _jsonOptions);

            var status = new EnrollmentStatus
            {
                IsEnrolled = apiResponse?.IsEnrolled ?? false,
                EnrolledFingerCount = apiResponse?.FingerCount ?? 0,
                EnrolledAt = apiResponse?.EnrolledAt
            };

            return DataResult<EnrollmentStatus>.Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get enrollment status for {RegNo}", regNo);
            return DataResult<EnrollmentStatus>.Fail(ex.Message);
        }
    }

    /// <summary>
    /// Get enrolled fingerprint templates for a specific student from API.
    /// </summary>
    public async Task<DataResult<List<FingerprintTemplate>>> GetEnrollmentTemplatesAsync(string regNo)
    {
        try
        {
            var path = BuildApiRegNoPath(_config.EnrollmentTemplatesPath, regNo);
            LogRequest("GET", path, new { regNo });
            var response = await _http.GetAsync(path);
            var body = await SafeReadResponseBodyAsync(response);
            LogResponse(path, response, body);

            if (!response.IsSuccessStatusCode)
            {
                return DataResult<List<FingerprintTemplate>>.Fail($"API error: {response.StatusCode}");
            }

            var records = ParseEnrollmentRecords(body);
            var templates = new List<FingerprintTemplate>();
            foreach (var record in records)
            {
                var base64 = string.IsNullOrWhiteSpace(record.TemplateData)
                    ? record.Template
                    : record.TemplateData;
                if (string.IsNullOrWhiteSpace(base64))
                {
                    continue;
                }

                byte[]? bytes;
                try
                {
                    bytes = Convert.FromBase64String(base64);
                }
                catch
                {
                    continue;
                }

                if (bytes.Length == 0)
                {
                    continue;
                }

                templates.Add(new FingerprintTemplate
                {
                    FingerIndex = record.FingerIndex,
                    Finger = record.FingerName ?? $"Finger {record.FingerIndex}",
                    TemplateData = bytes
                });
            }

            return DataResult<List<FingerprintTemplate>>.Ok(templates);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get enrollment templates for {RegNo}", regNo);
            return DataResult<List<FingerprintTemplate>>.Fail(ex.Message);
        }
    }

    /// <summary>
    /// Submit enrollment.
    /// </summary>
    public async Task<DataResult> SubmitEnrollmentAsync(EnrollmentRequest request)
    {
        try
        {
            var records = new List<ApiEnrollmentRecord>();
            foreach (var t in request.Templates)
            {
                LogTemplateByteLength(request.RegNo, t);
                records.Add(new ApiEnrollmentRecord
                {
                    RegNo = request.RegNo,
                    FingerIndex = t.FingerIndex,
                    FingerName = NormalizeFingerForApi(t.Finger, t.FingerIndex),
                    Template = Convert.ToBase64String(t.TemplateData),
                    TemplateData = Convert.ToBase64String(t.TemplateData),
                    ImagePreview = string.IsNullOrWhiteSpace(t.ImagePath) ? null : Path.GetFileName(t.ImagePath),
                    ImagePreviewData = LoadPreviewBase64(t.ImagePath),
                    CapturedAt = request.EnrolledAt
                });
            }

            var payload = new ApiEnrollmentSubmitRequest
            {
                RegNo = request.RegNo,
                Records = records
            };

            var path = NormalizeApiPath(_config.EnrollmentSubmitPath);

            // Serialize with explicit options to ensure correct format
            var actualJson = JsonSerializer.Serialize(payload, _jsonOptions);
            _logger.LogInformation("Enrollment actual JSON payload (first 500 chars): {Json}",
                actualJson.Length > 500 ? actualJson[..500] + "..." : actualJson);

            LogRequest("POST", path, new
            {
                payload.RegNo,
                Records = payload.Records.Select(t => new
                {
                    t.RegNo,
                    t.FingerName,
                    t.FingerIndex,
                    TemplateBytes = t.Template.Length,
                    t.ImagePreview,
                    t.CapturedAt
                }).ToList()
            });

            // Use explicit StringContent to ensure proper Content-Type and encoding
            using var content = new StringContent(actualJson, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(path, content);
            var body = await SafeReadResponseBodyAsync(response);
            LogResponse(path, response, body);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Enrollment submission failed: {Error}", body);
                return DataResult.Fail($"Enrollment failed: {response.StatusCode}");
            }

            return DataResult.Ok("Enrollment successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit enrollment for {RegNo}", request.RegNo);
            return DataResult.Fail(ex.Message, "NETWORK_ERROR");
        }
    }

    /// <summary>
    /// Clock in with fingerprint.
    /// </summary>
    public async Task<ClockInResponse> ClockInAsync(ClockInRequest request)
    {
        try
        {
            var templateBase64 = Convert.ToBase64String(request.FingerprintTemplate);
            var payload = new ApiClockInRequest
            {
                TemplateBase64 = templateBase64,
                Timestamp = request.Timestamp,
                DeviceId = request.DeviceId
            };
            _logger.LogInformation("Clock-in payload template bytes: {ByteCount}", request.FingerprintTemplate?.Length ?? 0);

            var path = "/api/attendance/clockin";

            // Serialize with explicit options
            var actualJson = JsonSerializer.Serialize(payload, _jsonOptions);
            _logger.LogInformation("Clock-in actual JSON payload (first 300 chars): {Json}",
                actualJson.Length > 300 ? actualJson[..300] + "..." : actualJson);

            _logger.LogInformation("Clock-in POST {Path} (templateBase64 length: {Len} chars)",
                path, payload.TemplateBase64.Length);

            // Use explicit StringContent to ensure proper Content-Type and encoding
            using var content = new StringContent(actualJson, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(path, content);
            var body = await SafeReadResponseBodyAsync(response);
            _logger.LogInformation("Clock-in response: {StatusCode} - {Body}", (int)response.StatusCode, body);
            var apiResponse = JsonSerializer.Deserialize<ApiClockInResponse>(body, _jsonOptions);

            if (!response.IsSuccessStatusCode || apiResponse == null)
            {
                return new ClockInResponse
                {
                    Success = false,
                    Message = apiResponse?.Message ?? "Clock-in failed"
                };
            }

            return new ClockInResponse
            {
                Success = apiResponse.Success,
                Message = apiResponse.Message,
                Student = apiResponse.Student != null ? new StudentInfo
                {
                    RegNo = apiResponse.Student.RegNo ?? "",
                    Name = apiResponse.Student.Name ?? "",
                    ClassName = apiResponse.Student.ClassName ?? "",
                    PassportUrl = apiResponse.Student.PassportUrl
                } : null,
                ClockInTime = apiResponse.ClockInTime,
                AlreadyClockedIn = apiResponse.AlreadyClockedIn
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Clock-in API call failed");
            return new ClockInResponse
            {
                Success = false,
                Message = $"Network error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Clock out with fingerprint.
    /// </summary>
    public async Task<ClockOutResponse> ClockOutAsync(ClockOutRequest request)
    {
        try
        {
            var templateBase64 = Convert.ToBase64String(request.FingerprintTemplate);
            var payload = new ApiClockOutRequest
            {
                TemplateBase64 = templateBase64,
                Timestamp = request.Timestamp,
                DeviceId = request.DeviceId
            };
            _logger.LogInformation("Clock-out payload template bytes: {ByteCount}", request.FingerprintTemplate?.Length ?? 0);

            var path = "/api/attendance/clockout";

            // Serialize with explicit options
            var actualJson = JsonSerializer.Serialize(payload, _jsonOptions);
            _logger.LogInformation("Clock-out actual JSON payload (first 300 chars): {Json}",
                actualJson.Length > 300 ? actualJson[..300] + "..." : actualJson);

            LogRequest("POST", path, new
            {
                TemplateBytes = payload.TemplateBase64.Length,
                payload.Timestamp,
                payload.DeviceId
            });

            // Use explicit StringContent to ensure proper Content-Type and encoding
            using var content = new StringContent(actualJson, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(path, content);
            var body = await SafeReadResponseBodyAsync(response);
            LogResponse(path, response, body);
            var apiResponse = JsonSerializer.Deserialize<ApiClockOutResponse>(body, _jsonOptions);

            if (!response.IsSuccessStatusCode || apiResponse == null)
            {
                return new ClockOutResponse
                {
                    Success = false,
                    Message = apiResponse?.Message ?? "Clock-out failed"
                };
            }

            return new ClockOutResponse
            {
                Success = apiResponse.Success,
                Message = apiResponse.Message,
                Student = apiResponse.Student != null ? new StudentInfo
                {
                    RegNo = apiResponse.Student.RegNo ?? "",
                    Name = apiResponse.Student.Name ?? "",
                    ClassName = apiResponse.Student.ClassName ?? ""
                } : null,
                ClockInTime = apiResponse.ClockInTime,
                ClockOutTime = apiResponse.ClockOutTime,
                Duration = apiResponse.Duration,
                NotClockedIn = apiResponse.NotClockedIn
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Clock-out API call failed");
            return new ClockOutResponse
            {
                Success = false,
                Message = $"Network error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Get attendance records.
    /// </summary>
    public async Task<DataResult<List<AttendanceRecord>>> GetAttendanceAsync(DateTime from, DateTime to, string? regNo = null)
    {
        try
        {
            var url = $"/api/attendance?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}";
            if (!string.IsNullOrEmpty(regNo))
                url += $"&regNo={Uri.EscapeDataString(regNo)}";

            LogRequest("GET", url, new { from, to, regNo });
            var response = await _http.GetAsync(url);
            var body = await SafeReadResponseBodyAsync(response);
            LogResponse(url, response, body);

            if (!response.IsSuccessStatusCode)
                return DataResult<List<AttendanceRecord>>.Fail("Failed to get attendance");

            var apiRecords = JsonSerializer.Deserialize<List<ApiAttendanceRecord>>(body, _jsonOptions) ?? [];

            var records = apiRecords.Select(r => new AttendanceRecord
            {
                Id = r.Id,
                RegNo = r.RegNo ?? "",
                Name = r.Name ?? "",
                ClassName = r.ClassName ?? "",
                Date = r.Date,
                TimeIn = r.TimeIn,
                TimeOut = r.TimeOut,
                IsSynced = true
            }).ToList();

            return DataResult<List<AttendanceRecord>>.Ok(records);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get attendance");
            return DataResult<List<AttendanceRecord>>.Fail(ex.Message);
        }
    }

    private static string NormalizeFingerForApi(string? finger, int fingerIndex)
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

    // ==================== API DTOs ====================

    private record ApiStudentEnvelope
    {
        public bool Success { get; init; }
        public ApiStudentResponse? Student { get; init; }
    }

    private record ApiStudentResponse
    {
        public string? RegNo { get; init; }
        public string? MatricNo { get; init; }
        public string? Name { get; init; }
        public string? FullName { get; init; }
        public string? ClassName { get; init; }
        public string? Class { get; init; }
        public List<string>? Classes { get; init; }
        public string? Department { get; init; }
        public string? Faculty { get; init; }
        public string? PassportUrl { get; init; }
        public string? Passport { get; init; }
        public DateTime? RenewalDate { get; init; }
    }

    private record ApiEnrollmentStatusResponse
    {
        public bool IsEnrolled { get; init; }
        public int FingerCount { get; init; }
        public DateTime? EnrolledAt { get; init; }
    }

    private record ApiEnrollmentListEnvelope
    {
        public bool Success { get; init; }
        public List<ApiEnrollmentRecord>? Records { get; init; }
        public List<ApiEnrollmentRecord>? Data { get; init; }
    }

    private record ApiEnrollmentSubmitRequest
    {
        [JsonPropertyName("regno")]
        public string RegNo { get; init; } = "";
        [JsonPropertyName("records")]
        public List<ApiEnrollmentRecord> Records { get; init; } = [];
    }

    private record ApiEnrollmentRecord
    {
        [JsonPropertyName("regno")]
        public string RegNo { get; init; } = "";
        [JsonPropertyName("finger_index")]
        public int FingerIndex { get; init; }
        [JsonPropertyName("finger_name")]
        public string FingerName { get; init; } = "";
        [JsonPropertyName("template")]
        public string Template { get; init; } = "";
        [JsonPropertyName("template_data")]
        public string? TemplateData { get; init; }
        [JsonPropertyName("image_preview")]
        public string? ImagePreview { get; init; }
        [JsonPropertyName("image_preview_data")]
        public string? ImagePreviewData { get; init; }
        [JsonPropertyName("captured_at")]
        public DateTime CapturedAt { get; init; }
    }

    private record ApiClockInRequest
    {
        [JsonPropertyName("templateBase64")]
        public string TemplateBase64 { get; init; } = "";
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; init; }
        [JsonPropertyName("deviceId")]
        public string? DeviceId { get; init; }
    }

    private record ApiClockInResponse
    {
        public bool Success { get; init; }
        public string? Message { get; init; }
        public ApiStudentResponse? Student { get; init; }
        public DateTime? ClockInTime { get; init; }
        public bool AlreadyClockedIn { get; init; }
    }

    private record ApiClockOutRequest
    {
        [JsonPropertyName("templateBase64")]
        public string TemplateBase64 { get; init; } = "";
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; init; }
        [JsonPropertyName("deviceId")]
        public string? DeviceId { get; init; }
    }

    private record ApiClockOutResponse
    {
        public bool Success { get; init; }
        public string? Message { get; init; }
        public ApiStudentResponse? Student { get; init; }
        public DateTime? ClockInTime { get; init; }
        public DateTime? ClockOutTime { get; init; }
        public TimeSpan? Duration { get; init; }
        public bool NotClockedIn { get; init; }
    }

    private record ApiAttendanceRecord
    {
        public long Id { get; init; }
        public string? RegNo { get; init; }
        public string? Name { get; init; }
        public string? ClassName { get; init; }
        public DateTime Date { get; init; }
        public DateTime? TimeIn { get; init; }
        public DateTime? TimeOut { get; init; }
    }

    private void LogRequest(string method, string path, object? payload)
    {
        var url = new Uri(_http.BaseAddress ?? new Uri("http://localhost"), path);
        if (payload == null)
        {
            _logger.LogInformation("API request {Method} {Url}", method, url);
            return;
        }
        _logger.LogInformation("API request {Method} {Url} payload: {Payload}", method, url, SafeSerialize(payload));
    }

    private void LogResponse(string path, HttpResponseMessage response, string? body)
    {
        var url = new Uri(_http.BaseAddress ?? new Uri("http://localhost"), path);
        if (body == null)
        {
            _logger.LogInformation("API response {Url} {StatusCode}", url, (int)response.StatusCode);
            return;
        }
        _logger.LogInformation("API response {Url} {StatusCode} body: {Body}", url, (int)response.StatusCode, Truncate(body, 2000));
    }

    private static string SafeSerialize(object payload)
    {
        return JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    private static async Task<string> SafeReadResponseBodyAsync(HttpResponseMessage response)
    {
        try
        {
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            return $"<failed to read response body: {ex.Message}>";
        }
    }

    private static string Truncate(string value, int max)
    {
        if (value.Length <= max) return value;
        return value[..max] + "...(truncated)";
    }

    private static string BuildApiRegNoPath(string path, string regNo)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            path = "students";
        }

        var normalized = NormalizeApiPath(path);
        var encodedRegNo = Uri.EscapeDataString(regNo);

        // Check if path contains {regNo} placeholder
        if (normalized.Contains("{regNo}", StringComparison.OrdinalIgnoreCase))
        {
            var placeholderIndex = normalized.IndexOf("{regNo}", StringComparison.OrdinalIgnoreCase);
            var queryIndex = normalized.IndexOf('?', StringComparison.OrdinalIgnoreCase);
            var placeholderInQuery = queryIndex >= 0 && queryIndex < placeholderIndex;

            // If regNo contains slashes and the placeholder is in the path segment,
            // use query parameter instead to avoid encoded slashes in path segments.
            if (regNo.Contains('/') && !placeholderInQuery)
            {
                var pathWithoutPlaceholder = normalized
                    .Replace("/{regNo}", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("{regNo}", "", StringComparison.OrdinalIgnoreCase)
                    .TrimEnd('/');
                var separator = pathWithoutPlaceholder.Contains('?') ? "&" : "?";
                return $"{pathWithoutPlaceholder}{separator}regNo={encodedRegNo}";
            }

            return normalized.Replace("{regNo}", encodedRegNo, StringComparison.OrdinalIgnoreCase);
        }

        var sep = normalized.Contains('?') ? "&" : "?";
        return $"{normalized}{sep}regNo={encodedRegNo}";
    }

    private static string NormalizeApiPath(string path)
    {
        var trimmed = path.Trim();
        if (trimmed.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        if (trimmed.StartsWith("/", StringComparison.Ordinal))
        {
            return $"/api{trimmed}";
        }

        return $"/api/{trimmed}";
    }

    private void LogTemplateByteLength(string regNo, FingerprintTemplate template)
    {
        var bytes = template.TemplateData?.Length ?? 0;
        _logger.LogInformation(
            "Enrollment payload bytes RegNo={RegNo} Finger={Finger} Index={Index} Bytes={Bytes}",
            regNo, template.Finger, template.FingerIndex, bytes);
    }

    private static string? LoadPreviewBase64(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
        {
            return null;
        }

        try
        {
            var bytes = File.ReadAllBytes(imagePath);
            return bytes.Length == 0 ? null : Convert.ToBase64String(bytes);
        }
        catch
        {
            return null;
        }
    }

    private List<ApiEnrollmentRecord> ParseEnrollmentRecords(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return [];
        }

        try
        {
            var envelope = JsonSerializer.Deserialize<ApiEnrollmentListEnvelope>(body, _jsonOptions);
            if (envelope?.Records != null && envelope.Records.Count > 0)
            {
                return envelope.Records;
            }
            if (envelope?.Data != null && envelope.Data.Count > 0)
            {
                return envelope.Data;
            }
        }
        catch
        {
            // fall through to try array parsing
        }

        try
        {
            var direct = JsonSerializer.Deserialize<List<ApiEnrollmentRecord>>(body, _jsonOptions);
            return direct ?? [];
        }
        catch
        {
            return [];
        }
    }
}
