using LogiMarket.Application.UseCases.Catalogo;
using LogiMarket.Application.UseCases.Clientes;
using LogiMarket.Domain.Enums;
using LogiMarket.Tests.Fakes;

namespace LogiMarket.Tests.Application;

public sealed class GestionAccesoCasosUsoTests
{
    [Theory]
    [InlineData(RolUsuario.Admin, true, true, true, true, true, true, true)]
    [InlineData(RolUsuario.Ventas, false, false, true, false, false, true, false)]
    [InlineData(RolUsuario.Bodega, true, false, false, true, true, false, false)]
    [InlineData(RolUsuario.Consulta, true, false, false, true, false, true, false)]
    public void MatrizBasicaDeAcceso(
        RolUsuario rol,
        bool consultarCatalogo,
        bool administrarCatalogo,
        bool gestionarClientes,
        bool consultarInventario,
        bool ajustarInventario,
        bool verReportes,
        bool auditar)
    {
        Assert.Equal(consultarCatalogo, LogiMarket.Application.Security.PoliticaDeAcceso.PuedeConsultarCatalogo(rol));
        Assert.Equal(administrarCatalogo, LogiMarket.Application.Security.PoliticaDeAcceso.PuedeAdministrarCatalogo(rol));
        Assert.Equal(gestionarClientes, LogiMarket.Application.Security.PoliticaDeAcceso.PuedeGestionarClientes(rol));
        Assert.Equal(consultarInventario, LogiMarket.Application.Security.PoliticaDeAcceso.PuedeConsultarInventario(rol));
        Assert.Equal(ajustarInventario, LogiMarket.Application.Security.PoliticaDeAcceso.PuedeAjustarInventario(rol));
        Assert.Equal(verReportes, LogiMarket.Application.Security.PoliticaDeAcceso.PuedeVerReportes(rol));
        Assert.Equal(auditar, LogiMarket.Application.Security.PoliticaDeAcceso.PuedeAuditar(rol));
    }
    [Fact]
    public async Task Catalogo_ConsultaNoPuedeCambiarProductos()
    {
        var casoUso = new GestionCatalogoCasoUso(null!, null!, null!, new UsuarioActualFalso(RolUsuario.Consulta));

        var resultado = await casoUso.GuardarProductoAsync("P99", "Prueba", 10m, 0.2m, 1);

        Assert.False(resultado.Exitoso);
        Assert.Equal("NO_AUTORIZADO", resultado.Error!.Codigo);
    }

    [Fact]
    public async Task Clientes_ConsultaNoPuedeRegistrarClientes()
    {
        var almacen = new AlmacenEnMemoria();
        var casoUso = new GestionClientesCasoUso(almacen, almacen, new UsuarioActualFalso(RolUsuario.Consulta));

        var resultado = await casoUso.GuardarAsync(null, "Cliente bloqueado", CategoriaCliente.Normal);

        Assert.False(resultado.Exitoso);
        Assert.Equal(0, almacen.Commits);
        Assert.Empty(await ((LogiMarket.Application.Abstractions.Repositories.IClienteRepository)almacen).ListarAsync());
    }

    [Fact]
    public async Task Clientes_VentasPuedeRegistrarYEditar()
    {
        var almacen = new AlmacenEnMemoria();
        var casoUso = new GestionClientesCasoUso(almacen, almacen, new UsuarioActualFalso(RolUsuario.Ventas));

        var alta = await casoUso.GuardarAsync(null, "Cliente válido", CategoriaCliente.Normal);
        var cliente = Assert.Single(await ((LogiMarket.Application.Abstractions.Repositories.IClienteRepository)almacen).ListarAsync());
        var edicion = await casoUso.GuardarAsync(cliente.Id, "Cliente actualizado", CategoriaCliente.Vip);

        Assert.True(alta.Exitoso);
        Assert.True(edicion.Exitoso);
        Assert.Equal("Cliente actualizado", cliente.Nombre);
        Assert.Equal(CategoriaCliente.Vip, cliente.Categoria);
        Assert.Equal(2, almacen.Commits);
    }
}
