using System.Text.Json;
using BiometricFingerprintsAttendanceSystem.Services.Data;

namespace BiometricFingerprintsAttendanceSystem;

public sealed record AppConfig(
    string ConnectionString,
    string FingerprintDevice,
    bool EnableFingerprintSdks,
    string ApiBaseUrl,
    string ApiKey,
    string ApiKeyHeader,
    string StudentLookupPath,
    string EnrollmentStatusPath,
    string EnrollmentSubmitPath,
    string IdentifyPath,
    bool EnableDemoMode,
    string DemoAdminEmail,
    string DemoAdminPassword,
    string DemoStudentRegNo,
    string DemoStudentName,
    string DemoStudentClass,
    SyncMode SyncMode,
    int ApiTimeoutSeconds,
    int MaxFailedLoginAttempts,
    int LockoutMinutes,
    int MinimumFingersRequired,
    int CaptureTimeoutSeconds,
    int MinMatchScore,
    double MaxFalseAcceptRate)
{
    private const string DefaultConnectionString = "Server=localhost;Database=mda_biometrics;Uid=root;Pwd=;Port=3319;";
    private const string DefaultApiBaseUrl = "https://portal.mydreamsacademy.com.ng";

    public static AppConfig Load()
    {
        LoadDotEnv();
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(path))
        {
            return CreateDefaultConfig();
        }

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            return ParseConfig(doc);
        }
        catch
        {
            return CreateDefaultConfig();
        }
    }

    private static AppConfig CreateDefaultConfig()
    {
        var connectionString = DefaultConnectionString;
        var fingerprintDevice = "None";
        var enableFingerprintSdks = false;
        var apiBaseUrl = DefaultApiBaseUrl;
        var apiKey = string.Empty;
        var apiKeyHeader = "X-API-Key";
        var studentLookupPath = "students?regNo={regNo}";
        var enrollmentStatusPath = "enrollment/status?regNo={regNo}";
        var enrollmentSubmitPath = "enrollments";
        var identifyPath = "identify";
        var enableDemoMode = false;
        var demoAdminEmail = "demo@example.com";
        var demoAdminPassword = "demo1234";
        var demoStudentRegNo = "DEMO001";
        var demoStudentName = "Demo Student";
        var demoStudentClass = "Demo Class";
        var syncMode = SyncMode.OnlineFirst;
        var apiTimeoutSeconds = 30;
        var maxFailedLoginAttempts = 5;
        var lockoutMinutes = 15;
        var minimumFingersRequired = 2;
        var captureTimeoutSeconds = 30;
        var minMatchScore = 70;
        var maxFalseAcceptRate = 0.001;

        ApplyEnvOverrides(
            ref connectionString, ref fingerprintDevice, ref enableFingerprintSdks,
            ref apiBaseUrl, ref apiKey, ref apiKeyHeader,
            ref studentLookupPath, ref enrollmentStatusPath, ref enrollmentSubmitPath, ref identifyPath,
            ref enableDemoMode, ref demoAdminEmail, ref demoAdminPassword,
            ref demoStudentRegNo, ref demoStudentName, ref demoStudentClass,
            ref syncMode, ref apiTimeoutSeconds, ref maxFailedLoginAttempts, ref lockoutMinutes, ref minimumFingersRequired,
            ref captureTimeoutSeconds, ref minMatchScore, ref maxFalseAcceptRate);

        return new AppConfig(
            connectionString, fingerprintDevice, enableFingerprintSdks,
            apiBaseUrl, apiKey, apiKeyHeader,
            studentLookupPath, enrollmentStatusPath, enrollmentSubmitPath, identifyPath,
            enableDemoMode, demoAdminEmail, demoAdminPassword,
            demoStudentRegNo, demoStudentName, demoStudentClass,
            syncMode, apiTimeoutSeconds, maxFailedLoginAttempts, lockoutMinutes, minimumFingersRequired,
            captureTimeoutSeconds, minMatchScore, maxFalseAcceptRate);
    }

    private static AppConfig ParseConfig(JsonDocument doc)
    {
        var connectionString = DefaultConnectionString;
        var fingerprintDevice = "None";
        var enableFingerprintSdks = false;
        var apiBaseUrl = DefaultApiBaseUrl;
        var apiKey = string.Empty;
        var apiKeyHeader = "X-API-Key";
        var studentLookupPath = "students?regNo={regNo}";
        var enrollmentStatusPath = "enrollment/status?regNo={regNo}";
        var enrollmentSubmitPath = "enrollments";
        var identifyPath = "identify";
        var enableDemoMode = false;
        var demoAdminEmail = "demo@example.com";
        var demoAdminPassword = "demo1234";
        var demoStudentRegNo = "DEMO001";
        var demoStudentName = "Demo Student";
        var demoStudentClass = "Demo Class";
        var syncMode = SyncMode.OnlineFirst;
        var apiTimeoutSeconds = 30;
        var maxFailedLoginAttempts = 5;
        var lockoutMinutes = 15;
        var minimumFingersRequired = 2;
        var captureTimeoutSeconds = 30;
        var minMatchScore = 70;
        var maxFalseAcceptRate = 0.001;

        // Parse ConnectionStrings
        if (doc.RootElement.TryGetProperty("ConnectionStrings", out var conns) &&
            conns.TryGetProperty("Default", out var def) &&
            def.ValueKind == JsonValueKind.String)
        {
            var value = def.GetString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                connectionString = value;
            }
        }

        // Parse Fingerprint
        if (doc.RootElement.TryGetProperty("Fingerprint", out var fingerprint))
        {
            if (fingerprint.TryGetProperty("Device", out var device) && device.ValueKind == JsonValueKind.String)
            {
                fingerprintDevice = device.GetString() ?? fingerprintDevice;
            }

            if (fingerprint.TryGetProperty("EnableSdks", out var enable) && enable.ValueKind == JsonValueKind.True)
            {
                enableFingerprintSdks = true;
            }

            if (fingerprint.TryGetProperty("MinimumFingers", out var minFingers) && minFingers.ValueKind == JsonValueKind.Number)
            {
                minimumFingersRequired = minFingers.GetInt32();
            }

            if (fingerprint.TryGetProperty("CaptureTimeoutSeconds", out var captureTimeout) && captureTimeout.ValueKind == JsonValueKind.Number)
            {
                captureTimeoutSeconds = captureTimeout.GetInt32();
            }
            if (fingerprint.TryGetProperty("MinMatchScore", out var minScore) && minScore.ValueKind == JsonValueKind.Number)
            {
                minMatchScore = minScore.GetInt32();
            }
            if (fingerprint.TryGetProperty("MaxFalseAcceptRate", out var maxFar) && maxFar.ValueKind == JsonValueKind.Number)
            {
                maxFalseAcceptRate = maxFar.GetDouble();
            }
        }

        // Parse Api
        if (doc.RootElement.TryGetProperty("Api", out var api))
        {
            if (api.TryGetProperty("BaseUrl", out var baseUrl) && baseUrl.ValueKind == JsonValueKind.String)
            {
                apiBaseUrl = baseUrl.GetString() ?? apiBaseUrl;
            }

            if (api.TryGetProperty("ApiKey", out var key) && key.ValueKind == JsonValueKind.String)
            {
                apiKey = key.GetString() ?? apiKey;
            }

            if (api.TryGetProperty("ApiKeyHeader", out var header) && header.ValueKind == JsonValueKind.String)
            {
                apiKeyHeader = header.GetString() ?? apiKeyHeader;
            }

            if (api.TryGetProperty("StudentLookupPath", out var lookup) && lookup.ValueKind == JsonValueKind.String)
            {
                studentLookupPath = lookup.GetString() ?? studentLookupPath;
            }

            if (api.TryGetProperty("EnrollmentStatusPath", out var status) && status.ValueKind == JsonValueKind.String)
            {
                enrollmentStatusPath = status.GetString() ?? enrollmentStatusPath;
            }

            if (api.TryGetProperty("EnrollmentSubmitPath", out var submit) && submit.ValueKind == JsonValueKind.String)
            {
                enrollmentSubmitPath = submit.GetString() ?? enrollmentSubmitPath;
            }

            if (api.TryGetProperty("IdentifyPath", out var identify) && identify.ValueKind == JsonValueKind.String)
            {
                identifyPath = identify.GetString() ?? identifyPath;
            }

            if (api.TryGetProperty("TimeoutSeconds", out var timeout) && timeout.ValueKind == JsonValueKind.Number)
            {
                apiTimeoutSeconds = timeout.GetInt32();
            }

            // Parse SyncMode
            if (api.TryGetProperty("SyncMode", out var syncModeValue) && syncModeValue.ValueKind == JsonValueKind.String)
            {
                var syncModeStr = syncModeValue.GetString();
                if (Enum.TryParse<SyncMode>(syncModeStr, ignoreCase: true, out var parsedMode))
                {
                    syncMode = parsedMode;
                }
            }
        }

        if (doc.RootElement.TryGetProperty("Auth", out var auth))
        {
            if (auth.TryGetProperty("MaxFailedLoginAttempts", out var maxFailed) && maxFailed.ValueKind == JsonValueKind.Number)
            {
                maxFailedLoginAttempts = maxFailed.GetInt32();
            }

            if (auth.TryGetProperty("LockoutMinutes", out var lockout) && lockout.ValueKind == JsonValueKind.Number)
            {
                lockoutMinutes = lockout.GetInt32();
            }
        }

        // Parse Demo
        if (doc.RootElement.TryGetProperty("Demo", out var demo))
        {
            if (demo.TryGetProperty("Enabled", out var enabled) && enabled.ValueKind == JsonValueKind.True)
            {
                enableDemoMode = true;
            }

            if (demo.TryGetProperty("AdminEmail", out var adminEmail) && adminEmail.ValueKind == JsonValueKind.String)
            {
                demoAdminEmail = adminEmail.GetString() ?? demoAdminEmail;
            }

            if (demo.TryGetProperty("AdminPassword", out var adminPassword) && adminPassword.ValueKind == JsonValueKind.String)
            {
                demoAdminPassword = adminPassword.GetString() ?? demoAdminPassword;
            }

            if (demo.TryGetProperty("StudentRegNo", out var studentRegNo) && studentRegNo.ValueKind == JsonValueKind.String)
            {
                demoStudentRegNo = studentRegNo.GetString() ?? demoStudentRegNo;
            }

            if (demo.TryGetProperty("StudentName", out var studentName) && studentName.ValueKind == JsonValueKind.String)
            {
                demoStudentName = studentName.GetString() ?? demoStudentName;
            }

            if (demo.TryGetProperty("StudentClass", out var studentClass) && studentClass.ValueKind == JsonValueKind.String)
            {
                demoStudentClass = studentClass.GetString() ?? demoStudentClass;
            }
        }

        ApplyEnvOverrides(
            ref connectionString, ref fingerprintDevice, ref enableFingerprintSdks,
            ref apiBaseUrl, ref apiKey, ref apiKeyHeader,
            ref studentLookupPath, ref enrollmentStatusPath, ref enrollmentSubmitPath, ref identifyPath,
            ref enableDemoMode, ref demoAdminEmail, ref demoAdminPassword,
            ref demoStudentRegNo, ref demoStudentName, ref demoStudentClass,
            ref syncMode, ref apiTimeoutSeconds, ref maxFailedLoginAttempts, ref lockoutMinutes, ref minimumFingersRequired,
            ref captureTimeoutSeconds, ref minMatchScore, ref maxFalseAcceptRate);

        return new AppConfig(
            connectionString, fingerprintDevice, enableFingerprintSdks,
            apiBaseUrl, apiKey, apiKeyHeader,
            studentLookupPath, enrollmentStatusPath, enrollmentSubmitPath, identifyPath,
            enableDemoMode, demoAdminEmail, demoAdminPassword,
            demoStudentRegNo, demoStudentName, demoStudentClass,
            syncMode, apiTimeoutSeconds, maxFailedLoginAttempts, lockoutMinutes, minimumFingersRequired,
            captureTimeoutSeconds, minMatchScore, maxFalseAcceptRate);
    }

    private static void ApplyEnvOverrides(
        ref string connectionString,
        ref string fingerprintDevice,
        ref bool enableFingerprintSdks,
        ref string apiBaseUrl,
        ref string apiKey,
        ref string apiKeyHeader,
        ref string studentLookupPath,
        ref string enrollmentStatusPath,
        ref string enrollmentSubmitPath,
        ref string identifyPath,
        ref bool enableDemoMode,
        ref string demoAdminEmail,
        ref string demoAdminPassword,
        ref string demoStudentRegNo,
        ref string demoStudentName,
        ref string demoStudentClass,
        ref SyncMode syncMode,
        ref int apiTimeoutSeconds,
        ref int maxFailedLoginAttempts,
        ref int lockoutMinutes,
        ref int minimumFingersRequired,
        ref int captureTimeoutSeconds,
        ref int minMatchScore,
        ref double maxFalseAcceptRate)
    {
        ApplyEnvVariable("BIOCLOCK_CONNECTION_STRING", ref connectionString);
        ApplyEnvVariable("BIOCLOCK_FINGERPRINT_DEVICE", ref fingerprintDevice);
        ApplyEnvVariable("BIOCLOCK_ENABLE_FINGERPRINT_SDKS", ref enableFingerprintSdks);
        ApplyEnvVariable("BIOCLOCK_API_BASE_URL", ref apiBaseUrl);
        ApplyEnvVariable("BIOCLOCK_API_KEY", ref apiKey);
        ApplyEnvVariable("API_KEY", ref apiKey);
        ApplyEnvVariable("BIOCLOCK_API_KEY_HEADER", ref apiKeyHeader);
        ApplyEnvVariable("BIOCLOCK_STUDENT_LOOKUP_PATH", ref studentLookupPath);
        ApplyEnvVariable("BIOCLOCK_ENROLLMENT_STATUS_PATH", ref enrollmentStatusPath);
        ApplyEnvVariable("BIOCLOCK_ENROLLMENT_SUBMIT_PATH", ref enrollmentSubmitPath);
        ApplyEnvVariable("BIOCLOCK_IDENTIFY_PATH", ref identifyPath);
        ApplyEnvVariable("BIOCLOCK_DEMO_MODE", ref enableDemoMode);
        ApplyEnvVariable("BIOCLOCK_DEMO_ADMIN_EMAIL", ref demoAdminEmail);
        ApplyEnvVariable("BIOCLOCK_DEMO_ADMIN_PASSWORD", ref demoAdminPassword);
        ApplyEnvVariable("BIOCLOCK_DEMO_STUDENT_REGNO", ref demoStudentRegNo);
        ApplyEnvVariable("BIOCLOCK_DEMO_STUDENT_NAME", ref demoStudentName);
        ApplyEnvVariable("BIOCLOCK_DEMO_STUDENT_CLASS", ref demoStudentClass);
        ApplyEnvVariable("BIOCLOCK_SYNC_MODE", ref syncMode);
        ApplyEnvVariable("BIOCLOCK_API_TIMEOUT", ref apiTimeoutSeconds);
        ApplyEnvVariable("BIOCLOCK_AUTH_MAX_FAILED", ref maxFailedLoginAttempts);
        ApplyEnvVariable("BIOCLOCK_AUTH_LOCKOUT_MINUTES", ref lockoutMinutes);
        ApplyEnvVariable("BIOCLOCK_MIN_FINGERS", ref minimumFingersRequired);
        ApplyEnvVariable("BIOCLOCK_CAPTURE_TIMEOUT", ref captureTimeoutSeconds);
        ApplyEnvVariable("BIOCLOCK_MATCH_MIN_SCORE", ref minMatchScore);
        ApplyEnvVariable("BIOCLOCK_MATCH_MAX_FAR", ref maxFalseAcceptRate);
    }

    private static void ApplyEnvVariable(string envKey, ref string value)
    {
        var envValue = Environment.GetEnvironmentVariable(envKey);
        if (!string.IsNullOrWhiteSpace(envValue))
        {
            value = envValue;
        }
    }

    private static void ApplyEnvVariable(string envKey, ref bool value)
    {
        var envValue = Environment.GetEnvironmentVariable(envKey);
        if (bool.TryParse(envValue, out var parsedValue))
        {
            value = parsedValue;
        }
    }

    private static void ApplyEnvVariable(string envKey, ref int value)
    {
        var envValue = Environment.GetEnvironmentVariable(envKey);
        if (int.TryParse(envValue, out var parsedValue))
        {
            value = parsedValue;
        }
    }

    private static void ApplyEnvVariable(string envKey, ref double value)
    {
        var envValue = Environment.GetEnvironmentVariable(envKey);
        if (double.TryParse(envValue, out var parsedValue))
        {
            value = parsedValue;
        }
    }

    private static void ApplyEnvVariable(string envKey, ref SyncMode value)
    {
        var envValue = Environment.GetEnvironmentVariable(envKey);
        if (Enum.TryParse<SyncMode>(envValue, ignoreCase: true, out var parsedValue))
        {
            value = parsedValue;
        }
    }

    private static void LoadDotEnv()
    {
        var cwdPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
        var basePath = Path.Combine(AppContext.BaseDirectory, ".env");
        var path = File.Exists(cwdPath) ? cwdPath : basePath;
        if (!File.Exists(path))
        {
            return;
        }

        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            var idx = line.IndexOf('=');
            if (idx <= 0)
            {
                continue;
            }

            var key = line[..idx].Trim();
            var value = line[(idx + 1)..].Trim();
            if (value.Length >= 2 && value.StartsWith('"') && value.EndsWith('"'))
            {
                value = value[1..^1];
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}
