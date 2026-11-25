using Microsoft.Extensions.Configuration;
using Npgsql;
using TaskDev.Api.Adapter.OutBound.AdapterSql.Settings;

namespace TaskDev.Tests;

public class PostgresConnectionTests
{
    [Fact]
    public void ParsesManagedPostgresUrl()
    {
        const string url = "postgresql://usuario:abc%3Adef@db.example.test:5432/nome%20do%20banco?sslmode=require&channel_binding=require";
        var result = new NpgsqlConnectionStringBuilder(ConnectionPostgres.ConvertPostgresUrlToConnectionString(url));

        Assert.Equal("db.example.test", result.Host);
        Assert.Equal("abc:def", result.Password);
        Assert.Equal("nome do banco", result.Database);
        Assert.Equal(SslMode.Require, result.SslMode);
        Assert.Equal("Require", result["Channel Binding"]?.ToString());
    }

    [Fact]
    public void EmptyDatabaseUrlFallsBackToConnectionString()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["DATABASE_URL"] = "",
            ["ConnectionStrings:DefaultConnection"] = "Host=postgres;Database=taskdev;Username=taskdev;SSL Mode=Disable"
        }).Build();

        var factory = new ConnectionPostgres(config);

        Assert.Equal("postgres", new NpgsqlConnectionStringBuilder(factory.ConnectionString).Host);
    }
}
