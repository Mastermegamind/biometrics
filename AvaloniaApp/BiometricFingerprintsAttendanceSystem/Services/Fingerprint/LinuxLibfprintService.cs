using System.Runtime.InteropServices;
using BiometricFingerprintsAttendanceSystem.Services.Fingerprint.Libfprint;
using Microsoft.Extensions.Logging;

namespace BiometricFingerprintsAttendanceSystem.Services.Fingerprint;

/// <summary>
/// Linux fingerprint service using direct libfprint2 bindings.
/// Provides raw template access for database storage and API submission.
/// </summary>
public sealed class LinuxLibfprintService : FingerprintServiceBase
{
    private FpContextHandle? _context;
    private FpDeviceHandle? _device;
    private FingerprintDeviceStatus _deviceStatus = FingerprintDeviceStatus.Unknown;
    private FingerprintDeviceInfo? _deviceInfo;
    private CancellationTokenSource? _captureCts;
    private bool _isCapturing;
    private readonly SemaphoreSlim _deviceLock = new(1, 1);
    private readonly ILogger<LinuxLibfprintService>? _logger;
    private bool _disposed;

    public LinuxLibfprintService(ILogger<LinuxLibfprintService>? logger = null)
    {
        _logger = logger;
    }

    public override bool IsDeviceAvailable =>
        DeviceStatus is FingerprintDeviceStatus.Ready or FingerprintDeviceStatus.Connected;

    public override FingerprintDeviceStatus DeviceStatus
    {
        get => _deviceStatus;
        protected set => _deviceStatus = value;
    }

    public override FingerprintDeviceInfo? DeviceInfo => _deviceInfo;

    public override async Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                _logger?.LogInformation("Initializing libfprint2 fingerprint service");

                // Create context
                var contextPtr = LibfprintNative.fp_context_new();
                if (contextPtr == IntPtr.Zero)
                {
                    _logger?.LogError("Failed to create libfprint context");
                    OnDeviceStatusChanged(FingerprintDeviceStatus.Error);
                    return false;
                }

                _context = new FpContextHandle(contextPtr, ownsHandle: true);

                // Enumerate devices
                LibfprintNative.fp_context_enumerate(_context.DangerousGetHandle());

                var devicesPtr = LibfprintNative.fp_context_get_devices(_context.DangerousGetHandle());
                if (devicesPtr == IntPtr.Zero)
                {
                    _logger?.LogWarning("No fingerprint devices found (null device list)");
                    OnDeviceStatusChanged(FingerprintDeviceStatus.Disconnected);
                    return false;
                }

                var deviceCount = LibfprintNative.GetPtrArrayLength(devicesPtr);
                _logger?.LogInformation("Found {DeviceCount} fingerprint device(s)", deviceCount);

                if (deviceCount == 0)
                {
                    _logger?.LogWarning("No fingerprint devices found");
                    OnDeviceStatusChanged(FingerprintDeviceStatus.Disconnected);
                    return false;
                }

                // Get first device
                var devicePtr = LibfprintNative.GetPtrArrayIndex(devicesPtr, 0);
                if (devicePtr == IntPtr.Zero)
                {
                    _logger?.LogError("Failed to get device pointer");
                    OnDeviceStatusChanged(FingerprintDeviceStatus.Disconnected);
                    return false;
                }

                _device = new FpDeviceHandle(devicePtr, _context);

                // Open device
                if (!LibfprintNative.fp_device_open_sync(devicePtr, IntPtr.Zero, out var error))
                {
                    if (error != IntPtr.Zero)
                    {
                        var ex = LibfprintException.FromGError(error);
                        _logger?.LogError("Failed to open device: {Message}", ex.Message);
                    }
                    OnDeviceStatusChanged(FingerprintDeviceStatus.Error);
                    return false;
                }

                _device.MarkOpen();

                // Populate device info
                _deviceInfo = CreateDeviceInfo(devicePtr);
                _logger?.LogInformation("Opened device: {DeviceName} ({Driver})",
                    _deviceInfo.ProductName, _deviceInfo.Driver);

