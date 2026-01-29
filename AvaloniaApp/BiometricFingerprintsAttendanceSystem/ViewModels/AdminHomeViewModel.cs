using BiometricFingerprintsAttendanceSystem.Services;

namespace BiometricFingerprintsAttendanceSystem.ViewModels;

public sealed class AdminHomeViewModel : ViewModelBase
{
    private readonly IServiceRegistry _services;
    private readonly INavigationService _nav;

    private string _totalStudents = "—";
    private string _todayAttendance = "—";
    private string _enrolledCount = "—";
    private string _adminCount = "—";
    private string _deviceStatus = "Checking…";

    public AdminHomeViewModel(IServiceRegistry services, INavigationService nav)
    {
        _services = services;
        _nav = nav;

        var device = services.AppState.Config.FingerprintDevice;
        DeviceStatus = string.Equals(device, "None", StringComparison.OrdinalIgnoreCase)
            ? "Not Configured"
            : device;

        NavigateRegisterCommand = new RelayCommand(() => _nav.NavigateToKey("Register"));
        NavigateEnrollmentCommand = new RelayCommand(() => _nav.NavigateToKey("Enrollment"));
        NavigateReportCommand = new RelayCommand(() => _nav.NavigateToKey("Report"));
        NavigateAdminRegCommand = new RelayCommand(() => _nav.NavigateToKey("AdminReg"));

        _ = LoadStatsAsync();
    }

    public string Title => "Administrator Dashboard";

    public string TotalStudents
    {
        get => _totalStudents;
        private set => SetField(ref _totalStudents, value);
    }

    public string TodayAttendance
    {
        get => _todayAttendance;
        private set => SetField(ref _todayAttendance, value);
    }

    public string EnrolledCount
    {
        get => _enrolledCount;
        private set => SetField(ref _enrolledCount, value);
    }

    public string AdminCount
    {
        get => _adminCount;
        private set => SetField(ref _adminCount, value);
    }

    public string DeviceStatus
    {
        get => _deviceStatus;
        private set => SetField(ref _deviceStatus, value);
    }

    public RelayCommand NavigateRegisterCommand { get; }
    public RelayCommand NavigateEnrollmentCommand { get; }
    public RelayCommand NavigateReportCommand { get; }
    public RelayCommand NavigateAdminRegCommand { get; }

    private async Task LoadStatsAsync()
    {
        try
        {
            var students = await _services.Students.GetAllMatricNosAsync();
            TotalStudents = students.Count.ToString();
        }
        catch
        {
            TotalStudents = "0";
        }

        // Placeholder values — wire to actual repos when available
        TodayAttendance = "—";
        EnrolledCount = "—";
        AdminCount = "—";
    }
}
