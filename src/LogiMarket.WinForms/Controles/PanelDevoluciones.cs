using LogiMarket.Application.Abstractions.Repositories;
using LogiMarket.Application.Abstractions.Services;
using LogiMarket.Application.UseCases.Devoluciones;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;

namespace LogiMarket.WinForms.Controles;

public sealed class PanelDevoluciones : UserControl
{
    private readonly IPedidoRepository _pedidos;
    private readonly IProcesoDevolucion _devoluciones;
    private readonly IProcesadorOutbox _outbox;

    private readonly ComboBox _cmbPedido = new() { Width = 320, DropDownStyle = ComboBoxStyle.DropDownList, Font = EstiloUi.FuenteNormal };
    private readonly ComboBox _cmbMotivo = new() { Width = 180, DropDownStyle = ComboBoxStyle.DropDownList, Font = EstiloUi.FuenteNormal };
    private readonly CheckBox _chkSinUso = new() { Text = "Producto sin uso (requerido para arrepentimiento)", Checked = true, AutoSize = true, Font = EstiloUi.FuenteNormal };

    private readonly Label _lblFechaEntrega = new() { Text = "Entrega registrada: —", Font = EstiloUi.FuenteNormal, AutoSize = true };
    private readonly Label _lblDiasTranscurridos = new() { Text = "Días transcurridos: —", Font = EstiloUi.FuenteNormal, AutoSize = true };
    private readonly Label _lblPoliticaDefecto = new() { Text = "☑ DEFECTO: hasta 30 días", Font = EstiloUi.FuenteNormal, ForeColor = EstiloUi.ColorTexto, AutoSize = true };
    private readonly Label _lblPoliticaArrepentimiento = new() { Text = "☑ ARREPENTIMIENTO: 7 días sin uso", Font = EstiloUi.FuenteNormal, ForeColor = EstiloUi.ColorTexto, AutoSize = true };
    private readonly Label _lblReintegroStock = new() { Text = "☑ Reintegrar stock: según motivo aprobado", Font = EstiloUi.FuenteNormal, ForeColor = EstiloUi.ColorTexto, AutoSize = true };

    private readonly Label _lblTotalReembolso = new() { Text = "USD 0.00", Font = new Font("Segoe UI", 16f, FontStyle.Bold), ForeColor = EstiloUi.ColorPrimarioOscuro, AutoSize = true };
    private readonly Label _lblAclaracionFlete = new() { Text = "(Importe de productos con descuento, sin tarifa de envío)", Font = EstiloUi.FuenteSubtitulo, ForeColor = EstiloUi.ColorTextoSecundario, AutoSize = true };
    private readonly Button _btnConfirmar = new() { Text = "Confirmar devolución", Width = 260, Height = 40 };
    private readonly Label _lblMensaje = new() { AutoSize = true, Font = EstiloUi.FuenteNormal, Margin = new Padding(0, 10, 0, 0) };

    private Pedido? _pedidoActual;

    public PanelDevoluciones(IPedidoRepository pedidos, IProcesoDevolucion devoluciones, IProcesadorOutbox outbox)
    {
        _pedidos = pedidos;
        _devoluciones = devoluciones;
        _outbox = outbox;

        Dock = DockStyle.Fill;
        BackColor = Color.White;

        ConstruirInterfaz();
        Load += async (_, _) => await CargarAsync();
    }

    private void ConstruirInterfaz()
    {
        var encabezado = EstiloUi.CrearEncabezado("Devoluciones y reintegros", "Solicitud total, política de motivo y reembolso único");

        // Barra superior de selección
        var barraTop = new Panel { Dock = DockStyle.Top, Height = 56, Padding = new Padding(16, 8, 16, 0), BackColor = Color.White };
        var flujoTop = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };

        flujoTop.Controls.Add(new Label { Text = "Pedido entregado:", Font = EstiloUi.FuenteNegrita, AutoSize = true, Margin = new Padding(0, 6, 6, 0) });
        flujoTop.Controls.Add(_cmbPedido);

