using TaskDev.Api.Domain.Core.Entities;
using TaskDev.Api.Domain.Core.Enums;
using TaskDev.Api.Domain.Core.ValueObjects;
using TaskDev.Api.Domain.Ports.InBound;
using TaskDev.Api.Domain.Ports.OutBound;

namespace TaskDev.Api.Domain.UseCases;

public sealed class AdminUseCase : IAdminUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuthUseCase _authUseCase;

    public AdminUseCase(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IAuthUseCase authUseCase)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _authUseCase = authUseCase;
    }

    public async Task<BaseReturn<RegisterResult>> BootstrapAdminAsync(BootstrapAdminCommand command, CancellationToken cancellationToken = default)
    {
        var hasAdmin = await _userRepository.HasAnyAdminAsync();
        if (hasAdmin)
        {
            return BaseReturn<RegisterResult>.Forbidden("O sistema já possui um administrador cadastrado. Bootstrap bloqueado.");
        }

        var regResult = await _authUseCase.RegisterAsync(new RegisterCommand(command.FirstName, command.LastName, null), cancellationToken);
        if (!regResult.IsSuccess || regResult.SuccessObject == null)
        {
            return regResult;
        }

        var user = regResult.SuccessObject.User;
        await _userRepository.UpdateRoleAsync(user.Id, EnumRole.Admin);
        user.Role = EnumRole.Admin;

        return BaseReturn<RegisterResult>.Success(new RegisterResult(user, regResult.SuccessObject.GeneratedPassword));
    }

    public async Task<BaseReturn<IReadOnlyList<User>>> GetAllUsersAsync(Guid requestedByUserId, CancellationToken cancellationToken = default)
    {
        var requester = await _userRepository.GetByIdAsync(requestedByUserId);
        if (requester == null || (requester.Role != EnumRole.Admin && requester.Role != EnumRole.Operator))
        {
            return BaseReturn<IReadOnlyList<User>>.Forbidden();
        }

        var users = await _userRepository.GetAllAsync();
        return BaseReturn<IReadOnlyList<User>>.Success(users);
    }

    public async Task<BaseReturn<ResetPasswordResult>> ResetPasswordAsync(Guid requestedByUserId, Guid targetUserId, CancellationToken cancellationToken = default)
    {
        var requester = await _userRepository.GetByIdAsync(requestedByUserId);
        if (requester == null) return BaseReturn<ResetPasswordResult>.Unauthorized();

        var target = await _userRepository.GetByIdAsync(targetUserId);
        if (target == null) return BaseReturn<ResetPasswordResult>.NotFound("Usuário não encontrado.");

        if (requester.Role == EnumRole.Operator)
        {
            if (target.Role != EnumRole.Member)
            {
                return BaseReturn<ResetPasswordResult>.Forbidden("Operadores só podem redefinir senhas de usuários comuns (MEMBER).");
            }
        }
        else if (requester.Role != EnumRole.Admin)
        {
            return BaseReturn<ResetPasswordResult>.Forbidden();
        }

        var newPassword = _passwordHasher.GenerateSecurePassword(16);
        var newHash = _passwordHasher.HashPassword(newPassword);

        await _userRepository.UpdatePasswordHashAsync(target.Id, newHash);
        await _userRepository.IncrementAuthVersionAsync(target.Id);

        return BaseReturn<ResetPasswordResult>.Success(new ResetPasswordResult(target.Username, newPassword));
    }

    public async Task<BaseReturn<bool>> UpdateUserRoleAsync(Guid requestedByUserId, Guid targetUserId, string newRole, CancellationToken cancellationToken = default)
    {
        var requester = await _userRepository.GetByIdAsync(requestedByUserId);
        if (requester == null || requester.Role != EnumRole.Admin)
        {
            return BaseReturn<bool>.Forbidden("Apenas administradores podem alterar perfis de acesso.");
        }

        if (!EnumRole.IsValid(newRole))
        {
            return BaseReturn<bool>.ValidationError("Perfil inválido.");
        }

        var target = await _userRepository.GetByIdAsync(targetUserId);
        if (target == null) return BaseReturn<bool>.NotFound("Usuário não encontrado.");

        await _userRepository.UpdateRoleAsync(target.Id, newRole);
        return BaseReturn<bool>.Success(true);
    }

    public async Task<BaseReturn<bool>> UpdateUserActiveAsync(Guid requestedByUserId, Guid targetUserId, bool isActive, CancellationToken cancellationToken = default)
    {
        var requester = await _userRepository.GetByIdAsync(requestedByUserId);
        if (requester == null || requester.Role != EnumRole.Admin)
        {
            return BaseReturn<bool>.Forbidden("Apenas administradores podem ativar ou desativar contas.");
        }

        var target = await _userRepository.GetByIdAsync(targetUserId);
        if (target == null) return BaseReturn<bool>.NotFound("Usuário não encontrado.");

        if (target.Id == requester.Id && !isActive)
        {
            return BaseReturn<bool>.BusinessError("Você não pode desativar sua própria conta.");
        }

        await _userRepository.UpdateActiveStatusAsync(target.Id, isActive);
        if (!isActive)
        {
            await _userRepository.IncrementAuthVersionAsync(target.Id);
        }

        return BaseReturn<bool>.Success(true);
    }
}
