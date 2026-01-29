using System.Collections.ObjectModel;
using BiometricFingerprintsAttendanceSystem.Models;
using BiometricFingerprintsAttendanceSystem.Services;

namespace BiometricFingerprintsAttendanceSystem.ViewModels;

public sealed class AttendanceReportViewModel : ViewModelBase
{
    private readonly IServiceRegistry _services;
    private string _fromDate = DateTime.Now.ToShortDateString();
    private string _toDate = DateTime.Now.ToShortDateString();
    private string _studentName = string.Empty;
    private string _countText = string.Empty;
    private string _statusMessage = string.Empty;

    public AttendanceReportViewModel(IServiceRegistry services)
    {
        _services = services;
        Records = new ObservableCollection<AttendanceRecord>();
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        CountCommand = new AsyncRelayCommand(CountAsync, CanCount);
    }

    public string FromDate
    {
        get => _fromDate;
        set => SetField(ref _fromDate, value);
    }

    public string ToDate
    {
        get => _toDate;
        set => SetField(ref _toDate, value);
    }

    public string StudentName
    {
        get => _studentName;
        set
        {
            if (SetField(ref _studentName, value))
            {
                CountCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string CountText
    {
        get => _countText;
        private set => SetField(ref _countText, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    public ObservableCollection<AttendanceRecord> Records { get; }

    public AsyncRelayCommand LoadCommand { get; }
    public AsyncRelayCommand CountCommand { get; }

    private bool CanCount() => !string.IsNullOrWhiteSpace(StudentName);

    private async Task LoadAsync()
    {
        Records.Clear();
        var rows = await _services.Attendance.GetByDateRangeAsync(FromDate, ToDate);
        foreach (var row in rows)
        {
            Records.Add(row);
        }

        StatusMessage = $"Loaded {Records.Count} records.";
    }

    private async Task CountAsync()
    {
        var count = await _services.Attendance.CountByStudentAndDateRangeAsync(StudentName.Trim(), FromDate, ToDate);
        CountText = count.ToString();
        StatusMessage = "Attendance count updated.";
    }
}
