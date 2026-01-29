using BCrypt.Net;
using MySqlConnector;

namespace BiometricFingerprintsAttendanceSystem.Services.Db;

public sealed class UserRepository
{
    private readonly DbConnectionFactory _factory;
    private readonly bool _demoMode;
    private readonly string _demoAdminEmail;
    private readonly string _demoAdminPassword;

    public UserRepository(DbConnectionFactory factory)
        : this(factory, false, string.Empty, string.Empty)
    {
    }

    public UserRepository(DbConnectionFactory factory, bool demoMode, string demoAdminEmail, string demoAdminPassword)
    {
        _factory = factory;
        _demoMode = demoMode;
        _demoAdminEmail = demoAdminEmail;
        _demoAdminPassword = demoAdminPassword;
    }

    public async Task<string?> GetUserTypeAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        // Check demo credentials first if demo mode is enabled
        if (_demoMode &&
            string.Equals(username, _demoAdminEmail, StringComparison.OrdinalIgnoreCase) &&
            password == _demoAdminPassword)
        {
            return "Administrator";
        }

        try
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync(cancellationToken);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT usertype, password FROM admin_users WHERE username = @username LIMIT 1";
            cmd.Parameters.AddWithValue("@username", username);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            var userType = reader.IsDBNull(0) ? null : reader.GetString(0);
            var storedPassword = reader.IsDBNull(1) ? null : reader.GetString(1);
            if (string.IsNullOrWhiteSpace(userType) || string.IsNullOrWhiteSpace(storedPassword))
            {
                return null;
            }

            return BCrypt.Verify(password, storedPassword) ? userType : null;
        }
        catch
        {
            // In demo mode, if database is unavailable, only demo credentials work
            if (_demoMode &&
                string.Equals(username, _demoAdminEmail, StringComparison.OrdinalIgnoreCase) &&
                password == _demoAdminPassword)
            {
                return "Administrator";
            }
            throw;
        }
    }

    public bool IsDemoMode => _demoMode;
}
