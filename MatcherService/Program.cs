using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using MatcherService;
#if DIGITALPERSONA_SDK
using DPFP;
using DPFP.Capture;
using DPFP.Processing;
using DPFP.Verification;
#endif

var builder = WebApplication.CreateBuilder(args);

var config = builder.Configuration;
var minMatchScore = config.GetValue("Matcher:MinMatchScore", 70);
var maxFalseAcceptRate = config.GetValue("Matcher:MaxFalseAcceptRate", 0.001d);

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<TemplateStore>();

var app = builder.Build();

app.MapGet("/health", async (TemplateStore store) =>
{
    var dbOk = await store.CanConnectAsync();
    var sdkEnabled =
#if DIGITALPERSONA_SDK
        true;
#else
        false;
#endif
    return Results.Ok(new
    {
        status = dbOk ? "ok" : "degraded",
        database = dbOk,
        sdkEnabled,
        timeUtc = DateTimeOffset.UtcNow.ToString("O")
    });
});

app.MapPost("/match/refresh", async (TemplateStore store) =>
{
#if !DIGITALPERSONA_SDK
    return Results.StatusCode(501);
#else
    await store.RefreshAsync();
    return Results.Ok(new { success = true, message = "Template cache refreshed" });
#endif
});

app.MapPost("/match", async ([FromBody] MatchRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.TemplateBase64))
    {
        return Results.BadRequest(new { success = false, message = "templateBase64 is required" });
    }

    byte[] sampleBytes;
    try
    {
        sampleBytes = Convert.FromBase64String(request.TemplateBase64);
    }
    catch
    {
        return Results.BadRequest(new { success = false, message = "Invalid base64 template" });
    }

#if !DIGITALPERSONA_SDK
    return Results.StatusCode(501);
#else
    var store = app.Services.GetRequiredService<TemplateStore>();
    var templates = await store.GetTemplatesAsync(request.RegNo);

    MatchResult? best = null;
    foreach (var row in templates)
    {
        var result = DpfpMatcher.Verify(sampleBytes, row.TemplateData);
        if (!result.IsMatch)
        {
            continue;
        }

        var farOk = result.FalseAcceptRate <= 0 || result.FalseAcceptRate <= maxFalseAcceptRate;
        if (result.MatchScore >= minMatchScore && farOk)
        {
            if (best == null || result.MatchScore > best.MatchScore)
            {
                best = new MatchResult
                {
                    RegNo = row.RegNo,
                    FingerIndex = row.FingerIndex,
                    MatchScore = result.MatchScore,
                    FalseAcceptRate = result.FalseAcceptRate
                };
            }
        }
    }

    if (best == null)
    {
        return Results.Ok(new { success = false, message = "No match" });
    }

    return Results.Ok(new
    {
        success = true,
        regno = best.RegNo,
        finger_index = best.FingerIndex,
        match_score = best.MatchScore,
        far = best.FalseAcceptRate
    });
#endif
});

app.MapPost("/match/fallback", async ([FromBody] MatchRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.TemplateBase64))
    {
        return Results.BadRequest(new { success = false, message = "templateBase64 is required" });
    }

    byte[] sampleBytes;
    try
    {
        sampleBytes = Convert.FromBase64String(request.TemplateBase64);
    }
    catch
    {
        return Results.BadRequest(new { success = false, message = "Invalid base64 template" });
    }

#if !DIGITALPERSONA_SDK
    return Results.StatusCode(501);
#else
    var store = app.Services.GetRequiredService<TemplateStore>();
    var templates = await store.GetTemplatesFromFileAsync(request.RegNo);

    MatchResult? best = null;
    foreach (var row in templates)
    {
        var result = DpfpMatcher.Verify(sampleBytes, row.TemplateData);
        if (!result.IsMatch)
        {
            continue;
        }

        var farOk = result.FalseAcceptRate <= 0 || result.FalseAcceptRate <= maxFalseAcceptRate;
        if (result.MatchScore >= minMatchScore && farOk)
        {
            if (best == null || result.MatchScore > best.MatchScore)
            {
                best = new MatchResult
                {
                    RegNo = row.RegNo,
                    FingerIndex = row.FingerIndex,
                    MatchScore = result.MatchScore,
                    FalseAcceptRate = result.FalseAcceptRate
                };
            }
        }
    }

    if (best == null)
    {
        return Results.Ok(new { success = false, message = "No match" });
    }

    return Results.Ok(new
    {
        success = true,
        regno = best.RegNo,
        finger_index = best.FingerIndex,
        match_score = best.MatchScore,
        far = best.FalseAcceptRate
    });
#endif
});

app.Run();

internal sealed record MatchRequest
{
    public string TemplateBase64 { get; init; } = string.Empty;
    public string? RegNo { get; init; }
}

internal sealed record MatchResult
{
    public string RegNo { get; init; } = string.Empty;
    public int FingerIndex { get; init; }
    public int MatchScore { get; init; }
    public double FalseAcceptRate { get; init; }
}

#if DIGITALPERSONA_SDK
internal static class DpfpMatcher
{
    public static FingerprintVerifyResult Verify(byte[] sampleBytes, byte[] templateBytes)
    {
        try
        {
            var sampleFeatures = TryLoadFeatureSet(sampleBytes) ?? TryExtractFeatureSet(sampleBytes);
            if (sampleFeatures == null)
            {
                return FingerprintVerifyResult.Error("Invalid sample data");
            }

            using var templateStream = new MemoryStream(templateBytes);
            var template = new Template(templateStream);

            var result = Verification.Verify(sampleFeatures, template);
            if (result.Verified)
            {
                var score = Math.Min(100, (int)(100 - Math.Log10(result.FARAchieved + 1) * 10));
                return FingerprintVerifyResult.Match(score, result.FARAchieved);
            }

            return FingerprintVerifyResult.NoMatch();
        }
        catch (Exception ex)
        {
            return FingerprintVerifyResult.Error(ex.Message);
        }
    }

    private static FeatureSet? TryLoadFeatureSet(byte[] data)
    {
        try
        {
            using var stream = new MemoryStream(data);
            return new FeatureSet(stream);
        }
        catch
        {
            return null;
        }
    }

    private static FeatureSet? TryExtractFeatureSet(byte[] data)
    {
        try
        {
            using var sampleStream = new MemoryStream(data);
            var sample = new Sample(sampleStream);
            var extractor = new FeatureExtraction();
            var feedback = CaptureFeedback.None;
            var features = new FeatureSet();
            extractor.CreateFeatureSet(sample, DataPurpose.Verification, ref feedback, ref features);
            return feedback == CaptureFeedback.Good ? features : null;
        }
        catch
        {
            return null;
        }
    }
}

internal sealed class FingerprintVerifyResult
{
    public bool IsMatch { get; init; }
    public int MatchScore { get; init; }
    public double FalseAcceptRate { get; init; }
    public string? ErrorMessage { get; init; }

    public static FingerprintVerifyResult Match(int score, double far) => new()
    {
        IsMatch = true,
        MatchScore = score,
        FalseAcceptRate = far
    };

    public static FingerprintVerifyResult NoMatch() => new() { IsMatch = false };

    public static FingerprintVerifyResult Error(string message) => new()
    {
        IsMatch = false,
        ErrorMessage = message
    };
}
#endif