        flujoTop.Controls.Add(new Label { Text = "Motivo:", Font = EstiloUi.FuenteNegrita, AutoSize = true, Margin = new Padding(12, 6, 6, 0) });
        _cmbMotivo.Items.AddRange(Enum.GetNames<MotivoDevolucion>());
        _cmbMotivo.SelectedIndex = 0;
        flujoTop.Controls.Add(_cmbMotivo);

        var badgeEntregado = EstiloUi.CrearBadge("ENTREGADO", Color.FromArgb(220, 252, 231), EstiloUi.ColorExito);
        badgeEntregado.Margin = new Padding(12, 4, 0, 0);
        flujoTop.Controls.Add(badgeEntregado);

        barraTop.Controls.Add(flujoTop);
        _cmbPedido.SelectedIndexChanged += async (_, _) => await AlCambiarPedidoAsync();

        // Área central dividida: Validación de política (60%) + Resumen reembolso (40%)
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

        // Tarjeta Validación de política
        var tarjetaPolitica = EstiloUi.CrearTarjeta(0, 0, new Padding(20));
        tarjetaPolitica.Dock = DockStyle.Fill;

        var lblTituloPolitica = new Label
        {
            Text = "Validación de política",
            Font = EstiloUi.FuenteSeccion,
            ForeColor = EstiloUi.ColorTexto,
            Dock = DockStyle.Top,
            Height = 32
        };

