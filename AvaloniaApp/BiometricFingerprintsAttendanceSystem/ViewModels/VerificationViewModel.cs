using BiometricFingerprintsAttendanceSystem.Services;
using BiometricFingerprintsAttendanceSystem.Services.Api;

namespace BiometricFingerprintsAttendanceSystem.ViewModels;

public sealed class VerificationViewModel : ViewModelBase
{
    private readonly IServiceRegistry _services;
    private string _regNo = string.Empty;
    private string _name = string.Empty;
    private string _statusMessage = string.Empty;
    private string _falseAcceptRate = string.Empty;
    private string _hand = "Left";
    private int _fingerIndex = 1;
    private string _templateBase64 = string.Empty;

    public VerificationViewModel(IServiceRegistry services)
    {
        _services = services;
        IdentifyCommand = new AsyncRelayCommand(IdentifyAsync, CanIdentify);
        TimeInCommand = new AsyncRelayCommand(TimeInAsync, CanTimeIn);
    }

    public string RegNo
    {
        get => _regNo;
        private set => SetField(ref _regNo, value);
    }

    public string Name
    {
        get => _name;
        private set
        {
            if (SetField(ref _name, value))
            {
                TimeInCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string Hand
    {
        get => _hand;
        set => SetField(ref _hand, value);
    }

    public int FingerIndex
    {
        get => _fingerIndex;
        set => SetField(ref _fingerIndex, Math.Clamp(value, 1, 5));
    }

    public string TemplateBase64
    {
        get => _templateBase64;
        set
        {
            if (SetField(ref _templateBase64, value))
            {
                IdentifyCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string FalseAcceptRate
    {
        get => _falseAcceptRate;
        set => SetField(ref _falseAcceptRate, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    public AsyncRelayCommand IdentifyCommand { get; }
    public AsyncRelayCommand TimeInCommand { get; }

    private bool CanIdentify() => !string.IsNullOrWhiteSpace(TemplateBase64);
    private bool CanTimeIn() => !string.IsNullOrWhiteSpace(RegNo) && !string.IsNullOrWhiteSpace(Name);

    private async Task IdentifyAsync()
    {
        StatusMessage = "Identifying...";
        var profile = await _services.Api.IdentifyAsync(new IdentifyRequest
        {
            Hand = Hand,
            FingerIndex = FingerIndex,
            TemplateBase64 = TemplateBase64.Trim()
        });

        if (profile is null)
        {
            StatusMessage = "No match found.";
            RegNo = string.Empty;
            Name = string.Empty;
            return;
        }

        RegNo = profile.RegNo;
        Name = profile.Name;
        StatusMessage = "Student identified.";
    }

    private async Task TimeInAsync()
    {
        var date = DateTime.Now.ToShortDateString();
        var hasTimeIn = await _services.Attendance.HasTimeInAsync(RegNo.Trim(), date);
        if (hasTimeIn)
        {
            StatusMessage = "Duplicate time-in detected.";
            return;
        }

        await _services.Attendance.InsertTimeInAsync(RegNo.Trim(), Name.Trim(), date, DateTime.Now.DayOfWeek.ToString(), DateTime.Now.ToShortTimeString());
        StatusMessage = "Time-in recorded.";
    }
}
