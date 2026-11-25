using Dapper;
using TaskDev.Api.Adapter.OutBound.AdapterSql.Settings;
using TaskDev.Api.Domain.Core.Entities;
using TaskDev.Api.Domain.Ports.OutBound;

namespace TaskDev.Api.Adapter.OutBound.AdapterSql.Repository;

public class RepositoryAgentConfigPostgres : IAgentConfigRepository
{
    private readonly IDatabaseConnectionFactory _connectionFactory;

    public RepositoryAgentConfigPostgres(IDatabaseConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<AgentConfig?> GetByUserIdAsync(Guid userId)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = """
            SELECT user_id AS UserId, name, endpoint_url AS EndpointUrl,
                   provider_type AS ProviderType, model, encrypted_api_key AS EncryptedApiKey,
                   is_enabled AS IsEnabled, updated_at AS UpdatedAt
            FROM agent_configs
            WHERE user_id = @UserId;
        """;
        return await conn.QuerySingleOrDefaultAsync<AgentConfig>(sql, new { UserId = userId });
    }

    public async Task UpsertAsync(AgentConfig config)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = """
            INSERT INTO agent_configs (user_id, name, endpoint_url, provider_type, model, encrypted_api_key, is_enabled, updated_at)
            VALUES (@UserId, @Name, @EndpointUrl, @ProviderType, @Model, @EncryptedApiKey, @IsEnabled, NOW())
            ON CONFLICT (user_id) DO UPDATE
            SET name = EXCLUDED.name,
                endpoint_url = EXCLUDED.endpoint_url,
                provider_type = EXCLUDED.provider_type,
                model = EXCLUDED.model,
                encrypted_api_key = EXCLUDED.encrypted_api_key,
                is_enabled = EXCLUDED.is_enabled,
                updated_at = NOW();
        """;
        await conn.ExecuteAsync(sql, config);
    }

    public async Task DeleteAsync(Guid userId)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = "DELETE FROM agent_configs WHERE user_id = @UserId;";
        await conn.ExecuteAsync(sql, new { UserId = userId });
    }
}
