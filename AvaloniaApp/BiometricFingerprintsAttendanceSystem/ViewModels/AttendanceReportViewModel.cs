using System.Collections.ObjectModel;
using BiometricFingerprintsAttendanceSystem.Models;
using BiometricFingerprintsAttendanceSystem.Services;

namespace BiometricFingerprintsAttendanceSystem.ViewModels;

public sealed class AttendanceReportViewModel : ViewModelBase
{
    private readonly IServiceRegistry _services;
    private string _fromDate = LagosTime.Now.ToString("yyyy-MM-dd");
    private string _toDate = LagosTime.Now.ToString("yyyy-MM-dd");
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
        var normalizedFrom = NormalizeDateOrDefault(FromDate, LagosTime.Now.Date);
        var normalizedTo = NormalizeDateOrDefault(ToDate, LagosTime.Now.Date);
        FromDate = normalizedFrom;
        ToDate = normalizedTo;

        // Use IDataService to fetch from API (or offline cache based on sync mode)
        var fromDate = DateTime.Parse(normalizedFrom);
        var toDate = DateTime.Parse(normalizedTo);
        var result = await _services.Data.GetAttendanceAsync(fromDate, toDate);

        if (result.Success && result.Data != null)
        {
            foreach (var apiRecord in result.Data)
            {
                Records.Add(new AttendanceRecord
                {
                    RegNo = apiRecord.RegNo,
                    Name = apiRecord.Name,
                    Date = apiRecord.Date.ToString("yyyy-MM-dd"),
                    Day = apiRecord.Date.DayOfWeek.ToString(),
                    TimeIn = apiRecord.TimeIn?.ToString("HH:mm:ss") ?? string.Empty,
                    TimeOut = apiRecord.TimeOut?.ToString("HH:mm:ss") ?? string.Empty
                });
            }
            StatusMessage = $"Loaded {Records.Count} records.";
        }
        else
        {
            StatusMessage = result.Message ?? "Failed to load attendance records.";
        }
    }

    private async Task CountAsync()
    {
        var normalizedFrom = NormalizeDateOrDefault(FromDate, LagosTime.Now.Date);
        var normalizedTo = NormalizeDateOrDefault(ToDate, LagosTime.Now.Date);
        FromDate = normalizedFrom;
        ToDate = normalizedTo;

        var fromDate = DateTime.Parse(normalizedFrom);
        var toDate = DateTime.Parse(normalizedTo);
        var result = await _services.Data.GetAttendanceAsync(fromDate, toDate);

        if (result.Success && result.Data != null)
        {
            var needle = StudentName.Trim();
            var count = result.Data.Count(r => string.Equals(r.Name, needle, StringComparison.OrdinalIgnoreCase));
            CountText = count.ToString();
            StatusMessage = "Attendance count updated.";
        }
        else
        {
            CountText = "0";
            StatusMessage = result.Message ?? "Failed to load attendance records.";
        }
    }

    private static string NormalizeDateOrDefault(string input, DateTime fallback)
    {
        if (DateTime.TryParse(input, out var parsed))
        {
            return parsed.ToString("yyyy-MM-dd");
        }

        return fallback.ToString("yyyy-MM-dd");
    }
}

