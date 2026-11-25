namespace LogiMarket.WinForms.Mediador;

public enum TipoPanel
{
    Acceso, PanelGeneral, Catalogo, Clientes, Carrito, Pedidos,
    Cobro, Despacho, Devoluciones, Inventario, Reportes, Auditoria
}

public interface IMediadorPrincipal
{
    event Action<TipoPanel>? PanelCambiado;
    event Action<Guid>? PedidoConfirmado;
    Func<TipoPanel, bool>? ValidadorPanel { get; set; }

    void RegistrarPanelPrincipal(Control contenedor);
    void RegistrarFabricaDePanel(TipoPanel panel, Func<Control> fabrica);
    void MostrarPanel(TipoPanel panel);
    void NotificarPedidoConfirmado(Guid pedidoId);
}
