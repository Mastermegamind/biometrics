using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace BiometricFingerprintsAttendanceSystem.Services.Config;

public sealed class AppConfigService
{
    private readonly ILogger<AppConfigService> _logger;
    private readonly string _path;

    public AppConfigService(ILogger<AppConfigService> logger)
    {
        _logger = logger;
        _path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
    }

    public async Task<bool> SaveSettingsAsync(
        string apiBaseUrl,
        string syncMode,
        string fingerprintDevice,
        bool demoEnabled,
        CancellationToken cancellationToken = default)
    {
        try
        {
            JsonNode root;
            if (File.Exists(_path))
            {
                var json = await File.ReadAllTextAsync(_path, cancellationToken);
                root = JsonNode.Parse(json) ?? new JsonObject();
            }
            else
            {
                root = new JsonObject();
            }

            var apiNode = root["Api"] as JsonObject ?? new JsonObject();
            apiNode["BaseUrl"] = apiBaseUrl;
            apiNode["SyncMode"] = syncMode;
            root["Api"] = apiNode;

            var fingerprintNode = root["Fingerprint"] as JsonObject ?? new JsonObject();
            fingerprintNode["Device"] = fingerprintDevice;
            root["Fingerprint"] = fingerprintNode;

            var demoNode = root["Demo"] as JsonObject ?? new JsonObject();
            demoNode["Enabled"] = demoEnabled;
            root["Demo"] = demoNode;

            var options = new JsonSerializerOptions { WriteIndented = true };
            await File.WriteAllTextAsync(_path, root.ToJsonString(options), cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings");
            return false;
        }
    }
}
