using System.Threading;
using MySqlConnector;

namespace BiometricFingerprintsAttendanceSystem.Services.Db;

public sealed class DbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory(AppConfig config)
    {
        _connectionString = config.ConnectionString;
    }

    public MySqlConnection Create()
    {
        return new MySqlConnection(_connectionString);
    }

    public async Task<MySqlConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
