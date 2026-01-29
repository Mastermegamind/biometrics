namespace BiometricFingerprintsAttendanceSystem.Services.Fingerprint;

/// <summary>
/// Result of a fingerprint verification (1:1 matching) operation.
/// </summary>
public sealed class FingerprintVerifyResult
{
    /// <summary>
    /// Whether the fingerprint matched the template.
    /// </summary>
    public bool IsMatch { get; init; }

    /// <summary>
    /// The match score (0-100). Higher scores indicate better matches.
    /// </summary>
    public int MatchScore { get; init; }

    /// <summary>
    /// The False Accept Rate (FAR) threshold used for matching.
    /// Lower values are more secure (e.g., 1 in 100,000 = 0.00001).
    /// </summary>
    public double FalseAcceptRate { get; init; }

    /// <summary>
    /// Error message if verification failed due to an error.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful match result.
    /// </summary>
    public static FingerprintVerifyResult Match(int score, double far = 0.0001) => new()
    {
        IsMatch = true,
        MatchScore = score,
        FalseAcceptRate = far
    };

    /// <summary>
    /// Creates a non-match result.
    /// </summary>
    public static FingerprintVerifyResult NoMatch(int score = 0) => new()
    {
        IsMatch = false,
        MatchScore = score
    };

    /// <summary>
    /// Creates an error result.
    /// </summary>
    public static FingerprintVerifyResult Error(string message) => new()
    {
        IsMatch = false,
        ErrorMessage = message
    };
}
