namespace BiometricFingerprintsAttendanceSystem.Models;

public sealed class DemoFingerprintRecord
{
    public int FingerIndex { get; set; }
    public string TemplateBase64 { get; set; } = string.Empty;
    public string? ImageName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
