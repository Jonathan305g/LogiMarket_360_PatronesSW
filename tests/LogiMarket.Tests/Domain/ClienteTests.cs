using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Exceptions;

namespace LogiMarket.Tests.Domain;

public class ClienteTests
{
    [Fact]
    public void Constructor_DatosValidos_CreaClienteActivo()
    {
        var c = new Cliente("Ana", CategoriaCliente.Vip);

        Assert.Equal("Ana", c.Nombre);
        Assert.Equal(CategoriaCliente.Vip, c.Categoria);
        Assert.True(c.Activo);
    }

    [Fact]
    public void Constructor_NombreVacio_LanzaDomainException()
    {
        Assert.Throws<DomainException>(() => new Cliente("  ", CategoriaCliente.Normal));
    }

    [Fact]
    public void Desactivar_ClienteActivo_QuedaInactivo()
    {
        var c = new Cliente("Luis", CategoriaCliente.Normal);

        c.Desactivar();

        Assert.False(c.Activo);
    }
}