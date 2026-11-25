using DbUp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TaskDev.Api.Adapter.OutBound.AdapterSql.Settings;

namespace TaskDev.Api.Adapter.OutBound.AdapterSql.Migrations;

public interface IDatabaseMigrator
{
    void Migrate();
}

public class DatabaseMigrator : IDatabaseMigrator
{
    private readonly IDatabaseConnectionFactory _connectionFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseMigrator> _logger;

    public DatabaseMigrator(
        IDatabaseConnectionFactory connectionFactory,
        IConfiguration configuration,
        ILogger<DatabaseMigrator> logger)
    {
        _connectionFactory = connectionFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public void Migrate()
    {
        var rawMigrationConn = new[]
        {
            _configuration["DATABASE_URL_UNPOOLED"],
            _configuration.GetConnectionString("MigrationConnection")
        }.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? _connectionFactory.ConnectionString;
        var connectionString = ConnectionPostgres.ConvertPostgresUrlToConnectionString(rawMigrationConn);

        var candidatePaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "database", "migrations"),
            Path.Combine(Directory.GetCurrentDirectory(), "database", "migrations"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "database", "migrations"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "database", "migrations"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "database", "migrations"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "database", "migrations")
        };

        string? migrationsPath = null;
        foreach (var path in candidatePaths)
        {
            var fullPath = Path.GetFullPath(path);
            if (Directory.Exists(fullPath) && Directory.GetFiles(fullPath, "*.sql").Length > 0)
            {
                migrationsPath = fullPath;
                break;
            }
        }

        if (migrationsPath == null)
        {
            throw new DirectoryNotFoundException("Diretório de migrações SQL não encontrado.");
        }

        _logger.LogInformation("Executando migrações DbUp a partir de: {Path}", migrationsPath);

        var upgrader = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsFromFileSystem(migrationsPath)
            .LogToConsole()
            .Build();

        var result = upgrader.PerformUpgrade();

        if (!result.Successful)
        {
            _logger.LogError(result.Error, "Falha ao executar migrações DbUp no banco de dados.");
            throw result.Error;
        }

        _logger.LogInformation("Migrações do banco de dados concluídas com sucesso!");
    }
}
