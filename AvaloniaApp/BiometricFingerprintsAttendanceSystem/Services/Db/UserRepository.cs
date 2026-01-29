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
            cmd.CommandText = "SELECT usertype FROM registration WHERE username = @username AND password = @password";
            cmd.Parameters.AddWithValue("@username", username);
            cmd.Parameters.AddWithValue("@password", password);

            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            return result?.ToString();
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
