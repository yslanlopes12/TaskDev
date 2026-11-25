using Dapper;
using TaskDev.Api.Adapter.OutBound.AdapterSql.Settings;
using TaskDev.Api.Domain.Core.Entities;
using TaskDev.Api.Domain.Ports.OutBound;
using TaskDev.Api.Domain.UseCases;

namespace TaskDev.Api.Adapter.OutBound.AdapterSql.Repository;

public class RepositoryTaskPostgres : ITaskRepository
{
    private readonly IDatabaseConnectionFactory _connectionFactory;

    public RepositoryTaskPostgres(IDatabaseConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<TaskItem?> GetByIdAsync(Guid id)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = """
            SELECT id, space_id AS SpaceId, created_by_user_id AS CreatedByUserId,
                   assignee_user_id AS AssigneeUserId, title, description, status,
                   priority, due_date AS DueDate, position, xp_awarded AS XpAwarded,
                   completed_at AS CompletedAt, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM tasks
            WHERE id = @Id;
        """;
        return await conn.QuerySingleOrDefaultAsync<TaskItem>(sql, new { Id = id });
    }

    public async Task<IReadOnlyList<TaskItem>> GetBySpaceIdAsync(Guid spaceId)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = """
            SELECT id, space_id AS SpaceId, created_by_user_id AS CreatedByUserId,
                   assignee_user_id AS AssigneeUserId, title, description, status,
                   priority, due_date AS DueDate, position, xp_awarded AS XpAwarded,
                   completed_at AS CompletedAt, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM tasks
            WHERE space_id = @SpaceId
            ORDER BY position ASC;
        """;
        var result = await conn.QueryAsync<TaskItem>(sql, new { SpaceId = spaceId });
        return result.ToList();
    }

    public async Task<IReadOnlyList<TaskItem>> GetAccessibleTasksByUserIdAsync(Guid userId)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = """
            SELECT DISTINCT t.id, t.space_id AS SpaceId, t.created_by_user_id AS CreatedByUserId,
                            t.assignee_user_id AS AssigneeUserId, t.title, t.description, t.status,
                            t.priority, t.due_date AS DueDate, t.position, t.xp_awarded AS XpAwarded,
                            t.completed_at AS CompletedAt, t.created_at AS CreatedAt, t.updated_at AS UpdatedAt
            FROM tasks t
            INNER JOIN spaces s ON t.space_id = s.id
            LEFT JOIN space_members sm ON s.id = sm.space_id
            WHERE s.owner_user_id = @UserId OR sm.user_id = @UserId
            ORDER BY t.position ASC;
        """;
        var result = await conn.QueryAsync<TaskItem>(sql, new { UserId = userId });
        return result.ToList();
    }

    public async Task<Guid> CreateAsync(TaskItem task)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = """
            WITH new_task AS (
                INSERT INTO tasks (id, space_id, created_by_user_id, assignee_user_id, title, description, status, priority, due_date, position, xp_awarded, created_at, updated_at)
                VALUES (@Id, @SpaceId, @CreatedByUserId, @AssigneeUserId, @Title, @Description, @Status, @Priority, @DueDate, @Position, @XpAwarded, @CreatedAt, @UpdatedAt)
                RETURNING id
            ), new_subtasks AS (
                INSERT INTO subtasks (id, task_id, title, is_completed, position, created_at)
                SELECT entry.id, new_task.id, entry.title, FALSE, entry.ordinal * 1000, @CreatedAt
                FROM new_task
                CROSS JOIN unnest(@SubtaskIds, @SubtaskTitles) WITH ORDINALITY AS entry(id, title, ordinal)
            )
            SELECT id FROM new_task;
        """;
        var parameters = new
        {
            task.Id,
            task.SpaceId,
            task.CreatedByUserId,
            task.AssigneeUserId,
            task.Title,
            task.Description,
            task.Status,
            task.Priority,
            DueDate = task.DueDate.HasValue ? task.DueDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
            task.Position,
            task.XpAwarded,
            task.CreatedAt,
            task.UpdatedAt,
            SubtaskIds = task.Subtasks.Select(subtask => subtask.Id).ToArray(),
            SubtaskTitles = task.Subtasks.Select(subtask => subtask.Title).ToArray()
        };
        return await conn.ExecuteScalarAsync<Guid>(sql, parameters);
    }

    public async Task UpdateAsync(TaskItem task)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = """
            UPDATE tasks
            SET title = @Title, description = @Description, status = @Status,
                priority = @Priority, due_date = @DueDate, assignee_user_id = @AssigneeUserId,
                updated_at = NOW()
            WHERE id = @Id;
        """;
        var parameters = new
        {
            task.Id,
            task.Title,
            task.Description,
            task.Status,
            task.Priority,
            DueDate = task.DueDate.HasValue ? task.DueDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
            task.AssigneeUserId
        };
        await conn.ExecuteAsync(sql, parameters);
    }

    public async Task DeleteAsync(Guid id)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = "DELETE FROM tasks WHERE id = @Id;";
        await conn.ExecuteAsync(sql, new { Id = id });
    }

    public async Task<long> GetMaxPositionAsync(Guid spaceId, string status)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = "SELECT COALESCE(MAX(position), 0) FROM tasks WHERE space_id = @SpaceId AND status = @Status;";
        return await conn.ExecuteScalarAsync<long>(sql, new { SpaceId = spaceId, Status = status });
    }

    public async Task MoveAsync(Guid taskId, string newStatus, long newPosition)
    {
        using var conn = _connectionFactory.CreateConnection();
        conn.Open();
        using var transaction = conn.BeginTransaction();

        try
        {
            const string sql = """
                UPDATE tasks
                SET status = @Status, position = @Position, updated_at = NOW()
                WHERE id = @Id;
            """;
            await conn.ExecuteAsync(sql, new { Id = taskId, Status = newStatus, Position = newPosition }, transaction);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<(bool Success, int PointsAwarded)> CompleteWithXpTransactionAsync(Guid taskId, Guid completedByUserId)
    {
        using var conn = _connectionFactory.CreateConnection();
        conn.Open();
        using var transaction = conn.BeginTransaction();

        try
        {
            const string selectTaskSql = """
                SELECT id, priority, xp_awarded AS XpAwarded
                FROM tasks
                WHERE id = @Id
                FOR UPDATE;
            """;
            var task = await conn.QuerySingleOrDefaultAsync<TaskItem>(selectTaskSql, new { Id = taskId }, transaction);
            if (task == null)
            {
                transaction.Rollback();
                return (false, 0);
            }

            int points = 0;
            bool newlyAwarded = false;

            if (!task.XpAwarded)
            {
                int totalTargetXp = GamificationRules.GetXpForPriority(task.Priority);

                const string checkAwardedSql = "SELECT COALESCE(SUM(points), 0) FROM xp_events WHERE task_id = @TaskId;";
                int alreadyAwarded = await conn.ExecuteScalarAsync<int>(checkAwardedSql, new { TaskId = taskId }, transaction);

                int remainingPoints = Math.Max(0, totalTargetXp - alreadyAwarded);

                if (remainingPoints > 0)
                {
                    const string insertXpSql = """
                        INSERT INTO xp_events (id, user_id, task_id, subtask_id, points, created_at)
                        VALUES (@Id, @UserId, @TaskId, NULL, @Points, NOW());
                    """;
                    await conn.ExecuteAsync(insertXpSql, new
                    {
                        Id = Guid.NewGuid(),
                        UserId = completedByUserId,
                        TaskId = taskId,
                        Points = remainingPoints
                    }, transaction);

                    points = remainingPoints;
                    newlyAwarded = true;
                }
            }

            const string updateSubtasksSql = "UPDATE subtasks SET is_completed = TRUE WHERE task_id = @Id;";
            await conn.ExecuteAsync(updateSubtasksSql, new { Id = taskId }, transaction);

            const string updateTaskSql = """
                UPDATE tasks
                SET status = 'DONE', completed_at = NOW(), xp_awarded = TRUE, updated_at = NOW()
                WHERE id = @Id;
            """;
            await conn.ExecuteAsync(updateTaskSql, new { Id = taskId }, transaction);

            transaction.Commit();
            return (newlyAwarded, points);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<(bool Success, int PointsDeducted)> RevertXpTransactionAsync(Guid taskId, Guid userId, string newStatus)
    {
        using var conn = _connectionFactory.CreateConnection();
        conn.Open();
        using var transaction = conn.BeginTransaction();

        try
        {
            const string selectTaskSql = """
                SELECT id, priority, status, xp_awarded AS XpAwarded
                FROM tasks
                WHERE id = @Id
                FOR UPDATE;
            """;
            var task = await conn.QuerySingleOrDefaultAsync<TaskItem>(selectTaskSql, new { Id = taskId }, transaction);
            if (task == null)
            {
                transaction.Rollback();
                return (false, 0);
            }

            int pointsDeducted = 0;
            bool reverted = false;

            if (task.XpAwarded)
            {
                const string getParentPointsSql = "SELECT COALESCE(SUM(points), 0) FROM xp_events WHERE task_id = @TaskId AND subtask_id IS NULL;";
                int parentPoints = await conn.ExecuteScalarAsync<int>(getParentPointsSql, new { TaskId = taskId }, transaction);

                if (parentPoints > 0)
                {
                    const string deleteXpSql = "DELETE FROM xp_events WHERE task_id = @TaskId AND subtask_id IS NULL;";
                    await conn.ExecuteAsync(deleteXpSql, new { TaskId = taskId }, transaction);
                    pointsDeducted = parentPoints;
                    reverted = true;
                }
            }

            const string updateTaskSql = """
                UPDATE tasks
                SET status = @NewStatus, completed_at = NULL, xp_awarded = FALSE, updated_at = NOW()
                WHERE id = @Id;
            """;
            await conn.ExecuteAsync(updateTaskSql, new { Id = taskId, NewStatus = newStatus }, transaction);

            transaction.Commit();
            return (reverted, pointsDeducted);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task UpdateStatusAsync(Guid taskId, string newStatus)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = """
            UPDATE tasks
            SET status = @Status,
                completed_at = CASE WHEN @Status = 'DONE' THEN NOW() ELSE NULL END,
                updated_at = NOW()
            WHERE id = @Id;
        """;
        await conn.ExecuteAsync(sql, new { Id = taskId, Status = newStatus });
    }
}