        var flujoPolitica = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(0, 8, 0, 0)
        };

        flujoPolitica.Controls.Add(_lblFechaEntrega);
        flujoPolitica.Controls.Add(_lblDiasTranscurridos);
        flujoPolitica.Controls.Add(new Label { Text = string.Empty, Height = 8 }); // separador
        flujoPolitica.Controls.Add(_lblPoliticaDefecto);
        flujoPolitica.Controls.Add(_lblPoliticaArrepentimiento);
        flujoPolitica.Controls.Add(_lblReintegroStock);
        flujoPolitica.Controls.Add(new Label { Text = string.Empty, Height = 8 });
        flujoPolitica.Controls.Add(_chkSinUso);

        tarjetaPolitica.Controls.Add(flujoPolitica);
        tarjetaPolitica.Controls.Add(lblTituloPolitica);

        // Tarjeta Resumen de Reembolso
        var tarjetaResumen = EstiloUi.CrearTarjeta(0, 0, new Padding(20));
        tarjetaResumen.Dock = DockStyle.Fill;

        var lblTituloResumen = new Label
        {
            Text = "Resumen",
            Font = EstiloUi.FuenteSeccion,
            ForeColor = EstiloUi.ColorTexto,
            Dock = DockStyle.Top,
            Height = 32
        };

        var flujoResumen = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(0, 8, 0, 0)
        };

        flujoResumen.Controls.Add(new Label { Text = "Total del reembolso:", Font = EstiloUi.FuenteNormal, ForeColor = EstiloUi.ColorTextoSecundario, AutoSize = true });
        flujoResumen.Controls.Add(_lblTotalReembolso);
        flujoResumen.Controls.Add(_lblAclaracionFlete);

        EstiloUi.AplicarBotonPrimario(_btnConfirmar);
        _btnConfirmar.Margin = new Padding(0, 24, 0, 8);
        _btnConfirmar.Click += async (_, _) => await ConfirmarDevolucionAsync();
        flujoResumen.Controls.Add(_btnConfirmar);
        flujoResumen.Controls.Add(_lblMensaje);

        tarjetaResumen.Controls.Add(flujoResumen);
        tarjetaResumen.Controls.Add(lblTituloResumen);

        panelCentral.Controls.Add(tarjetaPolitica, 0, 0);
        panelCentral.Controls.Add(tarjetaResumen, 1, 0);

        Controls.Add(panelCentral);
        Controls.Add(barraTop);
        Controls.Add(encabezado);
    }

    private async Task CargarAsync()
    {
        _cmbPedido.Items.Clear();
        var resumenes = await _pedidos.ListarResumenAsync();
        foreach (var r in resumenes.Where(r => r.Estado == EstadoPedido.Entregado))
            _cmbPedido.Items.Add(r);

        if (_cmbPedido.Items.Count > 0)
        {
            _cmbPedido.SelectedIndex = 0;
            await AlCambiarPedidoAsync();
        }
        else
        {
            _pedidoActual = null;
            _lblFechaEntrega.Text = "No hay pedidos con estado ENTREGADO.";
            _lblDiasTranscurridos.Text = "Días transcurridos: —";
            _lblTotalReembolso.Text = "USD 0.00";
            _btnConfirmar.Enabled = false;
        }
    }

    private async Task AlCambiarPedidoAsync()
    {
        _lblMensaje.Text = string.Empty;
        if (_cmbPedido.SelectedItem is not ResumenPedido seleccionado)
        {
            _btnConfirmar.Enabled = false;
            return;
        }

        _pedidoActual = await _pedidos.ObtenerPorIdAsync(seleccionado.Id);
        if (_pedidoActual is null) return;

        var fechaEntrega = _pedidoActual.FechaEntregaUtc?.ToLocalTime() ?? DateTime.Now;
        var dias = (int)(DateTime.Now - fechaEntrega).TotalDays;

        _lblFechaEntrega.Text = $"Entrega registrada: {fechaEntrega:dd/MM/yyyy HH:mm}";
        _lblDiasTranscurridos.Text = $"Días transcurridos: {dias} día(s)";

        // El reembolso es exclusivamente sobre productos con descuento, sin incluir tarifa de envío
        var montoReembolso = _pedidoActual.SubtotalConDescuento;
        _lblTotalReembolso.Text = $"USD {montoReembolso.Valor:F2}";
        _btnConfirmar.Enabled = !_pedidoActual.Devuelto;

        if (_pedidoActual.Devuelto)
        {
            _lblMensaje.ForeColor = Color.FromArgb(217, 119, 6);
            _lblMensaje.Text = "Este pedido ya fue devuelto previamente.";
        }
    }

    private async Task ConfirmarDevolucionAsync()
    {
        if (_pedidoActual is null)
        {
            MostrarError("Selecciona un pedido entregado.");
            return;
        }

        try
        {
            var motivo = Enum.Parse<MotivoDevolucion>((string)_cmbMotivo.SelectedItem!);
            var confirmar = MessageBox.Show(this,
                $"¿Confirmas la devolución total del pedido seleccionado por motivo {motivo}? Esta acción reintegrará existencias y registrará un reembolso único.",
                "Confirmar devolución", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirmar != DialogResult.Yes) return;

            var solicitud = new SolicitudDevolucionDto(
                _pedidoActual.Id,
                motivo,
                DateTime.UtcNow,
                _chkSinUso.Checked);

            var resultado = await _devoluciones.SolicitarAsync(solicitud);
            if (!resultado.Exitoso)
            {
                MostrarError(resultado.Error?.Mensaje ?? "No se pudo procesar la devolución.");
                return;
            }

            _lblMensaje.ForeColor = EstiloUi.ColorExito;
            _lblMensaje.Text = $"Devolución procesada. Reembolso: USD {resultado.Devolucion!.Reembolso.Valor:F2} | Stock reintegrado.";
            var outboxPendiente = false;
            try { await _outbox.ProcesarPendientesAsync(); }
            catch { outboxPendiente = true; }
            if (outboxPendiente) _lblMensaje.Text += " Evento pendiente de reintento.";

            MessageBox.Show(this,
                $"¡Devolución aprobada!\n\nReembolso: USD {resultado.Devolucion!.Reembolso.Valor:F2}\nMotivo: {motivo}\nStock de componentes reintegrado correctamente.",
                "Devolución completada",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            await CargarAsync();
        }
        catch (Exception ex)
        {
            MostrarError($"Error: {ex.Message}");
        }
    }

    private void MostrarError(string mensaje)
    {
        _lblMensaje.ForeColor = EstiloUi.ColorPeligro;
        _lblMensaje.Text = mensaje;
    }
}
