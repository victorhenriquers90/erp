using System.Security.Cryptography;
using System.Text;

namespace ProjetoVarejo.Infrastructure.Data;

public static class SenhaHasher
{
    public static string Hash(string senha)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var key = Rfc2898DeriveBytes.Pbkdf2(senha, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
    }

    public static bool Verifica(string senha, string hash)
    {
        if (string.IsNullOrEmpty(hash)) return false;
        var partes = hash.Split('.');
        if (partes.Length != 2) return false;
        try
        {
            var salt = Convert.FromBase64String(partes[0]);
            var esperado = Convert.FromBase64String(partes[1]);
            if (salt.Length == 0 || esperado.Length == 0) return false;
            var calculado = Rfc2898DeriveBytes.Pbkdf2(senha ?? "", salt, 100_000, HashAlgorithmName.SHA256, 32);
            return CryptographicOperations.FixedTimeEquals(calculado, esperado);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
