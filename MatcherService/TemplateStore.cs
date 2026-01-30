using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace MatcherService;

public sealed class TemplateStore
{
    private const string CacheKey = "templates_all";
    private readonly IMemoryCache _cache;
    private readonly ILogger<TemplateStore> _logger;
    private readonly string _connectionString;
    private readonly TimeSpan _cacheDuration;
    private readonly string _cacheFilePath;

    public TemplateStore(IMemoryCache cache, IConfiguration configuration, ILogger<TemplateStore> logger)
    {
        _cache = cache;
        _logger = logger;
        _connectionString = configuration.GetConnectionString("Default")
            ?? "Server=localhost;Database=mda_biometrics;Uid=root;Pwd=;Port=3319;";
        var cacheSeconds = configuration.GetValue("Matcher:CacheSeconds", 60);
        _cacheDuration = TimeSpan.FromSeconds(cacheSeconds <= 0 ? 60 : cacheSeconds);
        _cacheFilePath = configuration.GetValue<string>("Matcher:CacheFilePath")
            ?? Path.Combine(AppContext.BaseDirectory, "template-cache.json");
    }

    public async Task<IReadOnlyList<TemplateRow>> GetTemplatesAsync(string? regNo)
    {
        if (!_cache.TryGetValue(CacheKey, out List<TemplateRow>? all) || all is null)
        {
            all = await LoadTemplatesAsync();
            _cache.Set(CacheKey, all, _cacheDuration);
        }

        if (string.IsNullOrWhiteSpace(regNo))
        {
            return all;
        }

        return all.Where(t => string.Equals(t.RegNo, regNo, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public async Task<IReadOnlyList<TemplateRow>> GetTemplatesFromFileAsync(string? regNo)
    {
        var all = await LoadTemplatesFromFileAsync();
        if (string.IsNullOrWhiteSpace(regNo))
        {
            return all;
        }

        return all.Where(t => string.Equals(t.RegNo, regNo, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public async Task RefreshAsync()
    {
        var all = await LoadTemplatesAsync();
        _cache.Set(CacheKey, all, _cacheDuration);
        await SaveTemplatesToFileAsync(all);
    }

    public async Task<bool> CanConnectAsync()
    {
        try
        {
            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            await conn.CloseAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Matcher DB connection failed");
            return false;
        }
    }

    private async Task<List<TemplateRow>> LoadTemplatesAsync()
    {
        var results = new List<TemplateRow>();
        try
        {
            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT regno, finger_index, template FROM fingerprint_enrollments";

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var regno = reader.GetString(0);
                var fingerIndex = reader.GetInt32(1);
                if (reader[2] is not byte[] bytes || bytes.Length == 0)
                {
                    continue;
                }

                results.Add(new TemplateRow(regno, fingerIndex, bytes));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load templates from DB");
        }

        return results;
    }

    private async Task SaveTemplatesToFileAsync(IReadOnlyList<TemplateRow> rows)
    {
        try
        {
            var payload = rows.Select(r => new TemplateRowDto
            {
                RegNo = r.RegNo,
                FingerIndex = r.FingerIndex,
                TemplateBase64 = Convert.ToBase64String(r.TemplateData)
            }).ToList();

            var json = System.Text.Json.JsonSerializer.Serialize(payload);
            await File.WriteAllTextAsync(_cacheFilePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write template cache file");
        }
    }

    private async Task<List<TemplateRow>> LoadTemplatesFromFileAsync()
    {
        var results = new List<TemplateRow>();
        try
        {
            if (!File.Exists(_cacheFilePath))
            {
                return results;
            }

            var json = await File.ReadAllTextAsync(_cacheFilePath);
            var dto = System.Text.Json.JsonSerializer.Deserialize<List<TemplateRowDto>>(json) ?? [];
            foreach (var row in dto)
            {
                if (string.IsNullOrWhiteSpace(row.RegNo) || row.FingerIndex <= 0 || string.IsNullOrWhiteSpace(row.TemplateBase64))
                {
                    continue;
                }

                byte[]? bytes;
                try
                {
                    bytes = Convert.FromBase64String(row.TemplateBase64);
                }
                catch
                {
                    continue;
                }

                if (bytes.Length == 0)
                {
                    continue;
                }

                results.Add(new TemplateRow(row.RegNo, row.FingerIndex, bytes));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read template cache file");
        }

        return results;
    }
}

public sealed record TemplateRow(string RegNo, int FingerIndex, byte[] TemplateData);

internal sealed record TemplateRowDto
{
    public string RegNo { get; init; } = string.Empty;
    public int FingerIndex { get; init; }
    public string TemplateBase64 { get; init; } = string.Empty;
}
