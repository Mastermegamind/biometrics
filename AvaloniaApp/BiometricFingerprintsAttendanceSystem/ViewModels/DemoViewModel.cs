using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using BiometricFingerprintsAttendanceSystem.Models;
using BiometricFingerprintsAttendanceSystem.Services;
using BiometricFingerprintsAttendanceSystem.Services.Fingerprint;

namespace BiometricFingerprintsAttendanceSystem.ViewModels;

public sealed class DemoViewModel : ViewModelBase
{
    private readonly IServiceRegistry _services;
    private string _statusMessage = string.Empty;
    private string _statusType = "info"; // info, success, error
    private bool _isEnrolled;
    private string? _lastClockIn;
    private string? _lastClockOut;
    private bool _isBusy;
    private string _logOutput = string.Empty;

    // Demo student info from config
    private readonly string _demoRegNo;
    private readonly string _demoName;
    private readonly string _demoClass;

    // In-memory demo data
    private readonly List<DemoFingerprintTemplate> _enrolledTemplates = new();
    private readonly ObservableCollection<DemoAttendanceRecord> _attendanceRecords = new();
    private readonly List<string> _logLines = new();
    private readonly ObservableCollection<DemoFingerSlot> _rightHandSlots = new();
    private readonly ObservableCollection<DemoFingerSlot> _leftHandSlots = new();
    private readonly string _fingerprintImageRoot;

    public DemoViewModel(IServiceRegistry services)
    {
        _services = services;

        // Get demo student info from config
        _demoRegNo = services.AppState.Config.DemoStudentRegNo;
        _demoName = services.AppState.Config.DemoStudentName;
        _demoClass = services.AppState.Config.DemoStudentClass;

        _fingerprintImageRoot = Path.Combine(
            Directory.GetCurrentDirectory(),
            "AvaloniaApp",
            "BiometricFingerprintsAttendanceSystem",
            "FingerprintImages");

        // Commands
        EnrollCommand = new AsyncRelayCommand(EnrollAsync, () => !IsBusy && !HasAllFingers);
        ClockInCommand = new AsyncRelayCommand(ClockInAsync, () => !IsBusy && HasAllFingers && CanClockIn);
        ClockOutCommand = new AsyncRelayCommand(ClockOutAsync, () => !IsBusy && HasAllFingers && CanClockOut);
        ResetDemoCommand = new RelayCommand(ResetDemo);

        SeedFingerSlots();
        SetStatus($"Demo Mode Active. Student: {_demoName} ({_demoRegNo})", "info");
        _ = LoadTemplatesAsync();
    }

    public string Title => "Demo Testing Mode";
    public string DemoRegNo => _demoRegNo;
    public string DemoName => _demoName;
    public string DemoClass => _demoClass;

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    public string StatusType
    {
        get => _statusType;
        private set => SetField(ref _statusType, value);
    }

    public bool IsEnrolled
    {
        get => _isEnrolled;
        private set
        {
            if (SetField(ref _isEnrolled, value))
            {
                RaiseCommandsCanExecuteChanged();
            }
        }
    }

    public string? LastClockIn
    {
        get => _lastClockIn;
        private set
        {
            if (SetField(ref _lastClockIn, value))
            {
                RaisePropertyChanged(nameof(CanClockIn));
                RaisePropertyChanged(nameof(CanClockOut));
                RaiseCommandsCanExecuteChanged();
            }
        }
    }

