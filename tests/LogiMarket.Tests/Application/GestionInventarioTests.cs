using LogiMarket.Application.UseCases.Inventario;
using LogiMarket.Domain.Enums;
using LogiMarket.Tests.Fakes;

namespace LogiMarket.Tests.Application;

public sealed class GestionInventarioTests
{
    [Fact]
    public async Task Bodega_AjustaExistenciaYGuardaMovimiento()
    {
        var escenario = new Escenario(RolUsuario.Bodega);
        var casoUso = new AjustarInventarioCasoUso(
            escenario.Almacen, escenario.Almacen, escenario.Almacen, escenario.Usuario);

        var resultado = await casoUso.EjecutarAsync(new SolicitudAjusteInventario("P01", -2));

        Assert.True(resultado.Exitoso);
        Assert.Equal(6, escenario.Stock("P01"));
        var movimiento = Assert.Single(escenario.Almacen.Movimientos);
        Assert.Equal(-2, movimiento.Cantidad);
        Assert.Equal(MotivoMovimiento.Ajuste, movimiento.Motivo);
        Assert.Equal(1, escenario.Almacen.Commits);
    }

    [Fact]
    public async Task Consulta_NoPuedeAjustarInventarioNiGuardarMovimiento()
    {
        var escenario = new Escenario(RolUsuario.Consulta);
        var casoUso = new AjustarInventarioCasoUso(
            escenario.Almacen, escenario.Almacen, escenario.Almacen, escenario.Usuario);

        var resultado = await casoUso.EjecutarAsync(new SolicitudAjusteInventario("P01", 2));

        Assert.False(resultado.Exitoso);
        Assert.Equal("NO_AUTORIZADO", resultado.Error!.Codigo);
        Assert.Equal(8, escenario.Stock("P01"));
        Assert.Empty(escenario.Almacen.Movimientos);
        Assert.Equal(0, escenario.Almacen.Commits);
    }

    [Fact]
    public async Task AjusteQueDejaStockNegativo_NoGuardaCambios()
    {
        var escenario = new Escenario(RolUsuario.Bodega);
        var casoUso = new AjustarInventarioCasoUso(
            escenario.Almacen, escenario.Almacen, escenario.Almacen, escenario.Usuario);

        var resultado = await casoUso.EjecutarAsync(new SolicitudAjusteInventario("P01", -9));

        Assert.False(resultado.Exitoso);
        Assert.Equal(8, escenario.Stock("P01"));
        Assert.Empty(escenario.Almacen.Movimientos);
        Assert.Equal(0, escenario.Almacen.Commits);
    }
}
