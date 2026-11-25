using LogiMarket.Application.Abstractions.Gateways;
using LogiMarket.Infrastructure.Payments.Adapter;
using LogiMarket.Infrastructure.Payments.Legacy;

namespace LogiMarket.Tests.Patterns.Structural;

public class AdapterTests
{
    [Fact]
    public async Task Adaptador_TraduceMontoACentavosYDevuelveReferencia()
    {
        IPasarelaPago pasarela = new AdaptadorPasarelaLegada(new PasarelaLegadaSimulada());

        var resultado = await pasarela.CobrarAsync(59.00m, "CLAVE-001");

        Assert.True(resultado.Exitoso);
        Assert.StartsWith("LEG-", resultado.Referencia);
    }

    [Fact]
    public async Task Adaptador_ClaveRepetida_FallaSinExcepcion()
    {
        IPasarelaPago pasarela = new AdaptadorPasarelaLegada(new PasarelaLegadaSimulada());
        await pasarela.CobrarAsync(10m, "CLAVE-REP");

        var resultado = await pasarela.CobrarAsync(10m, "CLAVE-REP");

        Assert.False(resultado.Exitoso);
    }

    [Fact]
    public async Task DosProveedoresDistintos_SonIntercambiablesPorLaMismaInterfaz()
    {
        IPasarelaPago[] proveedores =
        {
            new AdaptadorPasarelaLegada(new PasarelaLegadaSimulada()),
            new PasarelaModernaSimulada()
        };

        foreach (var p in proveedores)
            Assert.True((await p.CobrarAsync(15m, Guid.NewGuid().ToString())).Exitoso);
    }

    [Fact]
    public async Task Proveedores_RespetanElContratoDeAprobacionRechazoEIdempotencia() // P16, P38 (LSP)
    {
        Func<IPasarelaPago>[] fabricas =
        {
            () => new AdaptadorPasarelaLegada(new PasarelaLegadaSimulada()),
            () => new PasarelaModernaSimulada()
        };

        foreach (var fabrica in fabricas)
        {
            var proveedor = fabrica();
            var aprobado = await proveedor.CobrarAsync(15m, "CLAVE-OK");
            var duplicado = await proveedor.CobrarAsync(15m, "CLAVE-OK");
            var invalido = await proveedor.CobrarAsync(0m, "CLAVE-INVALIDA");

            Assert.True(aprobado.Aprobado);
            Assert.False(string.IsNullOrWhiteSpace(aprobado.Referencia));
            Assert.False(duplicado.Aprobado);
            Assert.Equal("CLAVE_DUPLICADA", duplicado.MotivoRechazo);
            Assert.False(invalido.Aprobado);
            Assert.Equal("MONTO_INVALIDO", invalido.MotivoRechazo);
        }
    }
}
