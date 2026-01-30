namespace BiometricFingerprintsAttendanceSystem.Services.Fingerprint;

public sealed class FingerprintMatchResult
{
    private string _regNo = string.Empty;

    public string RegNo
    {
        get => _regNo;
        set => _regNo = value ?? string.Empty;
    }

    public string MatricNo
    {
        get => RegNo;
        set => RegNo = value;
    }
    public int FalseAcceptRate { get; set; }
}
