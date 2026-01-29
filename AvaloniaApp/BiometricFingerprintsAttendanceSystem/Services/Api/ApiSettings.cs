namespace BiometricFingerprintsAttendanceSystem.Services.Api;

public sealed record ApiSettings(
    string BaseUrl,
    string ApiKey,
    string ApiKeyHeader,
    string StudentLookupPath,
    string EnrollmentStatusPath,
    string EnrollmentSubmitPath,
    string IdentifyPath
);
