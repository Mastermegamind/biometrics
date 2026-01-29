using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia.Media.Imaging;
using BiometricFingerprintsAttendanceSystem.Services;
using BiometricFingerprintsAttendanceSystem.Services.Data;
using BiometricFingerprintsAttendanceSystem.Services.Fingerprint;

namespace BiometricFingerprintsAttendanceSystem.ViewModels;

public class LiveEnrollmentViewModel : ViewModelBase
{
    private readonly IServiceRegistry _services;
    private readonly int _minimumFingers;

    private string _regNo = string.Empty;
    private string _studentName = string.Empty;
    private string _studentClass = string.Empty;
    private Bitmap? _studentPhoto;
    private bool _isStudentLoaded;
    private bool _isLoading;
    private bool _isCapturing;
    private string _statusMessage = "Enter registration number to begin";
    private string _selectedFinger = "LeftThumb";
    private int _enrolledCount;
    private bool _canSubmit;
    private Bitmap? _currentFingerprintImage;

    public LiveEnrollmentViewModel(IServiceRegistry services)
    {
        _services = services;
        _minimumFingers = services.AppState.Config.MinimumFingersRequired;

        // Initialize finger slots
        FingerSlots = new ObservableCollection<FingerSlotViewModel>
        {
            new("LeftThumb", "Left Thumb", 6),
            new("LeftIndex", "Left Index", 7),
            new("LeftMiddle", "Left Middle", 8),
            new("LeftRing", "Left Ring", 9),
            new("LeftLittle", "Left Little", 10),
            new("RightThumb", "Right Thumb", 1),
            new("RightIndex", "Right Index", 2),
            new("RightMiddle", "Right Middle", 3),
            new("RightRing", "Right Ring", 4),
            new("RightLittle", "Right Little", 5),
        };

        CapturedTemplates = new ObservableCollection<FingerprintTemplate>();

        // Commands
        LookupStudentCommand = new AsyncRelayCommand(LookupStudentAsync, () => !IsLoading && !string.IsNullOrWhiteSpace(RegNo));
        CaptureFingerCommand = new AsyncRelayCommand(CaptureFingerAsync, () => IsStudentLoaded && !IsCapturing);
        SubmitEnrollmentCommand = new AsyncRelayCommand(SubmitEnrollmentAsync, () => CanSubmit && !IsLoading);
        ClearCommand = new RelayCommand(Clear);
    }

    // ==================== Properties ====================

