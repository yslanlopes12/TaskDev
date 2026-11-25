using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using TaskDev.Api.Domain.Ports.OutBound;

namespace TaskDev.Api.Adapter.OutBound.AdapterHttp.Client;

public class PasswordHasherArgon2 : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 3;
    private const int MemorySize = 65536; // 64 MB
    private const int DegreeOfParallelism = 2;

    public string HashPassword(string password)
    {
        var salt = new byte[SaltSize];
        RandomNumberGenerator.Fill(salt);

        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = DegreeOfParallelism,
            MemorySize = MemorySize,
            Iterations = Iterations
        };

        var hash = argon2.GetBytes(HashSize);

        var saltB64 = Convert.ToBase64String(salt);
        var hashB64 = Convert.ToBase64String(hash);
        return $"$argon2id$v=19$m={MemorySize},t={Iterations},p={DegreeOfParallelism}${saltB64}${hashB64}";
    }

    public bool VerifyPassword(string password, string storedHash)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(storedHash) || !storedHash.StartsWith("$argon2id$"))
            {
                return false;
            }

            var parts = storedHash.Split('$');
            if (parts.Length != 6)
            {
                return false;
            }

            var paramParts = parts[3].Split(',');
            int memory = MemorySize;
            int iterations = Iterations;
            int parallelism = DegreeOfParallelism;

            foreach (var p in paramParts)
            {
                var kv = p.Split('=');
                if (kv.Length == 2)
                {
                    if (kv[0] == "m") int.TryParse(kv[1], out memory);
                    else if (kv[0] == "t") int.TryParse(kv[1], out iterations);
                    else if (kv[0] == "p") int.TryParse(kv[1], out parallelism);
                }
            }

            var salt = Convert.FromBase64String(parts[4]);
            var expectedHash = Convert.FromBase64String(parts[5]);

            using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
            {
                Salt = salt,
                DegreeOfParallelism = parallelism,
                MemorySize = memory,
                Iterations = iterations
            };

            var actualHash = argon2.GetBytes(expectedHash.Length);
            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch
        {
            return false;
        }
    }

    public string GenerateSecurePassword(int length = 16)
    {
        if (length < 16) length = 16;

        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghijkmnopqrstuvwxyz";
        const string digits = "23456789";
        const string special = "!@#$%^&*()_-+=[]{}";
        const string all = upper + lower + digits + special;

        var chars = new char[length];
        chars[0] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
        chars[1] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
        chars[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
        chars[3] = special[RandomNumberGenerator.GetInt32(special.Length)];

        for (int i = 4; i < length; i++)
        {
            chars[i] = all[RandomNumberGenerator.GetInt32(all.Length)];
        }

        for (int i = length - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars);
    }
}
