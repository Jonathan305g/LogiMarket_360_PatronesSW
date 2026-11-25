using LogiMarket.Application.Abstractions.Repositories;
using LogiMarket.Application.Queries.Interpreter;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using System.Text;

namespace LogiMarket.WinForms.Controles;

public sealed class PanelPedidos : UserControl
{
    private readonly IPedidoRepository _pedidos;

    private readonly TextBox _txtFiltro = new() { Width = 320, Font = EstiloUi.FuenteNormal };
    private readonly Button _btnFiltrar = new() { Text = "Filtrar (Interpreter)", Width = 150 };
    private readonly Label _lblErrorFiltro = new() { ForeColor = EstiloUi.ColorPeligro, AutoSize = true, Font = EstiloUi.FuenteSubtitulo };

    private readonly DataGridView _grilla = new() { Dock = DockStyle.Fill };
    private readonly TextBox _txtDetalle = new()
    {
        Dock = DockStyle.Fill,
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Vertical,
        Font = EstiloUi.FuenteMonocromatica,
        BackColor = Color.FromArgb(248, 250, 252)
    };

    private readonly Panel _panelHistorialVisual = new() { Dock = DockStyle.Bottom, Height = 48, BackColor = Color.White };
    private readonly Label _lblPasoBorrador = EstiloUi.CrearBadge("BORRADOR ●");
    private readonly Label _lblPasoValidado = EstiloUi.CrearBadge("VALIDADO ●");
    private readonly Label _lblPasoPagado = EstiloUi.CrearBadge("PAGADO ●");
    private readonly Label _lblPasoDespachado = EstiloUi.CrearBadge("DESPACHADO ●");
    private readonly Label _lblPasoEntregado = EstiloUi.CrearBadge("ENTREGADO ●");

    private List<ResumenPedido> _todosLosPedidos = new();

    public PanelPedidos(IPedidoRepository pedidos)
    {
        _pedidos = pedidos;

        Dock = DockStyle.Fill;
        BackColor = Color.White;

        ConstruirInterfaz();
        Load += async (_, _) => await CargarAsync();
    }

    private void ConstruirInterfaz()
    {
        var encabezado = EstiloUi.CrearEncabezado("Pedidos e historial", "Consulta de estados, detalle y trazabilidad de eventos");

        // Barra superior de filtros rápidos y búsqueda Interpreter
        var barraTop = new Panel { Dock = DockStyle.Top, Height = 80, Padding = new Padding(16, 8, 16, 4), BackColor = Color.White };

        var flujoFiltroGramatica = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 36, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        flujoFiltroGramatica.Controls.Add(new Label { Text = "Buscar pedido / Filtro:", Font = EstiloUi.FuenteNegrita, AutoSize = true, Margin = new Padding(0, 6, 6, 0) });
        _txtFiltro.PlaceholderText = "Ej: ESTADO = PAGADO AND TOTAL >= 50";
        flujoFiltroGramatica.Controls.Add(_txtFiltro);

        EstiloUi.AplicarBotonSecundario(_btnFiltrar);
        _btnFiltrar.Click += (_, _) => AplicarFiltro();
        flujoFiltroGramatica.Controls.Add(_btnFiltrar);

        var btnLimpiar = new Button { Text = "Limpiar", Width = 80 };
        EstiloUi.AplicarBotonSecundario(btnLimpiar);
        btnLimpiar.Click += async (_, _) =>
        {
            _txtFiltro.Clear();
            _lblErrorFiltro.Text = string.Empty;
            await CargarAsync();
        };
        flujoFiltroGramatica.Controls.Add(btnLimpiar);

        // Fila de botones rápidos de estado (Wireframe 06)
        var flujoPills = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 32, FlowDirection = FlowDirection.LeftToRight };
        flujoPills.Controls.Add(new Label { Text = "Filtros rápidos:", Font = EstiloUi.FuenteSubtitulo, ForeColor = EstiloUi.ColorTextoSecundario, AutoSize = true, Margin = new Padding(0, 4, 8, 0) });

        AgregarPillFiltro(flujoPills, "TODOS", string.Empty);
        AgregarPillFiltro(flujoPills, "BORRADOR", "ESTADO = BORRADOR");
        AgregarPillFiltro(flujoPills, "VALIDADO", "ESTADO = VALIDADO");
        AgregarPillFiltro(flujoPills, "PAGADO", "ESTADO = PAGADO");
        AgregarPillFiltro(flujoPills, "DESPACHADO", "ESTADO = DESPACHADO");
        AgregarPillFiltro(flujoPills, "ENTREGADO", "ESTADO = ENTREGADO");
        flujoPills.Controls.Add(_lblErrorFiltro);

