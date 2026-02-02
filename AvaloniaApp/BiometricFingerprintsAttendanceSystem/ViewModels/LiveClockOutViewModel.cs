using System.Windows.Input;
using System.Text;
using Avalonia.Media.Imaging;
using BiometricFingerprintsAttendanceSystem.Services;
using BiometricFingerprintsAttendanceSystem.Services.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BiometricFingerprintsAttendanceSystem.ViewModels;

public class LiveClockOutViewModel : ViewModelBase
{
    private readonly IServiceRegistry _services;
    private readonly ILogger<LiveClockOutViewModel> _logger;

    private string _studentName = string.Empty;
    private string _studentRegNo = string.Empty;
    private string _studentClass = string.Empty;
    private Bitmap? _studentPhoto;
    private Bitmap? _fingerprintImage;
    private string _clockInTime = string.Empty;
    private string _clockOutTime = string.Empty;
    private string _duration = string.Empty;
    private string _statusMessage = "Place your finger on the scanner to clock out";
    private string _templateCacheStatus = "Templates: not cached";
    private string _templateCacheLastRefresh = "Last refresh: --";
    private bool _isProcessing;
    private bool _isSuccess;
    private bool _showResult;

    public LiveClockOutViewModel(IServiceRegistry services)
    {
        _services = services;
        _logger = services.Provider.GetService(typeof(ILogger<LiveClockOutViewModel>)) as ILogger<LiveClockOutViewModel>
            ?? NullLogger<LiveClockOutViewModel>.Instance;

        ClockOutCommand = new AsyncRelayCommand(ProcessClockOutAsync, () => !IsProcessing);
        ResetCommand = new RelayCommand(Reset);

        // Auto-start scanning when view is loaded
        _ = StartScanningAsync();
        UpdateTemplateCacheIndicators();
    }

    // ==================== Properties ====================

    public string StudentName
    {
        get => _studentName;
        set => SetProperty(ref _studentName, value);
    }

    public string StudentRegNo
    {
        get => _studentRegNo;
        set => SetProperty(ref _studentRegNo, value);
    }

    public string StudentClass
    {
        get => _studentClass;
        set => SetProperty(ref _studentClass, value);
    }

    public Bitmap? StudentPhoto
    {
        get => _studentPhoto;
        set => SetProperty(ref _studentPhoto, value);
    }

    public Bitmap? FingerprintImage
    {
        get => _fingerprintImage;
        set => SetProperty(ref _fingerprintImage, value);
    }

    public string ClockInTime
    {
        get => _clockInTime;
        set => SetProperty(ref _clockInTime, value);
    }

    public string ClockOutTime
    {
        get => _clockOutTime;
        set => SetProperty(ref _clockOutTime, value);
    }

