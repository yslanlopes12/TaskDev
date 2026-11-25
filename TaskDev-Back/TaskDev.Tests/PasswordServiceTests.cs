using TaskDev.Api.Adapter.OutBound.AdapterHttp.Client;
using Xunit;

namespace TaskDev.Tests;

public class PasswordServiceTests
{
    private readonly PasswordHasherArgon2 _service = new();

    [Fact]
    public void GenerateSecurePassword_ReturnsAtLeast16Characters()
    {
        var password = _service.GenerateSecurePassword(16);
        Assert.NotNull(password);
        Assert.True(password.Length >= 16);
    }

    [Fact]
    public void HashPassword_ProducesArgon2idFormat()
    {
        var password = "SecurePassword123!@#";
        var hash = _service.HashPassword(password);

        Assert.StartsWith("$argon2id$", hash);
    }

    [Fact]
    public void VerifyPassword_ValidPassword_ReturnsTrue()
    {
        var password = "ValidPassword123!";
        var hash = _service.HashPassword(password);

        var isValid = _service.VerifyPassword(password, hash);
        Assert.True(isValid);
    }

    [Fact]
    public void VerifyPassword_InvalidPassword_ReturnsFalse()
    {
        var password = "CorrectPassword123!";
        var hash = _service.HashPassword(password);

        var isValid = _service.VerifyPassword("WrongPassword456!", hash);
        Assert.False(isValid);
    }
}
