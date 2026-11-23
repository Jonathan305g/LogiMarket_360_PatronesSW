using LogiMarket.Application.Notifications.FactoryMethod;
using LogiMarket.Tests.Fakes;

namespace LogiMarket.Tests.Patterns.Creational;

public class FactoryMethodTests
{
    [Fact]
    public async Task CreadorAvisoEmail_Notificar_EnviaPorElCanalDeEmailConElMismoContrato()
    {
        var emailEspia = new CanalEmailEspia();
        var smsEspia = new CanalSmsEspia();
        CreadorAviso creador = new CreadorAvisoEmail(emailEspia); // se trabaja contra la clase BASE

        var canal = await creador.NotificarAsync("ana@test.com", "Asunto", "Cuerpo");

        Assert.Equal("EMAIL", canal);
        var enviado = Assert.Single(emailEspia.Enviados);
        Assert.Equal("ana@test.com", enviado.Destinatario);
        Assert.Equal("Asunto", enviado.Asunto);
        Assert.Empty(smsEspia.Enviados); // el otro canal NO se tocó
    }

    [Fact]
    public async Task CreadorAvisoSms_Notificar_EnviaPorElCanalDeSms()
    {
        var emailEspia = new CanalEmailEspia();
        var smsEspia = new CanalSmsEspia();
        CreadorAviso creador = new CreadorAvisoSms(smsEspia);

        var canal = await creador.NotificarAsync("+593999999999", "Asunto", "Cuerpo");

        Assert.Equal("SMS", canal);
        var enviado = Assert.Single(smsEspia.Enviados);
        Assert.Contains("Cuerpo", enviado.Mensaje);
        Assert.Empty(emailEspia.Enviados);
    }

    [Fact]
    public async Task CambiarDeCreador_NoRequiereCrearConcretosEnElCodigoCliente()
    {
        // El código "cliente" solo conoce CreadorAviso (la clase base). Cambiar de EMAIL
        // a SMS es cambiar QUÉ INSTANCIA se inyecta, no tocar ninguna lógica de envío.
        var emailEspia = new CanalEmailEspia();
        var smsEspia = new CanalSmsEspia();

        async Task<string> Enviar(CreadorAviso creador) => await creador.NotificarAsync("destino", "A", "B");

        var porEmail = await Enviar(new CreadorAvisoEmail(emailEspia));
        var porSms = await Enviar(new CreadorAvisoSms(smsEspia));

        Assert.Equal("EMAIL", porEmail);
        Assert.Equal("SMS", porSms);
    }
}