namespace BiometricFingerprintsAttendanceSystem.Models;

public sealed class AttendanceRecord
{
    public string MatricNo { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Day { get; set; } = string.Empty;
    public string TimeIn { get; set; } = string.Empty;
    public string TimeOut { get; set; } = string.Empty;
}
