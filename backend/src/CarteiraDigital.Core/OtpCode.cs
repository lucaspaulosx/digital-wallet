using System.Security.Cryptography;
using System.Text;

namespace CarteiraDigital.Core;

/// <summary>Código de uso único de 6 dígitos. Só o hash (com sal) é guardado no banco.</summary>
public static class OtpCode
{
    public static string Generate() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    public static string NewSalt() => Convert.ToHexString(RandomNumberGenerator.GetBytes(16));

    public static string Hash(string code, string salt) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{salt}:{code}")));

    public static bool Verify(string code, string salt, string expectedHash)
    {
        var actual = Encoding.UTF8.GetBytes(Hash(code, salt));
        var expected = Encoding.UTF8.GetBytes(expectedHash);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
