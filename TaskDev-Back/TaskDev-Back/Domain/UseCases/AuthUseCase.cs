using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using TaskDev.Api.Domain.Core.Entities;
using TaskDev.Api.Domain.Core.ValueObjects;
using TaskDev.Api.Domain.Ports.InBound;
using TaskDev.Api.Domain.Ports.OutBound;

namespace TaskDev.Api.Domain.UseCases;

public sealed class AuthUseCase : IAuthUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly ISpaceRepository _spaceRepository;
    private readonly IXpRepository _xpRepository;
    private readonly IPasswordHasher _passwordHasher;

    public AuthUseCase(
        IUserRepository userRepository,
        ISpaceRepository spaceRepository,
        IXpRepository xpRepository,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _spaceRepository = spaceRepository;
        _xpRepository = xpRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<BaseReturn<RegisterResult>> RegisterAsync(RegisterCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.FirstName) || string.IsNullOrWhiteSpace(command.LastName))
        {
            return BaseReturn<RegisterResult>.ValidationError("Nome e sobrenome são obrigatórios.");
        }

        if (!string.IsNullOrWhiteSpace(command.Password) && command.Password.Trim().Length < 6)
        {
            return BaseReturn<RegisterResult>.ValidationError("A senha deve conter no mínimo 6 caracteres.");
        }

        var username = await GenerateUniqueUsernameAsync(command.FirstName, command.LastName);
        var rawPassword = !string.IsNullOrWhiteSpace(command.Password)
            ? command.Password.Trim()
            : _passwordHasher.GenerateSecurePassword(16);
        var passwordHash = _passwordHasher.HashPassword(rawPassword);

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = command.FirstName.Trim(),
            LastName = command.LastName.Trim(),
            Username = username,
            PasswordHash = passwordHash,
            Role = "MEMBER",
            IsActive = true,
            AuthVersion = 1,
            Timezone = command.Timezone
        };

        var userId = await _userRepository.CreateAsync(user);
        user.Id = userId;

        // Cria o espaço pessoal inicial
        try
        {
            var initialSpace = new Space
            {
                Id = Guid.NewGuid(),
                OwnerUserId = userId,
                Name = "Geral",
                Icon = "Folder",
                Color = "#a69aeb",
                Visibility = "PRIVATE"
            };
            await _spaceRepository.CreateAsync(initialSpace);
        }
        catch
        {
            // Não bloqueia o registro se o espaço inicial falhar
        }

        return BaseReturn<RegisterResult>.Success(new RegisterResult(user, rawPassword));
    }

    public async Task<BaseReturn<LoginResult>> LoginAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Username) || string.IsNullOrWhiteSpace(command.Password))
        {
            return BaseReturn<LoginResult>.ValidationError("Usuário e senha são obrigatórios.");
        }

        var user = await _userRepository.GetByUsernameAsync(command.Username.Trim());
        if (user == null || !user.IsActive)
        {
            return BaseReturn<LoginResult>.Unauthorized("Credenciais inválidas.");
        }

        var isPasswordValid = _passwordHasher.VerifyPassword(command.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            return BaseReturn<LoginResult>.Unauthorized("Credenciais inválidas.");
        }

        var totalXp = await _xpRepository.GetTotalXpByUserIdAsync(user.Id);
        var level = GamificationRules.CalculateLevel(totalXp);
        var rank = GamificationRules.CalculateRank(level);

        return BaseReturn<LoginResult>.Success(new LoginResult(user, totalXp, level, rank));
    }

    public async Task<BaseReturn<LoginResult>> GetMeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || !user.IsActive)
        {
            return BaseReturn<LoginResult>.Unauthorized();
        }

        var totalXp = await _xpRepository.GetTotalXpByUserIdAsync(user.Id);
        var level = GamificationRules.CalculateLevel(totalXp);
        var rank = GamificationRules.CalculateRank(level);

        return BaseReturn<LoginResult>.Success(new LoginResult(user, totalXp, level, rank));
    }

    private async Task<string> GenerateUniqueUsernameAsync(string firstName, string lastName)
    {
        var normalizedFirst = NormalizeName(firstName);
        var normalizedLast = NormalizeName(lastName);

        var baseUsername = $"{normalizedFirst}.{normalizedLast}";
        if (baseUsername.Length > 45)
        {
            baseUsername = baseUsername[..45];
        }

        var candidate = baseUsername;
        var suffix = 1;

        while (await _userRepository.ExistsUsernameAsync(candidate))
        {
            candidate = $"{baseUsername}{suffix}";
            suffix++;
        }

        return candidate;
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "user";

        var normalizedString = name.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                if (char.IsLetterOrDigit(c))
                {
                    stringBuilder.Append(c);
                }
            }
        }

        var clean = stringBuilder.ToString();
        clean = Regex.Replace(clean, @"\s+", "");
        return string.IsNullOrWhiteSpace(clean) ? "user" : clean;
    }
}
