using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using TaskDev.Api.Adapter.OutBound.AdapterHttp.Client;
using Xunit;

namespace TaskDev.Tests;

public class EncryptionServiceTests
{
    private readonly SecretEncryptorAesGcm _service;

    public EncryptionServiceTests()
    {
        // 32 bytes base64 encoded
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);
        var base64Key = Convert.ToBase64String(key);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:AgentEncryptionKey"] = base64Key
            })
            .Build();

        _service = new SecretEncryptorAesGcm(config);
    }

    [Fact]
    public void EncryptAndDecrypt_MatchesOriginalPlaintext()
    {
        var plaintext = "chave-ficticia-para-teste";
        var encrypted = _service.Encrypt(plaintext);

        Assert.NotNull(encrypted);
        Assert.NotEqual(plaintext, encrypted);

        var decrypted = _service.Decrypt(encrypted);
        Assert.Equal(plaintext, decrypted);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("invalida")]
    [InlineData("YWJj")]
    public void RejectsMissingOrInvalidKey(string? key)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["AGENT_ENCRYPTION_KEY"] = key })
            .Build();

        Assert.Throws<InvalidOperationException>(() => new SecretEncryptorAesGcm(configuration));
    }
}
