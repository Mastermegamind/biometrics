using System.Diagnostics;
using System.Text.RegularExpressions;

namespace BiometricFingerprintsAttendanceSystem.Services.Fingerprint;

/// <summary>
/// Linux fingerprint service using fprintd (D-Bus fingerprint daemon).
/// Supports DigitalPersona and other libfprint-compatible devices.
/// </summary>
public sealed partial class LinuxFprintdService : FingerprintServiceBase
{
    private FingerprintDeviceStatus _deviceStatus = FingerprintDeviceStatus.Unknown;
    private FingerprintDeviceInfo? _deviceInfo;
    private CancellationTokenSource? _captureCts;
    private bool _isCapturing;
    private readonly string _defaultUsername;

    public LinuxFprintdService(string? defaultUsername = null)
    {
        _defaultUsername = defaultUsername ?? Environment.UserName;
    }

    public override bool IsDeviceAvailable => DeviceStatus is FingerprintDeviceStatus.Ready or FingerprintDeviceStatus.Connected;

    public override FingerprintDeviceStatus DeviceStatus
    {
        get => _deviceStatus;
        protected set => _deviceStatus = value;
    }

    public override FingerprintDeviceInfo? DeviceInfo => _deviceInfo;

    public override async Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if fprintd is available
            var (exitCode, output, _) = await RunCommandAsync("which", "fprintd-list", cancellationToken);
            if (exitCode != 0)
            {
                OnDeviceStatusChanged(FingerprintDeviceStatus.Error);
                return false;
            }

            // Try to detect devices using fprintd-list
            (exitCode, output, _) = await RunCommandAsync("fprintd-list", _defaultUsername, cancellationToken);

            if (output.Contains("No devices available", StringComparison.OrdinalIgnoreCase))
            {
                OnDeviceStatusChanged(FingerprintDeviceStatus.Disconnected);
                return false;
            }

            // Parse device info from fprintd output or lsusb
            await DetectDeviceInfoAsync(cancellationToken);