    public string RegNo
    {
        get => _regNo;
        set
        {
            if (SetProperty(ref _regNo, value))
            {
                (LookupStudentCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public string StudentName
    {
        get => _studentName;
        set => SetProperty(ref _studentName, value);
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

    public bool IsStudentLoaded
    {
        get => _isStudentLoaded;
        set
        {
            if (SetProperty(ref _isStudentLoaded, value))
            {
                (CaptureFingerCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (SetProperty(ref _isLoading, value))
            {
                (LookupStudentCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (SubmitEnrollmentCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsCapturing
    {
        get => _isCapturing;
        set
        {
            if (SetProperty(ref _isCapturing, value))
            {
                (CaptureFingerCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public string SelectedFinger
    {
        get => _selectedFinger;
        set => SetProperty(ref _selectedFinger, value);
    }

    public int EnrolledCount
    {
        get => _enrolledCount;
        set
        {
            if (SetProperty(ref _enrolledCount, value))
            {
                CanSubmit = value >= _minimumFingers;
                OnPropertyChanged(nameof(EnrollmentProgress));
            }
        }
    }

    public string EnrollmentProgress => $"{EnrolledCount}/{_minimumFingers} fingers enrolled (minimum)";

    public bool CanSubmit
    {
        get => _canSubmit;
        set
        {
            if (SetProperty(ref _canSubmit, value))
            {
                (SubmitEnrollmentCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public Bitmap? CurrentFingerprintImage
    {
        get => _currentFingerprintImage;
        set => SetProperty(ref _currentFingerprintImage, value);
    }

    public ObservableCollection<FingerSlotViewModel> FingerSlots { get; }
    public ObservableCollection<FingerprintTemplate> CapturedTemplates { get; }

    public string SyncModeDisplay => $"Mode: {_services.Data.Mode}";
    public bool IsOnline => _services.Data.IsOnline;

    // ==================== Commands ====================

    public ICommand LookupStudentCommand { get; }
    public ICommand CaptureFingerCommand { get; }
    public ICommand SubmitEnrollmentCommand { get; }
    public ICommand ClearCommand { get; }

    // ==================== Methods ====================

    private async Task LookupStudentAsync()
    {
        if (string.IsNullOrWhiteSpace(RegNo)) return;

        IsLoading = true;
        StatusMessage = "Looking up student...";

        try
        {
            var result = await _services.Data.GetStudentAsync(RegNo.Trim());

            if (!result.Success || result.Data == null)
            {
                StatusMessage = result.Message ?? "Student not found";
                IsStudentLoaded = false;
                return;
            }

            var student = result.Data;
            StudentName = student.Name;
            StudentClass = student.ClassName;

            // Load photo
            if (student.PassportPhoto != null && student.PassportPhoto.Length > 0)
            {
                using var stream = new MemoryStream(student.PassportPhoto);
                StudentPhoto = new Bitmap(stream);
            }
            else if (!string.IsNullOrEmpty(student.PassportUrl))
            {
                // Try to load from URL via API
                var photoResult = await _services.Data.GetStudentPhotoAsync(RegNo);
                if (photoResult.Success && photoResult.Data != null)
                {
                    using var stream = new MemoryStream(photoResult.Data);
                    StudentPhoto = new Bitmap(stream);
                }
            }

            // Check existing enrollment
            var enrollmentStatus = await _services.Data.GetEnrollmentStatusAsync(RegNo);
            if (enrollmentStatus.Success && enrollmentStatus.Data?.IsEnrolled == true)
            {
                StatusMessage = $"Student already enrolled with {enrollmentStatus.Data.EnrolledFingerCount} fingers. You can add more or re-enroll.";
            }
            else
            {
                StatusMessage = "Student found. Select a finger and place on scanner.";
            }

            IsStudentLoaded = true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            IsStudentLoaded = false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task CaptureFingerAsync()
    {
        if (!IsStudentLoaded) return;

        var slot = FingerSlots.FirstOrDefault(s => s.Name == SelectedFinger);
        if (slot == null) return;

        IsCapturing = true;
        StatusMessage = $"Place your {slot.DisplayName} on the scanner...";

        try
        {
            // Initialize fingerprint device if needed
            var initResult = await _services.Fingerprint.InitializeAsync();
            if (!initResult)
            {
                StatusMessage = "Failed to initialize fingerprint scanner";
                return;
            }

            // Capture fingerprint
            var captureResult = await _services.Fingerprint.CaptureAsync();
            if (!captureResult.Success || captureResult.SampleData == null)
            {
                StatusMessage = captureResult.Message ?? "Capture failed. Please try again.";
                return;
            }

            // Create template from captured sample
            var templateData = await _services.Fingerprint.CreateTemplateAsync(captureResult.SampleData);
            if (templateData == null || templateData.Length == 0)
            {
                StatusMessage = "Failed to create fingerprint template. Please try again.";
                return;
            }

            // Store the template
            var template = new FingerprintTemplate
            {
                Finger = slot.Name,
                FingerIndex = slot.Index,
                TemplateData = templateData
            };

            // Remove existing template for this finger if any
            var existing = CapturedTemplates.FirstOrDefault(t => t.FingerIndex == slot.Index);
            if (existing != null)
            {
                CapturedTemplates.Remove(existing);
            }

            CapturedTemplates.Add(template);

            // Update UI
            slot.IsEnrolled = true;
            EnrolledCount = CapturedTemplates.Count;

            // Display captured image if available
            if (captureResult.ImageData != null && captureResult.ImageData.Length > 0)
            {
                using var stream = new MemoryStream(captureResult.ImageData);
                CurrentFingerprintImage = new Bitmap(stream);
            }

            StatusMessage = $"{slot.DisplayName} captured successfully! ({EnrolledCount}/{_minimumFingers})";

            // Auto-select next unenrolled finger
            SelectNextFinger();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Capture error: {ex.Message}";
        }
        finally
        {
            IsCapturing = false;
        }
    }

    private async Task SubmitEnrollmentAsync()
    {
        if (CapturedTemplates.Count < _minimumFingers)
        {
            StatusMessage = $"Please enroll at least {_minimumFingers} fingers";
            return;
        }

        IsLoading = true;
        StatusMessage = "Submitting enrollment...";

        try
        {
            var request = new EnrollmentRequest
            {
                RegNo = RegNo,
                Name = StudentName,
                ClassName = StudentClass,
                Templates = CapturedTemplates.ToList(),
                EnrolledAt = DateTime.UtcNow
            };

            var result = await _services.Data.SubmitEnrollmentAsync(request);

            if (result.Success)
            {
                StatusMessage = $"Enrollment successful! {CapturedTemplates.Count} fingers enrolled.";

                // Show sync status for hybrid modes
                if (_services.Data.Mode == SyncMode.OfflineFirst ||
                    (_services.Data.Mode == SyncMode.OnlineFirst && !_services.Data.IsOnline))
                {
                    StatusMessage += " (Will sync when online)";
                }
            }
            else
            {
                StatusMessage = result.Message ?? "Enrollment failed";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void Clear()
    {
        RegNo = string.Empty;
        StudentName = string.Empty;
        StudentClass = string.Empty;
        StudentPhoto = null;
        CurrentFingerprintImage = null;
        IsStudentLoaded = false;
        CapturedTemplates.Clear();
        EnrolledCount = 0;

        foreach (var slot in FingerSlots)
        {
            slot.IsEnrolled = false;
        }

        SelectedFinger = "LeftThumb";
        StatusMessage = "Enter registration number to begin";
    }

    private void SelectNextFinger()
    {
        // Priority: Left Thumb, Right Thumb, then others
        var priority = new[] { "LeftThumb", "RightThumb", "LeftIndex", "RightIndex", "LeftMiddle", "RightMiddle" };

        foreach (var fingerName in priority)
        {
            var slot = FingerSlots.FirstOrDefault(s => s.Name == fingerName && !s.IsEnrolled);
            if (slot != null)
            {
                SelectedFinger = slot.Name;
                return;
            }
        }

        // Select any unenrolled finger
        var anyUnenrolled = FingerSlots.FirstOrDefault(s => !s.IsEnrolled);
        if (anyUnenrolled != null)
        {
            SelectedFinger = anyUnenrolled.Name;
        }
    }
}

/// <summary>
/// Represents a finger slot for enrollment UI.
/// </summary>
public class FingerSlotViewModel : INotifyPropertyChanged
{
    private bool _isEnrolled;

    public FingerSlotViewModel(string name, string displayName, int index)
    {
        Name = name;
        DisplayName = displayName;
        Index = index;
    }

    public string Name { get; }
    public string DisplayName { get; }
    public int Index { get; }

    public bool IsEnrolled
    {
        get => _isEnrolled;
        set
        {
            if (_isEnrolled != value)
            {
                _isEnrolled = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEnrolled)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
            }
        }
    }

    public string Status => IsEnrolled ? "Enrolled" : "Not enrolled";

    public event PropertyChangedEventHandler? PropertyChanged;
}
