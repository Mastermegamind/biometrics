using BiometricFingerprintsAttendanceSystem.Models;
using MySqlConnector;

namespace BiometricFingerprintsAttendanceSystem.Services.Db;

public sealed class DemoFingerprintRepository
{
    private readonly DbConnectionFactory _factory;

    public DemoFingerprintRepository(DbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task EnsureDemoUserAsync(string regNo, string name, CancellationToken cancellationToken = default)
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync(cancellationToken);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO demo_user (regno, name)
                            VALUES (@regno, @name)
                            ON DUPLICATE KEY UPDATE name = VALUES(name)";
        cmd.Parameters.AddWithValue("@regno", regNo);
        cmd.Parameters.AddWithValue("@name", name);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpsertTemplateAsync(string regNo, int fingerIndex, string templateBase64, string? imageName, CancellationToken cancellationToken = default)
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync(cancellationToken);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO demo_fingerprints (regno, finger_index, template_base64, image_name)
                            VALUES (@regno, @finger_index, @template_base64, @image_name)
                            ON DUPLICATE KEY UPDATE template_base64 = VALUES(template_base64),
                            image_name = VALUES(image_name),
                            updated_at = CURRENT_TIMESTAMP";
        cmd.Parameters.AddWithValue("@regno", regNo);
        cmd.Parameters.AddWithValue("@finger_index", fingerIndex);
        cmd.Parameters.AddWithValue("@template_base64", templateBase64);
        cmd.Parameters.AddWithValue("@image_name", imageName ?? (object)DBNull.Value);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DemoFingerprintRecord>> GetTemplatesAsync(string regNo, CancellationToken cancellationToken = default)
    {
        var results = new List<DemoFingerprintRecord>();
        await using var conn = _factory.Create();
        await conn.OpenAsync(cancellationToken);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT finger_index, template_base64, image_name, created_at, updated_at
                            FROM demo_fingerprints
                            WHERE regno = @regno
                            ORDER BY finger_index";
        cmd.Parameters.AddWithValue("@regno", regNo);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new DemoFingerprintRecord
            {
                FingerIndex = reader.GetInt32("finger_index"),
                TemplateBase64 = reader.GetString("template_base64"),
                ImageName = reader.IsDBNull(reader.GetOrdinal("image_name"))
                    ? null
                    : reader.GetString("image_name"),
                CreatedAt = reader.GetDateTime("created_at"),
                UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at"))
                    ? null
                    : reader.GetDateTime("updated_at")
            });
        }

        return results;
    }

    public async Task ClearTemplatesAsync(string regNo, CancellationToken cancellationToken = default)
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync(cancellationToken);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM demo_fingerprints WHERE regno = @regno";
        cmd.Parameters.AddWithValue("@regno", regNo);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}
