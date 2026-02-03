using System.Windows.Input;

namespace BiometricFingerprintsAttendanceSystem.ViewModels;

public sealed class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => _execute();

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public sealed class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;

    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        if (_canExecute == null) return true;
        if (parameter is T typedParam)
            return _canExecute(typedParam);
        if (parameter == null && default(T) == null)
            return _canExecute(default);
        return false;
    }

    public void Execute(object? parameter)
    {
        if (parameter is T typedParam)
            _execute(typedParam);
        else if (parameter == null && default(T) == null)
            _execute(default);
        else if (parameter is string str && typeof(T) == typeof(string))
            _execute((T)(object)str);
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
