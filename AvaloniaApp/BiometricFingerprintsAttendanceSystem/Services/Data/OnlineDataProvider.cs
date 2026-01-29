using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

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
        _http = new HttpClient
        {
            BaseAddress = new Uri(config.ApiBaseUrl ?? "http://localhost:5000"),
            Timeout = TimeSpan.FromSeconds(config.ApiTimeoutSeconds > 0 ? config.ApiTimeoutSeconds : 30)
        };

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Add API key if configured
        if (!string.IsNullOrEmpty(config.ApiKey))
        {
            _http.DefaultRequestHeaders.Add("X-API-Key", config.ApiKey);
        }
    }

    /// <summary>
    /// Check if API is reachable.
    /// </summary>
    public async Task<bool> PingAsync()
    {
        try
        {
            var response = await _http.GetAsync("/api/health");
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
            var response = await _http.GetAsync($"/api/students/{Uri.EscapeDataString(regNo)}");

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return DataResult<StudentInfo>.Fail("Student not found", "NOT_FOUND");

                return DataResult<StudentInfo>.Fail($"API error: {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiStudentResponse>(json, _jsonOptions);

            if (apiResponse == null)
                return DataResult<StudentInfo>.Fail("Invalid API response");

            var student = new StudentInfo
            {
                RegNo = apiResponse.RegNo ?? apiResponse.MatricNo ?? regNo,
                Name = apiResponse.Name ?? apiResponse.FullName ?? "Unknown",
                ClassName = apiResponse.ClassName ?? apiResponse.Class ?? "",
                Department = apiResponse.Department,
                Faculty = apiResponse.Faculty,
                PassportUrl = apiResponse.PassportUrl ?? apiResponse.Passport
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
            var response = await _http.GetAsync($"/api/students/{Uri.EscapeDataString(regNo)}/photo");

            if (!response.IsSuccessStatusCode)
                return DataResult<byte[]>.Fail("Photo not found");

            var photoBytes = await response.Content.ReadAsByteArrayAsync();
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
            var response = await _http.GetAsync($"/api/enrollment/status/{Uri.EscapeDataString(regNo)}");

            if (!response.IsSuccessStatusCode)
            {
                return DataResult<EnrollmentStatus>.Ok(new EnrollmentStatus { IsEnrolled = false });
            }

            var json = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiEnrollmentStatusResponse>(json, _jsonOptions);

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
    /// Submit enrollment.
    /// </summary>
    public async Task<DataResult> SubmitEnrollmentAsync(EnrollmentRequest request)
    {
        try
        {
            var payload = new ApiEnrollmentSubmitRequest
            {
                RegNo = request.RegNo,
                Name = request.Name,
                ClassName = request.ClassName,
                Templates = request.Templates.Select(t => new ApiTemplatePayload
                {
                    Finger = t.Finger,
                    FingerIndex = t.FingerIndex,
                    TemplateBase64 = Convert.ToBase64String(t.TemplateData)
                }).ToList(),
                EnrolledAt = request.EnrolledAt
            };

            var response = await _http.PostAsJsonAsync("/api/enrollment/submit", payload, _jsonOptions);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Enrollment submission failed: {Error}", error);
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
            var payload = new ApiClockInRequest
            {
                TemplateBase64 = Convert.ToBase64String(request.FingerprintTemplate),
                Timestamp = request.Timestamp,
                DeviceId = request.DeviceId
            };

            var response = await _http.PostAsJsonAsync("/api/attendance/clockin", payload, _jsonOptions);
            var json = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiClockInResponse>(json, _jsonOptions);

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
            var payload = new ApiClockOutRequest
            {
                TemplateBase64 = Convert.ToBase64String(request.FingerprintTemplate),
                Timestamp = request.Timestamp,
                DeviceId = request.DeviceId
            };

            var response = await _http.PostAsJsonAsync("/api/attendance/clockout", payload, _jsonOptions);
            var json = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiClockOutResponse>(json, _jsonOptions);

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

            var response = await _http.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return DataResult<List<AttendanceRecord>>.Fail("Failed to get attendance");

            var json = await response.Content.ReadAsStringAsync();
            var apiRecords = JsonSerializer.Deserialize<List<ApiAttendanceRecord>>(json, _jsonOptions) ?? [];

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

    // ==================== API DTOs ====================

    private record ApiStudentResponse
    {
        public string? RegNo { get; init; }
        public string? MatricNo { get; init; }
        public string? Name { get; init; }
        public string? FullName { get; init; }
        public string? ClassName { get; init; }
        public string? Class { get; init; }
        public string? Department { get; init; }
        public string? Faculty { get; init; }
        public string? PassportUrl { get; init; }
        public string? Passport { get; init; }
    }

    private record ApiEnrollmentStatusResponse
    {
        public bool IsEnrolled { get; init; }
        public int FingerCount { get; init; }
        public DateTime? EnrolledAt { get; init; }
    }

    private record ApiEnrollmentSubmitRequest
    {
        public string RegNo { get; init; } = "";
        public string Name { get; init; } = "";
        public string ClassName { get; init; } = "";
        public List<ApiTemplatePayload> Templates { get; init; } = [];
        public DateTime EnrolledAt { get; init; }
    }

    private record ApiTemplatePayload
    {
        public string Finger { get; init; } = "";
        public int FingerIndex { get; init; }
        public string TemplateBase64 { get; init; } = "";
    }

    private record ApiClockInRequest
    {
        public string TemplateBase64 { get; init; } = "";
        public DateTime Timestamp { get; init; }
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
        public string TemplateBase64 { get; init; } = "";
        public DateTime Timestamp { get; init; }
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
}
