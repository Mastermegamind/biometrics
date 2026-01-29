using BiometricFingerprintsAttendanceSystem.Services;

namespace BiometricFingerprintsAttendanceSystem.ViewModels;

public sealed class AdminRegistrationViewModel : ViewModelBase
{
    private readonly IServiceRegistry _services;
    private string _username = string.Empty;
    private string _userType = string.Empty;
    private string _password = string.Empty;
    private string _name = string.Empty;
    private string _contactNo = string.Empty;
    private string _email = string.Empty;
    private string _statusMessage = string.Empty;

    public AdminRegistrationViewModel(IServiceRegistry services)
    {
        _services = services;
        SubmitCommand = new AsyncRelayCommand(SubmitAsync, CanSubmit);
    }

    public string Username
    {
        get => _username;
        set
        {
            if (SetField(ref _username, value))
            {
                SubmitCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string UserType
    {
        get => _userType;
        set
        {
            if (SetField(ref _userType, value))
            {
                SubmitCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            if (SetField(ref _password, value))
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

    public string ContactNo
    {
        get => _contactNo;
        set => SetField(ref _contactNo, value);
    }

    public string Email
    {
        get => _email;
        set => SetField(ref _email, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    public AsyncRelayCommand SubmitCommand { get; }

    private bool CanSubmit()
    {
        return !string.IsNullOrWhiteSpace(Username)
            && !string.IsNullOrWhiteSpace(Password)
            && !string.IsNullOrWhiteSpace(UserType)
            && !string.IsNullOrWhiteSpace(Name);
    }

    private async Task SubmitAsync()
    {
        await using var conn = _services.ConnectionFactory.Create();
        await conn.OpenAsync();

        await using var check = conn.CreateCommand();
        check.CommandText = "SELECT 1 FROM admin_users WHERE username = @username LIMIT 1";
        check.Parameters.AddWithValue("@username", Username.Trim());
        var exists = await check.ExecuteScalarAsync();

        if (exists is not null)
        {
            StatusMessage = "Duplicate record detected.";
            return;
        }

        await using var cmd = conn.CreateCommand();
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(Password);

        cmd.CommandText = "INSERT INTO admin_users (username, usertype, password, name, contactno, email) VALUES (@username, @usertype, @password, @name, @contactno, @email)";
        cmd.Parameters.AddWithValue("@username", Username.Trim());
        cmd.Parameters.AddWithValue("@usertype", UserType.Trim());
        cmd.Parameters.AddWithValue("@password", passwordHash);
        cmd.Parameters.AddWithValue("@name", Name.Trim());
        cmd.Parameters.AddWithValue("@contactno", ContactNo.Trim());
        cmd.Parameters.AddWithValue("@email", Email.Trim());

        await cmd.ExecuteNonQueryAsync();
        StatusMessage = "Admin user saved.";
        await _services.Audit.LogAsync(_services.AppState.CurrentUsername ?? "system", "AdminCreate", Username.Trim(), "Success", "Admin created");
    }
}
