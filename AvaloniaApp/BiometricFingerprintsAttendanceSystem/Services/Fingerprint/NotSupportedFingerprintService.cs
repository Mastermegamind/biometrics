namespace BiometricFingerprintsAttendanceSystem.Services.Fingerprint;

public sealed class NotSupportedFingerprintService : FingerprintServiceBase
{
    private FingerprintDeviceStatus _deviceStatus = FingerprintDeviceStatus.Unknown;

    public override bool IsDeviceAvailable => false;

    public override FingerprintDeviceStatus DeviceStatus
    {
        get => _deviceStatus;
        protected set => _deviceStatus = value;
    }

    public override FingerprintDeviceInfo? DeviceInfo => null;

    public override Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    public override Task StartCaptureAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Fingerprint SDK is not configured.");

    public override Task StopCaptureAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Fingerprint SDK is not configured.");

    public override Task<FingerprintCaptureResult> CaptureAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(FingerprintCaptureResult.Failed(FingerprintCaptureStatus.DeviceError, "Fingerprint SDK is not configured."));

    public override Task<byte[]?> CreateTemplateAsync(byte[] sampleData, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Fingerprint SDK is not configured.");

    public override Task<FingerprintVerifyResult> VerifyAsync(byte[] sample, byte[] template, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Fingerprint SDK is not configured.");

    public override Task<FingerprintMatchResult?> IdentifyAsync(byte[] sample, IReadOnlyDictionary<string, byte[]> templates, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Fingerprint SDK is not configured.");

    public override Task<FingerprintMatchResult?> MatchAsync(byte[] sample, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Fingerprint SDK is not configured.");

    public override Task<FingerprintEnrollResult> EnrollAsync(string username, FingerPosition finger, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Fingerprint SDK is not configured.");

    public override Task<bool> VerifyUserAsync(string username, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Fingerprint SDK is not configured.");

    public override Task<IReadOnlyList<FingerPosition>> ListEnrolledFingersAsync(string username, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Fingerprint SDK is not configured.");

    public override Task<bool> DeleteEnrolledFingersAsync(string username, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Fingerprint SDK is not configured.");

    public override int GetSampleQuality(byte[] sample) => 0;
}
