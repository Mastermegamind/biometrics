namespace BiometricFingerprintsAttendanceSystem.Services.Camera;

public sealed class OpenCvCameraService : ICameraService
{
    public Task<byte[]?> CaptureFrameAsync(CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Camera capture is not wired yet. Install and configure OpenCV, then implement capture.");
    }
}
