using System.Windows.Input;
using System.Text;
using System.Security.Cryptography;
using Avalonia.Media.Imaging;
using BiometricFingerprintsAttendanceSystem.Services;
using BiometricFingerprintsAttendanceSystem.Services.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BiometricFingerprintsAttendanceSystem.ViewModels;

public class LiveClockInViewModel : ViewModelBase
{
    private readonly IServiceRegistry _services;
    private readonly ILogger<LiveClockInViewModel> _logger;
    private CancellationTokenSource? _scanningCts;
    private static readonly TimeSpan ResultDisplayDuration = TimeSpan.FromSeconds(5);

    private string _studentName = string.Empty;
    private string _studentRegNo = string.Empty;
    private string _studentClass = string.Empty;
    private Bitmap? _studentPhoto;
    private Bitmap? _fingerprintImage;
    private string _clockInTime = string.Empty;
    private string _statusMessage = "Place your finger on the scanner to clock in";
    private string _templateCacheStatus = "Templates: not cached";
    private string _templateCacheLastRefresh = "Last refresh: --";
    private string _templateCacheHint = string.Empty;
    private bool _showTemplateCacheHint;
    private bool _isProcessing;
    private bool _isSuccess;
    private bool _showResult;
    private string? _lastCapturedTemplatePath;
    private static readonly TimeSpan CacheStaleThreshold = TimeSpan.FromMinutes(2);

