using LogiMarket.Application.Abstractions.Repositories;
using LogiMarket.Application.Abstractions.Services;
using LogiMarket.Domain.Enums;
using LogiMarket.WinForms.Sesion;
using LogiMarket.WinForms.Mediador;
using LogiMarket.Application.Security;

namespace LogiMarket.WinForms.Controles;

public sealed class PanelGeneral : UserControl
{
    private readonly IPedidoRepository _pedidos;
    private readonly ICatalogoRepository _catalogo;
    private readonly IProcesadorOutbox _procesadorOutbox;
    private readonly IDevolucionRepository _devoluciones;
    private readonly UsuarioActualEnSesion _sesion;
    private readonly IMediadorPrincipal _mediador;

    private readonly FlowLayoutPanel _panelMetricas = new()
    {
        Dock = DockStyle.Top,
        Height = 100,
        Padding = new Padding(12, 4, 12, 4),
        FlowDirection = FlowDirection.LeftToRight,
        WrapContents = false,
        BackColor = Color.White
    };

    private readonly Label _lblValActivos = new();
    private readonly Label _lblValDespachar = new();
    private readonly Label _lblValStockBajo = new();
    private readonly Label _lblValVentas = new();
    private readonly List<Panel> _tarjetasMetricas = new();

    private readonly DataGridView _grillaPedidos = new()
    {
        Dock = DockStyle.Fill
    };

    private readonly Label _lblAlertaStock = new() { AutoSize = true, Margin = new Padding(0, 4, 0, 4), Font = EstiloUi.FuenteNormal };
    private readonly Label _lblAlertaOutbox = new() { AutoSize = true, Margin = new Padding(0, 4, 0, 4), Font = EstiloUi.FuenteNormal };
    private readonly Label _lblAlertaDevoluciones = new() { AutoSize = true, Margin = new Padding(0, 4, 0, 4), Font = EstiloUi.FuenteNormal };

    public PanelGeneral(
        IPedidoRepository pedidos,
        ICatalogoRepository catalogo,
        IProcesadorOutbox procesadorOutbox,
        IDevolucionRepository devoluciones,
        UsuarioActualEnSesion sesion,
        IMediadorPrincipal mediador)
    {
        _pedidos = pedidos;
        _catalogo = catalogo;
        _procesadorOutbox = procesadorOutbox;
        _devoluciones = devoluciones;
        _sesion = sesion;
        _mediador = mediador;

        Dock = DockStyle.Fill;
        BackColor = Color.White;
        ConstruirInterfaz();
        _mediador.RegistrarActualizacion(TipoPanel.PanelGeneral, CargarDatosAsync);
        Load += async (_, _) => await CargarDatosAsync();
    }

