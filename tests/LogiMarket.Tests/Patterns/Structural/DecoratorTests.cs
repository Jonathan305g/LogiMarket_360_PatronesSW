using LogiMarket.Application.Abstractions.Gateways;
using LogiMarket.Infrastructure.Payments.Adapter;
using LogiMarket.Infrastructure.Payments.Decorator;
using LogiMarket.Infrastructure.Payments.Legacy;
using LogiMarket.Tests.Fakes;

namespace LogiMarket.Tests.Patterns.Structural;

public class DecoratorTests
{
    [Fact]
    public async Task CobroExitoso_RegistraInicioYFinEnLaBitacora()
    {
        var bitacora = new BitacoraDePrueba();
        IPasarelaPago pasarela = new AuditoriaPagoDecorator(new PasarelaModernaSimulada(), bitacora);

        var resultado = await pasarela.CobrarAsync(25m, "CLAVE-D1");

        Assert.True(resultado.Exitoso);
        Assert.Equal(2, bitacora.Registros.Count);
        Assert.Contains("INICIO", bitacora.Registros[0].Detalle);
        Assert.Contains("FIN", bitacora.Registros[1].Detalle);
    }

    [Fact]
    public async Task CobroRechazado_TambienQuedaRegistradoComoFin()
    {
        var bitacora = new BitacoraDePrueba();
        IPasarelaPago pasarela = new AuditoriaPagoDecorator(new PasarelaModernaSimulada(), bitacora);
        await pasarela.CobrarAsync(10m, "CLAVE-D2");

        await pasarela.CobrarAsync(10m, "CLAVE-D2");

        Assert.Equal(4, bitacora.Registros.Count);
        Assert.Contains("rechazado", bitacora.Registros[3].Detalle);
    }

    [Fact]
    public async Task ElDecorator_EnvuelveAlAdapterSinConocerSuImplementacion()
    {
        var bitacora = new BitacoraDePrueba();
        IPasarelaPago pasarela = new AuditoriaPagoDecorator(new AdaptadorPasarelaLegada(new PasarelaLegadaSimulada()), bitacora);

        var resultado = await pasarela.CobrarAsync(30m, "CLAVE-D3");

        Assert.True(resultado.Exitoso);
        Assert.Equal(2, bitacora.Registros.Count);
    }
}