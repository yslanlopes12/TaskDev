using System.Data;
using TaskDev.Api.Domain.Core.Entities;
using TaskDev.Api.Domain.Core.Models;

namespace TaskDev.Api.Domain.Ports.OutBound;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByUsernameAsync(string username);
    Task<bool> ExistsUsernameAsync(string username);
    Task<Guid> CreateAsync(User user);
    Task UpdateAsync(User user);
    Task IncrementAuthVersionAsync(Guid id);
    Task<IReadOnlyList<User>> GetAllAsync();
    Task<bool> HasAnyAdminAsync();
    Task UpdateRoleAsync(Guid id, string role);
    Task UpdateActiveStatusAsync(Guid id, bool isActive);
    Task UpdatePasswordHashAsync(Guid id, string passwordHash);
}

public interface ISpaceRepository
{
    Task<Space?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<Space>> GetAccessibleSpacesByUserIdAsync(Guid userId);
    Task<Guid> CreateAsync(Space space);
    Task UpdateAsync(Space space);
    Task DeleteAsync(Guid id);
    Task<bool> HasAccessAsync(Guid spaceId, Guid userId);
    Task<bool> IsOwnerAsync(Guid spaceId, Guid userId);
    Task AddMemberAsync(Guid spaceId, Guid userId);
    Task RemoveMemberAsync(Guid spaceId, Guid userId);
    Task<IReadOnlyList<User>> GetMembersAsync(Guid spaceId);
}

public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<TaskItem>> GetBySpaceIdAsync(Guid spaceId);
    Task<IReadOnlyList<TaskItem>> GetAccessibleTasksByUserIdAsync(Guid userId);
    Task<Guid> CreateAsync(TaskItem task);
    Task UpdateAsync(TaskItem task);
    Task DeleteAsync(Guid id);
    Task<long> GetMaxPositionAsync(Guid spaceId, string status);
    Task MoveAsync(Guid taskId, string newStatus, long newPosition);
    Task<(bool Success, int PointsAwarded)> CompleteWithXpTransactionAsync(Guid taskId, Guid completedByUserId);
    Task<(bool Success, int PointsDeducted)> RevertXpTransactionAsync(Guid taskId, Guid userId, string newStatus);
    Task UpdateStatusAsync(Guid taskId, string newStatus);
}

public interface ISubtaskRepository
{
    Task<Subtask?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<Subtask>> GetByTaskIdAsync(Guid taskId);
    Task<IReadOnlyList<Subtask>> GetByTaskIdsAsync(IReadOnlyList<Guid> taskIds);
    Task<Guid> CreateAsync(Subtask subtask);
    Task UpdateAsync(Subtask subtask);
    Task DeleteAsync(Guid id);
    Task ReorderAsync(Guid taskId, IReadOnlyList<Guid> subtaskIdsInOrder);
    Task<(bool Success, int PointsAwarded)> CompleteSubtaskWithXpAsync(Guid subtaskId, Guid userId, int points);
    Task<(bool Success, int PointsDeducted)> RevertSubtaskXpAsync(Guid subtaskId);
}

public interface IAgentConfigRepository
{
    Task<AgentConfig?> GetByUserIdAsync(Guid userId);
    Task UpsertAsync(AgentConfig config);
    Task DeleteAsync(Guid userId);
}

public interface IXpRepository
{
    Task<int> GetTotalXpByUserIdAsync(Guid userId);
    Task<bool> HasTaskXpAsync(Guid taskId);
    Task AddEventAsync(XpEvent xpEvent, IDbTransaction? transaction = null);
    Task<IReadOnlyList<RecentXpModel>> GetRecentXpEventsAsync(Guid userId, int limit = 5);
}

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string storedHash);
    string GenerateSecurePassword(int length = 16);
}

public interface ISecretEncryptor
{
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
}

public interface IAgentHttpClient
{
    Task<bool> TestConnectionAsync(string endpointUrl, string? apiKey, string providerType, string? model, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SuggestedTaskModel>> PreviewTasksAsync(string endpointUrl, string? apiKey, string providerType, string? model, string prompt, CancellationToken cancellationToken = default);
    Task<AgentRawCompletion> ChatAsync(string endpointUrl, string? apiKey, string providerType, string? model, string systemPrompt, IReadOnlyList<AgentChatMessageModel> messages, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> ListModelsAsync(string endpointUrl, string? apiKey, string providerType, CancellationToken cancellationToken = default);
}
