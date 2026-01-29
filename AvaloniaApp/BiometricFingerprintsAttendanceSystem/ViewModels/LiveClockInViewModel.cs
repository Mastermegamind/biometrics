using System.Windows.Input;
using Avalonia.Media.Imaging;
using BiometricFingerprintsAttendanceSystem.Services;
using BiometricFingerprintsAttendanceSystem.Services.Data;

namespace BiometricFingerprintsAttendanceSystem.ViewModels;

public class LiveClockInViewModel : ViewModelBase
{
    private readonly IServiceRegistry _services;

    private string _studentName = string.Empty;
    private string _studentRegNo = string.Empty;
    private string _studentClass = string.Empty;
    private Bitmap? _studentPhoto;
    private Bitmap? _fingerprintImage;
    private string _clockInTime = string.Empty;
    private string _statusMessage = "Place your finger on the scanner to clock in";
    private bool _isProcessing;
    private bool _isSuccess;
    private bool _showResult;

    public LiveClockInViewModel(IServiceRegistry services)
    {
        _services = services;

        ClockInCommand = new AsyncRelayCommand(ProcessClockInAsync, () => !IsProcessing);
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
    public string CurrentDate => DateTime.Now.ToString("dddd, MMMM dd, yyyy");
    public string CurrentTime => DateTime.Now.ToString("HH:mm:ss");

    // ==================== Commands ====================

    public ICommand ClockInCommand { get; }
    public ICommand ResetCommand { get; }

    // ==================== Methods ====================

    private async Task StartScanningAsync()
    {
        // Initialize fingerprint scanner
        try
        {
            var initResult = await _services.Fingerprint.InitializeAsync();
            if (!initResult)
            {
                StatusMessage = "Fingerprint scanner not available";
                return;
            }

            StatusMessage = "Ready. Place your finger on the scanner...";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Scanner error: {ex.Message}";
        }
    }

    private async Task ProcessClockInAsync()
    {
        IsProcessing = true;
        ShowResult = false;
        StatusMessage = "Scanning fingerprint...";

        try
        {
            // Capture fingerprint
            var captureResult = await _services.Fingerprint.CaptureAsync();
            if (!captureResult.Success || captureResult.SampleData == null)
            {
                StatusMessage = captureResult.Message ?? "Capture failed. Please try again.";
                return;
            }

            // Display fingerprint image
            if (captureResult.ImageData != null && captureResult.ImageData.Length > 0)
            {
                using var stream = new MemoryStream(captureResult.ImageData);
                FingerprintImage = new Bitmap(stream);
            }

            StatusMessage = "Verifying identity...";

            // Create template from captured sample
            var templateData = await _services.Fingerprint.CreateTemplateAsync(captureResult.SampleData);
            if (templateData == null || templateData.Length == 0)
            {
                StatusMessage = "Failed to process fingerprint. Please try again.";
                return;
            }

            // Send to API/local for clock-in
            var clockInRequest = new ClockInRequest
            {
                FingerprintTemplate = templateData,
                Timestamp = DateTime.UtcNow
            };

            var result = await _services.Data.ClockInAsync(clockInRequest);

            ShowResult = true;

            if (result.Success && result.Student != null)
            {
                IsSuccess = true;
                StudentName = result.Student.Name;
                StudentRegNo = result.Student.RegNo;
                StudentClass = result.Student.ClassName;
                ClockInTime = result.ClockInTime?.ToString("HH:mm:ss") ?? DateTime.Now.ToString("HH:mm:ss");

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
            }
            else
            {
                IsSuccess = false;
                StatusMessage = result.Message ?? "Fingerprint not recognized. Please try again.";
            }
        }
        catch (Exception ex)
        {
            IsSuccess = false;
            StatusMessage = $"Error: {ex.Message}";
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
        ShowResult = false;
        IsSuccess = false;
        StatusMessage = "Ready. Place your finger on the scanner...";

        // Notify time properties changed
        OnPropertyChanged(nameof(CurrentDate));
        OnPropertyChanged(nameof(CurrentTime));
    }
}
