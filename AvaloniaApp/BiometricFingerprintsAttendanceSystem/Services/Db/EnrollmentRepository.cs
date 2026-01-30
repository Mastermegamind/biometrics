using BiometricFingerprintsAttendanceSystem.Models;
using MySqlConnector;

namespace BiometricFingerprintsAttendanceSystem.Services.Db;

public sealed class EnrollmentRepository
{
    private readonly DbConnectionFactory _factory;

    public EnrollmentRepository(DbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<IReadOnlyList<EnrollmentTemplate>> GetAllTemplatesAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<EnrollmentTemplate>();
        await using var conn = _factory.Create();
        await conn.OpenAsync(cancellationToken);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT regno, finger_index, template FROM fingerprint_enrollments";

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var regNo = reader.GetString("regno");
            var fingerIndex = reader.GetInt32("finger_index");
            if (reader["template"] is byte[] bytes && bytes.Length > 0)
            {
                results.Add(new EnrollmentTemplate
                {
                    RegNo = regNo,
                    FingerIndex = fingerIndex,
                    TemplateData = bytes
                });
            }
        }

        return results;
    }

    public async Task<bool> HasEnrollmentAsync(string matricNo, CancellationToken cancellationToken = default)
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync(cancellationToken);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM fingerprint_enrollments WHERE regno = @regno";
        cmd.Parameters.AddWithValue("@regno", matricNo);

        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) > 0;
    }

    public async Task UpsertTemplateAsync(string matricNo, int fingerIndex, byte[] templateData, int fingerMask, CancellationToken cancellationToken = default)
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();

        cmd.CommandText = @"
            INSERT INTO fingerprint_enrollments (regno, finger_index, finger_name, template, captured_at)
            VALUES (@regno, @fingerIndex, @fingerName, @data, @capturedAt)
            ON DUPLICATE KEY UPDATE
                finger_name = VALUES(finger_name),
                template = VALUES(template),
                captured_at = VALUES(captured_at)";

        cmd.Parameters.AddWithValue("@regno", matricNo);
        cmd.Parameters.AddWithValue("@fingerIndex", fingerIndex);
        cmd.Parameters.AddWithValue("@fingerName", GetFingerName(fingerIndex).ToLowerInvariant().Replace(' ', '-'));
        cmd.Parameters.AddWithValue("@data", templateData);
        cmd.Parameters.AddWithValue("@capturedAt", LagosTime.Now);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task ClearTemplateAsync(string matricNo, int fingerIndex, int fingerMask, CancellationToken cancellationToken = default)
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();

        cmd.CommandText = "DELETE FROM fingerprint_enrollments WHERE regno = @regno AND finger_index = @fingerIndex";
        cmd.Parameters.AddWithValue("@regno", matricNo);
        cmd.Parameters.AddWithValue("@fingerIndex", fingerIndex);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int?> GetFingerMaskAsync(string matricNo, CancellationToken cancellationToken = default)
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT finger_index FROM fingerprint_enrollments WHERE regno = @regno";
        cmd.Parameters.AddWithValue("@regno", matricNo);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        var mask = 0;
        while (await reader.ReadAsync(cancellationToken))
        {
            var index = reader.GetInt32(0);
            if (index >= 1 && index <= 10)
            {
                mask |= (1 << (index - 1));
            }
        }

        return mask;
    }

    private static string GetFingerName(int index) => index switch
    {
        1 => "Right Thumb",
        2 => "Right Index Finger",
        3 => "Right Middle Finger",
        4 => "Right Ring Finger",
        5 => "Right Little Finger",
        6 => "Left Thumb",
        7 => "Left Index Finger",
        8 => "Left Middle Finger",
        9 => "Left Ring Finger",
        10 => "Left Little Finger",
        _ => $"Finger {index}"
    };
}