        barraTop.Controls.Add(flujoPills);
        barraTop.Controls.Add(flujoFiltroGramatica);

        // Área central dividida: Grilla pedidos (60%) + Detalle e Historial (40%)
        var panelCentral = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(16, 8, 16, 16),
            BackColor = Color.White
        };
        panelCentral.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));
        panelCentral.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));

        // Tarjeta Grilla
        var tarjetaGrilla = EstiloUi.CrearTarjeta(0, 0, new Padding(12));
        tarjetaGrilla.Dock = DockStyle.Fill;

        EstiloUi.AplicarGrilla(_grilla);
        _grilla.Columns.Add("Id", "Id");
        _grilla.Columns.Add("Codigo", "Código");
        _grilla.Columns.Add("Cliente", "Cliente");
        _grilla.Columns.Add("Estado", "Estado");
        _grilla.Columns.Add("Total", "Total");
        _grilla.Columns["Id"]!.Visible = false;

        _grilla.SelectionChanged += async (_, _) => await MostrarDetalleAsync();
        tarjetaGrilla.Controls.Add(_grilla);

        // Tarjeta Detalle
        var tarjetaDetalle = EstiloUi.CrearTarjeta(0, 0, new Padding(14));
        tarjetaDetalle.Dock = DockStyle.Fill;

        var lblTituloDetalle = new Label
        {
            Text = "Detalle del pedido e historial",
            Font = EstiloUi.FuenteSeccion,
            ForeColor = EstiloUi.ColorTexto,
            Dock = DockStyle.Top,
            Height = 28
        };

        // Panel de pasos del ciclo de estados
        var flujoPasos = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 8, 0, 0)
        };
        flujoPasos.Controls.Add(new Label { Text = "Ciclo:", Font = EstiloUi.FuenteNegrita, AutoSize = true, Margin = new Padding(0, 6, 6, 0) });
        flujoPasos.Controls.Add(_lblPasoBorrador);
        flujoPasos.Controls.Add(_lblPasoValidado);
        flujoPasos.Controls.Add(_lblPasoPagado);
        flujoPasos.Controls.Add(_lblPasoDespachado);
        flujoPasos.Controls.Add(_lblPasoEntregado);
        _panelHistorialVisual.Controls.Add(flujoPasos);

        tarjetaDetalle.Controls.Add(_txtDetalle);
        tarjetaDetalle.Controls.Add(_panelHistorialVisual);
        tarjetaDetalle.Controls.Add(lblTituloDetalle);

        panelCentral.Controls.Add(tarjetaGrilla, 0, 0);
        panelCentral.Controls.Add(tarjetaDetalle, 1, 0);

        Controls.Add(panelCentral);
        Controls.Add(barraTop);
        Controls.Add(encabezado);
    }

    private void AgregarPillFiltro(Control contenedor, string etiqueta, string query)
    {
        var btn = new Button
        {
            Text = etiqueta,
            AutoSize = true,
            Height = 24,
            Margin = new Padding(2, 0, 4, 0),
            Cursor = Cursors.Hand
        };
        EstiloUi.AplicarBotonSecundario(btn);
        btn.Click += (_, _) =>
        {
            _txtFiltro.Text = query;
            AplicarFiltro();
        };
        contenedor.Controls.Add(btn);
    }

    private async Task CargarAsync()
    {
        _todosLosPedidos = (await _pedidos.ListarResumenAsync()).ToList();
        MostrarPedidos(_todosLosPedidos);
    }

    private void MostrarPedidos(IEnumerable<ResumenPedido> lista)
    {
        _grilla.Rows.Clear();
        foreach (var r in lista)
        {
            _grilla.Rows.Add(
                r.Id,
                $"PED-{r.Id.ToString()[..6].ToUpperInvariant()}",
                r.Cliente,
                r.Estado.ToString().ToUpperInvariant(),
                $"USD {r.Total:F2}");
        }

        if (_grilla.Rows.Count > 0)
            _grilla.Rows[0].Selected = true;
        else
            LimpiarDetalle();
    }

    private void AplicarFiltro()
    {
        _lblErrorFiltro.Text = string.Empty;
        var filtro = _txtFiltro.Text.Trim();
        if (string.IsNullOrWhiteSpace(filtro))
        {
            MostrarPedidos(_todosLosPedidos);
            return;
        }

        // Si es una búsqueda simple por código o cliente
        if (!filtro.Contains('=') && !filtro.Contains('>') && !filtro.Contains('<'))
        {
            var simples = _todosLosPedidos.Where(p =>
                p.Cliente.Contains(filtro, StringComparison.OrdinalIgnoreCase) ||
                p.Id.ToString().Contains(filtro, StringComparison.OrdinalIgnoreCase));
            MostrarPedidos(simples);
            return;
        }

        try
        {
            // Interpreter pattern
            var consultas = _todosLosPedidos.Select(p => new PedidoConsulta(p.Id, p.Estado.ToString(), p.Total, p.Cliente));
            var filtrados = FiltroPedidos.Aplicar(filtro, consultas).ToList();
            var ids = filtrados.Select(f => f.PedidoId).ToHashSet();
            MostrarPedidos(_todosLosPedidos.Where(p => ids.Contains(p.Id)));
        }
        catch (Exception ex)
        {
            _lblErrorFiltro.Text = $"Error en expresión: {ex.Message}";
        }
    }

    private async Task MostrarDetalleAsync()
    {
        if (_grilla.SelectedRows.Count == 0)
        {
            LimpiarDetalle();
            return;
        }

        var id = (Guid)_grilla.SelectedRows[0].Cells["Id"].Value!;
        var pedido = await _pedidos.ObtenerPorIdAsync(id);
        if (pedido is null) return;

        var sb = new StringBuilder();
        sb.AppendLine($"PEDIDO: PED-{pedido.Id.ToString()[..8].ToUpperInvariant()}");
        sb.AppendLine($"Estado actual: {pedido.Estado}");
        sb.AppendLine($"Subtotal: USD {pedido.Subtotal.Valor:F2}");
        sb.AppendLine($"Descuento: - USD {pedido.Descuento.Valor:F2}");
        sb.AppendLine($"Entrega ({pedido.Entrega}): USD {pedido.Tarifa.Valor:F2}");
        sb.AppendLine($"TOTAL: USD {pedido.Total.Valor:F2}");
        sb.AppendLine($"Unidades físicas: {pedido.UnidadesFisicas}");
        sb.AppendLine("--------------------------------------------------");
        sb.AppendLine("LÍNEAS DEL PEDIDO:");
        foreach (var det in pedido.Detalles)
        {
            sb.AppendLine($"  • {det.Sku} - {det.Nombre} x{det.Cantidad} (P.U. USD {det.PrecioUnitario.Valor:F2} = USD {det.Subtotal.Valor:F2})");
            foreach (var comp in det.Componentes)
                sb.AppendLine($"      ↳ SKU final: {comp.Sku} x{comp.Cantidad}");
        }

        sb.AppendLine("--------------------------------------------------");
        sb.AppendLine("TRAZABILIDAD DE ESTADOS (STATE PATTERN):");
        foreach (var h in pedido.Historial)
            sb.AppendLine($"  [{h.FechaUtc.ToLocalTime():dd/MM/yyyy HH:mm:ss}] {h.Anterior} ➔ {h.Nuevo}");

        _txtDetalle.Text = sb.ToString();

        // Actualizar visualizador de pasos
        ActualizarPasosVisuales(pedido.Estado);
    }

    private void ActualizarPasosVisuales(EstadoPedido estado)
    {
        MarcarPaso(_lblPasoBorrador, estado >= EstadoPedido.Borrador && estado != EstadoPedido.Cancelado);
        MarcarPaso(_lblPasoValidado, estado >= EstadoPedido.Validado && estado != EstadoPedido.Cancelado);
        MarcarPaso(_lblPasoPagado, estado >= EstadoPedido.Pagado && estado != EstadoPedido.Cancelado);
        MarcarPaso(_lblPasoDespachado, estado >= EstadoPedido.Despachado && estado != EstadoPedido.Cancelado);
        MarcarPaso(_lblPasoEntregado, estado == EstadoPedido.Entregado);
    }

    private static void MarcarPaso(Label badge, bool alcanzado)
    {
        if (alcanzado)
        {
            badge.BackColor = Color.FromArgb(220, 252, 231);
            badge.ForeColor = EstiloUi.ColorExito;
        }
        else
        {
            badge.BackColor = EstiloUi.ColorPill;
            badge.ForeColor = EstiloUi.ColorTextoSecundario;
        }
    }

    private void LimpiarDetalle()
    {
        _txtDetalle.Clear();
        ActualizarPasosVisuales(EstadoPedido.Borrador);
    }
}