    private void ConstruirInterfaz()
    {
        var encabezado = EstiloUi.CrearEncabezado("Panel general", "Resumen de operaciones, pedidos y movimientos recientes");

        ConfigurarResumenPorRol();

        // Área central dividida: Pedidos recientes (izquierda) + Alertas (derecha)
        var panelCentral = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(16, 8, 16, 16),
            BackColor = Color.White
        };
        panelCentral.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70f));
        panelCentral.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));

        // Contenedor Pedidos Recientes
        var tarjetaPedidos = EstiloUi.CrearTarjeta(0, 0, new Padding(12));
        tarjetaPedidos.Dock = DockStyle.Fill;

        var headerPedidos = new Panel { Dock = DockStyle.Top, Height = 36 };
        var lblTituloPedidos = new Label
        {
            Text = "Pedidos recientes",
            Font = EstiloUi.FuenteSeccion,
            ForeColor = EstiloUi.ColorTexto,
            Dock = DockStyle.Left,
            AutoSize = true
        };
        var btnRefrescar = new Button
        {
            Text = "Actualizar",
            Dock = DockStyle.Right,
            Width = 90
        };
        EstiloUi.AplicarBotonSecundario(btnRefrescar);
        btnRefrescar.Click += async (_, _) => await CargarDatosAsync();

        headerPedidos.Controls.Add(lblTituloPedidos);
        headerPedidos.Controls.Add(btnRefrescar);

        EstiloUi.AplicarGrilla(_grillaPedidos);
        _grillaPedidos.Columns.Add("Pedido", "Pedido");
        _grillaPedidos.Columns.Add("Cliente", "Cliente");
        _grillaPedidos.Columns.Add("Estado", "Estado");
        _grillaPedidos.Columns.Add("Total", "Total");

        tarjetaPedidos.Controls.Add(_grillaPedidos);
        tarjetaPedidos.Controls.Add(headerPedidos);

        // Contenedor Alertas
        var tarjetaAlertas = EstiloUi.CrearTarjeta(0, 0, new Padding(14));
        tarjetaAlertas.Dock = DockStyle.Fill;

        var lblTituloAlertas = new Label
        {
            Text = "Alertas",
            Font = EstiloUi.FuenteSeccion,
            ForeColor = EstiloUi.ColorTexto,
            Dock = DockStyle.Top,
            Height = 32
        };

        var panelListaAlertas = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true
        };

        ConfigurarAlertasAccionesRapidas(panelListaAlertas);

        tarjetaAlertas.Controls.Add(panelListaAlertas);
        tarjetaAlertas.Controls.Add(lblTituloAlertas);

        panelCentral.Controls.Add(tarjetaPedidos, 0, 0);
        panelCentral.Controls.Add(tarjetaAlertas, 1, 0);

        Controls.Add(panelCentral);
        Controls.Add(_panelMetricas);
        Controls.Add(encabezado);
    }

    private void ConfigurarResumenPorRol()
    {
        _panelMetricas.Controls.Clear();
        _tarjetasMetricas.Clear();

        AgregarTarjetaMetricas("Pedidos activos", _lblValActivos, 180);

        if (_sesion.Rol is RolUsuario.Admin or RolUsuario.Bodega)
            AgregarTarjetaMetricas("Por despachar", _lblValDespachar, 180);

        if (_sesion.Rol is RolUsuario.Admin or RolUsuario.Bodega or RolUsuario.Consulta)
            AgregarTarjetaMetricas("Stock bajo", _lblValStockBajo, 180);

        if (_sesion.Rol is RolUsuario.Admin or RolUsuario.Ventas or RolUsuario.Consulta)
            AgregarTarjetaMetricas("Ventas (USD)", _lblValVentas, 200);
    }

    private void AgregarTarjetaMetricas(string titulo, Label valor, int ancho)
    {
        var tarjeta = CrearTarjeta(titulo, valor, ancho);
        _tarjetasMetricas.Add(tarjeta);
        _panelMetricas.Controls.Add(tarjeta);
    }

    private void ConfigurarAlertasAccionesRapidas(FlowLayoutPanel contenedor)
    {
        if (_sesion.Rol is RolUsuario.Admin or RolUsuario.Bodega or RolUsuario.Consulta)
            contenedor.Controls.Add(_lblAlertaStock);

        if (_sesion.Rol == RolUsuario.Admin)
            contenedor.Controls.Add(_lblAlertaOutbox);

        if (_sesion.Rol is RolUsuario.Admin or RolUsuario.Ventas)
            contenedor.Controls.Add(_lblAlertaDevoluciones);

        AgregarAccesoSiPermitido(contenedor, "Abrir catálogo", TipoPanel.Catalogo,
            PoliticaDeAcceso.PuedeConsultarCatalogo(_sesion.Rol));
        AgregarAccesoSiPermitido(contenedor, "Abrir clientes", TipoPanel.Clientes,
            PoliticaDeAcceso.PuedeGestionarClientes(_sesion.Rol));
        AgregarAccesoSiPermitido(contenedor, "Abrir pedidos", TipoPanel.Pedidos,
            PoliticaDeAcceso.PuedeVerPedidos(_sesion.Rol));
        AgregarAccesoSiPermitido(contenedor, "Abrir inventario", TipoPanel.Inventario,
            PoliticaDeAcceso.PuedeConsultarInventario(_sesion.Rol));
        AgregarAccesoSiPermitido(contenedor, "Abrir reportes", TipoPanel.Reportes,
            PoliticaDeAcceso.PuedeVerReportes(_sesion.Rol));
        AgregarAccesoSiPermitido(contenedor, "Nuevo carrito", TipoPanel.Carrito,
            PoliticaDeAcceso.PuedeAbrirCarrito(_sesion.Rol));
        AgregarAccesoSiPermitido(contenedor, "Cobrar pedido", TipoPanel.Cobro,
            PoliticaDeAcceso.PuedeCobrar(_sesion.Rol));
        AgregarAccesoSiPermitido(contenedor, "Gestionar despacho", TipoPanel.Despacho,
            PoliticaDeAcceso.PuedeDespachar(_sesion.Rol));
        AgregarAccesoSiPermitido(contenedor, "Gestionar devoluciones", TipoPanel.Devoluciones,
            PoliticaDeAcceso.PuedeProcesarDevolucion(_sesion.Rol));
        AgregarAccesoSiPermitido(contenedor, "Abrir auditoría", TipoPanel.Auditoria,
            PoliticaDeAcceso.PuedeAuditar(_sesion.Rol));
    }

    private void AgregarAccesoSiPermitido(FlowLayoutPanel contenedor, string texto, TipoPanel destino, bool permitido)
    {
        if (!permitido) return;
        contenedor.Controls.Add(CrearAccesoRapido(texto, destino));
    }

    private Button CrearAccesoRapido(string texto, TipoPanel destino)
    {
        var boton = new Button
        {
            Text = texto,
            Width = 230,
            Height = 34,
            Margin = new Padding(0, 4, 0, 4),
            Enabled = true
        };
        EstiloUi.AplicarBotonSecundario(boton);
        boton.Click += (_, _) => _mediador.MostrarPanel(destino);
        return boton;
    }

    private static Panel CrearTarjeta(string titulo, Label lblValor, int ancho)
    {
        var tarjeta = EstiloUi.CrearTarjeta(ancho, 84, new Padding(12, 10, 12, 10));
        var lblTitulo = new Label
        {
            Text = titulo,
            Font = EstiloUi.FuenteNegrita,
            ForeColor = EstiloUi.ColorTextoSecundario,
            Dock = DockStyle.Top,
            Height = 20
        };
        lblValor.Font = new Font("Segoe UI", 18f, FontStyle.Bold);
        lblValor.ForeColor = EstiloUi.ColorPrimarioOscuro;
        lblValor.Dock = DockStyle.Fill;
        lblValor.TextAlign = ContentAlignment.MiddleLeft;
        lblValor.Text = "—";

        tarjeta.Controls.Add(lblValor);
        tarjeta.Controls.Add(lblTitulo);
        return tarjeta;
    }

    public async Task CargarDatosAsync()
    {
        try
        {
            var resumenes = await _pedidos.ListarResumenAsync();

            int activos = resumenes.Count(p => p.Estado is EstadoPedido.Borrador or EstadoPedido.Validado or EstadoPedido.Pagado or EstadoPedido.Despachado);
            int porDespachar = resumenes.Count(p => p.Estado == EstadoPedido.Pagado);
            decimal ventas = resumenes
                .Where(p => p.Estado is EstadoPedido.Pagado or EstadoPedido.Despachado or EstadoPedido.Entregado)
                .Sum(p => p.Total);

            _lblValActivos.Text = activos.ToString("D2");
            _lblValDespachar.Text = porDespachar.ToString("D2");
            _lblValVentas.Text = ventas.ToString("N2");

            // Productos stock bajo
            var catalogo = await _catalogo.ListarResumenAsync();
            int stockBajo = catalogo.Count(p => !p.EsKit && (p.Stock ?? 0) <= 5);
            _lblValStockBajo.Text = stockBajo.ToString("D2");

            // Outbox pendientes
            int countOutbox = await _procesadorOutbox.ContarPendientesAsync();

            // Devoluciones
            var devoluciones = await _devoluciones.ListarAsync();
            int countDev = devoluciones.Count;

            // Alertas
            _lblAlertaStock.Text = $"• {stockBajo} producto(s) con stock bajo (≤ 5)";
            _lblAlertaStock.ForeColor = stockBajo > 0 ? EstiloUi.ColorPeligro : EstiloUi.ColorExito;

            _lblAlertaOutbox.Text = $"• {countOutbox} evento(s) Outbox pendientes";
            _lblAlertaOutbox.ForeColor = countOutbox > 0 ? Color.FromArgb(217, 119, 6) : EstiloUi.ColorExito;

            _lblAlertaDevoluciones.Text = $"• {countDev} devolución(es) registrada(s)";
            _lblAlertaDevoluciones.ForeColor = EstiloUi.ColorTexto;

            // Grilla pedidos recientes (últimos 15)
            _grillaPedidos.Rows.Clear();
            foreach (var r in resumenes.Take(15))
            {
                _grillaPedidos.Rows.Add(
                    $"PED-{r.Id.ToString()[..6].ToUpperInvariant()}",
                    r.Cliente,
                    r.Estado.ToString().ToUpperInvariant(),
                    $"USD {r.Total:F2}");
            }
        }
        catch (Exception ex)
        {
            _lblAlertaOutbox.Text = $"• No se pudo actualizar el resumen: {ex.Message}";
            _lblAlertaOutbox.ForeColor = EstiloUi.ColorPeligro;
        }
    }
}
