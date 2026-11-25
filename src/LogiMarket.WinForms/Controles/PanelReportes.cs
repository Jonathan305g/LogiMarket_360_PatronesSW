using LogiMarket.Application.Abstractions.Repositories;
using LogiMarket.Application.Abstractions.Services;
using LogiMarket.Application.Comprobantes.Bridge;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;

namespace LogiMarket.WinForms.Controles;

public sealed class PanelReportes : UserControl
{
    private readonly IPedidoRepository _pedidos;
    private readonly IDevolucionRepository _devoluciones;
    private readonly IProcesadorOutbox _procesadorOutbox;
    private readonly IFabricaSalidaArchivo _fabricaSalidaArchivo;

    private readonly DateTimePicker _dtpDesde = new() { Width = 115, Format = DateTimePickerFormat.Short, Font = EstiloUi.FuenteNormal };
    private readonly DateTimePicker _dtpHasta = new() { Width = 115, Format = DateTimePickerFormat.Short, Font = EstiloUi.FuenteNormal };
    private readonly Button _btnConsultar = new() { Text = "Consultar", Width = 95 };

    private readonly Label _lblValConfirmados = new();
    private readonly Label _lblValVentasNetas = new();
    private readonly Label _lblValDevoluciones = new();

    private readonly ComboBox _cmbPedido = new() { Width = 260, DropDownStyle = ComboBoxStyle.DropDownList, Font = EstiloUi.FuenteNormal };
    private readonly ComboBox _cmbTipoDocumento = new() { Width = 260, DropDownStyle = ComboBoxStyle.DropDownList, Font = EstiloUi.FuenteNormal };

    private readonly TextBox _txtComprobante = new()
    {
        Dock = DockStyle.Fill,
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Vertical,
        Font = EstiloUi.FuenteMonocromatica,
        BackColor = Color.FromArgb(248, 250, 252)
    };

    private readonly Button _btnVistaPrevia = new() { Text = "Vista previa", Width = 260, Height = 36 };
    private readonly Button _btnExportarTxt = new() { Text = "Exportar TXT", Width = 260, Height = 36 };
    private readonly Button _btnProcesarOutbox = new() { Text = "Procesar Outbox pendientes", Width = 260, Height = 36 };
    private readonly Label _lblMensaje = new() { AutoSize = true, Font = EstiloUi.FuenteNormal, Margin = new Padding(0, 10, 0, 0) };

    public PanelReportes(
        IPedidoRepository pedidos,
        IDevolucionRepository devoluciones,
        IProcesadorOutbox procesadorOutbox,
        IFabricaSalidaArchivo fabricaSalidaArchivo)
    {
        _pedidos = pedidos;
        _devoluciones = devoluciones;
        _procesadorOutbox = procesadorOutbox;
        _fabricaSalidaArchivo = fabricaSalidaArchivo;

        Dock = DockStyle.Fill;
        BackColor = Color.White;

        ConstruirInterfaz();
        Load += async (_, _) => await CargarAsync();
    }

