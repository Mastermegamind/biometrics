using BiometricFingerprintsAttendanceSystem.Models;
using MySqlConnector;

namespace BiometricFingerprintsAttendanceSystem.Services.Db;

public sealed class AttendanceRepository
{
    private readonly DbConnectionFactory _factory;

    public AttendanceRepository(DbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<bool> HasTimeInAsync(string matricNo, string date, CancellationToken cancellationToken = default)
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync(cancellationToken);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT 1 FROM attendance WHERE regno = @regno AND date = @date LIMIT 1";
        cmd.Parameters.AddWithValue("@regno", matricNo);
        cmd.Parameters.AddWithValue("@date", date);

        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result is not null;
    }

    public async Task InsertTimeInAsync(string matricNo, string name, string date, string day, string timeIn, CancellationToken cancellationToken = default)
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync(cancellationToken);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO attendance (regno, name, date, day, timein)
                            VALUES (@regno, @name, @date, @day, @timein)";
        cmd.Parameters.AddWithValue("@regno", matricNo);
        cmd.Parameters.AddWithValue("@name", name);
        cmd.Parameters.AddWithValue("@date", date);
        cmd.Parameters.AddWithValue("@day", day);
        cmd.Parameters.AddWithValue("@timein", timeIn);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<string?> GetTimeOutAsync(string matricNo, string date, CancellationToken cancellationToken = default)
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync(cancellationToken);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT timeout FROM attendance WHERE regno = @regno AND date = @date";
        cmd.Parameters.AddWithValue("@regno", matricNo);
        cmd.Parameters.AddWithValue("@date", date);

        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result?.ToString();
    }

    public async Task UpdateTimeOutAsync(string matricNo, string timeOut, CancellationToken cancellationToken = default)
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync(cancellationToken);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE attendance SET timeout = @timeout WHERE regno = @regno";
        cmd.Parameters.AddWithValue("@timeout", timeOut);
        cmd.Parameters.AddWithValue("@regno", matricNo);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AttendanceRecord>> GetByDateRangeAsync(string fromDate, string toDate, CancellationToken cancellationToken = default)
    {
        var results = new List<AttendanceRecord>();
        await using var conn = _factory.Create();
        await conn.OpenAsync(cancellationToken);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT regno, name, date, day, timein, timeout
                            FROM attendance WHERE date BETWEEN @from AND @to";
        cmd.Parameters.AddWithValue("@from", fromDate);
        cmd.Parameters.AddWithValue("@to", toDate);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new AttendanceRecord
            {
                RegNo = reader.GetString("regno"),
                Name = reader.GetString("name"),
                Date = reader.GetString("date"),
                Day = reader.GetString("day"),
                TimeIn = reader.GetString("timein"),
                TimeOut = reader.GetString("timeout")
            });
        }

        return results;
    }

    public async Task<int> CountByStudentAndDateRangeAsync(string name, string fromDate, string toDate, CancellationToken cancellationToken = default)
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync(cancellationToken);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM attendance WHERE name = @name AND date BETWEEN @from AND @to";
        cmd.Parameters.AddWithValue("@name", name);
        cmd.Parameters.AddWithValue("@from", fromDate);
        cmd.Parameters.AddWithValue("@to", toDate);

        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }
}
