using System.Data;
using Dapper;
using TaskDev.Api.Adapter.OutBound.AdapterSql.Migrations;
using TaskDev.Api.Adapter.OutBound.AdapterSql.Repository;
using TaskDev.Api.Adapter.OutBound.AdapterSql.Settings;
using TaskDev.Api.Domain.Ports.OutBound;

namespace TaskDev.Api.Infra.Configuration.OutBound;

public static class ConfigurationSqlAdapter
{
    static ConfigurationSqlAdapter()
    {
        SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
    }

    public static IServiceCollection AddSqlAdapterConfiguration(this IServiceCollection services)
    {
        services.AddSingleton<IDatabaseConnectionFactory, ConnectionPostgres>();
        services.AddTransient<IDatabaseMigrator, DatabaseMigrator>();

        services.AddScoped<IUserRepository, RepositoryUserPostgres>();
        services.AddScoped<ISpaceRepository, RepositorySpacePostgres>();
        services.AddScoped<ITaskRepository, RepositoryTaskPostgres>();
        services.AddScoped<ISubtaskRepository, RepositorySubtaskPostgres>();
        services.AddScoped<IAgentConfigRepository, RepositoryAgentConfigPostgres>();
        services.AddScoped<IXpRepository, RepositoryXpPostgres>();

        return services;
    }
}

public class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        parameter.DbType = DbType.Date;
        parameter.Value = value.ToDateTime(TimeOnly.MinValue);
    }

    public override DateOnly Parse(object value) =>
        value switch
        {
            DateTime dt => DateOnly.FromDateTime(dt),
            DateOnly d => d,
            string s when DateOnly.TryParse(s, out var d) => d,
            _ => default
        };
}
