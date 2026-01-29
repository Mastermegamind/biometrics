namespace BiometricFingerprintsAttendanceSystem.Models;

public sealed class EnrollmentTemplate
{
    public string MatricNo { get; set; } = string.Empty;
    public int FingerIndex { get; set; }
    public byte[] TemplateData { get; set; } = Array.Empty<byte>();
}
