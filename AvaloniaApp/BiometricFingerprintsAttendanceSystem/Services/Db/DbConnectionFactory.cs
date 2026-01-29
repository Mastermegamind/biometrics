using MySqlConnector;

namespace BiometricFingerprintsAttendanceSystem.Services.Db;

public sealed class DbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public MySqlConnection Create()
    {
        return new MySqlConnection(_connectionString);
    }
}