    private void ConstruirInterfaz()
    {
        var encabezado = EstiloUi.CrearEncabezado("Reportes y comprobantes", "Consulta, formatos de presentación y medios de salida");

        // Barra superior con filtros por fecha
        var barraTop = new Panel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(16, 8, 16, 0), BackColor = Color.White };
        var flujoTop = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };

        _dtpDesde.Value = DateTime.Today.AddDays(-30);
        _dtpHasta.Value = DateTime.Today.AddDays(1);

        flujoTop.Controls.Add(new Label { Text = "Desde:", Font = EstiloUi.FuenteNegrita, AutoSize = true, Margin = new Padding(0, 6, 4, 0) });
        flujoTop.Controls.Add(_dtpDesde);

        flujoTop.Controls.Add(new Label { Text = "Hasta:", Font = EstiloUi.FuenteNegrita, AutoSize = true, Margin = new Padding(12, 6, 4, 0) });
        flujoTop.Controls.Add(_dtpHasta);

        EstiloUi.AplicarBotonPrimario(_btnConsultar);
        _btnConsultar.Height = 28;
        _btnConsultar.Click += async (_, _) => await CargarAsync();
        flujoTop.Controls.Add(_btnConsultar);

        barraTop.Controls.Add(flujoTop);

        // 3 Tarjetas métricas
        var panelMetricas = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 88,
            Padding = new Padding(16, 4, 16, 4),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.White
        };

        panelMetricas.Controls.Add(CrearTarjetaMetrica("Pedidos confirmados", _lblValConfirmados, 200));
        panelMetricas.Controls.Add(CrearTarjetaMetrica("Ventas netas", _lblValVentasNetas, 240));
        panelMetricas.Controls.Add(CrearTarjetaMetrica("Devoluciones", _lblValDevoluciones, 200));

        // Área central dividida: Vista previa (65%) + Controles Comprobante (35%)
        var panelCentral = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(16, 8, 16, 16),
            BackColor = Color.White
        };
        panelCentral.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65f));
        panelCentral.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));

        // Tarjeta Vista Previa (Bridge: VistaPrevia)
        var tarjetaPreview = EstiloUi.CrearTarjeta(0, 0, new Padding(14));
        tarjetaPreview.Dock = DockStyle.Fill;

        var lblTituloPreview = new Label
        {
            Text = "Vista previa del documento (Bridge: VistaPrevia)",
            Font = EstiloUi.FuenteSeccion,
            ForeColor = EstiloUi.ColorTexto,
            Dock = DockStyle.Top,
            Height = 28
        };
        tarjetaPreview.Controls.Add(_txtComprobante);
        tarjetaPreview.Controls.Add(lblTituloPreview);

        // Tarjeta Salida / Comprobante (Bridge: ArchivoTXT)
        var tarjetaSalida = EstiloUi.CrearTarjeta(0, 0, new Padding(18));
        tarjetaSalida.Dock = DockStyle.Fill;

        var lblTituloSalida = new Label
        {
            Text = "Salida / comprobante",
            Font = EstiloUi.FuenteSeccion,
            ForeColor = EstiloUi.ColorTexto,
            Dock = DockStyle.Top,
            Height = 28
        };

        var flujoSalida = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(0, 8, 0, 0)
        };

        flujoSalida.Controls.Add(new Label { Text = "Seleccionar pedido:", Font = EstiloUi.FuenteNegrita, AutoSize = true, Margin = new Padding(0, 4, 0, 4) });
        flujoSalida.Controls.Add(_cmbPedido);

        flujoSalida.Controls.Add(new Label { Text = "Tipo de documento:", Font = EstiloUi.FuenteNegrita, AutoSize = true, Margin = new Padding(0, 10, 0, 4) });
        _cmbTipoDocumento.Items.Add("Comprobante de Venta");
        _cmbTipoDocumento.Items.Add("Comprobante de Devolución");
        _cmbTipoDocumento.SelectedIndex = 0;
        flujoSalida.Controls.Add(_cmbTipoDocumento);

        EstiloUi.AplicarBotonSecundario(_btnVistaPrevia);
        _btnVistaPrevia.Margin = new Padding(0, 16, 0, 8);
        _btnVistaPrevia.Click += async (_, _) => await VerComprobanteAsync();
        flujoSalida.Controls.Add(_btnVistaPrevia);

        EstiloUi.AplicarBotonPrimario(_btnExportarTxt);
        _btnExportarTxt.Margin = new Padding(0, 0, 0, 12);
        _btnExportarTxt.Click += async (_, _) => await GuardarTxtAsync();
        flujoSalida.Controls.Add(_btnExportarTxt);

        EstiloUi.AplicarBotonSecundario(_btnProcesarOutbox);
        _btnProcesarOutbox.Click += async (_, _) => await ProcesarOutboxAsync();
        flujoSalida.Controls.Add(_btnProcesarOutbox);

        flujoSalida.Controls.Add(_lblMensaje);

        tarjetaSalida.Controls.Add(flujoSalida);
        tarjetaSalida.Controls.Add(lblTituloSalida);

        panelCentral.Controls.Add(tarjetaPreview, 0, 0);
        panelCentral.Controls.Add(tarjetaSalida, 1, 0);

        Controls.Add(panelCentral);
        Controls.Add(panelMetricas);
        Controls.Add(barraTop);
        Controls.Add(encabezado);
    }

    private static Panel CrearTarjetaMetrica(string titulo, Label lblValor, int ancho)
    {
        var tarjeta = EstiloUi.CrearTarjeta(ancho, 76, new Padding(12, 8, 12, 8));
        var lblTitulo = new Label
        {
            Text = titulo,
            Font = EstiloUi.FuenteNegrita,
            ForeColor = EstiloUi.ColorTextoSecundario,
            Dock = DockStyle.Top,
            Height = 20
        };
        lblValor.Font = new Font("Segoe UI", 16f, FontStyle.Bold);
        lblValor.ForeColor = EstiloUi.ColorPrimarioOscuro;
        lblValor.Dock = DockStyle.Fill;
        lblValor.TextAlign = ContentAlignment.MiddleLeft;
        lblValor.Text = "—";

        tarjeta.Controls.Add(lblValor);
        tarjeta.Controls.Add(lblTitulo);
        return tarjeta;
    }

    private async Task CargarAsync()
    {
        _lblMensaje.Text = string.Empty;
        var desdeLocal = _dtpDesde.Value.Date;
        var hastaLocal = _dtpHasta.Value.Date.AddDays(1);
        if (desdeLocal >= hastaLocal)
        {
            MostrarError("El rango de fechas no es válido.");
            return;
        }

        var resumenes = await _pedidos.ListarResumenAsync();
        var ventasDelPeriodo = new List<(ResumenPedido Resumen, Pedido Pedido)>();
        foreach (var resumen in resumenes.Where(p =>
                     p.Estado is EstadoPedido.Pagado or EstadoPedido.Despachado or EstadoPedido.Entregado))
        {
            var pedido = await _pedidos.ObtenerPorIdAsync(resumen.Id);
            var fechaPagoUtc = pedido?.Historial
                .Where(c => c.Nuevo == EstadoPedido.Pagado)
                .Select(c => (DateTime?)c.FechaUtc)
                .LastOrDefault();
            if (pedido is not null && fechaPagoUtc.HasValue)
            {
                var fechaPagoLocal = fechaPagoUtc.Value.ToLocalTime();
                if (fechaPagoLocal >= desdeLocal && fechaPagoLocal < hastaLocal)
                    ventasDelPeriodo.Add((resumen, pedido));
            }
        }

        _cmbPedido.Items.Clear();
        foreach (var fila in ventasDelPeriodo)
            _cmbPedido.Items.Add(fila.Resumen);

        if (_cmbPedido.Items.Count > 0)
            _cmbPedido.SelectedIndex = 0;

        var devoluciones = (await _devoluciones.ListarAsync())
            .Where(d => d.FechaUtc.ToLocalTime() >= desdeLocal && d.FechaUtc.ToLocalTime() < hastaLocal)
            .ToList();
        var reembolsos = devoluciones.Sum(d => d.Reembolso.Valor);
        var ventasNetas = ventasDelPeriodo.Sum(f => f.Pedido.Total.Valor) - reembolsos;

        _lblValConfirmados.Text = ventasDelPeriodo.Count.ToString("D2");
        _lblValVentasNetas.Text = $"USD {ventasNetas:N2}";
        _lblValDevoluciones.Text = devoluciones.Count.ToString("D2");
    }

    private async Task<Pedido?> PedidoSeleccionadoAsync() =>
        _cmbPedido.SelectedItem is ResumenPedido resumen
            ? await _pedidos.ObtenerPorIdAsync(resumen.Id)
            : null;

    private async Task VerComprobanteAsync()
    {
        _lblMensaje.Text = string.Empty;
        var pedido = await PedidoSeleccionadoAsync();
        if (pedido is null)
        {
            MostrarError("Selecciona un pedido.");
            return;
        }

        var salida = new VistaPrevia();
        if (_cmbTipoDocumento.SelectedIndex == 0)
        {
            var comp = new ComprobanteVenta(pedido, salida);
            _txtComprobante.Text = comp.Generar();
        }
        else
        {
            var dev = (await _devoluciones.ListarAsync()).FirstOrDefault(d => d.PedidoId == pedido.Id);
            if (dev is null)
            {
                MostrarError("El pedido seleccionado no registra devolución.");
                return;
            }
            var comp = new ComprobanteDevolucion(dev, salida);
            _txtComprobante.Text = comp.Generar();
        }

        MostrarExito("Vista previa generada con Bridge (VistaPrevia).");
    }

    private async Task GuardarTxtAsync()
    {
        _lblMensaje.Text = string.Empty;
        var pedido = await PedidoSeleccionadoAsync();
        if (pedido is null)
        {
            MostrarError("Selecciona un pedido.");
            return;
        }

        try
        {
            var carpeta = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LogiMarket360", "Comprobantes");
            Directory.CreateDirectory(carpeta);
            var ruta = Path.Combine(carpeta, $"Comprobante_{pedido.Id.ToString()[..8]}.txt");
            var salida = _fabricaSalidaArchivo.Crear(carpeta);

            if (_cmbTipoDocumento.SelectedIndex == 0)
            {
                var comp = new ComprobanteVenta(pedido, salida);
                comp.Generar();
            }
            else
            {
                var dev = (await _devoluciones.ListarAsync()).FirstOrDefault(d => d.PedidoId == pedido.Id);
                if (dev is null)
                {
                    MostrarError("El pedido seleccionado no registra devolución.");
                    return;
                }
                var comp = new ComprobanteDevolucion(dev, salida);
                comp.Generar();
            }

            MostrarExito($"Archivo TXT exportado en:\n{ruta}");
            if (File.Exists(ruta))
                _txtComprobante.Text = await File.ReadAllTextAsync(ruta);
        }
        catch (Exception ex)
        {
            MostrarError($"Error al exportar: {ex.Message}");
        }
    }

    private async Task ProcesarOutboxAsync()
    {
        try
        {
            await _procesadorOutbox.ProcesarPendientesAsync();
            MostrarExito("Eventos Outbox pendientes procesados correctamente.");
        }
        catch (Exception ex)
        {
            MostrarError($"Fallo al procesar Outbox: {ex.Message}");
        }
    }

    private void MostrarError(string mensaje)
    {
        _lblMensaje.ForeColor = EstiloUi.ColorPeligro;
        _lblMensaje.Text = mensaje;
    }

    private void MostrarExito(string mensaje)
    {
        _lblMensaje.ForeColor = EstiloUi.ColorExito;
        _lblMensaje.Text = mensaje;
    }
}
