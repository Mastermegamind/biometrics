using System.Text.Json.Serialization;

namespace BiometricFingerprintsAttendanceSystem.Services.Api;

public sealed class StudentProfile
{
    [JsonPropertyName("regNo")]
    public string RegNo { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("className")]
    public string ClassName { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("phone")]
    public string Phone { get; set; } = string.Empty;

    [JsonPropertyName("passport")]
    public string Passport { get; set; } = string.Empty;
}

public sealed class EnrollmentStatus
{
    [JsonPropertyName("isEnrolled")]
    public bool IsEnrolled { get; set; }
}

public sealed class EnrollmentSubmission
{
    [JsonPropertyName("regNo")]
    public string RegNo { get; set; } = string.Empty;

    [JsonPropertyName("templates")]
    public List<FingerprintTemplatePayload> Templates { get; set; } = new();

    [JsonPropertyName("adminOverride")]
    public bool AdminOverride { get; set; }
}

public sealed class FingerprintTemplatePayload
{
    [JsonPropertyName("hand")]
    public string Hand { get; set; } = string.Empty;

    [JsonPropertyName("fingerIndex")]
    public int FingerIndex { get; set; }

    [JsonPropertyName("template")]
    public string TemplateBase64 { get; set; } = string.Empty;
}

public sealed class IdentifyRequest
{
    [JsonPropertyName("hand")]
    public string Hand { get; set; } = string.Empty;

    [JsonPropertyName("fingerIndex")]
    public int FingerIndex { get; set; }

    [JsonPropertyName("template")]
    public string TemplateBase64 { get; set; } = string.Empty;
}

public sealed class ApiResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