            OnDeviceStatusChanged(FingerprintDeviceStatus.Ready);
            return true;
        }
        catch (Exception)
        {
            OnDeviceStatusChanged(FingerprintDeviceStatus.Error);
            return false;
        }
    }

    private async Task DetectDeviceInfoAsync(CancellationToken cancellationToken)
    {
        // Try to get device info from lsusb
        var (exitCode, output, _) = await RunCommandAsync("lsusb", "", cancellationToken);
        if (exitCode != 0) return;

        // Look for known fingerprint device patterns
        var lines = output.Split('\n');
        foreach (var line in lines)
        {
            // DigitalPersona pattern: Bus 001 Device 014: ID 05ba:000a DigitalPersona, Inc. Fingerprint Reader
            var match = DeviceRegex().Match(line);
            if (match.Success)
            {
                var vendorId = match.Groups[1].Value;
                var productId = match.Groups[2].Value;
                var description = match.Groups[3].Value;

                _deviceInfo = new FingerprintDeviceInfo
                {
                    VendorId = vendorId,
                    ProductId = productId,
                    Vendor = GetVendorName(vendorId),
                    ProductName = description.Trim(),
                    Driver = "libfprint/fprintd",
                    DeviceType = GetDeviceType(vendorId, productId),
                    SupportsEnrollment = true,
                    SupportsVerification = true,
                    SupportsIdentification = true
                };
                return;
            }
        }

        // Default device info if detection fails
        _deviceInfo = new FingerprintDeviceInfo
        {
            Vendor = "Unknown",
            ProductName = "Fingerprint Reader",
            Driver = "libfprint/fprintd",
            DeviceType = FingerprintDeviceType.Fprintd,
            SupportsEnrollment = true,
            SupportsVerification = true,
            SupportsIdentification = false
        };
    }

    private static string GetVendorName(string vendorId) => vendorId.ToUpperInvariant() switch
    {
        "05BA" => "DigitalPersona",
        "138A" => "Validity Sensors",
        "147E" => "Upek",
        "1C7A" => "LighTuning",
        "27C6" => "Goodix",
        "06CB" => "Synaptics",
        "04F3" => "Elan",
        "2808" => "FocalTech",
        _ => "Unknown"
    };

    private static FingerprintDeviceType GetDeviceType(string vendorId, string productId) => vendorId.ToUpperInvariant() switch
    {
        "05BA" when productId.Equals("000A", StringComparison.OrdinalIgnoreCase) => FingerprintDeviceType.DigitalPersona4500,
        "05BA" => FingerprintDeviceType.DigitalPersona4500, // Other DigitalPersona models
        "138A" => FingerprintDeviceType.ValiditySensors,    // Validity Sensors (Dell/HP)
        "27C6" => FingerprintDeviceType.Goodix,             // Goodix
        "06CB" => FingerprintDeviceType.Synaptics,          // Synaptics (Lenovo/Dell)
        "04F3" => FingerprintDeviceType.Elan,               // Elan Microelectronics (ASUS/Acer)
        "2808" => FingerprintDeviceType.FocalTech,          // FocalTech
        "1C7A" => FingerprintDeviceType.LighTuning,         // LighTuning
        "147E" => FingerprintDeviceType.Upek,               // Upek/AuthenTec
        _ => FingerprintDeviceType.Fprintd
    };

    public override async Task StartCaptureAsync(CancellationToken cancellationToken = default)
    {
        if (_isCapturing) return;

        _captureCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _isCapturing = true;
        OnDeviceStatusChanged(FingerprintDeviceStatus.WaitingForFinger);

        // Start background capture loop
        _ = Task.Run(async () =>
        {
            while (!_captureCts.Token.IsCancellationRequested)
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
                await Task.Delay(100, _captureCts.Token);
            }
        }, _captureCts.Token);
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

    public override async Task<FingerprintCaptureResult> CaptureAsync(CancellationToken cancellationToken = default)
    {
        // fprintd doesn't have a standalone capture command.
        // For raw capture, we'd need to use libfprint directly.
        // This implementation uses fprintd-verify with a temporary enrollment.
        // For better raw capture support, consider adding libfprint bindings.

        OnDeviceStatusChanged(FingerprintDeviceStatus.Capturing);

        try
        {
            // Use fprintd-verify which captures and processes a fingerprint
            var (exitCode, output, error) = await RunCommandAsync(
                "timeout",
                $"10 fprintd-verify {_defaultUsername}",
                cancellationToken);

            OnDeviceStatusChanged(FingerprintDeviceStatus.Ready);

            if (exitCode == 0 || output.Contains("verify-match", StringComparison.OrdinalIgnoreCase))
            {
                // fprintd doesn't return raw data, return success indicator
                return FingerprintCaptureResult.Successful(
                    sampleData: [],
                    templateData: null,
                    quality: 80);
            }

            if (output.Contains("verify-no-match", StringComparison.OrdinalIgnoreCase))
            {
                return FingerprintCaptureResult.Successful([], null, 60);
            }

            if (output.Contains("verify-unknown-error", StringComparison.OrdinalIgnoreCase) ||
                error.Contains("No enrolled fingerprints", StringComparison.OrdinalIgnoreCase))
            {
                return FingerprintCaptureResult.Failed(FingerprintCaptureStatus.NoFinger,
                    "No enrolled fingerprints. Please enroll first.");
            }

            return FingerprintCaptureResult.Failed(FingerprintCaptureStatus.DeviceError, error);
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
        // fprintd handles template creation internally during enrollment
        // This would require libfprint direct bindings for raw template extraction
        return Task.FromResult<byte[]?>(null);
    }

    public override async Task<FingerprintVerifyResult> VerifyAsync(byte[] sample, byte[] template, CancellationToken cancellationToken = default)
    {
        // fprintd uses system-stored templates, not application-provided ones
        // For template-based verification, use IdentifyAsync with stored templates
        // or implement direct libfprint bindings

        var success = await VerifyUserAsync(_defaultUsername, cancellationToken);
        return success
            ? FingerprintVerifyResult.Match(85)
            : FingerprintVerifyResult.NoMatch();
    }

    public override Task<FingerprintMatchResult?> IdentifyAsync(byte[] sample, IReadOnlyDictionary<string, byte[]> templates, CancellationToken cancellationToken = default)
    {
        // fprintd doesn't support 1:N identification with custom templates
        // Would need libfprint direct bindings for this functionality
        throw new NotSupportedException("fprintd does not support application-managed template identification. Use VerifyUserAsync for system-managed verification.");
    }

    public override Task<FingerprintMatchResult?> MatchAsync(byte[] sample, CancellationToken cancellationToken = default)
    {
        // Legacy method - redirect to VerifyUserAsync
        throw new NotSupportedException("Use VerifyUserAsync for fprintd-based verification.");
    }

    public override async Task<FingerprintEnrollResult> EnrollAsync(string username, FingerPosition finger, CancellationToken cancellationToken = default)
    {
        try
        {
            OnDeviceStatusChanged(FingerprintDeviceStatus.Capturing);

            var fingerName = finger.ToFprintdName();

            // fprintd-enroll requires running as root or with polkit authorization
            var (exitCode, output, error) = await RunCommandAsync(
                "fprintd-enroll",
                $"-f {fingerName} {username}",
                cancellationToken,
                timeoutSeconds: 60);

            OnDeviceStatusChanged(FingerprintDeviceStatus.Ready);

            if (exitCode == 0 || output.Contains("Enrollment completed", StringComparison.OrdinalIgnoreCase))
            {
                return FingerprintEnrollResult.Successful(finger);
            }

            if (output.Contains("enroll-stage-passed", StringComparison.OrdinalIgnoreCase))
            {
                // Parse stage information
                var stageMatch = StageRegex().Match(output);
                if (stageMatch.Success)
                {
                    var current = int.Parse(stageMatch.Groups[1].Value);
                    var total = int.Parse(stageMatch.Groups[2].Value);
                    return FingerprintEnrollResult.StageComplete(finger, current, total);
                }
                return FingerprintEnrollResult.StageComplete(finger, 1, 5); // Default stages
            }

            if (error.Contains("Not authorized", StringComparison.OrdinalIgnoreCase) ||
                error.Contains("Permission denied", StringComparison.OrdinalIgnoreCase))
            {
                return FingerprintEnrollResult.Failed(FingerprintEnrollStatus.PermissionDenied,
                    "Permission denied. Run with sudo or configure polkit.");
            }

            if (error.Contains("already enrolled", StringComparison.OrdinalIgnoreCase))
            {
                return FingerprintEnrollResult.Failed(FingerprintEnrollStatus.AlreadyEnrolled);
            }

            return FingerprintEnrollResult.Failed(FingerprintEnrollStatus.Unknown, error);
        }
        catch (OperationCanceledException)
        {
            OnDeviceStatusChanged(FingerprintDeviceStatus.Ready);
            return FingerprintEnrollResult.Failed(FingerprintEnrollStatus.Cancelled);
        }
        catch (Exception ex)
        {
            OnDeviceStatusChanged(FingerprintDeviceStatus.Error);
            return FingerprintEnrollResult.Failed(FingerprintEnrollStatus.DeviceError, ex.Message);
        }
    }

    public override async Task<bool> VerifyUserAsync(string username, CancellationToken cancellationToken = default)
    {
        try
        {
            OnDeviceStatusChanged(FingerprintDeviceStatus.Capturing);

            var (exitCode, output, _) = await RunCommandAsync(
                "timeout",
                $"15 fprintd-verify {username}",
                cancellationToken);

            OnDeviceStatusChanged(FingerprintDeviceStatus.Ready);

            return exitCode == 0 || output.Contains("verify-match", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            OnDeviceStatusChanged(FingerprintDeviceStatus.Ready);
            return false;
        }
    }

    public override async Task<IReadOnlyList<FingerPosition>> ListEnrolledFingersAsync(string username, CancellationToken cancellationToken = default)
    {
        var fingers = new List<FingerPosition>();

        try
        {
            var (exitCode, output, _) = await RunCommandAsync("fprintd-list", username, cancellationToken);

            if (exitCode != 0) return fingers;

            // Parse output like:
            // found 2 prints for user ishikote
            //  - #0: left-index-finger
            //  - #1: right-thumb
            var matches = EnrolledFingerRegex().Matches(output);
            foreach (Match match in matches)
            {
                var fingerName = match.Groups[1].Value;
                var position = FingerPositionExtensions.FromFprintdName(fingerName);
                if (position != FingerPosition.Unknown)
                {
                    fingers.Add(position);
                }
            }
        }
        catch
        {
            // Return empty list on error
        }

        return fingers;
    }

    public override async Task<bool> DeleteEnrolledFingersAsync(string username, CancellationToken cancellationToken = default)
    {
        try
        {
            var (exitCode, _, _) = await RunCommandAsync("fprintd-delete", username, cancellationToken);
            return exitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public override int GetSampleQuality(byte[] sample)
    {
        // fprintd doesn't expose quality metrics directly
        // Return a default value
        return sample.Length > 0 ? 70 : 0;
    }

    private static async Task<(int exitCode, string output, string error)> RunCommandAsync(
        string command,
        string arguments,
        CancellationToken cancellationToken,
        int timeoutSeconds = 30)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        var completed = await Task.WhenAny(
            process.WaitForExitAsync(cancellationToken),
            Task.Delay(TimeSpan.FromSeconds(timeoutSeconds), cancellationToken));

        if (!process.HasExited)
        {
            process.Kill();
            return (-1, "", "Process timed out");
        }

        var output = await outputTask;
        var error = await errorTask;

        return (process.ExitCode, output, error);
    }

    [GeneratedRegex(@"ID\s+([0-9a-fA-F]{4}):([0-9a-fA-F]{4})\s+(.+)$", RegexOptions.Multiline)]
    private static partial Regex DeviceRegex();

    [GeneratedRegex(@"stage\s+(\d+)\s+of\s+(\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex StageRegex();

    [GeneratedRegex(@"-\s+#\d+:\s+(\S+)", RegexOptions.Multiline)]
    private static partial Regex EnrolledFingerRegex();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _captureCts?.Cancel();
            _captureCts?.Dispose();
        }
        base.Dispose(disposing);
    }
}
