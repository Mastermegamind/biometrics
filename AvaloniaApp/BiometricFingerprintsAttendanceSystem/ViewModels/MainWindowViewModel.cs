using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using BiometricFingerprintsAttendanceSystem.Services;

namespace BiometricFingerprintsAttendanceSystem.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase, INavigationService
{
    private ViewModelBase _currentView;
    private bool _isDarkMode;
    private bool _isSidebarVisible;
    private readonly Stack<string> _navigationHistory = new();
    private string _currentKey = string.Empty;

    public MainWindowViewModel(ServiceRegistry services)
    {
        Services = services;
        InitializeTheme();

        Views = new Dictionary<string, ViewModelBase>
        {
            ["Home"] = new LoginViewModel(services, this),
            ["Admin"] = new AdminHomeViewModel(services, this),
            ["User"] = new UserHomeViewModel(this),
            ["Register"] = new RegisterViewModel(services),
            ["Enrollment"] = new EnrollmentViewModel(services),
            ["TimeIn"] = new VerificationViewModel(services),
            ["TimeOut"] = new TimeOutViewModel(services),
            ["Report"] = new AttendanceReportViewModel(services),
            ["AdminReg"] = new AdminRegistrationViewModel(services),
            ["Demo"] = new DemoViewModel(services)
        };

        NavItems = new ObservableCollection<NavItemViewModel>
        {
            new("\U0001F3E0", "Home", () => NavigateToKey("Home")),
            new("\U0001F4CA", "Dashboard", () => NavigateToKey("Admin")),
            new("\U0001F464", "Register Student", () => NavigateToKey("Register")),
            new("\U0001F91A", "Enroll Fingerprints", () => NavigateToKey("Enrollment")),
            new("\u23F1", "Clock In", () => NavigateToKey("TimeIn")),
            new("\u23F3", "Clock Out", () => NavigateToKey("TimeOut")),
            new("\U0001F4CB", "Attendance Report", () => NavigateToKey("Report")),
            new("\U0001F6E1", "Admin Registration", () => NavigateToKey("AdminReg"))
        };

        // Add Demo Testing item if demo mode is enabled
        if (services.IsDemoMode)
        {
            NavItems.Add(new("\U0001F9EA", "Demo Testing", () => NavigateToKey("Demo")));
        }

        _currentKey = "Home";
        _currentView = Views["Home"];
        BackCommand = new RelayCommand(GoBack, () => CanGoBack);

        UpdateSidebarVisibility();
        Services.AppState.UserTypeChanged += HandleUserTypeChanged;
    }

    public ServiceRegistry Services { get; }

    public ObservableCollection<NavItemViewModel> NavItems { get; }

    public Dictionary<string, ViewModelBase> Views { get; }

    public RelayCommand BackCommand { get; }

    public ViewModelBase CurrentView
    {
        get => _currentView;
        private set => SetField(ref _currentView, value);
    }

    public bool CanGoBack => _navigationHistory.Count > 0;

    public bool IsBackVisible => !string.Equals(_currentKey, "Home", StringComparison.OrdinalIgnoreCase);

    public bool IsSidebarVisible
    {
        get => _isSidebarVisible;
        private set
        {
            if (SetField(ref _isSidebarVisible, value))
            {
                RaisePropertyChanged(nameof(SidebarWidth));
            }
        }
    }

    public GridLength SidebarWidth => IsSidebarVisible ? new GridLength(270) : new GridLength(0);

    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            if (SetField(ref _isDarkMode, value))
            {
                ApplyTheme();
            }
        }
    }

    public void NavigateTo(ViewModelBase viewModel)
    {
        if (TryGetKey(viewModel, out var key))
        {
            NavigateToKey(key);
            return;
        }

        CurrentView = viewModel;
    }

    public bool NavigateToKey(string key)
    {
        if (!Views.TryGetValue(key, out var view))
        {
            return false;
        }

        if (!string.Equals(_currentKey, key, StringComparison.Ordinal))
        {
            if (!string.IsNullOrWhiteSpace(_currentKey))
            {
                _navigationHistory.Push(_currentKey);
            }

            _currentKey = key;
        }

        CurrentView = view;
        RaiseNavigationStateChanged();
        return true;
    }

    private void HandleUserTypeChanged()
    {
        UpdateSidebarVisibility();
    }

    private void UpdateSidebarVisibility()
    {
        IsSidebarVisible = string.Equals(Services.AppState.CurrentUserType, "Administrator", StringComparison.OrdinalIgnoreCase);
    }

    private void GoBack()
    {
        if (_navigationHistory.Count == 0)
        {
            return;
        }

        var previousKey = _navigationHistory.Pop();
        if (Views.TryGetValue(previousKey, out var view))
        {
            _currentKey = previousKey;
            CurrentView = view;
            RaiseNavigationStateChanged();
        }
    }

    private bool TryGetKey(ViewModelBase viewModel, out string key)
    {
        foreach (var pair in Views)
        {
            if (ReferenceEquals(pair.Value, viewModel))
            {
                key = pair.Key;
                return true;
            }
        }

        key = string.Empty;
        return false;
    }

    private void RaiseNavigationStateChanged()
    {
        BackCommand.RaiseCanExecuteChanged();
        RaisePropertyChanged(nameof(CanGoBack));
        RaisePropertyChanged(nameof(IsBackVisible));
    }

    private void InitializeTheme()
    {
        var app = Application.Current;
        if (app?.RequestedThemeVariant == ThemeVariant.Dark)
        {
            _isDarkMode = true;
        }
        else if (app?.RequestedThemeVariant == ThemeVariant.Light)
        {
            _isDarkMode = false;
        }
        else
        {
            _isDarkMode = false;
        }
    }

    private void ApplyTheme()
    {
        var app = Application.Current;
        if (app is null)
        {
            return;
        }

        app.RequestedThemeVariant = _isDarkMode ? ThemeVariant.Dark : ThemeVariant.Light;
    }
}

public sealed class NavItemViewModel
{
    public NavItemViewModel(string icon, string title, Action onClick)
    {
        Icon = icon;
        Title = title;
        NavigateCommand = new RelayCommand(onClick);
    }

    public string Icon { get; }
    public string Title { get; }
    public RelayCommand NavigateCommand { get; }
}
