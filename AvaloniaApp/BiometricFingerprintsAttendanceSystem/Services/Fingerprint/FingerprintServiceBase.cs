namespace BiometricFingerprintsAttendanceSystem.Services.Fingerprint;

/// <summary>
/// Base class for fingerprint service implementations providing common functionality.
/// </summary>
public abstract class FingerprintServiceBase : IFingerprintService
{
    private bool _disposed;

    public abstract bool IsDeviceAvailable { get; }
    public abstract FingerprintDeviceStatus DeviceStatus { get; protected set; }
    public abstract FingerprintDeviceInfo? DeviceInfo { get; }

    public event EventHandler<FingerprintCaptureEventArgs>? FingerprintCaptured;
    public event EventHandler<FingerprintDeviceStatus>? DeviceStatusChanged;

    protected void OnFingerprintCaptured(FingerprintCaptureEventArgs e)
    {
        FingerprintCaptured?.Invoke(this, e);
    }

    protected void OnDeviceStatusChanged(FingerprintDeviceStatus status)
    {
        DeviceStatus = status;
        DeviceStatusChanged?.Invoke(this, status);
    }

    public abstract Task<bool> InitializeAsync(CancellationToken cancellationToken = default);
    public abstract Task StartCaptureAsync(CancellationToken cancellationToken = default);
    public abstract Task StopCaptureAsync(CancellationToken cancellationToken = default);
    public abstract Task<FingerprintCaptureResult> CaptureAsync(CancellationToken cancellationToken = default);
    public abstract Task<byte[]?> CreateTemplateAsync(byte[] sampleData, CancellationToken cancellationToken = default);
    public abstract Task<FingerprintVerifyResult> VerifyAsync(byte[] sample, byte[] template, CancellationToken cancellationToken = default);
    public abstract Task<FingerprintMatchResult?> IdentifyAsync(byte[] sample, IReadOnlyDictionary<string, byte[]> templates, CancellationToken cancellationToken = default);
    public abstract Task<FingerprintMatchResult?> MatchAsync(byte[] sample, CancellationToken cancellationToken = default);
    public abstract Task<FingerprintEnrollResult> EnrollAsync(string username, FingerPosition finger, CancellationToken cancellationToken = default);
    public abstract Task<bool> VerifyUserAsync(string username, CancellationToken cancellationToken = default);
    public abstract Task<IReadOnlyList<FingerPosition>> ListEnrolledFingersAsync(string username, CancellationToken cancellationToken = default);
    public abstract Task<bool> DeleteEnrolledFingersAsync(string username, CancellationToken cancellationToken = default);
    public abstract int GetSampleQuality(byte[] sample);

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed resources
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
