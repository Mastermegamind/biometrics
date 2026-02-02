using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using BiometricFingerprintsAttendanceSystem.Services.Fingerprint;
using BiometricFingerprintsAttendanceSystem.Services.Time;

namespace BiometricFingerprintsAttendanceSystem.Services.Data;

/// <summary>
/// Matches live fingerprint templates against templates fetched from the online API.
/// </summary>
public sealed class OnlineTemplateMatcher
{
    private const string CacheKey = "online_enrollment_templates_all";
    private readonly OnlineDataProvider _online;
    private readonly IFingerprintService _fingerprint;
    private readonly AppConfig _config;
    private readonly IMemoryCache _cache;
    private readonly ILogger<OnlineTemplateMatcher> _logger;

    public DateTime? LastRefreshAt { get; private set; }
    public int CachedTemplateCount { get; private set; }

    public OnlineTemplateMatcher(
        OnlineDataProvider online,
        IFingerprintService fingerprint,
        AppConfig config,
        IMemoryCache cache,
        ILogger<OnlineTemplateMatcher> logger)
    {
        _online = online;
        _fingerprint = fingerprint;
        _config = config;
        _cache = cache;
        _logger = logger;
    }

    public async Task<DataResult<OnlineMatchResult>> MatchAsync(byte[] sampleTemplate, CancellationToken cancellationToken = default)
    {
        if (sampleTemplate == null || sampleTemplate.Length == 0)
        {
            return DataResult<OnlineMatchResult>.Fail("Invalid fingerprint sample", "INVALID_SAMPLE");
        }

        var templateResult = await GetTemplatesAsync(cancellationToken);
        if (!templateResult.Success || templateResult.Data == null || templateResult.Data.Count == 0)
        {
            return DataResult<OnlineMatchResult>.Fail(
                templateResult.Message ?? "No templates available",
                templateResult.ErrorCode ?? "TEMPLATES_UNAVAILABLE");
        }

        OnlineMatchResult? bestMatch = null;
        foreach (var entry in templateResult.Data)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var verify = await _fingerprint.VerifyAsync(sampleTemplate, entry.TemplateData, cancellationToken);
            if (!verify.IsMatch)
            {
                continue;
            }

            if (verify.MatchScore < _config.MinMatchScore)
            {
                continue;
            }

            if (verify.FalseAcceptRate > 0 && verify.FalseAcceptRate > _config.MaxFalseAcceptRate)
            {
                continue;
            }

            if (bestMatch == null || verify.MatchScore > bestMatch.MatchScore)
            {
                bestMatch = new OnlineMatchResult
                {
                    RegNo = entry.RegNo,
                    FingerIndex = entry.FingerIndex,
                    FingerName = entry.FingerName,
                    MatchScore = verify.MatchScore,
                    MatchFar = verify.FalseAcceptRate
                };
            }
        }

        if (bestMatch == null)
        {
            return DataResult<OnlineMatchResult>.Fail("Fingerprint not recognized", "NO_MATCH");
        }

        _logger.LogInformation(
            "Online match found RegNo={RegNo} FingerIndex={FingerIndex} Score={Score} FAR={FAR}",
            bestMatch.RegNo, bestMatch.FingerIndex, bestMatch.MatchScore, bestMatch.MatchFar);

        return DataResult<OnlineMatchResult>.Ok(bestMatch);
    }

    public void ClearCache()
    {
        _cache.Remove(CacheKey);
        CachedTemplateCount = 0;
        LastRefreshAt = null;
    }

    private async Task<DataResult<List<TemplateEntry>>> GetTemplatesAsync(CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(CacheKey, out List<TemplateEntry>? cached) && cached != null && cached.Count > 0)
        {
            CachedTemplateCount = cached.Count;
            return DataResult<List<TemplateEntry>>.Ok(cached);
        }

        var fetched = await _online.GetAllEnrollmentTemplatesAsync();
        if (!fetched.Success || fetched.Data == null || fetched.Data.Count == 0)
        {
            if (cached != null && cached.Count > 0)
            {
                return DataResult<List<TemplateEntry>>.Ok(cached, "Using cached templates");
            }

            return DataResult<List<TemplateEntry>>.Fail(
                fetched.Message ?? "Failed to fetch templates",
                "TEMPLATES_UNAVAILABLE");
        }

        var flattened = new List<TemplateEntry>();
        foreach (var (regNo, templates) in fetched.Data)
        {
            foreach (var template in templates)
            {
                if (template.TemplateData.Length == 0)
                {
                    continue;
                }
                flattened.Add(new TemplateEntry
                {
                    RegNo = regNo,
                    FingerIndex = template.FingerIndex,
                    FingerName = template.Finger,
                    TemplateData = template.TemplateData
                });
            }
        }

        _cache.Set(CacheKey, flattened, TimeSpan.FromMinutes(2));
        CachedTemplateCount = flattened.Count;
        LastRefreshAt = LagosTime.Now;
        return DataResult<List<TemplateEntry>>.Ok(flattened);
    }

    public sealed record OnlineMatchResult
    {
        public string RegNo { get; init; } = string.Empty;
        public int FingerIndex { get; init; }
        public string FingerName { get; init; } = string.Empty;
        public int MatchScore { get; init; }
        public double MatchFar { get; init; }
    }

    private sealed record TemplateEntry
    {
        public string RegNo { get; init; } = string.Empty;
        public int FingerIndex { get; init; }
        public string FingerName { get; init; } = string.Empty;
        public byte[] TemplateData { get; init; } = [];
    }
}
