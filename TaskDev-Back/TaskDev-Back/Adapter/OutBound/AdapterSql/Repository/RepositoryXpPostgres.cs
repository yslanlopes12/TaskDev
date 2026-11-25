using System.Data;
using Dapper;
using TaskDev.Api.Adapter.OutBound.AdapterSql.Settings;
using TaskDev.Api.Domain.Core.Entities;
using TaskDev.Api.Domain.Core.Models;
using TaskDev.Api.Domain.Ports.OutBound;

namespace TaskDev.Api.Adapter.OutBound.AdapterSql.Repository;

public class RepositoryXpPostgres : IXpRepository
{
    private readonly IDatabaseConnectionFactory _connectionFactory;

    public RepositoryXpPostgres(IDatabaseConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> GetTotalXpByUserIdAsync(Guid userId)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = "SELECT COALESCE(SUM(points), 0) FROM xp_events WHERE user_id = @UserId;";
        return await conn.ExecuteScalarAsync<int>(sql, new { UserId = userId });
    }

    public async Task<bool> HasTaskXpAsync(Guid taskId)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = "SELECT COUNT(1) FROM xp_events WHERE task_id = @TaskId;";
        var count = await conn.ExecuteScalarAsync<int>(sql, new { TaskId = taskId });
        return count > 0;
    }

    public async Task AddEventAsync(XpEvent xpEvent, IDbTransaction? transaction = null)
    {
        const string sql = """
            INSERT INTO xp_events (id, user_id, task_id, points, created_at)
            VALUES (@Id, @UserId, @TaskId, @Points, @CreatedAt)
            ON CONFLICT (task_id) DO NOTHING;
        """;

        if (transaction != null)
        {
            await transaction.Connection!.ExecuteAsync(sql, xpEvent, transaction);
        }
        else
        {
            using var conn = _connectionFactory.CreateConnection();
            await conn.ExecuteAsync(sql, xpEvent);
        }
    }

    public async Task<IReadOnlyList<RecentXpModel>> GetRecentXpEventsAsync(Guid userId, int limit = 5)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = """
            SELECT x.id, x.points, t.title AS TaskTitle, x.created_at AS CreatedAt
            FROM xp_events x
            LEFT JOIN tasks t ON x.task_id = t.id
            WHERE x.user_id = @UserId
            ORDER BY x.created_at DESC
            LIMIT @Limit;
        """;
        var result = await conn.QueryAsync<RecentXpModel>(sql, new { UserId = userId, Limit = limit });
        return result.ToList();
    }
}
