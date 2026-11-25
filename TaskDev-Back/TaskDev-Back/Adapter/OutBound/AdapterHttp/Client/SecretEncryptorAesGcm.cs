using System.Security.Cryptography;
using System.Text;
using TaskDev.Api.Domain.Ports.OutBound;

namespace TaskDev.Api.Adapter.OutBound.AdapterHttp.Client;

public class SecretEncryptorAesGcm : ISecretEncryptor
{
    private readonly byte[] _key;
    private const int NonceSize = 12; // 96 bits recomendado pelo NIST para AES-GCM
    private const int TagSize = 16;   // 128 bits

    public SecretEncryptorAesGcm(IConfiguration configuration)
    {
        var base64Key = configuration["AGENT_ENCRYPTION_KEY"] ?? configuration["Security:AgentEncryptionKey"];
        var key = new byte[32];
        if (string.IsNullOrWhiteSpace(base64Key) ||
            !Convert.TryFromBase64String(base64Key, key, out var bytesWritten) || bytesWritten != key.Length)
        {
            throw new InvalidOperationException("Configure AGENT_ENCRYPTION_KEY com uma chave de 32 bytes em Base64.");
        }
        _key = key;
    }

    public string Encrypt(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext)) return string.Empty;

        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);

        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagSize];

        using var aesGcm = new AesGcm(_key, TagSize);
        aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        // Formato final: Base64(nonce + tag + ciphertext)
        var combined = new byte[NonceSize + TagSize + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, combined, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, combined, NonceSize, TagSize);
        Buffer.BlockCopy(ciphertext, 0, combined, NonceSize + TagSize, ciphertext.Length);

        return Convert.ToBase64String(combined);
    }

    public string Decrypt(string ciphertextBase64)
    {
        if (string.IsNullOrEmpty(ciphertextBase64)) return string.Empty;

        try
        {
            var combined = Convert.FromBase64String(ciphertextBase64);
            if (combined.Length < NonceSize + TagSize)
                return string.Empty;

            var nonce = new byte[NonceSize];
            var tag = new byte[TagSize];
            var cipherLength = combined.Length - NonceSize - TagSize;
            var ciphertext = new byte[cipherLength];
            var plaintext = new byte[cipherLength];

            Buffer.BlockCopy(combined, 0, nonce, 0, NonceSize);
            Buffer.BlockCopy(combined, NonceSize, tag, 0, TagSize);
            Buffer.BlockCopy(combined, NonceSize + TagSize, ciphertext, 0, cipherLength);

            using var aesGcm = new AesGcm(_key, TagSize);
            aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);

            return Encoding.UTF8.GetString(plaintext);
        }
        catch
        {
            return string.Empty;
        }
    }
}
