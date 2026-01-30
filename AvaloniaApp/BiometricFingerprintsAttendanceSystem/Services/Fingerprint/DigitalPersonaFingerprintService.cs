// DigitalPersona SDK integration for Windows
// Requires: DPFPDevNET.dll, DPFPEngNET.dll, DPFPShrNET.dll, DPFPVerNET.dll
// Enable by setting IncludeFingerprintSdks=true and DigitalPersonaSdkPath in build

#if DIGITALPERSONA_SDK
using DPFP;
using DPFP.Capture;
using DPFP.Processing;
using DPFP.Verification;
#endif

namespace BiometricFingerprintsAttendanceSystem.Services.Fingerprint;

/// <summary>
/// DigitalPersona U.are.U 4500 fingerprint service for Windows.
/// Uses the DigitalPersona One Touch SDK.
/// </summary>
public sealed class DigitalPersonaFingerprintService : FingerprintServiceBase
#if DIGITALPERSONA_SDK
    , DPFP.Capture.EventHandler
#endif
{
    private FingerprintDeviceStatus _deviceStatus = FingerprintDeviceStatus.Unknown;
    private FingerprintDeviceInfo? _deviceInfo;
    private bool _isCapturing;
    private TaskCompletionSource<FingerprintCaptureResult>? _captureCompletionSource;

#if DIGITALPERSONA_SDK
    private Capture? _capture;
    private Enrollment? _enrollment;
    private readonly object _syncLock = new();
#endif

    public override bool IsDeviceAvailable => DeviceStatus is FingerprintDeviceStatus.Ready or FingerprintDeviceStatus.Connected;

    public override FingerprintDeviceStatus DeviceStatus
    {
        get => _deviceStatus;
        protected set => _deviceStatus = value;
    }

    public override FingerprintDeviceInfo? DeviceInfo => _deviceInfo;

    public override Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
    {
#if DIGITALPERSONA_SDK
        try
        {
            _capture = new Capture();
            _enrollment = new Enrollment();

            _capture.EventHandler = this;

            _deviceInfo = new FingerprintDeviceInfo
            {
                Vendor = "DigitalPersona",
                ProductName = "U.are.U 4500",
                VendorId = "05BA",
                ProductId = "000A",
                Driver = "DigitalPersona One Touch SDK",
                DeviceType = FingerprintDeviceType.DigitalPersona4500,
                SupportsEnrollment = true,
                SupportsVerification = true,
                SupportsIdentification = true,
                ImageWidth = 355,
                ImageHeight = 390,
                ImageDpi = 500
            };

            OnDeviceStatusChanged(FingerprintDeviceStatus.Ready);
            return Task.FromResult(true);
        }
        catch (Exception)
        {
            OnDeviceStatusChanged(FingerprintDeviceStatus.Error);
            return Task.FromResult(false);
        }
#else
        OnDeviceStatusChanged(FingerprintDeviceStatus.Error);
        return Task.FromResult(false);
#endif
    }

    public override Task StartCaptureAsync(CancellationToken cancellationToken = default)
    {
#if DIGITALPERSONA_SDK
        if (_isCapturing || _capture is null) return Task.CompletedTask;

        try
        {
            _capture.StartCapture();
            _isCapturing = true;
            OnDeviceStatusChanged(FingerprintDeviceStatus.WaitingForFinger);
        }
        catch (Exception)
        {
            OnDeviceStatusChanged(FingerprintDeviceStatus.Error);
        }
#endif
        return Task.CompletedTask;
    }

    public override Task StopCaptureAsync(CancellationToken cancellationToken = default)
    {
#if DIGITALPERSONA_SDK
        if (!_isCapturing || _capture is null) return Task.CompletedTask;

        try
        {
            _capture.StopCapture();
            _isCapturing = false;
            OnDeviceStatusChanged(FingerprintDeviceStatus.Ready);
        }
        catch
        {
            // Ignore errors during stop
        }
#endif
        return Task.CompletedTask;
    }

    public override async Task<FingerprintCaptureResult> CaptureAsync(CancellationToken cancellationToken = default)
    {
#if DIGITALPERSONA_SDK
        if (_capture is null)
        {
            return FingerprintCaptureResult.Failed(FingerprintCaptureStatus.DeviceError, "Device not initialized.");
        }

        _captureCompletionSource = new TaskCompletionSource<FingerprintCaptureResult>();

        using var registration = cancellationToken.Register(() =>
        {
            _captureCompletionSource.TrySetResult(
                FingerprintCaptureResult.Failed(FingerprintCaptureStatus.Cancelled));
        });

        try
        {
            if (!_isCapturing)
            {
                _capture.StartCapture();
                _isCapturing = true;
            }

            OnDeviceStatusChanged(FingerprintDeviceStatus.WaitingForFinger);

            // Wait for capture with timeout
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            var completedTask = await Task.WhenAny(_captureCompletionSource.Task, timeoutTask);

            if (completedTask == timeoutTask)
            {
                return FingerprintCaptureResult.Failed(FingerprintCaptureStatus.Timeout);
            }

            return await _captureCompletionSource.Task;
        }
        catch (OperationCanceledException)
        {
            return FingerprintCaptureResult.Failed(FingerprintCaptureStatus.Cancelled);
        }
        catch (Exception ex)
        {
            return FingerprintCaptureResult.Failed(FingerprintCaptureStatus.DeviceError, ex.Message);
        }
        finally
        {
            OnDeviceStatusChanged(FingerprintDeviceStatus.Ready);
        }
#else
        await Task.CompletedTask;
        return FingerprintCaptureResult.Failed(FingerprintCaptureStatus.DeviceError,
            "DigitalPersona SDK not available. Rebuild with IncludeFingerprintSdks=true.");
#endif
    }

    public override Task<byte[]?> CreateTemplateAsync(byte[] sampleData, CancellationToken cancellationToken = default)
    {
#if DIGITALPERSONA_SDK
        try
        {
            using var stream = new MemoryStream(sampleData);
            var sample = new Sample(stream);
            var featureExtractor = new FeatureExtraction();
            var features = new FeatureSet();
            var feedback = CaptureFeedback.None;

            featureExtractor.CreateFeatureSet(sample, DataPurpose.Enrollment, ref feedback, ref features);

            if (feedback == CaptureFeedback.Good)
            {
                using var outputStream = new MemoryStream();
                features.Serialize(outputStream);
                return Task.FromResult<byte[]?>(outputStream.ToArray());
            }

            return Task.FromResult<byte[]?>(null);
        }
        catch
        {
            return Task.FromResult<byte[]?>(null);
        }
#else
        return Task.FromResult<byte[]?>(null);
#endif
    }

    public override Task<FingerprintVerifyResult> VerifyAsync(byte[] sample, byte[] template, CancellationToken cancellationToken = default)
    {
#if DIGITALPERSONA_SDK
        try
        {
            using var sampleStream = new MemoryStream(sample);
            using var templateStream = new MemoryStream(template);

            var sampleFeatures = new FeatureSet(sampleStream);
            var templateObj = new Template(templateStream);

            var result = Verification.Verify(sampleFeatures, templateObj);

            if (result.Verified)
            {
                // Convert FAR to score (higher score = better match)
                var score = Math.Min(100, (int)(100 - Math.Log10(result.FARAchieved + 1) * 10));
                return Task.FromResult(FingerprintVerifyResult.Match(score, result.FARAchieved));
            }

            return Task.FromResult(FingerprintVerifyResult.NoMatch());
        }
        catch (Exception ex)
        {
            return Task.FromResult(FingerprintVerifyResult.Error(ex.Message));
        }
#else
        return Task.FromResult(FingerprintVerifyResult.Error("DigitalPersona SDK not available."));
#endif
    }

    public override Task<FingerprintMatchResult?> IdentifyAsync(byte[] sample, IReadOnlyDictionary<string, byte[]> templates, CancellationToken cancellationToken = default)
    {
#if DIGITALPERSONA_SDK
        try
        {
            using var sampleStream = new MemoryStream(sample);
            var sampleFeatures = new FeatureSet(sampleStream);

            foreach (var (matricNo, templateData) in templates)
            {
                using var templateStream = new MemoryStream(templateData);
                var template = new Template(templateStream);
                var result = Verification.Verify(sampleFeatures, template);

                if (result.Verified)
                {
                    return Task.FromResult<FingerprintMatchResult?>(new FingerprintMatchResult
                    {
                        MatricNo = matricNo,
                        FalseAcceptRate = (int)(result.FARAchieved * 1000000)
                    });
                }
            }

            return Task.FromResult<FingerprintMatchResult?>(null);
        }
        catch
        {
            return Task.FromResult<FingerprintMatchResult?>(null);
        }
#else
        return Task.FromResult<FingerprintMatchResult?>(null);
#endif
    }

    public override Task<FingerprintMatchResult?> MatchAsync(byte[] sample, CancellationToken cancellationToken = default)
    {
        // Legacy method - needs external template database
        throw new NotSupportedException("Use IdentifyAsync with a template dictionary for matching.");
    }

    public override Task<FingerprintEnrollResult> EnrollAsync(string username, FingerPosition finger, CancellationToken cancellationToken = default)
    {
#if DIGITALPERSONA_SDK
        // DigitalPersona SDK doesn't use system enrollment like fprintd
        // Instead, return template data for application-managed storage
        return Task.FromResult(FingerprintEnrollResult.Failed(FingerprintEnrollStatus.Unknown,
            "Use CaptureAsync and CreateTemplateAsync for DigitalPersona enrollment."));
#else
        return Task.FromResult(FingerprintEnrollResult.Failed(FingerprintEnrollStatus.DeviceError,
            "DigitalPersona SDK not available."));
#endif
    }

    public override Task<bool> VerifyUserAsync(string username, CancellationToken cancellationToken = default)
    {
        // DigitalPersona doesn't use system-managed fingerprints
        throw new NotSupportedException("Use VerifyAsync with stored templates for DigitalPersona verification.");
    }

    public override Task<IReadOnlyList<FingerPosition>> ListEnrolledFingersAsync(string username, CancellationToken cancellationToken = default)
    {
        // DigitalPersona doesn't use system-managed fingerprints
        return Task.FromResult<IReadOnlyList<FingerPosition>>([]);
    }

    public override Task<bool> DeleteEnrolledFingersAsync(string username, CancellationToken cancellationToken = default)
    {
        // DigitalPersona doesn't use system-managed fingerprints
        return Task.FromResult(false);
    }

    public override int GetSampleQuality(byte[] sample)
    {
#if DIGITALPERSONA_SDK
        try
        {
            using var stream = new MemoryStream(sample);
            var sampleObj = new Sample(stream);
            // Quality is embedded in the sample metadata
            return 80; // Default quality for valid samples
        }
        catch
        {
            return 0;
        }
#else
        return sample.Length > 0 ? 50 : 0;
#endif
    }

#if DIGITALPERSONA_SDK
    // DPFP.Capture.EventHandler implementation

    public void OnComplete(object capture, string readerSerialNumber, Sample sample)
    {
        OnDeviceStatusChanged(FingerprintDeviceStatus.Processing);

        try
        {
            var featureExtractor = new FeatureExtraction();
            var features = new FeatureSet();
            var feedback = CaptureFeedback.None;

            featureExtractor.CreateFeatureSet(sample, DataPurpose.Verification, ref feedback, ref features);

            // Serialize sample and features to byte arrays
            byte[] sampleBytes;
            byte[]? featureBytes = null;

            using (var sampleStream = new MemoryStream())
            {
                sample.Serialize(sampleStream);
                sampleBytes = sampleStream.ToArray();
            }

            if (feedback == CaptureFeedback.Good)
            {
                using var featureStream = new MemoryStream();
                features.Serialize(featureStream);
                featureBytes = featureStream.ToArray();
            }

            var captureResult = feedback switch
            {
                CaptureFeedback.Good => FingerprintCaptureResult.Successful(sampleBytes, featureBytes, 85),
                CaptureFeedback.None => FingerprintCaptureResult.Failed(FingerprintCaptureStatus.NoFinger),
                CaptureFeedback.TooLight => FingerprintCaptureResult.Failed(FingerprintCaptureStatus.PoorQuality, "Image too light. Press harder."),
                CaptureFeedback.TooNoisy => FingerprintCaptureResult.Failed(FingerprintCaptureStatus.PoorQuality, "Image too noisy. Clean the sensor."),
                CaptureFeedback.LowContrast => FingerprintCaptureResult.Failed(FingerprintCaptureStatus.PoorQuality, "Low contrast. Adjust finger position."),
                CaptureFeedback.NotEnoughFeatures => FingerprintCaptureResult.Failed(FingerprintCaptureStatus.PoorQuality, "Not enough features. Try again."),
                CaptureFeedback.TooSmall => FingerprintCaptureResult.Failed(FingerprintCaptureStatus.Partial, "Image too small. Center your finger."),
                CaptureFeedback.TooShort => FingerprintCaptureResult.Failed(FingerprintCaptureStatus.TooFast, "Swipe too short."),
                CaptureFeedback.TooSlow => FingerprintCaptureResult.Failed(FingerprintCaptureStatus.TooSlow, "Swipe too slow."),
                CaptureFeedback.TooFast => FingerprintCaptureResult.Failed(FingerprintCaptureStatus.TooFast, "Swipe too fast."),
                CaptureFeedback.TooSkewed => FingerprintCaptureResult.Failed(FingerprintCaptureStatus.PoorQuality, "Finger too skewed."),
                _ => FingerprintCaptureResult.Failed(FingerprintCaptureStatus.Unknown)
            };

            _captureCompletionSource?.TrySetResult(captureResult);

            OnFingerprintCaptured(new FingerprintCaptureEventArgs
            {
                Success = captureResult.Success,
                SampleData = sampleBytes,
                TemplateData = captureResult.TemplateData,
                Quality = captureResult.Quality,
                Status = captureResult.Status
            });
        }
        catch (Exception ex)
        {
            _captureCompletionSource?.TrySetResult(
                FingerprintCaptureResult.Failed(FingerprintCaptureStatus.DeviceError, ex.Message));
        }
        finally
        {
            OnDeviceStatusChanged(FingerprintDeviceStatus.Ready);
        }
    }

    public void OnFingerGone(object capture, string readerSerialNumber)
    {
        // Finger removed from sensor
    }

    public void OnFingerTouch(object capture, string readerSerialNumber)
    {
        OnDeviceStatusChanged(FingerprintDeviceStatus.Capturing);
    }

    public void OnReaderConnect(object capture, string readerSerialNumber)
    {
        OnDeviceStatusChanged(FingerprintDeviceStatus.Ready);
    }

    public void OnReaderDisconnect(object capture, string readerSerialNumber)
    {
        OnDeviceStatusChanged(FingerprintDeviceStatus.Disconnected);
    }

    public void OnSampleQuality(object capture, string readerSerialNumber, CaptureFeedback feedback)
    {
        // Quality feedback during capture
    }
#endif

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
#if DIGITALPERSONA_SDK
            try
            {
                _capture?.StopCapture();
                _capture?.Dispose();
            }
            catch
            {
                // Ignore dispose errors
            }
#endif
        }
        base.Dispose(disposing);
    }
}
