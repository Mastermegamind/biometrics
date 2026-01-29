using BiometricFingerprintsAttendanceSystem.Services.Db;
using Microsoft.Extensions.Logging;

namespace BiometricFingerprintsAttendanceSystem.Services.Security;

public sealed class LoginAttemptRepository
{
    private readonly DbConnectionFactory _db;
    private readonly ILogger<LoginAttemptRepository> _logger;
    private readonly int _maxFailedAttempts;
    private readonly TimeSpan _lockoutWindow;

    public LoginAttemptRepository(DbConnectionFactory db, AppConfig config, ILogger<LoginAttemptRepository> logger)
    {
        _db = db;
        _logger = logger;
        _maxFailedAttempts = config.MaxFailedLoginAttempts <= 0 ? 5 : config.MaxFailedLoginAttempts;
        _lockoutWindow = TimeSpan.FromMinutes(config.LockoutMinutes <= 0 ? 15 : config.LockoutMinutes);
    }

    public async Task<DateTime?> GetActiveLockoutAsync(string username, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var conn = await _db.CreateConnectionAsync(cancellationToken);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT lockout_until
                FROM login_attempts
                WHERE username = @username AND lockout_until IS NOT NULL
                ORDER BY attempted_at DESC
                LIMIT 1";
            cmd.Parameters.AddWithValue("@username", username);

            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            if (result == null || result == DBNull.Value)
            {
                return null;
            }

            var lockoutUntil = Convert.ToDateTime(result);
            return lockoutUntil > DateTime.UtcNow ? lockoutUntil : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read lockout state for {Username}", username);
            return null;
        }
    }

    public async Task RecordSuccessAsync(string username, string? message = null, CancellationToken cancellationToken = default)
    {
        await RecordAttemptAsync(username, true, 0, null, message, cancellationToken);
    }

    public async Task<DateTime?> RecordFailureAsync(string username, string? message = null, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var since = now - _lockoutWindow;
        int failureCount = 1;

        try
        {
            await using var conn = await _db.CreateConnectionAsync(cancellationToken);
            await using var countCmd = conn.CreateCommand();
            countCmd.CommandText = @"
                SELECT COUNT(*)
                FROM login_attempts
                WHERE username = @username AND success = 0 AND attempted_at >= @since";
            countCmd.Parameters.AddWithValue("@username", username);
            countCmd.Parameters.AddWithValue("@since", since);

            var result = await countCmd.ExecuteScalarAsync(cancellationToken);
            if (result != null && result != DBNull.Value)
            {
                failureCount = Convert.ToInt32(result) + 1;
            }

            DateTime? lockoutUntil = null;
            if (failureCount >= _maxFailedAttempts)
            {
                lockoutUntil = now.Add(_lockoutWindow);
            }

            await RecordAttemptAsync(username, false, failureCount, lockoutUntil, message, cancellationToken, conn);
            return lockoutUntil;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record login failure for {Username}", username);
            return null;
        }
    }

    private async Task RecordAttemptAsync(
        string username,
        bool success,
        int failureCount,
        DateTime? lockoutUntil,
        string? message,
        CancellationToken cancellationToken,
        MySqlConnector.MySqlConnection? existingConnection = null)
    {
        await using var conn = existingConnection ?? await _db.CreateConnectionAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO login_attempts (username, success, lockout_until, failure_count, message)
            VALUES (@username, @success, @lockout_until, @failure_count, @message)";
        cmd.Parameters.AddWithValue("@username", username);
        cmd.Parameters.AddWithValue("@success", success ? 1 : 0);
        cmd.Parameters.AddWithValue("@lockout_until", lockoutUntil);
        cmd.Parameters.AddWithValue("@failure_count", failureCount);
        cmd.Parameters.AddWithValue("@message", message);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}