    public string Duration
    {
        get => _duration;
        set => SetProperty(ref _duration, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public string TemplateCacheStatus
    {
        get => _templateCacheStatus;
        set => SetProperty(ref _templateCacheStatus, value);
    }

    public string TemplateCacheLastRefresh
    {
        get => _templateCacheLastRefresh;
        set => SetProperty(ref _templateCacheLastRefresh, value);
    }

    public bool IsProcessing
    {
        get => _isProcessing;
        set
        {
            if (SetProperty(ref _isProcessing, value))
            {
                (ClockOutCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsSuccess
    {
        get => _isSuccess;
        set => SetProperty(ref _isSuccess, value);
    }

    public bool ShowResult
    {
        get => _showResult;
        set => SetProperty(ref _showResult, value);
    }

    public string SyncModeDisplay => $"Mode: {_services.Data.Mode}";
    public bool IsOnline => _services.Data.IsOnline;
    public string CurrentDate => LagosTime.Now.ToString("dddd, MMMM dd, yyyy");
    public string CurrentTime => LagosTime.Now.ToString("HH:mm:ss");

    // ==================== Commands ====================

    public ICommand ClockOutCommand { get; }
    public ICommand ResetCommand { get; }

    // ==================== Methods ====================

    private async Task StartScanningAsync()
    {
        try
        {
            var initResult = await _services.Fingerprint.InitializeAsync();
            if (!initResult)
            {
                StatusMessage = "Fingerprint scanner not available";
                _logger.LogWarning("Live clock-out scanner init failed");
                return;
            }

            StatusMessage = "Ready. Place your finger on the scanner...";
            _logger.LogInformation("Live clock-out scanner ready");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Scanner error: {ex.Message}";
            _logger.LogError(ex, "Live clock-out scanner error");
        }
    }

    private async Task ProcessClockOutAsync()
    {
        IsProcessing = true;
        ShowResult = false;
        StatusMessage = "Scanning fingerprint...";
        _logger.LogInformation("Live clock-out started");

        try
        {
            // Capture fingerprint
            var captureResult = await _services.Fingerprint.CaptureAsync();
            if (!captureResult.Success || captureResult.SampleData == null)
            {
                StatusMessage = captureResult.Message ?? "Capture failed. Please try again.";
                _logger.LogWarning("Live clock-out capture failed: {Message}", captureResult.Message ?? "Capture failed");
                return;
            }

            // Display fingerprint image
            if (captureResult.ImageData != null && captureResult.ImageData.Length > 0)
            {
                using var stream = new MemoryStream(captureResult.ImageData);
                FingerprintImage = new Bitmap(stream);
            }

            StatusMessage = "Verifying identity...";

            // Create template from captured sample (or use capture template if available)
            var templateData = captureResult.TemplateData;
            if (templateData == null || templateData.Length == 0)
            {
                templateData = await _services.Fingerprint.CreateTemplateAsync(captureResult.SampleData);
            }
            if ((templateData == null || templateData.Length == 0) &&
                captureResult.SampleData != null && captureResult.SampleData.Length > 0)
            {
                templateData = captureResult.SampleData;
                _logger.LogWarning("Live clock-out using sample data as template fallback");
            }
            if (templateData == null || templateData.Length == 0)
            {
                StatusMessage = "Fingerprint device did not provide a template. Please try again.";
                _logger.LogWarning("Live clock-out template creation failed");
                return;
            }

            SaveClockOutTemplate(templateData, "unknown", "captured");

            var clockOutRequest = new ClockOutRequest
            {
                FingerprintTemplate = templateData,
                Timestamp = LagosTime.Now
            };

            ClockOutResponse result;

            // First, try to authenticate against locally cached templates (synced from online API)
            var authResult = await _services.TemplateSync.AuthenticateAsync(templateData);
            UpdateTemplateCacheIndicators();

            if (authResult.Success && !string.IsNullOrEmpty(authResult.RegNo))
            {
                SaveClockOutTemplate(templateData, authResult.RegNo, "localcache");
                _logger.LogInformation(
                    "Local cache auth success: RegNo={RegNo} Score={Score} FAR={FAR}",
                    authResult.RegNo, authResult.MatchScore, authResult.MatchFar);

                // Use verified clock-out endpoint (no server-side matching needed)
                var verifiedRequest = new VerifiedClockRequest
                {
                    RegNo = authResult.RegNo,
                    FingerIndex = authResult.FingerIndex,
                    MatchScore = authResult.MatchScore,
                    MatchFar = authResult.MatchFar,
                    Timestamp = LagosTime.Now
                };

                // Try online verified clock-out first, fall back to offline if unavailable
                if (_services.Data.IsOnline)
                {
                    result = await _services.OnlineData.ClockOutVerifiedAsync(verifiedRequest);
                    if (!result.Success && result.Message?.Contains("Network") == true)
                    {
                        _logger.LogWarning("Online verified clock-out failed (network), falling back to offline");
                        result = await _services.Data.ClockOutAsync(clockOutRequest);
                    }
                }
                else
                {
                    result = await _services.Data.ClockOutAsync(clockOutRequest);
                }
            }
            else
            {
                // Local cache auth failed - try OnlineMatcher as fallback (fetches templates from API)
                _logger.LogInformation("Local cache auth failed ({Message}), trying OnlineMatcher fallback", authResult.Message);
                var matchResult = await _services.OnlineMatcher.MatchAsync(templateData);

                if (matchResult.Success && matchResult.Data != null)
                {
                    SaveClockOutTemplate(templateData, matchResult.Data.RegNo, "onlinematcher");
                    var verifiedRequest = new VerifiedClockRequest
                    {
                        RegNo = matchResult.Data.RegNo,
                        FingerIndex = matchResult.Data.FingerIndex,
                        MatchScore = matchResult.Data.MatchScore,
                        MatchFar = matchResult.Data.MatchFar,
                        Timestamp = LagosTime.Now
                    };

                    result = await _services.OnlineData.ClockOutVerifiedAsync(verifiedRequest);
                }
                else
                {
                    // Final fallback to offline matching (uses locally enrolled templates)
                    _logger.LogWarning("OnlineMatcher also failed, falling back to offline clock-out");
                    result = await _services.Data.ClockOutAsync(clockOutRequest);
                }
            }

            ShowResult = true;

            if (result.Success && result.Student != null)
            {
                _logger.LogInformation("Live clock-out success for RegNo {RegNo}", result.Student.RegNo);
                IsSuccess = true;
                StudentName = result.Student.Name;
                StudentRegNo = result.Student.RegNo;
                StudentClass = result.Student.ClassName;
                ClockInTime = result.ClockInTime?.ToString("HH:mm:ss") ?? "--:--:--";
                ClockOutTime = result.ClockOutTime?.ToString("HH:mm:ss") ?? LagosTime.Now.ToString("HH:mm:ss");

                // Calculate and display duration
                if (result.Duration.HasValue)
                {
                    Duration = FormatDuration(result.Duration.Value);
                }

                // Load photo
                if (result.Student.PassportPhoto != null)
                {
                    using var stream = new MemoryStream(result.Student.PassportPhoto);
                    StudentPhoto = new Bitmap(stream);
                }
                else if (!string.IsNullOrEmpty(result.Student.PassportUrl))
                {
                    var photoResult = await _services.Data.GetStudentPhotoAsync(result.Student.RegNo);
                    if (photoResult.Success && photoResult.Data != null)
                    {
                        using var stream = new MemoryStream(photoResult.Data);
                        StudentPhoto = new Bitmap(stream);
                    }
                    else
                    {
                        _logger.LogWarning("Live clock-out photo fetch failed for RegNo {RegNo}: {Message}", result.Student.RegNo, photoResult.Message ?? "No photo data");
                    }
                }

                StatusMessage = "Clock-out successful!";

                // Show sync indicator for offline modes
                if (_services.Data.Mode != SyncMode.OnlineOnly && !_services.Data.IsOnline)
                {
                    StatusMessage += " (Saved offline, will sync later)";
                }
            }
            else if (result.NotClockedIn)
            {
                IsSuccess = false;
                StudentName = result.Student?.Name ?? "";
                StudentRegNo = result.Student?.RegNo ?? "";
                StatusMessage = "You haven't clocked in today! Please clock in first.";
                _logger.LogWarning("Live clock-out not clocked in for RegNo {RegNo}", StudentRegNo);
            }
            else
            {
                IsSuccess = false;
                StatusMessage = result.Message ?? "Fingerprint not recognized. Please try again.";
                _logger.LogWarning("Live clock-out failed: {Message}", result.Message ?? "Unknown error");
            }
        }
        catch (Exception ex)
        {
            IsSuccess = false;
            StatusMessage = $"Error: {ex.Message}";
            _logger.LogError(ex, "Live clock-out error");
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private void Reset()
    {
        StudentName = string.Empty;
        StudentRegNo = string.Empty;
        StudentClass = string.Empty;
        StudentPhoto = null;
        FingerprintImage = null;
        ClockInTime = string.Empty;
        ClockOutTime = string.Empty;
        Duration = string.Empty;
        ShowResult = false;
        IsSuccess = false;
        StatusMessage = "Ready. Place your finger on the scanner...";
        UpdateTemplateCacheIndicators();

        // Notify time properties changed
        OnPropertyChanged(nameof(CurrentDate));
        OnPropertyChanged(nameof(CurrentTime));
    }

    private void UpdateTemplateCacheIndicators()
    {
        var sync = _services.TemplateSync;

        if (sync.IsSyncing)
        {
            TemplateCacheStatus = "Templates: syncing...";
            TemplateCacheLastRefresh = "Last sync: in progress";
            return;
        }

        TemplateCacheStatus = sync.CachedTemplateCount > 0
            ? $"Templates: cached ({sync.CachedTemplateCount} from {sync.CachedStudentCount} students)"
            : "Templates: not cached";

        TemplateCacheLastRefresh = sync.LastSyncAt.HasValue
            ? $"Last sync: {sync.LastSyncAt.Value:HH:mm:ss}"
            : "Last sync: --";

        if (!string.IsNullOrEmpty(sync.LastSyncError))
        {
            TemplateCacheLastRefresh += $" (Error: {sync.LastSyncError})";
        }
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
        {
            return $"{(int)duration.TotalHours}h {duration.Minutes}m";
        }
        return $"{duration.Minutes}m {duration.Seconds}s";
    }

    private void SaveClockOutTemplate(byte[] templateData, string regNo, string source)
    {
        try
        {
            var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dir = Path.Combine(baseDir, "BiometricFingerprintsAttendanceSystem", "cache", "clockout-templates");
            Directory.CreateDirectory(dir);

            var safeRegNo = SanitizeFileName(regNo);
            var timestamp = LagosTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            var baseName = $"clockout_{timestamp}_{source}_{safeRegNo}_{templateData.Length}b";
            var binPath = Path.Combine(dir, $"{baseName}.bin");
            var b64Path = Path.Combine(dir, $"{baseName}.b64");

            File.WriteAllBytes(binPath, templateData);
            File.WriteAllText(b64Path, Convert.ToBase64String(templateData), Encoding.ASCII);

            _logger.LogInformation("Saved clock-out template to {BinPath}", binPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save clock-out template to disk");
        }
    }

    private static string SanitizeFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "unknown";
        }

        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            builder.Append(invalid.Contains(ch) ? '_' : ch);
        }
        return builder.ToString();
    }
}

