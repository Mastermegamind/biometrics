namespace BiometricFingerprintsAttendanceSystem;

public sealed class AppState
{
    private string _currentUserType = string.Empty;
    private string _currentUsername = string.Empty;

    public AppState(AppConfig config)
    {
        Config = config;
    }

    public AppConfig Config { get; }

    public string CurrentUserType
    {
        get => _currentUserType;
        set
        {
            if (!string.Equals(_currentUserType, value, StringComparison.Ordinal))
            {
                _currentUserType = value;
                UserTypeChanged?.Invoke();
            }
        }
    }

    public string CurrentUsername
    {
        get => _currentUsername;
        set => _currentUsername = value;
    }

    public event Action? UserTypeChanged;
}