    public string? LastClockOut
    {
        get => _lastClockOut;
        private set
        {
            if (SetField(ref _lastClockOut, value))
            {
                RaisePropertyChanged(nameof(CanClockIn));
                RaisePropertyChanged(nameof(CanClockOut));
                RaiseCommandsCanExecuteChanged();
            }
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetField(ref _isBusy, value))
            {
                RaiseCommandsCanExecuteChanged();
            }
        }
    }

    public bool CanClockIn => HasAllFingers && string.IsNullOrEmpty(LastClockIn);
    public bool CanClockOut => HasAllFingers && !string.IsNullOrEmpty(LastClockIn) && string.IsNullOrEmpty(LastClockOut);

    public int EnrolledTemplateCount => _enrolledTemplates.Count;
    public bool HasAllFingers => EnrolledTemplateCount >= 10;
    public ObservableCollection<DemoAttendanceRecord> AttendanceRecords => _attendanceRecords;
    public ObservableCollection<DemoFingerSlot> RightHandSlots => _rightHandSlots;
    public ObservableCollection<DemoFingerSlot> LeftHandSlots => _leftHandSlots;

    public string LogOutput
    {
        get => _logOutput;
        private set => SetField(ref _logOutput, value);
    }

    public AsyncRelayCommand EnrollCommand { get; }
    public AsyncRelayCommand ClockInCommand { get; }
    public AsyncRelayCommand ClockOutCommand { get; }
    public RelayCommand ResetDemoCommand { get; }

    private async Task EnrollAsync()
    {
        IsBusy = true;
        SetStatus("Starting fingerprint enrollment...", "info");
        Log("=== ENROLLMENT START ===");

        try
        {
            var fingerprint = _services.Fingerprint;
            var deviceType = fingerprint.GetType().Name;
            var deviceInfo = fingerprint.DeviceInfo;

            await _services.DemoFingerprints.EnsureDemoUserAsync(_demoRegNo, _demoName);

            Log($"Fingerprint service type: {deviceType}");
            Log($"Device available: {fingerprint.IsDeviceAvailable}");
            Log($"Device status: {fingerprint.DeviceStatus}");

            if (deviceInfo != null)
            {
                Log($"Device vendor: {deviceInfo.Vendor}");
                Log($"Device product: {deviceInfo.ProductName}");
                Log($"Device driver: {deviceInfo.Driver}");
                Log($"Device type: {deviceInfo.DeviceType}");
            }
            else
            {
                Log("Device info is NULL");
            }

            if (!fingerprint.IsDeviceAvailable)
            {
                Log("Device not available, attempting initialization...");
                SetStatus("Initializing fingerprint device...", "info");

                var initialized = await fingerprint.InitializeAsync();
                Log($"Initialization result: {initialized}");
                Log($"Device status after init: {fingerprint.DeviceStatus}");

                if (!initialized)
                {
                    var errorMsg = $"Fingerprint device not available. Service: {deviceType}, Status: {fingerprint.DeviceStatus}";
                    Log($"ERROR: {errorMsg}");
                    SetStatus(errorMsg, "error");
                    return;
                }
            }

            SetStatus("Place your finger on the scanner...", "info");
            if (fingerprint is LinuxFprintdService)
            {
                Log("Linux fprintd detected. Calling EnrollAsync...");
                var enroll = await fingerprint.EnrollAsync(Environment.UserName, FingerPosition.RightThumb);
                Log($"Enroll result - Success: {enroll.Success}, Status: {enroll.Status}, Complete: {enroll.IsComplete}");

                if (!enroll.Success)
                {
                    var errorDetail = $"Enrollment failed - Status: {enroll.Status}, Error: {enroll.ErrorMessage ?? "No error message"}";
                    Log($"ERROR: {errorDetail}");
                    SetStatus(errorDetail, "error");
                    return;
                }

                IsEnrolled = HasAllFingers;
                RaisePropertyChanged(nameof(EnrolledTemplateCount));
                RaisePropertyChanged(nameof(HasAllFingers));
                RaiseCommandsCanExecuteChanged();
                UpdateSlotStates(new List<DemoFingerprintRecord>
                {
                    new DemoFingerprintRecord
                    {
                        FingerIndex = 1,
                        TemplateBase64 = string.Empty,
                        CreatedAt = DateTime.Now
                    }
                });
                SetStatus("Enrollment successful! Finger stored in system (fprintd). Continue until 10 fingers are enrolled.", "success");
                return;
            }

            Log("Calling CaptureAsync...");

            // Capture fingerprint
            var result = await fingerprint.CaptureAsync();

            Log($"Capture result - Success: {result.Success}");
            Log($"Capture result - Status: {result.Status}");
            Log($"Capture result - Quality: {result.Quality}");
            Log($"Capture result - ErrorMessage: {result.ErrorMessage ?? "(null)"}");
            Log($"Capture result - SampleData: {(result.SampleData != null ? $"{result.SampleData.Length} bytes" : "null")}");
            Log($"Capture result - TemplateData: {(result.TemplateData != null ? $"{result.TemplateData.Length} bytes" : "null")}");

            if (!result.Success)
            {
                var errorDetail = $"Capture failed - Status: {result.Status}, Error: {result.ErrorMessage ?? "No error message"}, Quality: {result.Quality}";
                Log($"ERROR: {errorDetail}");
                SetStatus(errorDetail, "error");
                return;
            }

            // Create template
            byte[]? template = result.TemplateData;
            if (template == null && result.SampleData != null)
            {
                Log("TemplateData is null, creating template from SampleData...");
                template = await fingerprint.CreateTemplateAsync(result.SampleData);
                Log($"Created template: {(template != null ? $"{template.Length} bytes" : "null")}");
            }

            if (template == null || template.Length == 0)
            {
                Log("ERROR: Failed to create fingerprint template - template is null or empty");
                SetStatus("Failed to create fingerprint template. SampleData may be empty.", "error");
                return;
            }

            var base64Template = Convert.ToBase64String(template);
            var existing = await _services.DemoFingerprints.GetTemplatesAsync(_demoRegNo);
            var slot = GetNextFingerIndex(existing);

            var existingSlot = existing.FirstOrDefault(r => r.FingerIndex == slot);
            if (existingSlot?.ImageName is not null)
            {
                var oldPath = Path.Combine(_fingerprintImageRoot, existingSlot.ImageName);
                if (File.Exists(oldPath))
                {
                    File.Delete(oldPath);
                }
            }

            var imageName = await SaveFingerprintImageAsync(slot, null);
            await _services.DemoFingerprints.UpsertTemplateAsync(_demoRegNo, slot, base64Template, imageName);
            await LoadTemplatesAsync();

            IsEnrolled = HasAllFingers;
            RaisePropertyChanged(nameof(EnrolledTemplateCount));
            RaisePropertyChanged(nameof(HasAllFingers));
            RaiseCommandsCanExecuteChanged();

            Log($"SUCCESS: Template stored in slot {slot} ({template.Length} bytes)");
            SetStatus($"Enrollment successful! Template saved in slot {slot}. ({EnrolledTemplateCount}/10)", "success");
        }
        catch (Exception ex)
        {
            Log($"EXCEPTION: {ex.GetType().Name}: {ex.Message}");
            Log($"Stack trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Log($"Inner exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            }
            SetStatus($"Enrollment error: {ex.Message}", "error");
        }
        finally
        {
            Log("=== ENROLLMENT END ===");
            IsBusy = false;
        }
    }

    private void Log(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var logLine = $"[{timestamp}] {message}";
        Console.WriteLine($"[DEMO] {logLine}");

        _logLines.Add(logLine);
        // Keep only last 50 lines
        while (_logLines.Count > 50)
        {
            _logLines.RemoveAt(0);
        }
        LogOutput = string.Join("\n", _logLines);
    }

    private void ClearLogs()
    {
        _logLines.Clear();
        LogOutput = string.Empty;
    }

    private async Task ClockInAsync()
    {
        IsBusy = true;
        SetStatus("Scanning fingerprint for clock-in...", "info");

        try
        {
            var fingerprint = _services.Fingerprint;

            if (!fingerprint.IsDeviceAvailable)
            {
                var initialized = await fingerprint.InitializeAsync();
                if (!initialized)
                {
                    SetStatus("Fingerprint device not available.", "error");
                    return;
                }
            }

            SetStatus("Place your finger on the scanner...", "info");

            if (fingerprint is LinuxFprintdService)
            {
                var verified = await fingerprint.VerifyUserAsync(Environment.UserName);
                if (!verified)
                {
                    SetStatus("Fingerprint verification failed.", "error");
                    return;
                }
            }
            else
            {
                // Capture fingerprint
                var result = await fingerprint.CaptureAsync();

                if (!result.Success)
                {
                    SetStatus($"Capture failed: {result.ErrorMessage ?? result.Status.ToString()}", "error");
                    return;
                }

                var sample = result.SampleData ?? result.TemplateData;
                if (sample is null || sample.Length == 0)
                {
                    SetStatus("Capture failed: empty sample data.", "error");
                    return;
                }

                var templates = await _services.DemoFingerprints.GetTemplatesAsync(_demoRegNo);
                if (templates.Count == 0)
                {
                    SetStatus("No demo templates stored. Please enroll first.", "error");
                    return;
                }

                var matched = await VerifyAgainstTemplatesAsync(fingerprint, sample, templates);
                if (!matched)
                {
                    SetStatus("Fingerprint not recognized.", "error");
                    return;
                }
            }

            // In demo mode, we verify the capture was successful and record clock-in
            // Real verification would compare against stored templates

            var now = DateTime.Now;
            LastClockIn = now.ToString("hh:mm:ss tt");

            var record = new DemoAttendanceRecord
            {
                RegNo = _demoRegNo,
                Name = _demoName,
                Date = now.ToString("yyyy-MM-dd"),
                Day = now.DayOfWeek.ToString(),
                TimeIn = LastClockIn,
                TimeOut = null
            };

            _attendanceRecords.Insert(0, record);

            SetStatus($"Clock-in recorded at {LastClockIn}. Welcome, {_demoName}!", "success");
        }
        catch (Exception ex)
        {
            SetStatus($"Clock-in error: {ex.Message}", "error");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ClockOutAsync()
    {
        IsBusy = true;
        SetStatus("Scanning fingerprint for clock-out...", "info");

        try
        {
            var fingerprint = _services.Fingerprint;

            if (!fingerprint.IsDeviceAvailable)
            {
                var initialized = await fingerprint.InitializeAsync();
                if (!initialized)
                {
                    SetStatus("Fingerprint device not available.", "error");
                    return;
                }
            }

            SetStatus("Place your finger on the scanner...", "info");

            if (fingerprint is LinuxFprintdService)
            {
                var verified = await fingerprint.VerifyUserAsync(Environment.UserName);
                if (!verified)
                {
                    SetStatus("Fingerprint verification failed.", "error");
                    return;
                }
            }
            else
            {
                // Capture fingerprint
                var result = await fingerprint.CaptureAsync();

                if (!result.Success)
                {
                    SetStatus($"Capture failed: {result.ErrorMessage ?? result.Status.ToString()}", "error");
                    return;
                }

                var sample = result.SampleData ?? result.TemplateData;
                if (sample is null || sample.Length == 0)
                {
                    SetStatus("Capture failed: empty sample data.", "error");
                    return;
                }

                var templates = await _services.DemoFingerprints.GetTemplatesAsync(_demoRegNo);
                if (templates.Count == 0)
                {
                    SetStatus("No demo templates stored. Please enroll first.", "error");
                    return;
                }

                var matched = await VerifyAgainstTemplatesAsync(fingerprint, sample, templates);
                if (!matched)
                {
                    SetStatus("Fingerprint not recognized.", "error");
                    return;
                }
            }

            var now = DateTime.Now;
            LastClockOut = now.ToString("hh:mm:ss tt");

            // Update the latest attendance record
            if (_attendanceRecords.Count > 0)
            {
                _attendanceRecords[0].TimeOut = LastClockOut;
            }

            SetStatus($"Clock-out recorded at {LastClockOut}. Goodbye, {_demoName}!", "success");
        }
        catch (Exception ex)
        {
            SetStatus($"Clock-out error: {ex.Message}", "error");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ResetDemo()
    {
        _enrolledTemplates.Clear();
        _attendanceRecords.Clear();
        IsEnrolled = false;
        LastClockIn = null;
        LastClockOut = null;

        _ = _services.DemoFingerprints.ClearTemplatesAsync(_demoRegNo);
        _ = LoadTemplatesAsync();
        UpdateSlotStates(Array.Empty<DemoFingerprintRecord>());

        RaisePropertyChanged(nameof(EnrolledTemplateCount));
        RaisePropertyChanged(nameof(HasAllFingers));
        RaiseCommandsCanExecuteChanged();

        SetStatus("Demo reset. You can start fresh with enrollment.", "info");
    }

    private async Task LoadTemplatesAsync()
    {
        try
        {
            var templates = await _services.DemoFingerprints.GetTemplatesAsync(_demoRegNo);
            _enrolledTemplates.Clear();
            foreach (var template in templates)
            {
                byte[] data;
                try
                {
                    data = Convert.FromBase64String(template.TemplateBase64);
                }
                catch
                {
                    continue;
                }

                _enrolledTemplates.Add(new DemoFingerprintTemplate
                {
                    Hand = "Right",
                    FingerIndex = template.FingerIndex,
                    TemplateData = data,
                    CapturedAt = template.UpdatedAt ?? template.CreatedAt
                });
            }

            IsEnrolled = HasAllFingers;
            RaisePropertyChanged(nameof(EnrolledTemplateCount));
            RaisePropertyChanged(nameof(HasAllFingers));
            RaiseCommandsCanExecuteChanged();
            UpdateSlotStates(templates);
        }
        catch
        {
            // ignore demo load errors
        }
    }

    private void SeedFingerSlots()
    {
        _rightHandSlots.Clear();
        _leftHandSlots.Clear();

        _rightHandSlots.Add(new DemoFingerSlot(1, "Right Thumb"));
        _rightHandSlots.Add(new DemoFingerSlot(2, "Right Index"));
        _rightHandSlots.Add(new DemoFingerSlot(3, "Right Middle"));
        _rightHandSlots.Add(new DemoFingerSlot(4, "Right Ring"));
        _rightHandSlots.Add(new DemoFingerSlot(5, "Right Little"));

        _leftHandSlots.Add(new DemoFingerSlot(6, "Left Thumb"));
        _leftHandSlots.Add(new DemoFingerSlot(7, "Left Index"));
        _leftHandSlots.Add(new DemoFingerSlot(8, "Left Middle"));
        _leftHandSlots.Add(new DemoFingerSlot(9, "Left Ring"));
        _leftHandSlots.Add(new DemoFingerSlot(10, "Left Little"));
    }

    private void UpdateSlotStates(IReadOnlyList<DemoFingerprintRecord> records)
    {
        var enrolled = records.Select(r => r.FingerIndex).ToHashSet();
        var imageMap = records
            .Where(r => !string.IsNullOrWhiteSpace(r.ImageName))
            .ToDictionary(r => r.FingerIndex, r => r.ImageName!);

        foreach (var slot in _rightHandSlots)
        {
            slot.IsEnrolled = enrolled.Contains(slot.FingerIndex);
            slot.ImagePath = ResolveImagePath(imageMap, slot.FingerIndex);
        }

        foreach (var slot in _leftHandSlots)
        {
            slot.IsEnrolled = enrolled.Contains(slot.FingerIndex);
            slot.ImagePath = ResolveImagePath(imageMap, slot.FingerIndex);
        }
    }

    private string? ResolveImagePath(IReadOnlyDictionary<int, string> imageMap, int fingerIndex)
    {
        if (!imageMap.TryGetValue(fingerIndex, out var name))
        {
            return null;
        }

        var path = Path.Combine(_fingerprintImageRoot, name);
        return File.Exists(path) ? path : null;
    }

    private async Task<string?> SaveFingerprintImageAsync(int fingerIndex, byte[]? pngBytes)
    {
        if (pngBytes is null || pngBytes.Length == 0)
        {
            return null;
        }

        Directory.CreateDirectory(_fingerprintImageRoot);
        var fileName = $"{_demoRegNo}_finger{fingerIndex}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        var path = Path.Combine(_fingerprintImageRoot, fileName);
        await File.WriteAllBytesAsync(path, pngBytes);
        return fileName;
    }

    private static int GetNextFingerIndex(IReadOnlyList<DemoFingerprintRecord> records)
    {
        if (records.Count < 10)
        {
            for (var i = 1; i <= 10; i++)
            {
                if (!records.Any(r => r.FingerIndex == i))
                {
                    return i;
                }
            }
        }

        var oldest = records
            .OrderBy(r => r.UpdatedAt ?? r.CreatedAt)
            .First();

        return oldest.FingerIndex;
    }

    private static async Task<bool> VerifyAgainstTemplatesAsync(
        IFingerprintService fingerprint,
        byte[] sample,
        IReadOnlyList<DemoFingerprintRecord> templates)
    {
        foreach (var template in templates)
        {
            byte[] templateBytes;
            try
            {
                templateBytes = Convert.FromBase64String(template.TemplateBase64);
            }
            catch
            {
                continue;
            }

            var result = await fingerprint.VerifyAsync(sample, templateBytes);
            if (result.IsMatch)
            {
                return true;
            }
        }

        return false;
    }

    private void SetStatus(string message, string type)
    {
        StatusMessage = message;
        StatusType = type;
    }

    private void RaiseCommandsCanExecuteChanged()
    {
        EnrollCommand.RaiseCanExecuteChanged();
        ClockInCommand.RaiseCanExecuteChanged();
        ClockOutCommand.RaiseCanExecuteChanged();
    }
}

public sealed class DemoFingerprintTemplate
{
    public required string Hand { get; set; }
    public required int FingerIndex { get; set; }
    public required byte[] TemplateData { get; set; }
    public DateTime CapturedAt { get; set; }
}

public sealed class DemoFingerSlot : ViewModelBase
{
    private bool _isEnrolled;
    private string? _imagePath;

    public DemoFingerSlot(int fingerIndex, string label)
    {
        FingerIndex = fingerIndex;
        Label = label;
    }

    public int FingerIndex { get; }
    public string Label { get; }

    public bool IsEnrolled
    {
        get => _isEnrolled;
        set
        {
            if (SetField(ref _isEnrolled, value))
            {
                RaisePropertyChanged(nameof(StatusText));
            }
        }
    }

    public string? ImagePath
    {
        get => _imagePath;
        set
        {
            if (SetField(ref _imagePath, value))
            {
                RaisePropertyChanged(nameof(HasImage));
            }
        }
    }

    public bool HasImage => !string.IsNullOrWhiteSpace(ImagePath);

    public string StatusText => IsEnrolled ? "Enrolled" : "Empty";
}

public sealed class DemoAttendanceRecord : ViewModelBase
{
    private string? _timeOut;

    public required string RegNo { get; set; }
    public required string Name { get; set; }
    public required string Date { get; set; }
    public required string Day { get; set; }
    public required string TimeIn { get; set; }

    public string? TimeOut
    {
        get => _timeOut;
        set => SetField(ref _timeOut, value);
    }

    public string Duration
    {
        get
        {
            if (string.IsNullOrEmpty(TimeIn) || string.IsNullOrEmpty(TimeOut))
                return "—";

            if (DateTime.TryParse(TimeIn, out var inTime) && DateTime.TryParse(TimeOut, out var outTime))
            {
                var duration = outTime - inTime;
                return $"{duration.Hours}h {duration.Minutes}m";
            }
            return "—";
        }
    }
}
