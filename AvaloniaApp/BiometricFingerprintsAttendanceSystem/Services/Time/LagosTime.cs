namespace BiometricFingerprintsAttendanceSystem.Services.Time;

public static class LagosTime
{
    private static readonly Lazy<TimeZoneInfo> LagosZone = new(ResolveLagosZone);

    public static DateTime Now => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, LagosZone.Value).DateTime;

    private static TimeZoneInfo ResolveLagosZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Africa/Lagos");
        }
        catch
        {
            // Windows timezone ID fallback
            return TimeZoneInfo.FindSystemTimeZoneById("W. Central Africa Standard Time");
        }
    }
}
