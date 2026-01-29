using BiometricFingerprintsAttendanceSystem.Services.Db;
using Microsoft.Extensions.Logging;

namespace BiometricFingerprintsAttendanceSystem.Services.Security;

public sealed class PasswordResetRepository
{
    private readonly DbConnectionFactory _db;
    private readonly ILogger<PasswordResetRepository> _logger;

    public PasswordResetRepository(DbConnectionFactory db, ILogger<PasswordResetRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task RecordResetAsync(
        string adminUsername,
        string targetUsername,
        string? message = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var conn = await _db.CreateConnectionAsync(cancellationToken);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO password_reset (admin_username, target_username, message)
                VALUES (@admin_username, @target_username, @message)";
            cmd.Parameters.AddWithValue("@admin_username", adminUsername);
            cmd.Parameters.AddWithValue("@target_username", targetUsername);
            cmd.Parameters.AddWithValue("@message", message);

            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record password reset from {Admin} to {Target}", adminUsername, targetUsername);
        }
    }
}
