using System.Net.Http.Json;
using System.Text.Json;

namespace BiometricFingerprintsAttendanceSystem.Services.Api;

public sealed class BiometricsApiClient
{
    private readonly HttpClient _http;
    private readonly ApiSettings _settings;

    public BiometricsApiClient(ApiSettings settings)
    {
        _settings = settings;
        _http = new HttpClient
        {
            BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/")
        };

        if (!string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            _http.DefaultRequestHeaders.Remove(settings.ApiKeyHeader);
            _http.DefaultRequestHeaders.Add(settings.ApiKeyHeader, settings.ApiKey);
        }
    }

    public async Task<StudentProfile?> GetStudentByRegAsync(string regNo, CancellationToken cancellationToken = default)
    {
        var path = _settings.StudentLookupPath.Replace("{regNo}", Uri.EscapeDataString(regNo));
        var response = await _http.GetAsync(path, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseStudent(json);
    }

    public async Task<EnrollmentStatus?> GetEnrollmentStatusAsync(string regNo, CancellationToken cancellationToken = default)
    {
        var path = _settings.EnrollmentStatusPath.Replace("{regNo}", Uri.EscapeDataString(regNo));
        var response = await _http.GetAsync(path, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<EnrollmentStatus>(cancellationToken: cancellationToken);
    }

    public async Task<ApiResult> SubmitEnrollmentAsync(EnrollmentSubmission submission, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync(_settings.EnrollmentSubmitPath, submission, cancellationToken);
        var message = response.IsSuccessStatusCode ? "Enrollment saved." : await response.Content.ReadAsStringAsync(cancellationToken);
        return new ApiResult { Success = response.IsSuccessStatusCode, Message = message };
    }

    public async Task<StudentProfile?> IdentifyAsync(IdentifyRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync(_settings.IdentifyPath, request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseStudent(json);
    }

    public async Task<bool> PingAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, string.Empty);
            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static StudentProfile? ParseStudent(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("data", out var data))
            {
                root = data;
            }

            var profile = new StudentProfile
            {
                RegNo = GetString(root, "regNo", "regno", "matricNo", "matricno"),
                Name = GetString(root, "name", "fullName"),
                ClassName = GetString(root, "class", "className", "classname"),
                Email = GetString(root, "email"),
                Phone = GetString(root, "phone", "phoneNo", "phoneno"),
                Passport = GetString(root, "passport", "passportUrl", "photo")
            };

            if (string.IsNullOrWhiteSpace(profile.RegNo) && string.IsNullOrWhiteSpace(profile.Name))
            {
                return null;
            }

            return profile;
        }
        catch
        {
            return null;
        }
    }

    private static string GetString(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
            {
                return value.GetString() ?? string.Empty;
            }
        }

        return string.Empty;
    }
}
