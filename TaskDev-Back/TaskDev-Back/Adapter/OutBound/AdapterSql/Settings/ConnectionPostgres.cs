using System.Data;
using Npgsql;

namespace TaskDev.Api.Adapter.OutBound.AdapterSql.Settings;

public interface IDatabaseConnectionFactory
{
    IDbConnection CreateConnection();
    string ConnectionString { get; }
}

public class ConnectionPostgres : IDatabaseConnectionFactory
{
    private readonly string _connectionString;

    public ConnectionPostgres(IConfiguration configuration)
    {
        var rawConn = configuration["DATABASE_URL"];
        if (string.IsNullOrWhiteSpace(rawConn))
        {
            rawConn = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        }

        _connectionString = ConvertPostgresUrlToConnectionString(rawConn);
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException("Configure DATABASE_URL ou ConnectionStrings__DefaultConnection.");
        }
    }

    public string ConnectionString => _connectionString;

    public IDbConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }

    public static string ConvertPostgresUrlToConnectionString(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        if (raw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
            raw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(raw);
            var userInfo = uri.UserInfo.Split(':', 2);
            var username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : "";
            var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
            var database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));
            var port = uri.Port > 0 ? uri.Port : 5432;

            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = uri.Host,
                Port = port,
                Database = database,
                Username = username,
                Password = password,
                SslMode = SslMode.Require
            };

            var query = uri.Query.TrimStart('?');
            if (!string.IsNullOrEmpty(query))
            {
                var queryParams = query.Split('&');
                foreach (var param in queryParams)
                {
                    var kv = param.Split('=', 2);
                    if (kv.Length == 2)
                    {
                        var key = kv[0].ToLowerInvariant();
                        var val = Uri.UnescapeDataString(kv[1]);
                        if (key == "sslmode" && Enum.TryParse<SslMode>(val, true, out var mode))
                        {
                            builder.SslMode = mode;
                        }
                        else if (key == "channel_binding")
                        {
                            builder["Channel Binding"] = val;
                        }
                    }
                }
            }
            return builder.ConnectionString;
        }

        return raw;
    }
}
