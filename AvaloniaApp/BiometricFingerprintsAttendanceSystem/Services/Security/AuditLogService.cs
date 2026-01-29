using BiometricFingerprintsAttendanceSystem.Services.Db;
using Microsoft.Extensions.Logging;

namespace BiometricFingerprintsAttendanceSystem.Services.Security;

public sealed class AuditLogService
{
    private readonly DbConnectionFactory _db;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(DbConnectionFactory db, ILogger<AuditLogService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task LogAsync(
        string actor,
        string action,
        string? target,
        string status,
        string? message = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var conn = await _db.CreateConnectionAsync(cancellationToken);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO audit_log (actor, action, target, status, message)
                VALUES (@actor, @action, @target, @status, @message)";
            cmd.Parameters.AddWithValue("@actor", actor);
            cmd.Parameters.AddWithValue("@action", action);
            cmd.Parameters.AddWithValue("@target", target);
            cmd.Parameters.AddWithValue("@status", status);
            cmd.Parameters.AddWithValue("@message", message);

            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write audit log {Action} for {Actor}", action, actor);
        }
    }
}
