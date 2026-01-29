namespace BiometricFingerprintsAttendanceSystem.ViewModels;

public interface INavigationService
{
    void NavigateTo(ViewModelBase viewModel);
    bool NavigateToKey(string key);
}
