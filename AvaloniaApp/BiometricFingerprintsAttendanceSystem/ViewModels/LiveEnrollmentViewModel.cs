using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Net.Http;
using System.Windows.Input;
using Avalonia.Media.Imaging;
using BiometricFingerprintsAttendanceSystem.Services;
using BiometricFingerprintsAttendanceSystem.Services.Data;
using BiometricFingerprintsAttendanceSystem.Services.Fingerprint;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BiometricFingerprintsAttendanceSystem.ViewModels;

public class LiveEnrollmentViewModel : ViewModelBase
{
    private readonly IServiceRegistry _services;
    private readonly int _minimumFingers;
    private readonly ILogger<LiveEnrollmentViewModel> _logger;
    private readonly HttpClient _http;

    private string _regNo = string.Empty;
    private string _studentName = string.Empty;
    private string _studentRegNo = string.Empty;
    private string _studentClass = string.Empty;
    private string _passportUrl = string.Empty;
    private string _renewalDate = string.Empty;
    private string _enrollmentStatus = "Unknown";
    private int _enrollmentFingerCount;
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
        _logger = services.Provider.GetService(typeof(ILogger<LiveEnrollmentViewModel>)) as ILogger<LiveEnrollmentViewModel>
            ?? NullLogger<LiveEnrollmentViewModel>.Instance;
        _http = new HttpClient();

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
        foreach (var slot in FingerSlots)
        {
            slot.PropertyChanged += OnFingerSlotPropertyChanged;
        }
        UpdateSelectedFingerSlot();

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

    public string PassportUrl
    {
        get => _passportUrl;
        set => SetProperty(ref _passportUrl, value);
    }

    public string RenewalDate
    {
        get => _renewalDate;
        set => SetProperty(ref _renewalDate, value);
    }

    public string EnrollmentStatus
    {
        get => _enrollmentStatus;
        set => SetProperty(ref _enrollmentStatus, value);
    }

