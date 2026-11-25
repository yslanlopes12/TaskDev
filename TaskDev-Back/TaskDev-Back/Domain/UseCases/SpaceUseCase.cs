using TaskDev.Api.Domain.Core.Entities;
using TaskDev.Api.Domain.Core.ValueObjects;
using TaskDev.Api.Domain.Ports.InBound;
using TaskDev.Api.Domain.Ports.OutBound;

namespace TaskDev.Api.Domain.UseCases;

public sealed class SpaceUseCase : ISpaceUseCase
{
    private readonly ISpaceRepository _spaceRepository;
    private readonly IUserRepository _userRepository;

    public SpaceUseCase(ISpaceRepository spaceRepository, IUserRepository userRepository)
    {
        _spaceRepository = spaceRepository;
        _userRepository = userRepository;
    }

    public async Task<BaseReturn<IReadOnlyList<Space>>> GetSpacesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var spaces = await _spaceRepository.GetAccessibleSpacesByUserIdAsync(userId);
        return BaseReturn<IReadOnlyList<Space>>.Success(spaces);
    }

    public async Task<BaseReturn<Space>> GetByIdAsync(Guid spaceId, Guid userId, CancellationToken cancellationToken = default)
    {
        var hasAccess = await _spaceRepository.HasAccessAsync(spaceId, userId);
        if (!hasAccess)
        {
            return BaseReturn<Space>.NotFound("Espaço não encontrado ou acesso não permitido.");
        }

        var space = await _spaceRepository.GetByIdAsync(spaceId);
        if (space == null)
        {
            return BaseReturn<Space>.NotFound("Espaço não encontrado.");
        }

        return BaseReturn<Space>.Success(space);
    }

    public async Task<BaseReturn<Space>> CreateSpaceAsync(CreateSpaceCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return BaseReturn<Space>.ValidationError("O nome do espaço é obrigatório.");
        }

        var space = new Space
        {
            Id = Guid.NewGuid(),
            OwnerUserId = command.UserId,
            Name = command.Name.Trim(),
            Icon = command.Icon ?? "Folder",
            Color = command.Color ?? "#a69aeb",
            Visibility = command.Visibility.ToUpperInvariant() == "SHARED" ? "SHARED" : "PRIVATE"
        };

        var createdId = await _spaceRepository.CreateAsync(space);
        space.Id = createdId;

        return BaseReturn<Space>.Success(space);
    }

    public async Task<BaseReturn<Space>> UpdateSpaceAsync(UpdateSpaceCommand command, CancellationToken cancellationToken = default)
    {
        var isOwner = await _spaceRepository.IsOwnerAsync(command.SpaceId, command.UserId);
        if (!isOwner)
        {
            return BaseReturn<Space>.Forbidden("Apenas o proprietário pode editar as configurações do espaço.");
        }

        var existing = await _spaceRepository.GetByIdAsync(command.SpaceId);
        if (existing == null)
        {
            return BaseReturn<Space>.NotFound("Espaço não encontrado.");
        }

        existing.Name = command.Name.Trim();
        existing.Icon = command.Icon ?? existing.Icon;
        existing.Color = command.Color ?? existing.Color;
        existing.Visibility = command.Visibility.ToUpperInvariant() == "SHARED" ? "SHARED" : "PRIVATE";
        existing.UpdatedAt = DateTimeOffset.UtcNow;

        await _spaceRepository.UpdateAsync(existing);
        return BaseReturn<Space>.Success(existing);
    }

    public async Task<BaseReturn<bool>> DeleteSpaceAsync(Guid spaceId, Guid userId, CancellationToken cancellationToken = default)
    {
        var isOwner = await _spaceRepository.IsOwnerAsync(spaceId, userId);
        if (!isOwner)
        {
            return BaseReturn<bool>.Forbidden("Apenas o proprietário pode excluir o espaço.");
        }

        await _spaceRepository.DeleteAsync(spaceId);
        return BaseReturn<bool>.Success(true);
    }

    public async Task<BaseReturn<bool>> AddMemberAsync(Guid spaceId, Guid ownerUserId, string memberUsername, CancellationToken cancellationToken = default)
    {
        var isOwner = await _spaceRepository.IsOwnerAsync(spaceId, ownerUserId);
        if (!isOwner)
        {
            return BaseReturn<bool>.Forbidden("Apenas o proprietário pode adicionar membros ao espaço.");
        }

        var targetUser = await _userRepository.GetByUsernameAsync(memberUsername.Trim());
        if (targetUser == null)
        {
            return BaseReturn<bool>.NotFound($"Usuário '@{memberUsername}' não encontrado.");
        }

        if (targetUser.Id == ownerUserId)
        {
            return BaseReturn<bool>.BusinessError("O proprietário já faz parte do espaço implicitamente.");
        }

        await _spaceRepository.AddMemberAsync(spaceId, targetUser.Id);
        return BaseReturn<bool>.Success(true);
    }

    public async Task<BaseReturn<bool>> RemoveMemberAsync(Guid spaceId, Guid ownerUserId, Guid targetUserId, CancellationToken cancellationToken = default)
    {
        var isOwner = await _spaceRepository.IsOwnerAsync(spaceId, ownerUserId);
        if (!isOwner)
        {
            return BaseReturn<bool>.Forbidden("Apenas o proprietário pode remover membros do espaço.");
        }

        await _spaceRepository.RemoveMemberAsync(spaceId, targetUserId);
        return BaseReturn<bool>.Success(true);
    }

    public async Task<BaseReturn<IReadOnlyList<User>>> GetMembersAsync(Guid spaceId, Guid userId, CancellationToken cancellationToken = default)
    {
        var hasAccess = await _spaceRepository.HasAccessAsync(spaceId, userId);
        if (!hasAccess)
        {
            return BaseReturn<IReadOnlyList<User>>.Forbidden();
        }

        var members = await _spaceRepository.GetMembersAsync(spaceId);
        return BaseReturn<IReadOnlyList<User>>.Success(members);
    }
}
