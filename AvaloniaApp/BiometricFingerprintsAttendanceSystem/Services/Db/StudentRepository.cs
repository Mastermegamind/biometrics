using BiometricFingerprintsAttendanceSystem.Models;
using MySqlConnector;

namespace BiometricFingerprintsAttendanceSystem.Services.Db;

public sealed class StudentRepository
{
    private readonly DbConnectionFactory _factory;

    public StudentRepository(DbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<bool> StudentExistsAsync(string name, string matricNo, string department, CancellationToken cancellationToken = default)
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync(cancellationToken);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT 1 FROM students WHERE name = @name AND matricno = @matricno AND department = @dept LIMIT 1";
        cmd.Parameters.AddWithValue("@name", name);
        cmd.Parameters.AddWithValue("@matricno", matricNo);
        cmd.Parameters.AddWithValue("@dept", department);

        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result is not null;
    }

    public async Task CreateAsync(Student student, CancellationToken cancellationToken = default)
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync(cancellationToken);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO students (matricno, name, faculty, department, bloodgroup, gradyear, gender, passport)
                            VALUES (@matricno, @name, @faculty, @dept, @blood, @grad, @gender, @passport)";
        cmd.Parameters.AddWithValue("@matricno", student.MatricNo);
        cmd.Parameters.AddWithValue("@name", student.Name);
        cmd.Parameters.AddWithValue("@faculty", student.Faculty);
        cmd.Parameters.AddWithValue("@dept", student.Department);
        cmd.Parameters.AddWithValue("@blood", student.BloodGroup);
        cmd.Parameters.AddWithValue("@grad", student.GradYear);
        cmd.Parameters.AddWithValue("@gender", student.Gender);
        cmd.Parameters.AddWithValue("@passport", student.PassportImage);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<Student?> GetByMatricNoAsync(string matricNo, CancellationToken cancellationToken = default)
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync(cancellationToken);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT matricno, name, faculty, department, bloodgroup, gradyear, gender, passport FROM students WHERE matricno = @matricno";
        cmd.Parameters.AddWithValue("@matricno", matricNo);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new Student
        {
            MatricNo = reader.GetString("matricno"),
            Name = reader.GetString("name"),
            Faculty = reader.GetString("faculty"),
            Department = reader.GetString("department"),
            BloodGroup = reader.GetString("bloodgroup"),
            GradYear = reader.GetString("gradyear"),
            Gender = reader.GetString("gender"),
            PassportImage = (byte[])reader["passport"]
        };
    }

    public async Task<IReadOnlyList<string>> GetAllMatricNosAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<string>();
        await using var conn = _factory.Create();
        await conn.OpenAsync(cancellationToken);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT matricno FROM students";

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(reader.GetString("matricno"));
        }

        return results;
    }
}
