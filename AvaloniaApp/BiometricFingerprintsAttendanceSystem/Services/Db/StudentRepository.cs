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
        cmd.CommandText = "SELECT 1 FROM students WHERE name = @name AND (regno = @regno OR matricno = @regno) AND department = @dept LIMIT 1";
        cmd.Parameters.AddWithValue("@name", name);
        cmd.Parameters.AddWithValue("@regno", matricNo);
        cmd.Parameters.AddWithValue("@dept", department);

        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result is not null;
    }

    public async Task CreateAsync(Student student, CancellationToken cancellationToken = default)
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync(cancellationToken);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO students (regno, matricno, name, faculty, department, bloodgroup, gradyear, gender, passport_filename)
                            VALUES (@regno, @matricno, @name, @faculty, @dept, @blood, @grad, @gender, @passportFilename)";
        cmd.Parameters.AddWithValue("@regno", string.IsNullOrWhiteSpace(student.RegNo) ? student.MatricNo : student.RegNo);
        cmd.Parameters.AddWithValue("@matricno", string.IsNullOrWhiteSpace(student.MatricNo) ? DBNull.Value : student.MatricNo);
        cmd.Parameters.AddWithValue("@name", student.Name);
        cmd.Parameters.AddWithValue("@faculty", student.Faculty);
        cmd.Parameters.AddWithValue("@dept", student.Department);
        cmd.Parameters.AddWithValue("@blood", student.BloodGroup);
        cmd.Parameters.AddWithValue("@grad", student.GradYear);
        cmd.Parameters.AddWithValue("@gender", student.Gender);
        cmd.Parameters.AddWithValue("@passportFilename", SavePassportToDisk(student.RegNo, student.PassportImage));

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<Student?> GetByRegNoAsync(string regNo, CancellationToken cancellationToken = default)
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync(cancellationToken);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT regno, matricno, name, faculty, department, bloodgroup, gradyear, gender, passport_filename FROM students WHERE regno = @regno OR matricno = @regno";
        cmd.Parameters.AddWithValue("@regno", regNo);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new Student
        {
            RegNo = reader.GetString("regno"),
            MatricNo = reader.IsDBNull(reader.GetOrdinal("matricno")) ? string.Empty : reader.GetString("matricno"),
            Name = reader.GetString("name"),
            Faculty = reader.GetString("faculty"),
            Department = reader.GetString("department"),
            BloodGroup = reader.GetString("bloodgroup"),
            GradYear = reader.GetString("gradyear"),
            Gender = reader.GetString("gender"),
            PassportImage = TryLoadPassportFromDisk(reader.IsDBNull(reader.GetOrdinal("passport_filename")) ? null : reader.GetString("passport_filename")) ?? Array.Empty<byte>()
        };
    }

    public async Task<IReadOnlyList<string>> GetAllRegNosAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<string>();
        await using var conn = _factory.Create();
        await conn.OpenAsync(cancellationToken);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT regno FROM students";

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(reader.GetString("regno"));
        }

        return results;
    }

    private static string GetPassportStorageDir()
    {
        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(baseDir, "MdaBiometrics", "passports");
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static string? SavePassportToDisk(string regNo, byte[]? bytes)
    {
        if (bytes == null || bytes.Length == 0)
        {
            return null;
        }

        var dir = GetPassportStorageDir();
        var fileName = $"passport_{SanitizeFileName(regNo)}_{LagosTime.Now:yyyyMMddHHmmss}.jpg";
        var path = Path.Combine(dir, fileName);
        File.WriteAllBytes(path, bytes);
        return fileName;
    }

    private static byte[]? TryLoadPassportFromDisk(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        var path = Path.Combine(GetPassportStorageDir(), fileName);
        return File.Exists(path) ? File.ReadAllBytes(path) : null;
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
        return cleaned.Replace(' ', '_');
    }
}

