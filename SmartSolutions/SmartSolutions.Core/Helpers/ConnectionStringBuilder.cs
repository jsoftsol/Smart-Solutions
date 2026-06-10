using Microsoft.Data.SqlClient;

namespace SmartSolutions.Core.Helpers;

public static class ConnectionStringBuilder
{
    public static string Build(
        string server,
        string database,
        bool windowsAuth,
        string? username = null,
        string? password = null)
    {
        if (string.IsNullOrWhiteSpace(server))
            throw new ArgumentException("Server name is required.", nameof(server));
        if (string.IsNullOrWhiteSpace(database))
            throw new ArgumentException("Database name is required.", nameof(database));
        if (!windowsAuth)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username is required for SQL authentication.", nameof(username));
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password is required for SQL authentication.", nameof(password));
        }

        var sb = new SqlConnectionStringBuilder
        {
            DataSource             = server,
            InitialCatalog         = database,
            TrustServerCertificate = true
        };

        if (windowsAuth)
        {
            sb.IntegratedSecurity = true;
        }
        else
        {
            sb.UserID   = username;
            sb.Password = password;
        }

        return sb.ToString();
    }
}
