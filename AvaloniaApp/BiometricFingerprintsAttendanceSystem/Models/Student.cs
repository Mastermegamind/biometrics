namespace BiometricFingerprintsAttendanceSystem.Models;

public sealed class Student
{
    public string MatricNo { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Faculty { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string BloodGroup { get; set; } = string.Empty;
    public string GradYear { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public byte[] PassportImage { get; set; } = Array.Empty<byte>();
}
