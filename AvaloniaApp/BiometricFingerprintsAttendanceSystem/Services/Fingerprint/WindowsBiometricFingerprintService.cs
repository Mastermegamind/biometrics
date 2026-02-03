// Windows Biometric Framework (WBF) fingerprint service
// Uses Windows Hello infrastructure - works with any WBF-compatible fingerprint reader
// This is the fallback service when vendor-specific SDKs are not available

using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace BiometricFingerprintsAttendanceSystem.Services.Fingerprint;

/// <summary>
/// Windows Biometric Framework fingerprint service.
/// Uses WinBio API for fingerprint capture and verification.
/// Works with any fingerprint reader that has WBF-compatible drivers.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsBiometricFingerprintService : FingerprintServiceBase
{
    private FingerprintDeviceStatus _deviceStatus = FingerprintDeviceStatus.Unknown;
    private FingerprintDeviceInfo? _deviceInfo;
    private bool _isCapturing;
    private nint _sessionHandle;
    private nint _unitId;
    private readonly object _syncLock = new();
    private TaskCompletionSource<FingerprintCaptureResult>? _captureCompletionSource;

    // WinBio constants
    private const uint WINBIO_TYPE_FINGERPRINT = 0x00000008;
    private const uint WINBIO_POOL_SYSTEM = 0x00000001;
    private const uint WINBIO_FLAG_DEFAULT = 0x00000000;
    private const uint WINBIO_FLAG_BASIC = 0x00010000;
    private const uint WINBIO_FLAG_RAW = 0x00000001;
    private const uint WINBIO_ID_TYPE_NULL = 0;
    private const uint WINBIO_ID_TYPE_WILDCARD = 1;
    private const uint WINBIO_ID_TYPE_GUID = 2;
    private const uint WINBIO_ID_TYPE_SID = 3;
    private const int S_OK = 0;

    public override bool IsDeviceAvailable => DeviceStatus is FingerprintDeviceStatus.Ready or FingerprintDeviceStatus.Connected;

    public override FingerprintDeviceStatus DeviceStatus
    {
        get => _deviceStatus;
        protected set => _deviceStatus = value;
    }

    public override FingerprintDeviceInfo? DeviceInfo => _deviceInfo;

    public override Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows())
        {
            OnDeviceStatusChanged(FingerprintDeviceStatus.Error);
            return Task.FromResult(false);
        }

        try
        {
            // Open a WinBio session
            var hr = WinBioOpenSession(
                WINBIO_TYPE_FINGERPRINT,
                WINBIO_POOL_SYSTEM,
                WINBIO_FLAG_DEFAULT,
                nint.Zero,
                0,
                nint.Zero,
                out _sessionHandle);

            if (hr != S_OK)
            {
                OnDeviceStatusChanged(FingerprintDeviceStatus.Disconnected);
                return Task.FromResult(false);
            }

            // Enumerate units to find fingerprint sensors
            hr = WinBioEnumBiometricUnits(
                WINBIO_TYPE_FINGERPRINT,
                out var unitSchemaArray,
                out var unitCount);

            if (hr != S_OK || unitCount == 0)
            {
                WinBioCloseSession(_sessionHandle);
                _sessionHandle = nint.Zero;
                OnDeviceStatusChanged(FingerprintDeviceStatus.Disconnected);
                return Task.FromResult(false);
            }

            // Get first available unit
            _unitId = GetFirstUnitId(unitSchemaArray, unitCount);
            WinBioFree(unitSchemaArray);

            // Set device info based on WBF
            _deviceInfo = new FingerprintDeviceInfo
            {
                Vendor = "Windows Biometric Framework",
                ProductName = "WBF Fingerprint Reader",
                Driver = "Windows Biometric Service",
                DeviceType = FingerprintDeviceType.WindowsBiometric,
                SupportsEnrollment = true,
                SupportsVerification = true,
                SupportsIdentification = true,
                ImageWidth = 0,  // WBF doesn't expose raw image dimensions
                ImageHeight = 0,
                ImageDpi = 0
            };

            OnDeviceStatusChanged(FingerprintDeviceStatus.Ready);
            return Task.FromResult(true);
        }
        catch (Exception)
        {
            OnDeviceStatusChanged(FingerprintDeviceStatus.Error);
            return Task.FromResult(false);
        }
    }

    public override Task StartCaptureAsync(CancellationToken cancellationToken = default)
    {
        if (_isCapturing || _sessionHandle == nint.Zero) return Task.CompletedTask;

        _isCapturing = true;
        OnDeviceStatusChanged(FingerprintDeviceStatus.WaitingForFinger);
        return Task.CompletedTask;
    }

    public override Task StopCaptureAsync(CancellationToken cancellationToken = default)
    {
        if (!_isCapturing) return Task.CompletedTask;

        _isCapturing = false;

        if (_sessionHandle != nint.Zero)
        {
            WinBioCancel(_sessionHandle);
        }

        OnDeviceStatusChanged(FingerprintDeviceStatus.Ready);
        return Task.CompletedTask;
    }

    public override async Task<FingerprintCaptureResult> CaptureAsync(CancellationToken cancellationToken = default)
    {
        if (_sessionHandle == nint.Zero)
        {
            return FingerprintCaptureResult.Failed(FingerprintCaptureStatus.DeviceError, "WBF session not initialized.");
        }

        _captureCompletionSource = new TaskCompletionSource<FingerprintCaptureResult>();

        using var registration = cancellationToken.Register(() =>
        {
            WinBioCancel(_sessionHandle);
            _captureCompletionSource.TrySetResult(
                FingerprintCaptureResult.Failed(FingerprintCaptureStatus.Cancelled));
        });

        try
        {
            OnDeviceStatusChanged(FingerprintDeviceStatus.WaitingForFinger);

            // Capture sample using WinBio
            var result = await Task.Run(() =>
            {
                var hr = WinBioCaptureSample(
                    _sessionHandle,
                    WINBIO_FLAG_RAW,
                    out var unitId,
                    out var sample,
                    out var sampleSize,
                    out var rejectDetail);

                if (hr == S_OK && sampleSize > 0)
                {
                    var sampleData = new byte[sampleSize];
                    Marshal.Copy(sample, sampleData, 0, (int)sampleSize);
                    WinBioFree(sample);

                    return FingerprintCaptureResult.Successful(sampleData, null, 80);
                }

                if (sample != nint.Zero)
                {
                    WinBioFree(sample);
                }

                return MapRejectDetailToResult(rejectDetail);
            }, cancellationToken);

            OnDeviceStatusChanged(FingerprintDeviceStatus.Ready);
            return result;
        }
        catch (OperationCanceledException)
        {
            OnDeviceStatusChanged(FingerprintDeviceStatus.Ready);
            return FingerprintCaptureResult.Failed(FingerprintCaptureStatus.Cancelled);
        }
        catch (Exception ex)
        {
            OnDeviceStatusChanged(FingerprintDeviceStatus.Error);
            return FingerprintCaptureResult.Failed(FingerprintCaptureStatus.DeviceError, ex.Message);
        }
    }

    public override Task<byte[]?> CreateTemplateAsync(byte[] sampleData, CancellationToken cancellationToken = default)
    {
        // WBF handles template creation internally
        // Return the sample data as a pseudo-template for storage
        // Real template extraction requires Windows Hello APIs
        return Task.FromResult<byte[]?>(sampleData.Length > 0 ? sampleData : null);
    }

    public override async Task<FingerprintVerifyResult> VerifyAsync(byte[] sample, byte[] template, CancellationToken cancellationToken = default)
    {
        if (_sessionHandle == nint.Zero)
        {
            return FingerprintVerifyResult.Error("WBF session not initialized.");
        }

        try
        {
            // WBF uses identity-based verification, not template-based
            // We perform a verify against the current user
            var result = await Task.Run(() =>
            {
                var hr = WinBioVerify(
                    _sessionHandle,
                    out var identity,
                    out var subFactor,
                    out var rejectDetail);

                return hr == S_OK
                    ? FingerprintVerifyResult.Match(85)
                    : FingerprintVerifyResult.NoMatch();
            }, cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            return FingerprintVerifyResult.Error(ex.Message);
        }
    }

    public override async Task<FingerprintMatchResult?> IdentifyAsync(byte[] sample, IReadOnlyDictionary<string, byte[]> templates, CancellationToken cancellationToken = default)
    {
        if (_sessionHandle == nint.Zero)
        {
            return null;
        }

        try
        {
            // WBF identify returns the matched identity
            var result = await Task.Run(() =>
            {
                var hr = WinBioIdentify(
                    _sessionHandle,
                    out var unitId,
                    out var identity,
                    out var subFactor,
                    out var rejectDetail);

                return hr == S_OK ? identity : nint.Zero;
            }, cancellationToken);

            if (result != nint.Zero)
            {
                // WBF identified someone, but we need to map to our template system
                // This would require Windows SID to user mapping
                return null; // Caller should use VerifyAsync for template-based matching
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public override Task<FingerprintMatchResult?> MatchAsync(byte[] sample, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Use IdentifyAsync with a template dictionary for WBF matching.");
    }

    public override async Task<FingerprintEnrollResult> EnrollAsync(string username, FingerPosition finger, CancellationToken cancellationToken = default)
    {
        if (_sessionHandle == nint.Zero)
        {
            return FingerprintEnrollResult.Failed(FingerprintEnrollStatus.DeviceError, "WBF session not initialized.");
        }

        try
        {
            OnDeviceStatusChanged(FingerprintDeviceStatus.Capturing);

            // WBF enrollment requires Windows Hello setup
            // This is a simplified implementation that captures for application storage
            var captureResult = await CaptureAsync(cancellationToken);

            if (captureResult.Success && captureResult.SampleData != null)
            {
                return FingerprintEnrollResult.Successful(finger, captureResult.SampleData);
            }

            return FingerprintEnrollResult.Failed(
                FingerprintEnrollStatus.PoorQuality,
                captureResult.ErrorMessage ?? "Failed to capture fingerprint.");
        }
        catch (OperationCanceledException)
        {
            return FingerprintEnrollResult.Failed(FingerprintEnrollStatus.Cancelled);
        }
        catch (Exception ex)
        {
            OnDeviceStatusChanged(FingerprintDeviceStatus.Error);
            return FingerprintEnrollResult.Failed(FingerprintEnrollStatus.DeviceError, ex.Message);
        }
        finally
        {
            OnDeviceStatusChanged(FingerprintDeviceStatus.Ready);
        }
    }

    public override async Task<bool> VerifyUserAsync(string username, CancellationToken cancellationToken = default)
    {
        if (_sessionHandle == nint.Zero) return false;

        try
        {
            var result = await Task.Run(() =>
            {
                var hr = WinBioVerify(
                    _sessionHandle,
                    out var identity,
                    out var subFactor,
                    out var rejectDetail);

                return hr == S_OK;
            }, cancellationToken);

            return result;
        }
        catch
        {
            return false;
        }
    }

    public override Task<IReadOnlyList<FingerPosition>> ListEnrolledFingersAsync(string username, CancellationToken cancellationToken = default)
    {
        // WBF doesn't expose per-finger enrollment info easily
        // Would need to enumerate through Windows Hello data
        return Task.FromResult<IReadOnlyList<FingerPosition>>([]);
    }

    public override Task<bool> DeleteEnrolledFingersAsync(string username, CancellationToken cancellationToken = default)
    {
        // WBF enrollment deletion requires Windows Hello settings
        return Task.FromResult(false);
    }

    public override int GetSampleQuality(byte[] sample)
    {
        // WBF doesn't expose quality metrics directly
        return sample.Length > 0 ? 75 : 0;
    }

    public override async Task<MultiCaptureEnrollmentResult> EnrollFingerMultiCaptureAsync(
        int requiredSamples = 4,
        Action<int, int, string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (_sessionHandle == nint.Zero)
        {
            return MultiCaptureEnrollmentResult.Failed("WBF session not initialized.");
        }

        var samples = new List<byte[]>();

        try
        {
            for (int i = 0; i < requiredSamples && !cancellationToken.IsCancellationRequested; i++)
            {
                progress?.Invoke(i + 1, requiredSamples, $"Place finger on scanner ({i + 1}/{requiredSamples})...");

                var captureResult = await CaptureAsync(cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                {
                    return MultiCaptureEnrollmentResult.Cancelled(samples.Count);
                }

                if (!captureResult.Success || captureResult.SampleData == null)
                {
                    progress?.Invoke(i + 1, requiredSamples, captureResult.Message ?? "Capture failed. Try again...");
                    i--; // Retry this sample
                    continue;
                }

                samples.Add(captureResult.SampleData);

                var remaining = requiredSamples - samples.Count;
                if (remaining > 0)
                {
                    progress?.Invoke(samples.Count, requiredSamples, $"Good! {remaining} more scan{(remaining == 1 ? "" : "s")} needed...");
                }
            }

            if (samples.Count < requiredSamples)
            {
                return MultiCaptureEnrollmentResult.Failed($"Only {samples.Count} samples collected.", samples.Count);
            }

            // WBF doesn't have enrollment like DPFP, just return the last sample as template
            progress?.Invoke(requiredSamples, requiredSamples, "Enrollment complete!");
            return MultiCaptureEnrollmentResult.Successful(samples[^1], null, samples.Count);
        }
        catch (OperationCanceledException)
        {
            return MultiCaptureEnrollmentResult.Cancelled(samples.Count);
        }
        catch (Exception ex)
        {
            return MultiCaptureEnrollmentResult.Failed($"Enrollment error: {ex.Message}", samples.Count);
        }
    }

    private static nint GetFirstUnitId(nint unitSchemaArray, nint unitCount)
    {
        if (unitCount <= 0 || unitSchemaArray == nint.Zero)
            return nint.Zero;

        // First unit ID is at the beginning of the array
        return Marshal.ReadIntPtr(unitSchemaArray);
    }

    private static FingerprintCaptureResult MapRejectDetailToResult(uint rejectDetail)
    {
        // WinBio reject detail codes
        return rejectDetail switch
        {
            0 => FingerprintCaptureResult.Failed(FingerprintCaptureStatus.Unknown),
            1 => FingerprintCaptureResult.Failed(FingerprintCaptureStatus.TooFast, "Finger moved too fast."),
            2 => FingerprintCaptureResult.Failed(FingerprintCaptureStatus.TooSlow, "Finger moved too slow."),
            3 => FingerprintCaptureResult.Failed(FingerprintCaptureStatus.PoorQuality, "Poor image quality."),
            4 => FingerprintCaptureResult.Failed(FingerprintCaptureStatus.TooDry, "Finger too dry."),
            5 => FingerprintCaptureResult.Failed(FingerprintCaptureStatus.TooWet, "Finger too wet."),
            6 => FingerprintCaptureResult.Failed(FingerprintCaptureStatus.Partial, "Partial fingerprint captured."),
            _ => FingerprintCaptureResult.Failed(FingerprintCaptureStatus.DeviceError, $"WinBio error: {rejectDetail}")
        };
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            lock (_syncLock)
            {
                if (_sessionHandle != nint.Zero)
                {
                    WinBioCloseSession(_sessionHandle);
                    _sessionHandle = nint.Zero;
                }
            }
        }
        base.Dispose(disposing);
    }

    #region WinBio P/Invoke

    [DllImport("winbio.dll", SetLastError = true)]
    private static extern int WinBioOpenSession(
        uint Factor,
        uint PoolType,
        uint Flags,
        nint UnitArray,
        nint UnitCount,
        nint DatabaseId,
        out nint SessionHandle);

    [DllImport("winbio.dll", SetLastError = true)]
    private static extern int WinBioCloseSession(nint SessionHandle);

    [DllImport("winbio.dll", SetLastError = true)]
    private static extern int WinBioEnumBiometricUnits(
        uint Factor,
        out nint UnitSchemaArray,
        out nint UnitCount);

    [DllImport("winbio.dll", SetLastError = true)]
    private static extern int WinBioCaptureSample(
        nint SessionHandle,
        uint Purpose,
        out nint UnitId,
        out nint Sample,
        out nint SampleSize,
        out uint RejectDetail);

    [DllImport("winbio.dll", SetLastError = true)]
    private static extern int WinBioVerify(
        nint SessionHandle,
        out nint Identity,
        out byte SubFactor,
        out uint RejectDetail);

    [DllImport("winbio.dll", SetLastError = true)]
    private static extern int WinBioIdentify(
        nint SessionHandle,
        out nint UnitId,
        out nint Identity,
        out byte SubFactor,
        out uint RejectDetail);

    [DllImport("winbio.dll", SetLastError = true)]
    private static extern int WinBioCancel(nint SessionHandle);

    [DllImport("winbio.dll", SetLastError = true)]
    private static extern int WinBioFree(nint Address);

    #endregion
}
