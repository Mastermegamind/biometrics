using System.Collections.ObjectModel;
using System.Text;
using BiometricFingerprintsAttendanceSystem.Services;
using BiometricFingerprintsAttendanceSystem.Services.Api;

namespace BiometricFingerprintsAttendanceSystem.ViewModels;

public sealed class EnrollmentViewModel : ViewModelBase
{
    private readonly IServiceRegistry _services;
    private string _regNo = string.Empty;
    private string _studentName = string.Empty;
    private string _className = string.Empty;
    private string _email = string.Empty;
    private string _phone = string.Empty;
    private string _passport = string.Empty;
    private string _statusMessage = string.Empty;
    private bool _studentConfirmed;
    private bool _allowReEnroll;
    private string _selectedHand = "Left";
    private int _selectedFingerIndex = 1;
    private string _templateBase64 = string.Empty;
    private FingerprintTemplatePayload? _selectedTemplate;

    public EnrollmentViewModel(IServiceRegistry services)
    {
        _services = services;
        _services.AppState.UserTypeChanged += OnUserTypeChanged;
        Templates = new ObservableCollection<FingerprintTemplatePayload>();
        LookupCommand = new AsyncRelayCommand(LookupStudentAsync, CanLookup);
        ConfirmCommand = new RelayCommand(ConfirmStudent, CanConfirm);
        AddTemplateCommand = new RelayCommand(AddTemplate, CanAddTemplate);
        RemoveTemplateCommand = new RelayCommand(RemoveSelectedTemplate, CanRemoveTemplate);
        SubmitCommand = new AsyncRelayCommand(SubmitAsync, CanSubmit);
    }