    public int EnrollmentFingerCount
    {
        get => _enrollmentFingerCount;
        set => SetProperty(ref _enrollmentFingerCount, value);
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
        set
        {
            if (SetProperty(ref _selectedFinger, value))
            {
                UpdateSelectedFingerSlot();
            }
        }
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
        _logger.LogInformation("Live enrollment lookup started for RegNo {RegNo}", RegNo.Trim());

        try
        {
            var result = await _services.Data.GetStudentAsync(RegNo.Trim());

            if (!result.Success || result.Data == null)
            {
                StatusMessage = result.Message ?? "Student not found";
                IsStudentLoaded = false;
                _logger.LogWarning("Live enrollment lookup failed for RegNo {RegNo}: {Message}", RegNo.Trim(), result.Message ?? "Student not found");
                return;
            }

            var student = result.Data;
            StudentName = student.Name;
            StudentRegNo = student.RegNo;
            StudentClass = student.ClassName;
            PassportUrl = student.PassportUrl ?? string.Empty;
            RenewalDate = student.RenewalDate?.ToString("yyyy-MM-dd") ?? string.Empty;

            // Load photo
            if (TryLoadCachedPassport(RegNo))
            {
                _logger.LogInformation("Loaded cached passport image for RegNo {RegNo}", RegNo.Trim());
            }
            else if (student.PassportPhoto != null && student.PassportPhoto.Length > 0)
            {
                var saved = await SavePassportToTempAsync(RegNo, student.PassportPhoto, "photo_bytes");
                if (saved != null)
                {
                    StudentPhoto = new Bitmap(saved);
                }
            }
            else if (!string.IsNullOrEmpty(student.PassportUrl))
            {
                // Try to load from URL via API
                var photoResult = await _services.Data.GetStudentPhotoAsync(RegNo);
                if (photoResult.Success && photoResult.Data != null)
                {
                    var saved = await SavePassportToTempAsync(RegNo, photoResult.Data, "api_photo");
                    if (saved != null)
                    {
                        StudentPhoto = new Bitmap(saved);
                    }
                }
                else
                {
                    _logger.LogWarning("Failed to load student photo for RegNo {RegNo}: {Message}", RegNo.Trim(), photoResult.Message ?? "No photo data");
                    await TryLoadPassportFromUrlAsync(student.PassportUrl);
                }
            }

            // Check existing enrollment
            var enrollmentStatus = await _services.Data.GetEnrollmentStatusAsync(RegNo);
            if (enrollmentStatus.Success && enrollmentStatus.Data?.IsEnrolled == true)
            {
                EnrollmentStatus = "Enrolled";
                EnrollmentFingerCount = enrollmentStatus.Data.EnrolledFingerCount;
                StatusMessage = $"Student already enrolled with {enrollmentStatus.Data.EnrolledFingerCount} fingers. You can add more or re-enroll.";
            }
            else
            {
                EnrollmentStatus = "Not enrolled";
                EnrollmentFingerCount = enrollmentStatus.Data?.EnrolledFingerCount ?? 0;
                StatusMessage = "Student found. Select a finger and place on scanner.";
                if (!enrollmentStatus.Success)
                {
                    _logger.LogWarning("Enrollment status lookup failed for RegNo {RegNo}: {Message}", RegNo.Trim(), enrollmentStatus.Message ?? "Unknown error");
                }
            }

            IsStudentLoaded = true;
            _logger.LogInformation("Live enrollment lookup success for RegNo {RegNo}", RegNo.Trim());
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            IsStudentLoaded = false;
            _logger.LogError(ex, "Live enrollment lookup error for RegNo {RegNo}", RegNo.Trim());
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
        _logger.LogInformation("Live enrollment capture started for RegNo {RegNo}, Finger {Finger}", RegNo.Trim(), slot.Name);

        try
        {
            // Initialize fingerprint device if needed
            var initResult = await _services.Fingerprint.InitializeAsync();
            if (!initResult)
            {
                StatusMessage = "Failed to initialize fingerprint scanner";
                _logger.LogWarning("Live enrollment fingerprint init failed for RegNo {RegNo}", RegNo.Trim());
                return;
            }

            // Capture fingerprint
            var captureResult = await _services.Fingerprint.CaptureAsync();
            if (!captureResult.Success || captureResult.SampleData == null)
            {
                StatusMessage = captureResult.Message ?? "Capture failed. Please try again.";
                _logger.LogWarning("Live enrollment capture failed for RegNo {RegNo}: {Message}", RegNo.Trim(), captureResult.Message ?? "Capture failed");
                return;
            }

            // Create template from captured sample
            var templateData = await _services.Fingerprint.CreateTemplateAsync(captureResult.SampleData);
            if (templateData == null || templateData.Length == 0)
            {
                StatusMessage = "Failed to create fingerprint template. Please try again.";
                _logger.LogWarning("Live enrollment template creation failed for RegNo {RegNo}", RegNo.Trim());
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
            _logger.LogInformation("Live enrollment capture success for RegNo {RegNo}, Finger {Finger}", RegNo.Trim(), slot.Name);

            // Auto-select next unenrolled finger
            SelectNextFinger();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Capture error: {ex.Message}";
            _logger.LogError(ex, "Live enrollment capture error for RegNo {RegNo}", RegNo.Trim());
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
        _logger.LogInformation("Live enrollment submit started for RegNo {RegNo} with {Count} templates", RegNo.Trim(), CapturedTemplates.Count);

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
                _logger.LogInformation("Live enrollment submit success for RegNo {RegNo}", RegNo.Trim());

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
                _logger.LogWarning("Live enrollment submit failed for RegNo {RegNo}: {Message}", RegNo.Trim(), result.Message ?? "Enrollment failed");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            _logger.LogError(ex, "Live enrollment submit error for RegNo {RegNo}", RegNo.Trim());
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
        StudentRegNo = string.Empty;
        StudentClass = string.Empty;
        PassportUrl = string.Empty;
        RenewalDate = string.Empty;
        EnrollmentStatus = "Unknown";
        EnrollmentFingerCount = 0;
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

    private void UpdateSelectedFingerSlot()
    {
        foreach (var slot in FingerSlots)
        {
            slot.IsSelected = string.Equals(slot.Name, SelectedFinger, StringComparison.Ordinal);
        }
    }

    private void OnFingerSlotPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(FingerSlotViewModel.IsSelected))
        {
            return;
        }

        if (sender is FingerSlotViewModel slot && slot.IsSelected)
        {
            SelectedFinger = slot.Name;
        }
    }

    private async Task<bool> TryLoadPassportFromUrlAsync(string url)
    {
        try
        {
            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Passport URL fetch failed {Url} {StatusCode}", url, (int)response.StatusCode);
                return false;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync();
            if (bytes.Length == 0)
            {
                _logger.LogWarning("Passport URL returned empty body {Url}", url);
                return false;
            }

            var saved = await SavePassportToTempAsync(RegNo, bytes, "passport_url");
            if (saved == null)
            {
                return false;
            }

            StudentPhoto = new Bitmap(saved);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Passport URL fetch error {Url}", url);
            return false;
        }
    }

    private static async Task<string?> SavePassportToTempAsync(string regNo, byte[] bytes, string suffix)
    {
        try
        {
            var fileName = $"passport_{SanitizeFileToken(regNo)}_{suffix}.jpg";
            var path = Path.Combine(Path.GetTempPath(), fileName);
            await File.WriteAllBytesAsync(path, bytes);
            return path;
        }
        catch
        {
            return null;
        }
    }

    private bool TryLoadCachedPassport(string regNo)
    {
        var path = GetCachedPassportPath(regNo);
        if (path == null || !File.Exists(path))
        {
            return false;
        }

        try
        {
            var info = new FileInfo(path);
            if (info.Length == 0)
            {
                return false;
            }

            StudentPhoto = new Bitmap(path);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load cached passport image {Path}", path);
            return false;
        }
    }

    private static string? GetCachedPassportPath(string regNo)
    {
        if (string.IsNullOrWhiteSpace(regNo))
        {
            return null;
        }

        var token = SanitizeFileToken(regNo);
        var dir = Path.GetTempPath();
        var existing = Directory.GetFiles(dir, $"passport_{token}_*.jpg")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
        return existing;
    }

    private static string SanitizeFileToken(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
        return cleaned.Replace(' ', '_');
    }
}

/// <summary>
/// Represents a finger slot for enrollment UI.
/// </summary>
public class FingerSlotViewModel : INotifyPropertyChanged
{
    private bool _isEnrolled;
    private bool _isSelected;

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

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
