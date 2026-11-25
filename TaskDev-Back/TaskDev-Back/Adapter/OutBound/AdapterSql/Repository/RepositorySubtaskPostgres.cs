using Dapper;
using TaskDev.Api.Adapter.OutBound.AdapterSql.Settings;
using TaskDev.Api.Domain.Core.Entities;
using TaskDev.Api.Domain.Ports.OutBound;

namespace TaskDev.Api.Adapter.OutBound.AdapterSql.Repository;

public class RepositorySubtaskPostgres : ISubtaskRepository
{
    private readonly IDatabaseConnectionFactory _connectionFactory;

    public RepositorySubtaskPostgres(IDatabaseConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Subtask?> GetByIdAsync(Guid id)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = """
            SELECT id, task_id AS TaskId, title, is_completed AS IsCompleted, position, created_at AS CreatedAt
            FROM subtasks
            WHERE id = @Id;
        """;
        return await conn.QuerySingleOrDefaultAsync<Subtask>(sql, new { Id = id });
    }

    public async Task<IReadOnlyList<Subtask>> GetByTaskIdAsync(Guid taskId)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = """
            SELECT id, task_id AS TaskId, title, is_completed AS IsCompleted, position, created_at AS CreatedAt
            FROM subtasks
            WHERE task_id = @TaskId
            ORDER BY position ASC;
        """;
        var result = await conn.QueryAsync<Subtask>(sql, new { TaskId = taskId });
        return result.ToList();
    }

    public async Task<IReadOnlyList<Subtask>> GetByTaskIdsAsync(IReadOnlyList<Guid> taskIds)
    {
        if (taskIds == null || taskIds.Count == 0) return Array.Empty<Subtask>();
        using var conn = _connectionFactory.CreateConnection();
        const string sql = """
            SELECT id, task_id AS TaskId, title, is_completed AS IsCompleted, position, created_at AS CreatedAt
            FROM subtasks
            WHERE task_id = ANY(@TaskIds)
            ORDER BY position ASC;
        """;
        var result = await conn.QueryAsync<Subtask>(sql, new { TaskIds = taskIds.ToArray() });
        return result.ToList();
    }

    public async Task<Guid> CreateAsync(Subtask subtask)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = """
            INSERT INTO subtasks (id, task_id, title, is_completed, position, created_at)
            VALUES (@Id, @TaskId, @Title, @IsCompleted, @Position, @CreatedAt)
            RETURNING id;
        """;
        return await conn.ExecuteScalarAsync<Guid>(sql, subtask);
    }

    public async Task UpdateAsync(Subtask subtask)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = """
            UPDATE subtasks
            SET title = @Title, is_completed = @IsCompleted
            WHERE id = @Id;
        """;
        await conn.ExecuteAsync(sql, subtask);
    }

    public async Task DeleteAsync(Guid id)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = "DELETE FROM subtasks WHERE id = @Id;";
        await conn.ExecuteAsync(sql, new { Id = id });
    }

    public async Task ReorderAsync(Guid taskId, IReadOnlyList<Guid> subtaskIdsInOrder)
    {
        using var conn = _connectionFactory.CreateConnection();
        conn.Open();
        using var transaction = conn.BeginTransaction();

        try
        {
            long position = 1000;
            foreach (var subtaskId in subtaskIdsInOrder)
            {
                const string sql = "UPDATE subtasks SET position = @Position WHERE id = @Id AND task_id = @TaskId;";
                await conn.ExecuteAsync(sql, new { Position = position, Id = subtaskId, TaskId = taskId }, transaction);
                position += 1000;
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<(bool Success, int PointsAwarded)> CompleteSubtaskWithXpAsync(Guid subtaskId, Guid userId, int points)
    {
        using var conn = _connectionFactory.CreateConnection();
        conn.Open();
        using var transaction = conn.BeginTransaction();
        try
        {
            const string getSubtaskSql = """
                SELECT id, task_id AS TaskId, is_completed AS IsCompleted
                FROM subtasks
                WHERE id = @Id
                FOR UPDATE;
            """;
            var subtask = await conn.QuerySingleOrDefaultAsync<Subtask>(getSubtaskSql, new { Id = subtaskId }, transaction);
            if (subtask == null)
            {
                transaction.Rollback();
                return (false, 0);
            }

            const string updateSubtaskSql = "UPDATE subtasks SET is_completed = TRUE WHERE id = @Id;";
            await conn.ExecuteAsync(updateSubtaskSql, new { Id = subtaskId }, transaction);

            int awarded = 0;
            if (points > 0)
            {
                const string insertXpSql = """
                    INSERT INTO xp_events (id, user_id, task_id, subtask_id, points, created_at)
                    VALUES (@Id, @UserId, @TaskId, @SubtaskId, @Points, NOW())
                    ON CONFLICT (subtask_id) DO NOTHING;
                """;
                var rows = await conn.ExecuteAsync(insertXpSql, new
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    TaskId = subtask.TaskId,
                    SubtaskId = subtaskId,
                    Points = points
                }, transaction);

                if (rows > 0)
                {
                    awarded = points;
                }
            }

            transaction.Commit();
            return (true, awarded);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<(bool Success, int PointsDeducted)> RevertSubtaskXpAsync(Guid subtaskId)
    {
        using var conn = _connectionFactory.CreateConnection();
        conn.Open();
        using var transaction = conn.BeginTransaction();
        try
        {
            const string getSubtaskSql = """
                SELECT id, task_id AS TaskId, is_completed AS IsCompleted
                FROM subtasks
                WHERE id = @Id
                FOR UPDATE;
            """;
            var subtask = await conn.QuerySingleOrDefaultAsync<Subtask>(getSubtaskSql, new { Id = subtaskId }, transaction);
            if (subtask == null)
            {
                transaction.Rollback();
                return (false, 0);
            }

            const string updateSubtaskSql = "UPDATE subtasks SET is_completed = FALSE WHERE id = @Id;";
            await conn.ExecuteAsync(updateSubtaskSql, new { Id = subtaskId }, transaction);

            const string deleteXpSql = "DELETE FROM xp_events WHERE subtask_id = @SubtaskId RETURNING points;";
            var deletedPoints = await conn.QueryFirstOrDefaultAsync<int?>(deleteXpSql, new { SubtaskId = subtaskId }, transaction);

            transaction.Commit();
            return (true, deletedPoints ?? 0);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
