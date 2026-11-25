using LogiMarket.WinForms.Mediador;

namespace LogiMarket.Tests.Patterns.Behavioral;

public class MediatorTests
{
    [Fact]
    public void Mediador_RegistraFabricasYCambiaPaneles_NotificaCambio() // P28
    {
        var mediador = new MediadorPrincipal();
        using var contenedor = new Panel();
        mediador.RegistrarPanelPrincipal(contenedor);

        var panelGeneralCreado = false;
        var panelPedidosCreado = false;

        mediador.RegistrarFabricaDePanel(TipoPanel.PanelGeneral, () =>
        {
            panelGeneralCreado = true;
            return new Label { Text = "Dashboard" };
        });

        mediador.RegistrarFabricaDePanel(TipoPanel.Pedidos, () =>
        {
            panelPedidosCreado = true;
            return new Label { Text = "Pedidos" };
        });

        TipoPanel? ultimoPanel = null;
        mediador.PanelCambiado += p => ultimoPanel = p;

        mediador.MostrarPanel(TipoPanel.PanelGeneral);

        Assert.True(panelGeneralCreado);
        Assert.Equal(TipoPanel.PanelGeneral, ultimoPanel);
        Assert.Single(contenedor.Controls);
        Assert.Equal("Dashboard", contenedor.Controls[0].Text);

        // Cambiar a Pedidos
        mediador.MostrarPanel(TipoPanel.Pedidos);

        Assert.True(panelPedidosCreado);
        Assert.Equal(TipoPanel.Pedidos, ultimoPanel);
        Assert.Single(contenedor.Controls);
        Assert.Equal("Pedidos", contenedor.Controls[0].Text);
    }

    [Fact]
    public void Mediador_NotificarPedidoConfirmado_DisparaEventoYTransicionaAPedidos() // P28
    {
        var mediador = new MediadorPrincipal();
        using var contenedor = new Panel();
        mediador.RegistrarPanelPrincipal(contenedor);

        mediador.RegistrarFabricaDePanel(TipoPanel.Pedidos, () => new Label { Text = "Vista Pedidos" });

        Guid? idNotificado = null;
        mediador.PedidoConfirmado += id => idNotificado = id;

        var idPrueba = Guid.NewGuid();
        mediador.NotificarPedidoConfirmado(idPrueba);

        Assert.Equal(idPrueba, idNotificado);
        Assert.Single(contenedor.Controls);
        Assert.Equal("Vista Pedidos", contenedor.Controls[0].Text);
    }

    [Fact]
    public async Task Mediador_ConfirmacionActualizaLasVistasOperativas()
    {
        var mediador = new MediadorPrincipal();
        using var contenedor = new Panel();
        mediador.RegistrarPanelPrincipal(contenedor);
        mediador.RegistrarFabricaDePanel(TipoPanel.Pedidos, () => new Label { Text = "Pedidos" });

        var catalogoActualizado = false;
        var inventarioActualizado = false;
        var pedidosActualizados = false;

        mediador.RegistrarActualizacion(TipoPanel.Catalogo, () =>
        {
            catalogoActualizado = true;
            return Task.CompletedTask;
        });
        mediador.RegistrarActualizacion(TipoPanel.Inventario, () =>
        {
            inventarioActualizado = true;
            return Task.CompletedTask;
        });
        mediador.RegistrarActualizacion(TipoPanel.Pedidos, () =>
        {
            pedidosActualizados = true;
            return Task.CompletedTask;
        });

        mediador.NotificarPedidoConfirmado(Guid.NewGuid());
        await mediador.ActualizarPanelesAsync(
            TipoPanel.Catalogo,
            TipoPanel.Inventario,
            TipoPanel.Pedidos);

        Assert.True(catalogoActualizado);
        Assert.True(inventarioActualizado);
        Assert.True(pedidosActualizados);
    }
}
