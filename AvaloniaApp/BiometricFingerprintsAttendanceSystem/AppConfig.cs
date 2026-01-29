using System.Text.Json;

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
    string DemoStudentClass)
{
    private const string DefaultConnectionString = "SERVER=localhost; DATABASE=mda_biometrics; userid=root; PASSWORD=root; PORT=3306;";
    private const string DefaultApiBaseUrl = "https://api.mydreamsacademy.com.ng/biometrics";

    public static AppConfig Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(path))
        {
            var connectionString = DefaultConnectionString;
            var fingerprintDevice = "None";
            var enableFingerprintSdks = false;
            var apiBaseUrl = DefaultApiBaseUrl;
            var apiKey = string.Empty;
            var apiKeyHeader = "x-api-key";
            var studentLookupPath = "students/{regNo}";
            var enrollmentStatusPath = "enrollments/{regNo}";
            var enrollmentSubmitPath = "enrollments";
            var identifyPath = "identify";
            var enableDemoMode = false;
            var demoAdminEmail = "demo@example.com";
            var demoAdminPassword = "demo1234";
            var demoStudentRegNo = "DEMO001";
            var demoStudentName = "Demo Student";
            var demoStudentClass = "Demo Class";

            ApplyEnvOverrides(
                ref connectionString,
                ref fingerprintDevice,
                ref enableFingerprintSdks,
                ref apiBaseUrl,
                ref apiKey,
                ref apiKeyHeader,
                ref studentLookupPath,
                ref enrollmentStatusPath,
                ref enrollmentSubmitPath,
                ref identifyPath,
                ref enableDemoMode,
                ref demoAdminEmail,
                ref demoAdminPassword,
                ref demoStudentRegNo,
                ref demoStudentName,
                ref demoStudentClass);

            return new AppConfig(
                connectionString,
                fingerprintDevice,
                enableFingerprintSdks,
                apiBaseUrl,
                apiKey,
                apiKeyHeader,
                studentLookupPath,
                enrollmentStatusPath,
                enrollmentSubmitPath,
                identifyPath,
                enableDemoMode,
                demoAdminEmail,
                demoAdminPassword,
                demoStudentRegNo,
                demoStudentName,
                demoStudentClass);
        }

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var connectionString = DefaultConnectionString;
            var fingerprintDevice = "None";
            var enableFingerprintSdks = false;
            var apiBaseUrl = DefaultApiBaseUrl;
            var apiKey = string.Empty;
            var apiKeyHeader = "x-api-key";
            var studentLookupPath = "students/{regNo}";
            var enrollmentStatusPath = "enrollments/{regNo}";
            var enrollmentSubmitPath = "enrollments";
            var identifyPath = "identify";
            var enableDemoMode = false;
            var demoAdminEmail = "demo@example.com";
            var demoAdminPassword = "demo1234";
            var demoStudentRegNo = "DEMO001";
            var demoStudentName = "Demo Student";
            var demoStudentClass = "Demo Class";

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
            }

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
            }

            // Parse Demo configuration
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
                ref connectionString,
                ref fingerprintDevice,
                ref enableFingerprintSdks,
                ref apiBaseUrl,
                ref apiKey,
                ref apiKeyHeader,
                ref studentLookupPath,
                ref enrollmentStatusPath,
                ref enrollmentSubmitPath,
                ref identifyPath,
                ref enableDemoMode,
                ref demoAdminEmail,
                ref demoAdminPassword,
                ref demoStudentRegNo,
                ref demoStudentName,
                ref demoStudentClass);

            return new AppConfig(
                connectionString,
                fingerprintDevice,
                enableFingerprintSdks,
                apiBaseUrl,
                apiKey,
                apiKeyHeader,
                studentLookupPath,
                enrollmentStatusPath,
                enrollmentSubmitPath,
                identifyPath,
                enableDemoMode,
                demoAdminEmail,
                demoAdminPassword,
                demoStudentRegNo,
                demoStudentName,
                demoStudentClass);
        }
        catch
        {
        }

        return new AppConfig(
            DefaultConnectionString,
            "None",
            false,
            DefaultApiBaseUrl,
            string.Empty,
            "x-api-key",
            "students/{regNo}",
            "enrollments/{regNo}",
            "enrollments",
            "identify",
            false,
            "demo@example.com",
            "demo1234",
            "DEMO001",
            "Demo Student",
            "Demo Class");
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
        ref string demoStudentClass)
    {
        var envConnection = Environment.GetEnvironmentVariable("BIOCLOCK_CONNECTION_STRING");
        if (!string.IsNullOrWhiteSpace(envConnection))
        {
            connectionString = envConnection;
        }

        var envDevice = Environment.GetEnvironmentVariable("BIOCLOCK_FINGERPRINT_DEVICE");
        if (!string.IsNullOrWhiteSpace(envDevice))
        {
            fingerprintDevice = envDevice;
        }

        var envSdks = Environment.GetEnvironmentVariable("BIOCLOCK_ENABLE_FINGERPRINT_SDKS");
        if (bool.TryParse(envSdks, out var enableSdks))
        {
            enableFingerprintSdks = enableSdks;
        }

        var envApiBase = Environment.GetEnvironmentVariable("BIOCLOCK_API_BASE_URL");
        if (!string.IsNullOrWhiteSpace(envApiBase))
        {
            apiBaseUrl = envApiBase;
        }

        var envApiKey = Environment.GetEnvironmentVariable("BIOCLOCK_API_KEY");
        if (!string.IsNullOrWhiteSpace(envApiKey))
        {
            apiKey = envApiKey;
        }

        var envApiHeader = Environment.GetEnvironmentVariable("BIOCLOCK_API_KEY_HEADER");
        if (!string.IsNullOrWhiteSpace(envApiHeader))
        {
            apiKeyHeader = envApiHeader;
        }

        var envStudentLookup = Environment.GetEnvironmentVariable("BIOCLOCK_STUDENT_LOOKUP_PATH");
        if (!string.IsNullOrWhiteSpace(envStudentLookup))
        {
            studentLookupPath = envStudentLookup;
        }

        var envEnrollmentStatus = Environment.GetEnvironmentVariable("BIOCLOCK_ENROLLMENT_STATUS_PATH");
        if (!string.IsNullOrWhiteSpace(envEnrollmentStatus))
        {
            enrollmentStatusPath = envEnrollmentStatus;
        }

        var envEnrollmentSubmit = Environment.GetEnvironmentVariable("BIOCLOCK_ENROLLMENT_SUBMIT_PATH");
        if (!string.IsNullOrWhiteSpace(envEnrollmentSubmit))
        {
            enrollmentSubmitPath = envEnrollmentSubmit;
        }

        var envIdentify = Environment.GetEnvironmentVariable("BIOCLOCK_IDENTIFY_PATH");
        if (!string.IsNullOrWhiteSpace(envIdentify))
        {
            identifyPath = envIdentify;
        }

        // Demo mode environment variables
        var envDemoMode = Environment.GetEnvironmentVariable("BIOCLOCK_DEMO_MODE");
        if (bool.TryParse(envDemoMode, out var demoMode))
        {
            enableDemoMode = demoMode;
        }

        var envDemoAdminEmail = Environment.GetEnvironmentVariable("BIOCLOCK_DEMO_ADMIN_EMAIL");
        if (!string.IsNullOrWhiteSpace(envDemoAdminEmail))
        {
            demoAdminEmail = envDemoAdminEmail;
        }

        var envDemoAdminPassword = Environment.GetEnvironmentVariable("BIOCLOCK_DEMO_ADMIN_PASSWORD");
        if (!string.IsNullOrWhiteSpace(envDemoAdminPassword))
        {
            demoAdminPassword = envDemoAdminPassword;
        }

        var envDemoStudentRegNo = Environment.GetEnvironmentVariable("BIOCLOCK_DEMO_STUDENT_REGNO");
        if (!string.IsNullOrWhiteSpace(envDemoStudentRegNo))
        {
            demoStudentRegNo = envDemoStudentRegNo;
        }

        var envDemoStudentName = Environment.GetEnvironmentVariable("BIOCLOCK_DEMO_STUDENT_NAME");
        if (!string.IsNullOrWhiteSpace(envDemoStudentName))
        {
            demoStudentName = envDemoStudentName;
        }

        var envDemoStudentClass = Environment.GetEnvironmentVariable("BIOCLOCK_DEMO_STUDENT_CLASS");
        if (!string.IsNullOrWhiteSpace(envDemoStudentClass))
        {
            demoStudentClass = envDemoStudentClass;
        }
    }
}
