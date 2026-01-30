using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace BiometricFingerprintsAttendanceSystem.Services.Logging;

public sealed class JsonFileLoggerProvider : ILoggerProvider
{
    private readonly string _logPath;
    private readonly object _lock = new();

    public JsonFileLoggerProvider(string logPath)
    {
        _logPath = logPath;
        var dir = Path.GetDirectoryName(_logPath);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new JsonFileLogger(categoryName, _logPath, _lock);
    }

    public void Dispose()
    {
    }

    private sealed class JsonFileLogger : ILogger
    {
        private readonly string _category;
        private readonly string _path;
        private readonly object _lock;

        public JsonFileLogger(string category, string path, object sharedLock)
        {
            _category = category;
            _path = path;
            _lock = sharedLock;
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var payload = new
            {
                timestamp = LagosTime.Now.ToString("O"),
                level = logLevel.ToString(),
                category = _category,
                eventId = eventId.Id,
                message = formatter(state, exception),
                exception = exception?.ToString()
            };

            var line = JsonSerializer.Serialize(payload);
            lock (_lock)
            {
                File.AppendAllText(_path, line + Environment.NewLine);
            }
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();
        public void Dispose() { }
    }
}

