using LogiMarket.Infrastructure.Seguridad;

namespace LogiMarket.Tests.Infrastructure;

public sealed class HasherContrasenaTests
{
    private readonly HasherContrasenaPbkdf2 _hasher = new();

    [Fact]
    public void Verificar_ConLaContrasenaCorrecta_Coincide()
    {
        var hash = _hasher.Hashear("Admin123!");

        Assert.True(_hasher.Verificar("Admin123!", hash));
    }

    [Fact]
    public void Verificar_ConContrasenaIncorrecta_NoCoincide()
    {
        var hash = _hasher.Hashear("Admin123!");

        Assert.False(_hasher.Verificar("OtraClave", hash));
    }

    [Fact]
    public void Hashear_LaMismaContrasenaDosVeces_UsaSalesDiferentes()
    {
        var hash1 = _hasher.Hashear("Admin123!");
        var hash2 = _hasher.Hashear("Admin123!");

        Assert.NotEqual(hash1, hash2);
        Assert.True(_hasher.Verificar("Admin123!", hash1));
        Assert.True(_hasher.Verificar("Admin123!", hash2));
    }

    [Theory]
    [InlineData("")]
    [InlineData("no-es-un-hash")]
    [InlineData("999999999999999999999.x.y")]
    [InlineData("600000.bm90LWJhc2U2NA==.bm90LWJhc2U2NA==")]
    public void Verificar_HashInvalido_DevuelveFalse(string hash)
    {
        Assert.False(_hasher.Verificar("Admin123!", hash));
    }
}
