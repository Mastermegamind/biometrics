namespace BiometricFingerprintsAttendanceSystem.ViewModels;

public sealed class UserHomeViewModel : ViewModelBase
{
    private readonly INavigationService _nav;

    public UserHomeViewModel(INavigationService nav)
    {
        _nav = nav;

        NavigateClockInCommand = new RelayCommand(() => _nav.NavigateToKey("TimeIn"));
        NavigateClockOutCommand = new RelayCommand(() => _nav.NavigateToKey("TimeOut"));
        NavigateReportCommand = new RelayCommand(() => _nav.NavigateToKey("Report"));
    }

    public string Title => "Staff Dashboard";

    public RelayCommand NavigateClockInCommand { get; }
    public RelayCommand NavigateClockOutCommand { get; }
    public RelayCommand NavigateReportCommand { get; }
}
