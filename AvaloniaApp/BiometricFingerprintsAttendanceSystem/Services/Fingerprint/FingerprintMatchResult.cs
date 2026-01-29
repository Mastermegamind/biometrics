namespace BiometricFingerprintsAttendanceSystem.Services.Fingerprint;

public sealed class FingerprintMatchResult
{
    public string MatricNo { get; set; } = string.Empty;
    public int FalseAcceptRate { get; set; }
}
