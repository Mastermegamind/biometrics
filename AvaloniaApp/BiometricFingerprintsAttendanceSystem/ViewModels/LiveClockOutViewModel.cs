using System.Windows.Input;
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
    public string CurrentDate => DateTime.Now.ToString("dddd, MMMM dd, yyyy");
    public string CurrentTime => DateTime.Now.ToString("HH:mm:ss");

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

            // Send to API/local for clock-out
            var clockOutRequest = new ClockOutRequest
            {
                FingerprintTemplate = templateData,
                Timestamp = DateTime.UtcNow
            };

            var result = await _services.Data.ClockOutAsync(clockOutRequest);

            ShowResult = true;

            if (result.Success && result.Student != null)
            {
                _logger.LogInformation("Live clock-out success for RegNo {RegNo}", result.Student.RegNo);
                IsSuccess = true;
                StudentName = result.Student.Name;
                StudentRegNo = result.Student.RegNo;
                StudentClass = result.Student.ClassName;
                ClockInTime = result.ClockInTime?.ToString("HH:mm:ss") ?? "--:--:--";
                ClockOutTime = result.ClockOutTime?.ToString("HH:mm:ss") ?? DateTime.Now.ToString("HH:mm:ss");

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

        // Notify time properties changed
        OnPropertyChanged(nameof(CurrentDate));
        OnPropertyChanged(nameof(CurrentTime));
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
        {
            return $"{(int)duration.TotalHours}h {duration.Minutes}m";
        }
        return $"{duration.Minutes}m {duration.Seconds}s";
    }
}
