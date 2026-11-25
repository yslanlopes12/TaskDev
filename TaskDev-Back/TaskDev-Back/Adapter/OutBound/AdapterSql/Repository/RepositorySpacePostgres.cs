using Dapper;
using TaskDev.Api.Adapter.OutBound.AdapterSql.Settings;
using TaskDev.Api.Domain.Core.Entities;
using TaskDev.Api.Domain.Ports.OutBound;

namespace TaskDev.Api.Adapter.OutBound.AdapterSql.Repository;

public class RepositorySpacePostgres : ISpaceRepository
{
    private readonly IDatabaseConnectionFactory _connectionFactory;

    public RepositorySpacePostgres(IDatabaseConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Space?> GetByIdAsync(Guid id)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = """
            SELECT id, owner_user_id AS OwnerUserId, name, icon, color, visibility,
                   created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM spaces
            WHERE id = @Id;
        """;
        return await conn.QuerySingleOrDefaultAsync<Space>(sql, new { Id = id });
    }

    public async Task<IReadOnlyList<Space>> GetAccessibleSpacesByUserIdAsync(Guid userId)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = """
            SELECT DISTINCT s.id, s.owner_user_id AS OwnerUserId, s.name, s.icon, s.color, s.visibility,
                            s.created_at AS CreatedAt, s.updated_at AS UpdatedAt
            FROM spaces s
            LEFT JOIN space_members sm ON s.id = sm.space_id
            WHERE s.owner_user_id = @UserId OR sm.user_id = @UserId
            ORDER BY s.created_at ASC;
        """;
        var result = await conn.QueryAsync<Space>(sql, new { UserId = userId });
        return result.ToList();
    }

    public async Task<Guid> CreateAsync(Space space)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = """
            INSERT INTO spaces (id, owner_user_id, name, icon, color, visibility, created_at, updated_at)
            VALUES (@Id, @OwnerUserId, @Name, @Icon, @Color, @Visibility, @CreatedAt, @UpdatedAt)
            RETURNING id;
        """;
        return await conn.ExecuteScalarAsync<Guid>(sql, space);
    }

    public async Task UpdateAsync(Space space)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = """
            UPDATE spaces
            SET name = @Name, icon = @Icon, color = @Color, visibility = @Visibility, updated_at = NOW()
            WHERE id = @Id;
        """;
        await conn.ExecuteAsync(sql, space);
    }

    public async Task DeleteAsync(Guid id)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = "DELETE FROM spaces WHERE id = @Id;";
        await conn.ExecuteAsync(sql, new { Id = id });
    }

    public async Task<bool> HasAccessAsync(Guid spaceId, Guid userId)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = """
            SELECT COUNT(1)
            FROM spaces s
            LEFT JOIN space_members sm ON s.id = sm.space_id
            WHERE s.id = @SpaceId AND (s.owner_user_id = @UserId OR sm.user_id = @UserId);
        """;
        var count = await conn.ExecuteScalarAsync<int>(sql, new { SpaceId = spaceId, UserId = userId });
        return count > 0;
    }

    public async Task<bool> IsOwnerAsync(Guid spaceId, Guid userId)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = "SELECT COUNT(1) FROM spaces WHERE id = @SpaceId AND owner_user_id = @UserId;";
        var count = await conn.ExecuteScalarAsync<int>(sql, new { SpaceId = spaceId, UserId = userId });
        return count > 0;
    }

    public async Task AddMemberAsync(Guid spaceId, Guid userId)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = """
            INSERT INTO space_members (space_id, user_id, joined_at)
            VALUES (@SpaceId, @UserId, NOW())
            ON CONFLICT (space_id, user_id) DO NOTHING;
        """;
        await conn.ExecuteAsync(sql, new { SpaceId = spaceId, UserId = userId });
    }

    public async Task RemoveMemberAsync(Guid spaceId, Guid userId)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = "DELETE FROM space_members WHERE space_id = @SpaceId AND user_id = @UserId;";
        await conn.ExecuteAsync(sql, new { SpaceId = spaceId, UserId = userId });
    }

    public async Task<IReadOnlyList<User>> GetMembersAsync(Guid spaceId)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = """
            SELECT u.id, u.first_name AS FirstName, u.last_name AS LastName, u.username,
                   u.role, u.is_active AS IsActive, u.created_at AS CreatedAt
            FROM space_members sm
            INNER JOIN users u ON sm.user_id = u.id
            WHERE sm.space_id = @SpaceId
            ORDER BY u.first_name ASC;
        """;
        var result = await conn.QueryAsync<User>(sql, new { SpaceId = spaceId });
        return result.ToList();
    }
}