                OnDeviceStatusChanged(FingerprintDeviceStatus.Ready);
                return true;
            }
            catch (DllNotFoundException ex)
            {
                _logger?.LogError(ex, "libfprint2 library not found. Install with: sudo apt install libfprint-2-2");
                OnDeviceStatusChanged(FingerprintDeviceStatus.Error);
                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to initialize libfprint service");
                OnDeviceStatusChanged(FingerprintDeviceStatus.Error);
                return false;
            }
        }, cancellationToken);
    }

    private static FingerprintDeviceInfo CreateDeviceInfo(IntPtr devicePtr)
    {
        var namePtr = LibfprintNative.fp_device_get_name(devicePtr);
        var driverPtr = LibfprintNative.fp_device_get_driver(devicePtr);
        var deviceIdPtr = LibfprintNative.fp_device_get_device_id(devicePtr);

        var name = Marshal.PtrToStringUTF8(namePtr) ?? "Unknown";
        var driver = Marshal.PtrToStringUTF8(driverPtr) ?? "libfprint";
        var deviceId = Marshal.PtrToStringUTF8(deviceIdPtr) ?? "";

        var scanType = LibfprintNative.fp_device_get_scan_type(devicePtr);
        var supportsCapture = LibfprintNative.fp_device_supports_capture(devicePtr);
        var supportsIdentify = LibfprintNative.fp_device_supports_identify(devicePtr);
        var enrollStages = LibfprintNative.fp_device_get_nr_enroll_stages(devicePtr);

        return new FingerprintDeviceInfo
        {
            Vendor = ExtractVendorFromName(name),
            ProductName = name,
            Driver = $"libfprint2/{driver}",
            DeviceType = FingerprintDeviceType.LibfprintDirect,
            SupportsEnrollment = true,
            SupportsVerification = true,
            SupportsIdentification = supportsIdentify
        };
    }

    private static string ExtractVendorFromName(string name)
    {
        if (name.Contains("DigitalPersona", StringComparison.OrdinalIgnoreCase)) return "DigitalPersona";
        if (name.Contains("Validity", StringComparison.OrdinalIgnoreCase)) return "Validity Sensors";
        if (name.Contains("Goodix", StringComparison.OrdinalIgnoreCase)) return "Goodix";
        if (name.Contains("Synaptics", StringComparison.OrdinalIgnoreCase)) return "Synaptics";
        if (name.Contains("Elan", StringComparison.OrdinalIgnoreCase)) return "Elan";
        if (name.Contains("AuthenTec", StringComparison.OrdinalIgnoreCase)) return "AuthenTec";
        if (name.Contains("Upek", StringComparison.OrdinalIgnoreCase)) return "Upek";
        return "Unknown";
    }

    public override async Task<FingerprintCaptureResult> CaptureAsync(CancellationToken cancellationToken = default)
    {
        if (_device is null || !_device.IsOpen)
        {
            return FingerprintCaptureResult.Failed(FingerprintCaptureStatus.DeviceError,
                "Device not initialized. Call InitializeAsync first.");
        }

        await _deviceLock.WaitAsync(cancellationToken);
        try
        {
            return await Task.Run(() =>
            {
                var devicePtr = _device.DangerousGetHandle();

                // Check if device supports direct capture
                if (LibfprintNative.fp_device_supports_capture(devicePtr))
                {
                    return CaptureViaImage(devicePtr);
                }
                else
                {
                    // Fall back to enrollment-based capture
                    return CaptureViaEnroll(devicePtr);
                }
            }, cancellationToken);
        }
        finally
        {
            _deviceLock.Release();
        }
    }

    private FingerprintCaptureResult CaptureViaImage(IntPtr devicePtr)
    {
        OnDeviceStatusChanged(FingerprintDeviceStatus.WaitingForFinger);
        _logger?.LogInformation("Starting fingerprint capture (image mode)");

        var imagePtr = LibfprintNative.fp_device_capture_sync(
            devicePtr,
            waitForFinger: true,
            IntPtr.Zero,
            out var error);

        if (imagePtr == IntPtr.Zero)
        {
            OnDeviceStatusChanged(FingerprintDeviceStatus.Ready);
            if (error != IntPtr.Zero)
            {
                var ex = LibfprintException.FromGError(error);
                _logger?.LogWarning("Capture failed: {Message}", ex.Message);
                return FingerprintCaptureResult.Failed(ex.ToCaptureStatus(), ex.Message);
            }
            return FingerprintCaptureResult.Failed(FingerprintCaptureStatus.NoFinger);
        }

        OnDeviceStatusChanged(FingerprintDeviceStatus.Processing);

        try
        {
            // Get raw image data
            var dataPtr = LibfprintNative.fp_image_get_data(imagePtr, out var length);
            byte[] imageData;
            if (dataPtr != IntPtr.Zero && length > 0)
            {
                imageData = new byte[length];
                Marshal.Copy(dataPtr, imageData, 0, (int)length);
            }
            else
            {
                imageData = [];
            }

            // Get image dimensions for quality estimation
            var width = LibfprintNative.fp_image_get_width(imagePtr);
            var height = LibfprintNative.fp_image_get_height(imagePtr);
            var quality = EstimateQuality(imageData, width, height);

            _logger?.LogInformation("Captured image: {Width}x{Height}, quality: {Quality}", width, height, quality);

            // For image-based capture, we need to do enrollment to get a template
            // Free the image first
            LibfprintNative.fp_image_unref(imagePtr);
            imagePtr = IntPtr.Zero;

            // Now do enrollment to get template
            var enrollResult = CaptureViaEnroll(devicePtr);
            if (enrollResult.Success && enrollResult.TemplateData != null)
            {
                return FingerprintCaptureResult.Successful(
                    sampleData: enrollResult.TemplateData,
                    templateData: enrollResult.TemplateData,
                    quality: quality);
            }

            // Return image data even if template creation failed
            return FingerprintCaptureResult.Successful(
                sampleData: imageData,
                templateData: null,
                quality: quality);
        }
        finally
        {
            if (imagePtr != IntPtr.Zero)
            {
                LibfprintNative.fp_image_unref(imagePtr);
            }
            OnDeviceStatusChanged(FingerprintDeviceStatus.Ready);
        }
    }

    private FingerprintCaptureResult CaptureViaEnroll(IntPtr devicePtr)
    {
        OnDeviceStatusChanged(FingerprintDeviceStatus.WaitingForFinger);
        _logger?.LogInformation("Starting fingerprint capture (enrollment mode)");

        // Create a template print for enrollment
        var templatePrint = LibfprintNative.fp_print_new(devicePtr);
        if (templatePrint == IntPtr.Zero)
        {
            OnDeviceStatusChanged(FingerprintDeviceStatus.Ready);
            return FingerprintCaptureResult.Failed(FingerprintCaptureStatus.DeviceError,
                "Failed to create print template.");
        }

        try
        {
            // Set finger position
            LibfprintNative.fp_print_set_finger(templatePrint, FpFinger.RightIndexFinger);

            var printPtr = LibfprintNative.fp_device_enroll_sync(
                devicePtr,
                templatePrint,
                IntPtr.Zero,
                IntPtr.Zero,  // No progress callback for now
                IntPtr.Zero,
                out var error);

            OnDeviceStatusChanged(FingerprintDeviceStatus.Processing);

            if (printPtr == IntPtr.Zero)
            {
                OnDeviceStatusChanged(FingerprintDeviceStatus.Ready);
                if (error != IntPtr.Zero)
                {
                    var ex = LibfprintException.FromGError(error);
                    _logger?.LogWarning("Enrollment capture failed: {Message}", ex.Message);
                    return FingerprintCaptureResult.Failed(ex.ToCaptureStatus(), ex.Message);
                }
                return FingerprintCaptureResult.Failed(FingerprintCaptureStatus.DeviceError,
                    "Enrollment returned null print.");
            }

            // Serialize the print to bytes
            var serializedPtr = LibfprintNative.fp_print_serialize(printPtr, out var length, out error);
            if (serializedPtr == IntPtr.Zero)
            {
                LibfprintNative.fp_print_unref(printPtr);
                OnDeviceStatusChanged(FingerprintDeviceStatus.Ready);
                if (error != IntPtr.Zero)
                {
                    var ex = LibfprintException.FromGError(error);
                    _logger?.LogWarning("Template serialization failed: {Message}", ex.Message);
                }
                return FingerprintCaptureResult.Failed(FingerprintCaptureStatus.DeviceError,
                    "Failed to serialize fingerprint template.");
            }

            try
            {
                var templateData = new byte[length];
                Marshal.Copy(serializedPtr, templateData, 0, (int)length);

                _logger?.LogInformation("Captured template: {Length} bytes", length);

                OnDeviceStatusChanged(FingerprintDeviceStatus.Ready);

                return FingerprintCaptureResult.Successful(
                    sampleData: templateData,
                    templateData: templateData,
                    quality: 80);
            }
            finally
            {
                LibfprintNative.g_free(serializedPtr);
                LibfprintNative.fp_print_unref(printPtr);
            }
        }
        finally
        {
            LibfprintNative.fp_print_unref(templatePrint);
        }
    }

    public override Task<byte[]?> CreateTemplateAsync(byte[] sampleData, CancellationToken cancellationToken = default)
    {
        // libfprint creates templates during enrollment.
        // If sampleData is already a serialized template (from CaptureAsync), return it.
        if (IsSerializedTemplate(sampleData))
        {
            return Task.FromResult<byte[]?>(sampleData);
        }

        // Cannot create template from raw image data alone
        _logger?.LogWarning("Cannot create template from raw data - use CaptureAsync instead");
        return Task.FromResult<byte[]?>(null);
    }

    private static bool IsSerializedTemplate(byte[] data)
    {
        // Serialized libfprint templates are typically larger and have specific structure
        // This is a heuristic check
        return data.Length > 100;
    }

    public override async Task<FingerprintVerifyResult> VerifyAsync(
        byte[] sample,
        byte[] template,
        CancellationToken cancellationToken = default)
    {
        if (_device is null || !_device.IsOpen)
        {
            return FingerprintVerifyResult.Error("Device not initialized.");
        }

        await _deviceLock.WaitAsync(cancellationToken);
        try
        {
            return await Task.Run(() =>
            {
                // Deserialize the stored template
                var templatePtr = DeserializeTemplate(template);
                if (templatePtr == IntPtr.Zero)
                {
                    return FingerprintVerifyResult.Error("Invalid template data.");
                }

                try
                {
                    var devicePtr = _device.DangerousGetHandle();

                    OnDeviceStatusChanged(FingerprintDeviceStatus.WaitingForFinger);
                    _logger?.LogInformation("Starting fingerprint verification");

                    var result = LibfprintNative.fp_device_verify_sync(
                        devicePtr,
                        templatePtr,
                        IntPtr.Zero,
                        out var matchedPrintPtr,
                        IntPtr.Zero,
                        IntPtr.Zero,
                        out var error);

                    OnDeviceStatusChanged(FingerprintDeviceStatus.Ready);

                    if (error != IntPtr.Zero)
                    {
                        var ex = LibfprintException.FromGError(error);
                        _logger?.LogWarning("Verification error: {Message}", ex.Message);
                        return FingerprintVerifyResult.Error(ex.Message);
                    }

                    _logger?.LogInformation("Verification result: {Match}", result ? "MATCH" : "NO MATCH");

                    return result
                        ? FingerprintVerifyResult.Match(85)
                        : FingerprintVerifyResult.NoMatch();
                }
                finally
                {
                    LibfprintNative.fp_print_unref(templatePtr);
                }
            }, cancellationToken);
        }
        finally
        {
            _deviceLock.Release();
        }
    }

    public override async Task<FingerprintMatchResult?> IdentifyAsync(
        byte[] sample,
        IReadOnlyDictionary<string, byte[]> templates,
        CancellationToken cancellationToken = default)
    {
        if (_device is null || !_device.IsOpen)
        {
            return null;
        }

        // Check if device supports native identify
        if (LibfprintNative.fp_device_supports_identify(_device.DangerousGetHandle()))
        {
            var result = await IdentifyNative(templates, cancellationToken);
            if (result != null) return result;
        }

        // Fall back to sequential verification
        return await IdentifyViaSequentialVerify(sample, templates, cancellationToken);
    }

    private async Task<FingerprintMatchResult?> IdentifyNative(
        IReadOnlyDictionary<string, byte[]> templates,
        CancellationToken cancellationToken)
    {
        await _deviceLock.WaitAsync(cancellationToken);
        try
        {
            return await Task.Run(() =>
            {
                var devicePtr = _device!.DangerousGetHandle();

                // Build array of prints
                var printPtrs = new List<IntPtr>();
                var matricNoMap = new Dictionary<IntPtr, string>();

                try
                {
                    foreach (var (matricNo, templateData) in templates)
                    {
                        var printPtr = DeserializeTemplate(templateData);
                        if (printPtr != IntPtr.Zero)
                        {
                            printPtrs.Add(printPtr);
                            matricNoMap[printPtr] = matricNo;
                        }
                    }

                    if (printPtrs.Count == 0)
                    {
                        _logger?.LogWarning("No valid templates to identify against");
                        return null;
                    }

                    // Create GPtrArray
                    var printsArray = LibfprintNative.g_ptr_array_new();
                    foreach (var ptr in printPtrs)
                    {
                        LibfprintNative.g_ptr_array_add(printsArray, ptr);
                    }

                    try
                    {
                        OnDeviceStatusChanged(FingerprintDeviceStatus.WaitingForFinger);
                        _logger?.LogInformation("Starting fingerprint identification against {Count} templates", printPtrs.Count);

                        var result = LibfprintNative.fp_device_identify_sync(
                            devicePtr,
                            printsArray,
                            IntPtr.Zero,
                            out var matchedPrintPtr,
                            out var printPtr,
                            IntPtr.Zero,
                            IntPtr.Zero,
                            out var error);

                        OnDeviceStatusChanged(FingerprintDeviceStatus.Ready);

                        if (error != IntPtr.Zero)
                        {
                            var ex = LibfprintException.FromGError(error);
                            _logger?.LogWarning("Identification error: {Message}", ex.Message);
                            return null;
                        }

                        if (result && matchedPrintPtr != IntPtr.Zero)
                        {
                            if (matricNoMap.TryGetValue(matchedPrintPtr, out var matricNo))
                            {
                                _logger?.LogInformation("Identified: {MatricNo}", matricNo);
                                return new FingerprintMatchResult
                                {
                                    MatricNo = matricNo,
                                    FalseAcceptRate = 1 // Low FAR for identified match
                                };
                            }
                        }

                        _logger?.LogInformation("No match found in identification");
                        return null;
                    }
                    finally
                    {
                        LibfprintNative.g_ptr_array_free(printsArray, false);
                    }
                }
                finally
                {
                    foreach (var ptr in printPtrs)
                    {
                        LibfprintNative.fp_print_unref(ptr);
                    }
                }
            }, cancellationToken);
        }
        finally
        {
            _deviceLock.Release();
        }
    }

    private async Task<FingerprintMatchResult?> IdentifyViaSequentialVerify(
        byte[] sample,
        IReadOnlyDictionary<string, byte[]> templates,
        CancellationToken cancellationToken)
    {
        _logger?.LogInformation("Using sequential verification for {Count} templates", templates.Count);

        foreach (var (matricNo, template) in templates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await VerifyAsync(sample, template, cancellationToken);
            if (result.IsMatch)
            {
                _logger?.LogInformation("Sequential verify match found: {MatricNo}", matricNo);
                return new FingerprintMatchResult
                {
                    MatricNo = matricNo,
                    FalseAcceptRate = (int)(result.FalseAcceptRate * 1000000)
                };
            }
        }

        _logger?.LogInformation("No match found in sequential verification");
        return null;
    }

    private static IntPtr DeserializeTemplate(byte[] data)
    {
        var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        try
        {
            var result = LibfprintNative.fp_print_deserialize(
                handle.AddrOfPinnedObject(),
                (nuint)data.Length,
                out var error);

            if (error != IntPtr.Zero)
            {
                LibfprintNative.g_error_free(error);
                return IntPtr.Zero;
            }

            return result;
        }
        finally
        {
            handle.Free();
        }
    }

    private static int EstimateQuality(byte[] imageData, uint width, uint height)
    {
        if (imageData.Length == 0) return 0;

        // Simple quality estimation based on contrast
        byte min = byte.MaxValue;
        byte max = byte.MinValue;
        long sum = 0;

        foreach (var pixel in imageData)
        {
            if (pixel < min) min = pixel;
            if (pixel > max) max = pixel;
            sum += pixel;
        }

        var contrast = max - min;
        var mean = sum / imageData.Length;

        // Quality score based on contrast and mean brightness
        var quality = Math.Min(100, (contrast * 100 / 255 + 50) / 2);

        // Penalize if too dark or too bright
        if (mean < 50 || mean > 200)
        {
            quality = (int)(quality * 0.7);
        }

        return quality;
    }

    public override Task StartCaptureAsync(CancellationToken cancellationToken = default)
    {
        if (_isCapturing) return Task.CompletedTask;

        _captureCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _isCapturing = true;
        OnDeviceStatusChanged(FingerprintDeviceStatus.WaitingForFinger);

        _ = Task.Run(async () =>
        {
            while (!_captureCts.Token.IsCancellationRequested)
            {
                try
                {
                    var result = await CaptureAsync(_captureCts.Token);
                    if (result.Success)
                    {
                        OnFingerprintCaptured(new FingerprintCaptureEventArgs
                        {
                            Success = true,
                            SampleData = result.SampleData,
                            TemplateData = result.TemplateData,
                            Quality = result.Quality,
                            Status = result.Status
                        });
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                await Task.Delay(100, _captureCts.Token);
            }
        }, _captureCts.Token);

        return Task.CompletedTask;
    }

    public override Task StopCaptureAsync(CancellationToken cancellationToken = default)
    {
        _captureCts?.Cancel();
        _captureCts?.Dispose();
        _captureCts = null;
        _isCapturing = false;
        OnDeviceStatusChanged(FingerprintDeviceStatus.Ready);
        return Task.CompletedTask;
    }

    public override Task<FingerprintMatchResult?> MatchAsync(byte[] sample, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Use IdentifyAsync with a template dictionary for 1:N matching.");
    }

    public override Task<FingerprintEnrollResult> EnrollAsync(string username, FingerPosition finger, CancellationToken cancellationToken = default)
    {
        // libfprint direct mode uses application-managed templates
        return Task.FromResult(FingerprintEnrollResult.Failed(
            FingerprintEnrollStatus.Unknown,
            "Use CaptureAsync for application-managed enrollment with libfprint direct bindings."));
    }

    public override Task<bool> VerifyUserAsync(string username, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Use VerifyAsync with stored templates for libfprint direct verification.");
    }

    public override Task<IReadOnlyList<FingerPosition>> ListEnrolledFingersAsync(string username, CancellationToken cancellationToken = default)
    {
        // Application-managed templates, not system-managed
        return Task.FromResult<IReadOnlyList<FingerPosition>>([]);
    }

    public override Task<bool> DeleteEnrolledFingersAsync(string username, CancellationToken cancellationToken = default)
    {
        // Application-managed templates
        return Task.FromResult(false);
    }

    public override int GetSampleQuality(byte[] sample)
    {
        if (sample.Length < 100) return 0;
        return EstimateQuality(sample, 0, 0);
    }

    protected override void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _captureCts?.Cancel();
            _captureCts?.Dispose();
            _deviceLock.Dispose();

            _device?.Dispose();
            _context?.Dispose();
        }

        _disposed = true;
        base.Dispose(disposing);
    }
}
