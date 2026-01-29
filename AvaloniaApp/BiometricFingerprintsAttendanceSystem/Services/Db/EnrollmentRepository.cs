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
        cmd.CommandText = "SELECT matricno, fingerdata1, fingerdata2, fingerdata3, fingerdata4, fingerdata5, fingerdata6, fingerdata7, fingerdata8, fingerdata9, fingerdata10 FROM new_enrollment";

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var matricNo = reader.GetString("matricno");
            for (var i = 1; i <= 10; i++)
            {
                var column = $"fingerdata{i}";
                if (reader[column] is byte[] bytes && bytes.Length > 0)
                {
                    results.Add(new EnrollmentTemplate
                    {
                        MatricNo = matricNo,
                        FingerIndex = i,
                        TemplateData = bytes
                    });
                }
            }
        }

        return results;
    }

    public async Task<bool> HasEnrollmentAsync(string matricNo, CancellationToken cancellationToken = default)
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync(cancellationToken);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM new_enrollment WHERE matricno = @matricno";
        cmd.Parameters.AddWithValue("@matricno", matricNo);

        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) > 0;
    }

    public async Task UpsertTemplateAsync(string matricNo, int fingerIndex, byte[] templateData, int fingerMask, CancellationToken cancellationToken = default)
    {
        var exists = await HasEnrollmentAsync(matricNo, cancellationToken);

        await using var conn = _factory.Create();
        await conn.OpenAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();

        if (exists)
        {
            cmd.CommandText = $"UPDATE new_enrollment SET fingerdata{fingerIndex} = @data, fingermask = @mask WHERE matricno = @matricno";
        }
        else
        {
            cmd.CommandText = $"INSERT INTO new_enrollment (matricno, fingerdata{fingerIndex}, fingermask) VALUES (@matricno, @data, @mask)";
        }

        cmd.Parameters.AddWithValue("@matricno", matricNo);
        cmd.Parameters.AddWithValue("@data", templateData);
        cmd.Parameters.AddWithValue("@mask", fingerMask);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task ClearTemplateAsync(string matricNo, int fingerIndex, int fingerMask, CancellationToken cancellationToken = default)
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();

        cmd.CommandText = $"UPDATE new_enrollment SET fingerdata{fingerIndex} = NULL, fingermask = @mask WHERE matricno = @matricno";
        cmd.Parameters.AddWithValue("@matricno", matricNo);
        cmd.Parameters.AddWithValue("@mask", fingerMask);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int?> GetFingerMaskAsync(string matricNo, CancellationToken cancellationToken = default)
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT fingermask FROM new_enrollment WHERE matricno = @matricno";
        cmd.Parameters.AddWithValue("@matricno", matricNo);

        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result is null ? null : Convert.ToInt32(result);
    }
}
