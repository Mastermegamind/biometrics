using BiometricFingerprintsAttendanceSystem.Services;

namespace BiometricFingerprintsAttendanceSystem.ViewModels;

public sealed class AdminPasswordResetViewModel : ViewModelBase
{
    private readonly IServiceRegistry _services;
    private string _username = string.Empty;
    private string _newPassword = string.Empty;
    private string _confirmPassword = string.Empty;
    private string _statusMessage = string.Empty;

    public AdminPasswordResetViewModel(IServiceRegistry services)
    {
        _services = services;
        ResetCommand = new AsyncRelayCommand(ResetAsync, CanReset);
    }

    public string Username
    {
        get => _username;
        set
        {
            if (SetField(ref _username, value))
            {
                ResetCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string NewPassword
    {
        get => _newPassword;
        set
        {
            if (SetField(ref _newPassword, value))
            {
                ResetCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string ConfirmPassword
    {
        get => _confirmPassword;
        set
        {
            if (SetField(ref _confirmPassword, value))
            {
                ResetCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    public AsyncRelayCommand ResetCommand { get; }

    private bool CanReset()
    {
        return !string.IsNullOrWhiteSpace(Username)
            && !string.IsNullOrWhiteSpace(NewPassword)
            && !string.IsNullOrWhiteSpace(ConfirmPassword);
    }

    private async Task ResetAsync()
    {
        if (!string.Equals(NewPassword, ConfirmPassword, StringComparison.Ordinal))
        {
            StatusMessage = "Passwords do not match.";
            return;
        }

        var target = Username.Trim();
        var updated = await _services.Users.UpdateAdminPasswordAsync(target, NewPassword);
        if (!updated)
        {
            StatusMessage = "Admin account not found.";
            await _services.Audit.LogAsync(_services.AppState.CurrentUsername, "AdminPasswordReset", target, "Failed", "Admin not found");
            return;
        }

        await _services.PasswordResets.RecordResetAsync(_services.AppState.CurrentUsername, target, "Local admin reset");
        await _services.Audit.LogAsync(_services.AppState.CurrentUsername, "AdminPasswordReset", target, "Success", "Password reset");

        StatusMessage = "Password reset successful.";
        Username = string.Empty;
        NewPassword = string.Empty;
        ConfirmPassword = string.Empty;
    }
}
