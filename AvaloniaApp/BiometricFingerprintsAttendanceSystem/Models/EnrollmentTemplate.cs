namespace BiometricFingerprintsAttendanceSystem.Models;

public sealed class EnrollmentTemplate
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
    public int FingerIndex { get; set; }
    public byte[] TemplateData { get; set; } = Array.Empty<byte>();
}
