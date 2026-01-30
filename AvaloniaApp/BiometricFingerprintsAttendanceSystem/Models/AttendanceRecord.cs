namespace BiometricFingerprintsAttendanceSystem.Models;

public sealed class AttendanceRecord
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
    public string Date { get; set; } = string.Empty;
    public string Day { get; set; } = string.Empty;
    public string TimeIn { get; set; } = string.Empty;
    public string TimeOut { get; set; } = string.Empty;
}
