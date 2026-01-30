using BiometricFingerprintsAttendanceSystem.Models;
using BiometricFingerprintsAttendanceSystem.Services;

namespace BiometricFingerprintsAttendanceSystem.ViewModels;

public sealed class RegisterViewModel : ViewModelBase
{
    private readonly IServiceRegistry _services;
    private string _regNo = string.Empty;
    private string _name = string.Empty;
    private string _faculty = string.Empty;
    private string _department = string.Empty;
    private string _bloodGroup = string.Empty;
    private string _gradYear = string.Empty;
    private string _gender = string.Empty;
    private byte[]? _passportImage;
    private string _statusMessage = string.Empty;

    public RegisterViewModel(IServiceRegistry services)
    {
        _services = services;
        SubmitCommand = new AsyncRelayCommand(SubmitAsync, CanSubmit);
    }

    public string RegNo
    {
        get => _regNo;
        set
        {
            if (SetField(ref _regNo, value))
            {
                SubmitCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string Name
    {
        get => _name;
        set
        {
            if (SetField(ref _name, value))
            {
                SubmitCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string Faculty
    {
        get => _faculty;
        set => SetField(ref _faculty, value);
    }

    public string Department
    {
        get => _department;
        set
        {
            if (SetField(ref _department, value))
            {
                SubmitCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string BloodGroup
    {
        get => _bloodGroup;
        set => SetField(ref _bloodGroup, value);
    }

    public string GradYear
    {
        get => _gradYear;
        set => SetField(ref _gradYear, value);
    }

    public string Gender
    {
        get => _gender;
        set => SetField(ref _gender, value);
    }

    public byte[]? PassportImage
    {
        get => _passportImage;
        set
        {
            if (SetField(ref _passportImage, value))
            {
                SubmitCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    public AsyncRelayCommand SubmitCommand { get; }

    private bool CanSubmit()
    {
        return !string.IsNullOrWhiteSpace(RegNo)
            && !string.IsNullOrWhiteSpace(Name)
            && !string.IsNullOrWhiteSpace(Department)
            && PassportImage is not null;
    }

    private async Task SubmitAsync()
    {
        StatusMessage = "Saving student...";
        var exists = await _services.Students.StudentExistsAsync(Name, RegNo, Department);
        if (exists)
        {
            StatusMessage = "Duplicate record detected.";
            return;
        }

        var student = new Student
        {
            RegNo = RegNo.Trim(),
            Name = Name.Trim(),
            Faculty = Faculty.Trim(),
            Department = Department.Trim(),
            BloodGroup = BloodGroup.Trim(),
            GradYear = GradYear.Trim(),
            Gender = Gender.Trim(),
            PassportImage = PassportImage ?? Array.Empty<byte>()
        };

        await _services.Students.CreateAsync(student);
        StatusMessage = "Student saved.";
    }
}