    public LiveClockInViewModel(IServiceRegistry services)
    {
        _services = services;
        _logger = services.Provider.GetService(typeof(ILogger<LiveClockInViewModel>)) as ILogger<LiveClockInViewModel>
            ?? NullLogger<LiveClockInViewModel>.Instance;

        ClockInCommand = new AsyncRelayCommand(ProcessClockInAsync, () => !IsProcessing);
        DebugCompareTemplatesCommand = new AsyncRelayCommand(DebugCompareTemplatesAsync, () => !IsProcessing);
        ResetCommand = new RelayCommand(Reset);

        // Auto-start continuous scanning when view is loaded
        _ = StartContinuousScanningAsync();
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

    public string TemplateCacheHint
    {
        get => _templateCacheHint;
        set => SetProperty(ref _templateCacheHint, value);
    }

    public bool ShowTemplateCacheHint
    {
        get => _showTemplateCacheHint;
        set => SetProperty(ref _showTemplateCacheHint, value);
    }

    public bool IsProcessing
    {
        get => _isProcessing;
        set
        {
            if (SetProperty(ref _isProcessing, value))
            {
                (ClockInCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
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

    public ICommand ClockInCommand { get; }
    public ICommand DebugCompareTemplatesCommand { get; }
    public ICommand ResetCommand { get; }

    // ==================== Methods ====================

    private async Task StartContinuousScanningAsync()
    {
        _scanningCts?.Cancel();
        _scanningCts = new CancellationTokenSource();
        var token = _scanningCts.Token;

        try
        {
            var initResult = await _services.Fingerprint.InitializeAsync();
            if (!initResult)
            {
                StatusMessage = "Fingerprint scanner not available";
                _logger.LogWarning("Live clock-in scanner init failed");
                return;
            }

            StatusMessage = "Ready. Place your finger on the scanner...";
            _logger.LogInformation("Live clock-in continuous scanner ready");

            // Continuous scanning loop
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await ProcessClockInAsync();

                    if (ShowResult)
                    {
                        // Display result for a few seconds before auto-reset
                        await Task.Delay(ResultDisplayDuration, token);
                        Reset();
                    }
                    else
                    {
                        // Brief delay before retrying if capture failed
                        await Task.Delay(500, token);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Continuous scanning iteration error");
                    await Task.Delay(1000, token);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when stopping
        }
        catch (Exception ex)
        {
            StatusMessage = $"Scanner error: {ex.Message}";
            _logger.LogError(ex, "Live clock-in continuous scanner error");
        }
    }

    public void StopScanning()
    {
        _scanningCts?.Cancel();
    }

    private async Task ProcessClockInAsync()
    {
        IsProcessing = true;
        ShowResult = false;
        StatusMessage = "Scanning fingerprint...";
        _logger.LogInformation("Live clock-in started");

        try
        {
            // Capture fingerprint
            var captureResult = await _services.Fingerprint.CaptureAsync();
            if (!captureResult.Success || captureResult.SampleData == null)
            {
                StatusMessage = captureResult.Message ?? "Capture failed. Please try again.";
                _logger.LogWarning("Live clock-in capture failed: {Message}", captureResult.Message ?? "Capture failed");
                return;
            }

            // Display fingerprint image
            if (captureResult.ImageData != null && captureResult.ImageData.Length > 0)
            {
                using var stream = new MemoryStream(captureResult.ImageData);
                FingerprintImage = new Bitmap(stream);
            }

            StatusMessage = "Verifying identity...";
            await RefreshTemplateCacheAsync();
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
                _logger.LogWarning("Live clock-in using sample data as template fallback");
            }
            if (templateData == null || templateData.Length == 0)
            {
                StatusMessage = "Fingerprint device did not provide a template. Please try again.";
                _logger.LogWarning("Live clock-in template creation failed");
                return;
            }

            _lastCapturedTemplatePath = SaveClockInTemplate(templateData, "unknown", "captured");

            ClockInResponse result;

            // First, try to authenticate against locally cached templates (synced from online API)
            var authResult = await _services.TemplateSync.AuthenticateAsync(templateData);
            UpdateTemplateCacheIndicators();

            if (authResult.Success && !string.IsNullOrEmpty(authResult.RegNo))
            {
                SaveClockInTemplate(templateData, authResult.RegNo, "localcache");
                _logger.LogInformation(
                    "Local cache auth success: RegNo={RegNo} Score={Score} FAR={FAR}",
                    authResult.RegNo, authResult.MatchScore, authResult.MatchFar);

                // Use verified clock-in endpoint (no server-side matching needed)
                var verifiedRequest = new VerifiedClockRequest
                {
                    RegNo = authResult.RegNo,
                    FingerIndex = authResult.FingerIndex,
                    MatchScore = authResult.MatchScore,
                    MatchFar = authResult.MatchFar,
                    Timestamp = LagosTime.Now
                };

                // Record clock-in using verified identity (offline-first safe)
                result = await _services.Data.ClockInVerifiedAsync(verifiedRequest);
            }
            else
            {
                // Only use synced local disk cache for validation
                _logger.LogWarning("Local cache auth failed: {Message}", authResult.Message ?? "Fingerprint not recognized");
                result = new ClockInResponse
                {
                    Success = false,
                    Message = authResult.Message ?? "Fingerprint not recognized"
                };
            }

            ShowResult = true;

            if (result.Success && result.Student != null)
            {
                _logger.LogInformation("Live clock-in success for RegNo {RegNo}", result.Student.RegNo);
                IsSuccess = true;
                StudentName = result.Student.Name;
                StudentRegNo = result.Student.RegNo;
                StudentClass = result.Student.ClassName;
                ClockInTime = result.ClockInTime?.ToString("HH:mm:ss") ?? LagosTime.Now.ToString("HH:mm:ss");

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
                        _logger.LogWarning("Live clock-in photo fetch failed for RegNo {RegNo}: {Message}", result.Student.RegNo, photoResult.Message ?? "No photo data");
                    }
                }

                StatusMessage = "Clock-in successful!";

                // Show sync indicator for offline modes
                if (_services.Data.Mode != SyncMode.OnlineOnly && !_services.Data.IsOnline)
                {
                    StatusMessage += " (Saved offline, will sync later)";
                }
            }
            else if (result.AlreadyClockedIn)
            {
                IsSuccess = false;
                StudentName = result.Student?.Name ?? "";
                StudentRegNo = result.Student?.RegNo ?? "";
                StatusMessage = "Already clocked in today!";
                _logger.LogWarning("Live clock-in already clocked in for RegNo {RegNo}", StudentRegNo);
            }
            else
            {
                IsSuccess = false;
                StatusMessage = result.Message ?? "Fingerprint not recognized. Please try again.";
                _logger.LogWarning("Live clock-in failed: {Message}", result.Message ?? "Unknown error");
            }
        }
        catch (Exception ex)
        {
            IsSuccess = false;
            StatusMessage = $"Error: {ex.Message}";
            _logger.LogError(ex, "Live clock-in error");
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private string? SaveClockInTemplate(byte[] templateData, string regNo, string source)
    {
        try
        {
            var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dir = Path.Combine(baseDir, "BiometricFingerprintsAttendanceSystem", "cache", "clockin-templates");
            Directory.CreateDirectory(dir);

            var safeRegNo = SanitizeFileName(regNo);
            var timestamp = LagosTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            var baseName = $"clockin_{timestamp}_{source}_{safeRegNo}_{templateData.Length}b";
            var binPath = Path.Combine(dir, $"{baseName}.bin");
            var b64Path = Path.Combine(dir, $"{baseName}.b64");

            File.WriteAllBytes(binPath, templateData);
            File.WriteAllText(b64Path, Convert.ToBase64String(templateData), Encoding.ASCII);

            _logger.LogInformation("Saved clock-in template to {BinPath}", binPath);
            return binPath;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save clock-in template to disk");
            return null;
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

    private async Task DebugCompareTemplatesAsync()
    {
        try
        {
            var capturedPath = _lastCapturedTemplatePath;
            if (string.IsNullOrWhiteSpace(capturedPath) || !File.Exists(capturedPath))
            {
                capturedPath = TryGetLatestCapturedTemplatePath();
            }

            if (string.IsNullOrWhiteSpace(capturedPath) || !File.Exists(capturedPath))
            {
                _logger.LogWarning("Debug compare: no captured template file found");
                return;
            }

            var capturedBytes = await File.ReadAllBytesAsync(capturedPath);
            var capturedHash = ComputeSha256Hex(capturedBytes);
            _logger.LogInformation("Debug compare: captured template {Path} bytes={Bytes} sha256={Hash}",
                capturedPath, capturedBytes.Length, capturedHash);

            var cachedTemplates = await _services.TemplateSync.GetAllCachedTemplatesAsync();
            if (cachedTemplates.Count == 0)
            {
                _logger.LogWarning("Debug compare: no cached templates available");
                return;
            }

            var matches = new List<string>();
            foreach (var t in cachedTemplates)
            {
                var hash = ComputeSha256Hex(t.TemplateData);
                if (hash == capturedHash)
                {
                    matches.Add($"{t.RegNo}:{t.FingerName}:{t.FingerIndex}");
                }
            }

            if (matches.Count > 0)
            {
                _logger.LogInformation("Debug compare: captured hash matches {Count} cached template(s): {Matches}",
                    matches.Count, string.Join(", ", matches));
            }
            else
            {
                _logger.LogWarning("Debug compare: captured hash did not match any cached template (count={Count})",
                    cachedTemplates.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Debug compare failed");
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

        var now = LagosTime.Now;
        var hasTemplates = sync.CachedTemplateCount > 0;
        var isStale = !sync.LastSyncAt.HasValue || now - sync.LastSyncAt.Value > CacheStaleThreshold;

        if (!hasTemplates)
        {
            TemplateCacheHint = "No templates cached. Sync required.";
            ShowTemplateCacheHint = true;
        }
        else if (isStale)
        {
            var age = sync.LastSyncAt.HasValue ? now - sync.LastSyncAt.Value : TimeSpan.Zero;
            TemplateCacheHint = sync.LastSyncAt.HasValue
                ? $"Cache is {Math.Max(1, (int)age.TotalMinutes)}m old. Refresh recommended."
                : "Cache age unknown. Refresh recommended.";
            ShowTemplateCacheHint = true;
        }
        else
        {
            TemplateCacheHint = string.Empty;
            ShowTemplateCacheHint = false;
        }
    }

    private async Task RefreshTemplateCacheAsync()
    {
        try
        {
            if (_services.TemplateSync.IsSyncing)
            {
                UpdateTemplateCacheIndicators();
                return;
            }

            if (!_services.Data.IsOnline)
            {
                await _services.Data.CheckOnlineStatusAsync();
            }

            if (!_services.Data.IsOnline)
            {
                UpdateTemplateCacheIndicators();
                return;
            }

            StatusMessage = "Refreshing template cache...";
            var result = await _services.TemplateSync.ForceSyncAsync();
            if (!result.Success)
            {
                _logger.LogWarning("Template cache refresh failed: {Message}", result.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Template cache refresh failed");
        }
        finally
        {
            UpdateTemplateCacheIndicators();
        }
    }

    private static string? TryGetLatestCapturedTemplatePath()
    {
        try
        {
            var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dir = Path.Combine(baseDir, "BiometricFingerprintsAttendanceSystem", "cache", "clockin-templates");
            if (!Directory.Exists(dir))
            {
                return null;
            }

            var latest = Directory.GetFiles(dir, "clockin_*_captured_*_*.bin")
                .Select(p => new FileInfo(p))
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .FirstOrDefault();
            return latest?.FullName;
        }
        catch
        {
            return null;
        }
    }

    private static string ComputeSha256Hex(byte[] data)
    {
        var hash = SHA256.HashData(data);
        return Convert.ToHexString(hash);
    }
}

