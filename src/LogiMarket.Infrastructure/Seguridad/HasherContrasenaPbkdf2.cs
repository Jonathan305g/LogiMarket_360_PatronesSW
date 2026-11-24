using System.Globalization;
using System.Security.Cryptography;
using LogiMarket.Application.Abstractions.Services;

namespace LogiMarket.Infrastructure.Seguridad;

public sealed class HasherContrasenaPbkdf2 : IHasherContrasena
{
    private const int Iteraciones = 600_000;
    private const int IteracionesMinimasAceptadas = 100_000;
    private const int IteracionesMaximasAceptadas = 2_000_000;
    private const int TamanoSalBytes = 16;
    private const int TamanoHashBytes = 32;

    public string Hashear(string contrasena)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contrasena);
        var sal = RandomNumberGenerator.GetBytes(TamanoSalBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(contrasena, sal, Iteraciones, HashAlgorithmName.SHA256, TamanoHashBytes);
        return $"{Iteraciones}.{Convert.ToBase64String(sal)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verificar(string contrasena, string hashAlmacenado)
    {
        if (string.IsNullOrEmpty(contrasena) || string.IsNullOrWhiteSpace(hashAlmacenado))
            return false;

        var partes = hashAlmacenado.Split('.');
        if (partes.Length != 3
            || !int.TryParse(partes[0], NumberStyles.None, CultureInfo.InvariantCulture, out var iteraciones)
            || iteraciones is < IteracionesMinimasAceptadas or > IteracionesMaximasAceptadas)
            return false;

        try
        {
            var sal = Convert.FromBase64String(partes[1]);
            var hashEsperado = Convert.FromBase64String(partes[2]);
            if (sal.Length != TamanoSalBytes || hashEsperado.Length != TamanoHashBytes)
                return false;

            var hashCalculado = Rfc2898DeriveBytes.Pbkdf2(
                contrasena, sal, iteraciones, HashAlgorithmName.SHA256, hashEsperado.Length);
            return CryptographicOperations.FixedTimeEquals(hashCalculado, hashEsperado);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
