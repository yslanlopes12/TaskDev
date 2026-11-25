using Dapper;
using TaskDev.Api.Adapter.OutBound.AdapterSql.Settings;
using TaskDev.Api.Domain.Core.Entities;
using TaskDev.Api.Domain.Ports.OutBound;

namespace TaskDev.Api.Adapter.OutBound.AdapterSql.Repository;

public class RepositoryUserPostgres : IUserRepository
{
    private readonly IDatabaseConnectionFactory _connectionFactory;

    public RepositoryUserPostgres(IDatabaseConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = """
            SELECT id, first_name AS FirstName, last_name AS LastName, username,
                   password_hash AS PasswordHash, role, is_active AS IsActive,
                   auth_version AS AuthVersion, timezone, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM users
            WHERE id = @Id;
        """;
        return await conn.QuerySingleOrDefaultAsync<User>(sql, new { Id = id });
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = """
            SELECT id, first_name AS FirstName, last_name AS LastName, username,
                   password_hash AS PasswordHash, role, is_active AS IsActive,
                   auth_version AS AuthVersion, timezone, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM users
            WHERE LOWER(username) = LOWER(@Username);
        """;
        return await conn.QuerySingleOrDefaultAsync<User>(sql, new { Username = username });
    }

    public async Task<bool> ExistsUsernameAsync(string username)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = "SELECT COUNT(1) FROM users WHERE LOWER(username) = LOWER(@Username);";
        var count = await conn.ExecuteScalarAsync<int>(sql, new { Username = username });
        return count > 0;
    }

    public async Task<Guid> CreateAsync(User user)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = """
            INSERT INTO users (id, first_name, last_name, username, password_hash, role, is_active, auth_version, timezone, created_at, updated_at)
            VALUES (@Id, @FirstName, @LastName, @Username, @PasswordHash, @Role, @IsActive, @AuthVersion, @Timezone, @CreatedAt, @UpdatedAt)
            RETURNING id;
        """;
        return await conn.ExecuteScalarAsync<Guid>(sql, user);
    }

    public async Task UpdateAsync(User user)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = """
            UPDATE users
            SET first_name = @FirstName, last_name = @LastName, timezone = @Timezone, updated_at = NOW()
            WHERE id = @Id;
        """;
        await conn.ExecuteAsync(sql, user);
    }

    public async Task IncrementAuthVersionAsync(Guid id)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = "UPDATE users SET auth_version = auth_version + 1, updated_at = NOW() WHERE id = @Id;";
        await conn.ExecuteAsync(sql, new { Id = id });
    }

    public async Task<IReadOnlyList<User>> GetAllAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = """
            SELECT id, first_name AS FirstName, last_name AS LastName, username,
                   password_hash AS PasswordHash, role, is_active AS IsActive,
                   auth_version AS AuthVersion, timezone, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM users
            ORDER BY created_at DESC;
        """;
        var result = await conn.QueryAsync<User>(sql);
        return result.ToList();
    }

    public async Task<bool> HasAnyAdminAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = "SELECT COUNT(1) FROM users WHERE role = 'ADMIN' AND is_active = TRUE;";
        var count = await conn.ExecuteScalarAsync<int>(sql);
        return count > 0;
    }

    public async Task UpdateRoleAsync(Guid id, string role)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = "UPDATE users SET role = @Role, updated_at = NOW() WHERE id = @Id;";
        await conn.ExecuteAsync(sql, new { Id = id, Role = role });
    }

    public async Task UpdateActiveStatusAsync(Guid id, bool isActive)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = "UPDATE users SET is_active = @IsActive, updated_at = NOW() WHERE id = @Id;";
        await conn.ExecuteAsync(sql, new { Id = id, IsActive = isActive });
    }

    public async Task UpdatePasswordHashAsync(Guid id, string passwordHash)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = "UPDATE users SET password_hash = @PasswordHash, updated_at = NOW() WHERE id = @Id;";
        await conn.ExecuteAsync(sql, new { Id = id, PasswordHash = passwordHash });
    }
}