    public string RegNo
    {
        get => _regNo;
        set
        {
            if (SetField(ref _regNo, value))
            {
                LookupCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string StudentName
    {
        get => _studentName;
        private set => SetField(ref _studentName, value);
    }

    public string ClassName
    {
        get => _className;
        private set => SetField(ref _className, value);
    }

    public string Email
    {
        get => _email;
        private set => SetField(ref _email, value);
    }

    public string Phone
    {
        get => _phone;
        private set => SetField(ref _phone, value);
    }

    public string Passport
    {
        get => _passport;
        private set => SetField(ref _passport, value);
    }

    public bool StudentConfirmed
    {
        get => _studentConfirmed;
        private set
        {
            if (SetField(ref _studentConfirmed, value))
            {
                SubmitCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool AllowReEnroll
    {
        get => _allowReEnroll;
        set
        {
            if (SetField(ref _allowReEnroll, value))
            {
                SubmitCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string SelectedHand
    {
        get => _selectedHand;
        set => SetField(ref _selectedHand, value);
    }

    public int SelectedFingerIndex
    {
        get => _selectedFingerIndex;
        set => SetField(ref _selectedFingerIndex, Math.Clamp(value, 1, 5));
    }

    public string TemplateBase64
    {
        get => _templateBase64;
        set
        {
            if (SetField(ref _templateBase64, value))
            {
                AddTemplateCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public FingerprintTemplatePayload? SelectedTemplate
    {
        get => _selectedTemplate;
        set
        {
            if (SetField(ref _selectedTemplate, value))
            {
                RemoveTemplateCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    public ObservableCollection<FingerprintTemplatePayload> Templates { get; }

    public AsyncRelayCommand LookupCommand { get; }
    public RelayCommand ConfirmCommand { get; }
    public RelayCommand AddTemplateCommand { get; }
    public RelayCommand RemoveTemplateCommand { get; }
    public AsyncRelayCommand SubmitCommand { get; }

    public bool IsAdmin => _services.AppState != null && string.Equals(_services.AppState.CurrentUserType, "Administrator", StringComparison.OrdinalIgnoreCase);

    private bool CanLookup() => IsAdmin && !string.IsNullOrWhiteSpace(RegNo);
    private bool CanConfirm() => !string.IsNullOrWhiteSpace(StudentName);
    private bool CanAddTemplate() => StudentConfirmed && !string.IsNullOrWhiteSpace(TemplateBase64);
    private bool CanRemoveTemplate() => SelectedTemplate is not null;

    private async Task LookupStudentAsync()
    {
        if (!IsAdmin)
        {
            StatusMessage = "Enrollment is restricted to administrators.";
            return;
        }

        ResetStudent();
        StatusMessage = "Looking up student...";
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var profile = await _services.Api.GetStudentByRegAsync(RegNo.Trim(), cts.Token);
            if (profile is null)
            {
                StatusMessage = "Student not found via API.";
                return;
            }

            StudentName = profile.Name;
            ClassName = profile.ClassName;
            Email = profile.Email;
            Phone = profile.Phone;
            Passport = profile.Passport;
            StatusMessage = "Student found. Confirm details before enrolling.";
        }
        catch (TaskCanceledException)
        {
            StatusMessage = "API request timed out. Check network connection.";
        }
        catch
        {
            StatusMessage = "API request failed. Please try again.";
        }
    }

    private void ConfirmStudent()
    {
        StudentConfirmed = true;
        StatusMessage = "Student confirmed. Capture fingerprints.";
    }

    private void AddTemplate()
    {
        if (!IsValidBase64(TemplateBase64))
        {
            StatusMessage = "Invalid Base64 template.";
            return;
        }

        if (SelectedFingerIndex != 1 && SelectedFingerIndex != 2)
        {
            StatusMessage = "Finger index must be 1 (thumb) or 2 (index).";
            return;
        }

        Templates.Add(new FingerprintTemplatePayload
        {
            Hand = SelectedHand,
            FingerIndex = SelectedFingerIndex,
            TemplateBase64 = TemplateBase64.Trim()
        });

        TemplateBase64 = string.Empty;
        StatusMessage = "Template added.";
        SubmitCommand.RaiseCanExecuteChanged();
    }

    private void RemoveSelectedTemplate()
    {
        if (SelectedTemplate is null)
        {
            return;
        }

        Templates.Remove(SelectedTemplate);
        SelectedTemplate = null;
        SubmitCommand.RaiseCanExecuteChanged();
    }

    private bool CanSubmit()
    {
        if (!StudentConfirmed)
        {
            return false;
        }

        if (!IsAdmin && AllowReEnroll)
        {
            return false;
        }

        return MeetsMinimumFingerCount();
    }

    private async Task SubmitAsync()
    {
        StatusMessage = "Submitting enrollment...";
        var status = await _services.Api.GetEnrollmentStatusAsync(RegNo.Trim());
        if (status?.IsEnrolled == true && !(IsAdmin && AllowReEnroll))
        {
            StatusMessage = "Student already enrolled. Admin override required.";
            return;
        }

        var payload = new EnrollmentSubmission
        {
            RegNo = RegNo.Trim(),
            AdminOverride = IsAdmin && AllowReEnroll,
            Templates = Templates.ToList()
        };

        var result = await _services.Api.SubmitEnrollmentAsync(payload);
        StatusMessage = result.Message;
    }

    private bool MeetsMinimumFingerCount()
    {
        var leftThumb = Templates.Any(t => string.Equals(t.Hand, "Left", StringComparison.OrdinalIgnoreCase) && t.FingerIndex == 1);
        var leftIndex = Templates.Any(t => string.Equals(t.Hand, "Left", StringComparison.OrdinalIgnoreCase) && t.FingerIndex == 2);
        var rightThumb = Templates.Any(t => string.Equals(t.Hand, "Right", StringComparison.OrdinalIgnoreCase) && t.FingerIndex == 1);
        var rightIndex = Templates.Any(t => string.Equals(t.Hand, "Right", StringComparison.OrdinalIgnoreCase) && t.FingerIndex == 2);
        return leftThumb && leftIndex && rightThumb && rightIndex;
    }

    private static bool IsValidBase64(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        value = value.Trim();
        var buffer = new byte[value.Length];
        return Convert.TryFromBase64String(value, buffer, out _);
    }

    private void ResetStudent()
    {
        StudentConfirmed = false;
        StudentName = string.Empty;
        ClassName = string.Empty;
        Email = string.Empty;
        Phone = string.Empty;
        Passport = string.Empty;
        Templates.Clear();
    }

    private void OnUserTypeChanged()
    {
        RaisePropertyChanged(nameof(IsAdmin));
        LookupCommand.RaiseCanExecuteChanged();
        SubmitCommand.RaiseCanExecuteChanged();
    }
}
