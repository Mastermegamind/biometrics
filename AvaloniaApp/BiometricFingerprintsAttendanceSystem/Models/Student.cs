namespace BiometricFingerprintsAttendanceSystem.Models;

public sealed class Student
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
    public string Name { get; set; } = string.Empty;
    public string Faculty { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string BloodGroup { get; set; } = string.Empty;
    public string GradYear { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public byte[] PassportImage { get; set; } = Array.Empty<byte>();
}
