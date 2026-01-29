namespace BiometricFingerprintsAttendanceSystem.Services.Camera;

public interface ICameraService
{
    Task<byte[]?> CaptureFrameAsync(CancellationToken cancellationToken = default);
}
