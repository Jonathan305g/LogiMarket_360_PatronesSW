using LogiMarket.Application.Security;
using LogiMarket.Application.Security.Proxy;
using LogiMarket.Domain.Enums;
using LogiMarket.Tests.Fakes;

namespace LogiMarket.Tests.Patterns.Structural;

public class ProxyTests
{
    [Theory]
    [InlineData(RolUsuario.Consulta)]
    [InlineData(RolUsuario.Bodega)]
    public async Task Cobrar_RolSinPermiso_SeRechazaSinTocarLaPasarelaYQuedaAuditado(RolUsuario rol)
    {
        var pasarela = new PasarelaEspia();
        var auditoria = new AuditoriaSeguridadEspia();
        var proxy = new PagoAutorizadoProxy(pasarela, new UsuarioActualFalso(rol, "pepe"), auditoria);

        await Assert.ThrowsAsync<AccesoDenegadoException>(() => proxy.CobrarAsync(59m, "CLAVE-1"));

        Assert.Equal(0, pasarela.Invocaciones); // la pasarela (y todo lo que hay detrás) NO se invocó

        var intento = Assert.Single(auditoria.Intentos); // el intento sí quedó en la auditoría de seguridad
        Assert.Equal("pepe", intento.Usuario);
        Assert.Equal(rol, intento.Rol);
        Assert.Equal("COBRAR", intento.Operacion);
    }

    [Theory]
    [InlineData(RolUsuario.Ventas)]
    [InlineData(RolUsuario.Admin)]
    public async Task Cobrar_RolConPermiso_DelegaTalCualYNoAuditaNada(RolUsuario rol)
    {
        var pasarela = new PasarelaEspia();
        var auditoria = new AuditoriaSeguridadEspia();
        var proxy = new PagoAutorizadoProxy(pasarela, new UsuarioActualFalso(rol), auditoria);

        var resultado = await proxy.CobrarAsync(59m, "CLAVE-1");

        Assert.True(resultado.Aprobado);
        var llamada = Assert.Single(pasarela.Llamadas);
        Assert.Equal(59m, llamada.Monto);        // el monto no se altera
        Assert.Equal("CLAVE-1", llamada.Clave);  // la clave de idempotencia tampoco
        Assert.Empty(auditoria.Intentos);
    }
}