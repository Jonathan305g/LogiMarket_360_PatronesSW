using LogiMarket.Application.Abstractions.Repositories;
using LogiMarket.Application.UseCases.ConfirmarPedido.Facade;
using LogiMarket.Application.Commands.Command;
using LogiMarket.Application.Abstractions.Services;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.WinForms.Mediador;
using LogiMarket.WinForms.Sesion;
using LogiMarket.Application.Security;

namespace LogiMarket.WinForms.Controles;

public sealed class PanelCobro : UserControl
{
    private readonly IPedidoRepository _pedidos;
    private readonly IConfirmacionPedido _confirmacion;
    private readonly UsuarioActualEnSesion _sesion;
    private readonly IMediadorPrincipal _mediador;
    private readonly ISelectorPasarelaPago _selectorPasarela;
    private readonly IProcesadorOutbox _outbox;
    private readonly InvocadorComandos _invocador;

    private readonly ComboBox _cmbPedido = new() { Width = 320, DropDownStyle = ComboBoxStyle.DropDownList, Font = EstiloUi.FuenteNormal };
    private readonly Label _lblPedidoTitulo = new() { Text = "Pedido —", Font = EstiloUi.FuenteSeccion, ForeColor = EstiloUi.ColorTexto, Dock = DockStyle.Top, Height = 28 };
    private readonly Label _lblCliente = new() { Text = "—", Font = EstiloUi.FuenteNormal, AutoSize = true };
    private readonly Label _lblEstado = new() { Text = "—", Font = EstiloUi.FuenteNegrita, AutoSize = true };
    private readonly Label _lblSubtotal = new() { Text = "USD 0.00", Font = EstiloUi.FuenteNormal, AutoSize = true };
    private readonly Label _lblDescuento = new() { Text = "USD 0.00", Font = EstiloUi.FuenteNormal, ForeColor = EstiloUi.ColorPeligro, AutoSize = true };
    private readonly Label _lblEntrega = new() { Text = "USD 0.00", Font = EstiloUi.FuenteNormal, AutoSize = true };
    private readonly Label _lblTotal = new() { Text = "USD 0.00", Font = new Font("Segoe UI", 14f, FontStyle.Bold), ForeColor = EstiloUi.ColorPrimarioOscuro, AutoSize = true };

    private readonly ComboBox _cmbProveedor = new() { Width = 280, DropDownStyle = ComboBoxStyle.DropDownList, Font = EstiloUi.FuenteNormal };
    private readonly TextBox _txtClave = new() { Width = 200, Font = EstiloUi.FuenteMonocromatica };
    private readonly Button _btnGenerarClave = new() { Text = "Auto", Width = 60 };
    private readonly Label _lblBadgeRol = EstiloUi.CrearBadge("ROL: ADMIN");
    private readonly Label _lblBadgeCobroUnico = EstiloUi.CrearBadge("COBRO ÚNICO");
    private readonly Button _btnConfirmar = new() { Text = "Confirmar cobro", Width = 280, Height = 40 };
    private readonly Label _lblResultado = new() { Text = "Resultado / referencia: pendiente", Font = EstiloUi.FuenteNormal, ForeColor = EstiloUi.ColorTextoSecundario, AutoSize = true };

    private Pedido? _pedidoSeleccionado;

    public PanelCobro(
        IPedidoRepository pedidos,
        IConfirmacionPedido confirmacion,
        UsuarioActualEnSesion sesion,
        IMediadorPrincipal mediador,
        ISelectorPasarelaPago selectorPasarela,
        InvocadorComandos invocador,
        IProcesadorOutbox outbox)
    {
        _pedidos = pedidos;
        _confirmacion = confirmacion;
        _sesion = sesion;
        _mediador = mediador;
        _selectorPasarela = selectorPasarela;
        _outbox = outbox;
        _invocador = invocador;

        Dock = DockStyle.Fill;
        BackColor = Color.White;

        ConstruirInterfaz();
        Load += async (_, _) => await CargarAsync();
    }

    private void ConstruirInterfaz()
    {
        var encabezado = EstiloUi.CrearEncabezado("Confirmación y cobro", "Autorización del rol, validaciones y pasarela simulada");

        // Barra superior con selección de pedido
        var barraTop = new Panel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(16, 8, 16, 0), BackColor = Color.White };
        var flujoTop = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        flujoTop.Controls.Add(new Label { Text = "Seleccionar pedido:", Font = EstiloUi.FuenteNegrita, AutoSize = true, Margin = new Padding(0, 6, 8, 0) });
        flujoTop.Controls.Add(_cmbPedido);

        var btnActualizar = new Button { Text = "Actualizar", Width = 90 };
        EstiloUi.AplicarBotonSecundario(btnActualizar);
        btnActualizar.Click += async (_, _) => await CargarAsync();
        flujoTop.Controls.Add(btnActualizar);
        barraTop.Controls.Add(flujoTop);

        _cmbPedido.SelectedIndexChanged += async (_, _) => await MostrarDetallePedidoAsync();

        // 2 Columnas de tarjetas: Izquierda (Detalle pedido) + Derecha (Proceso de pago)
        var panelCentral = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(16, 8, 16, 16),
            BackColor = Color.White
        };
        panelCentral.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        panelCentral.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

        // Tarjeta Izquierda (Pedido)
        var tarjetaPedido = EstiloUi.CrearTarjeta(0, 0, new Padding(20));
        tarjetaPedido.Dock = DockStyle.Fill;

        var tablaDetalles = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 6,
            Padding = new Padding(0, 10, 0, 0)
        };
        tablaDetalles.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
        tablaDetalles.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));

        tablaDetalles.Controls.Add(new Label { Text = "Cliente:", Font = EstiloUi.FuenteNormal, ForeColor = EstiloUi.ColorTextoSecundario, AutoSize = true }, 0, 0);
        tablaDetalles.Controls.Add(_lblCliente, 1, 0);

        tablaDetalles.Controls.Add(new Label { Text = "Estado:", Font = EstiloUi.FuenteNormal, ForeColor = EstiloUi.ColorTextoSecundario, AutoSize = true }, 0, 1);
        tablaDetalles.Controls.Add(_lblEstado, 1, 1);

        tablaDetalles.Controls.Add(new Label { Text = "Subtotal:", Font = EstiloUi.FuenteNormal, ForeColor = EstiloUi.ColorTextoSecundario, AutoSize = true }, 0, 2);
        tablaDetalles.Controls.Add(_lblSubtotal, 1, 2);

        tablaDetalles.Controls.Add(new Label { Text = "Descuento:", Font = EstiloUi.FuenteNormal, ForeColor = EstiloUi.ColorTextoSecundario, AutoSize = true }, 0, 3);
        tablaDetalles.Controls.Add(_lblDescuento, 1, 3);

        tablaDetalles.Controls.Add(new Label { Text = "Entrega:", Font = EstiloUi.FuenteNormal, ForeColor = EstiloUi.ColorTextoSecundario, AutoSize = true }, 0, 4);
        tablaDetalles.Controls.Add(_lblEntrega, 1, 4);

        tablaDetalles.Controls.Add(new Label { Text = "Total:", Font = EstiloUi.FuenteNegrita, ForeColor = EstiloUi.ColorPrimarioOscuro, AutoSize = true, Margin = new Padding(0, 10, 0, 0) }, 0, 5);
        tablaDetalles.Controls.Add(_lblTotal, 1, 5);

        tarjetaPedido.Controls.Add(tablaDetalles);
        tarjetaPedido.Controls.Add(_lblPedidoTitulo);

        // Tarjeta Derecha (Proceso de pago)
        var tarjetaPago = EstiloUi.CrearTarjeta(0, 0, new Padding(20));
        tarjetaPago.Dock = DockStyle.Fill;

        var lblTituloProceso = new Label
        {
            Text = "Proceso de pago",
            Font = EstiloUi.FuenteSeccion,
            ForeColor = EstiloUi.ColorTexto,
            Dock = DockStyle.Top,
            Height = 28
        };

        var flujoPago = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(0, 8, 0, 0)
        };

        flujoPago.Controls.Add(new Label { Text = "Proveedor simulado:", Font = EstiloUi.FuenteNegrita, AutoSize = true, Margin = new Padding(0, 4, 0, 4) });
        _cmbProveedor.Items.Add("Pasarela Legacy / V2 (Adapter)");
        _cmbProveedor.Items.Add("Pasarela Directa Simulada");
        _cmbProveedor.SelectedIndex = 0;
        _cmbProveedor.SelectedIndexChanged += (_, _) => _selectorPasarela.Seleccionar(
            _cmbProveedor.SelectedIndex == 1
                ? ProveedorPagoSimulado.Directo
                : ProveedorPagoSimulado.Legado);
        flujoPago.Controls.Add(_cmbProveedor);

        var flujoBadges = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0, 14, 0, 10) };
        _lblBadgeRol.Text = $"ROL: {_sesion.Rol.ToString().ToUpperInvariant()}";
        flujoBadges.Controls.Add(_lblBadgeRol);
        flujoBadges.Controls.Add(_lblBadgeCobroUnico);
        flujoPago.Controls.Add(flujoBadges);

        flujoPago.Controls.Add(new Label { Text = "Clave de idempotencia:", Font = EstiloUi.FuenteNegrita, AutoSize = true, Margin = new Padding(0, 4, 0, 4) });
        var filaClave = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0, 0, 0, 8) };
        filaClave.Controls.Add(_txtClave);
        EstiloUi.AplicarBotonSecundario(_btnGenerarClave);
        _btnGenerarClave.Click += (_, _) => _txtClave.Text = $"KEY-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}";
        filaClave.Controls.Add(_btnGenerarClave);
        flujoPago.Controls.Add(filaClave);

        EstiloUi.AplicarBotonPrimario(_btnConfirmar);
        _btnConfirmar.Enabled = PoliticaDeAcceso.PuedeCobrar(_sesion.Rol);
        _btnConfirmar.Click += async (_, _) => await CobrarAsync();
        flujoPago.Controls.Add(_btnConfirmar);

        _lblResultado.Margin = new Padding(0, 12, 0, 0);
        flujoPago.Controls.Add(_lblResultado);

        tarjetaPago.Controls.Add(flujoPago);
        tarjetaPago.Controls.Add(lblTituloProceso);

        panelCentral.Controls.Add(tarjetaPedido, 0, 0);
        panelCentral.Controls.Add(tarjetaPago, 1, 0);

        Controls.Add(panelCentral);
        Controls.Add(barraTop);
        Controls.Add(encabezado);
    }

    private async Task CargarAsync()
    {
        _cmbPedido.Items.Clear();
        var resumenes = await _pedidos.ListarResumenAsync();
        foreach (var r in resumenes.Where(r => r.Estado is EstadoPedido.Borrador or EstadoPedido.Validado))
            _cmbPedido.Items.Add(r);

        _lblBadgeRol.Text = $"ROL: {_sesion.Rol.ToString().ToUpperInvariant()}";
        if (_cmbPedido.Items.Count > 0)
        {
            _cmbPedido.SelectedIndex = 0;
            await MostrarDetallePedidoAsync();
        }
        else
        {
            LimpiarVistaPedido();
        }
    }

    private async Task MostrarDetallePedidoAsync()
    {
        if (_cmbPedido.SelectedItem is not ResumenPedido seleccionado)
        {
            LimpiarVistaPedido();
            return;
        }

        _pedidoSeleccionado = await _pedidos.ObtenerPorIdAsync(seleccionado.Id);
        if (_pedidoSeleccionado is null) return;

        _lblPedidoTitulo.Text = $"Pedido PED-{_pedidoSeleccionado.Id.ToString()[..6].ToUpperInvariant()}";
        _lblCliente.Text = seleccionado.Cliente;
        _lblEstado.Text = _pedidoSeleccionado.Estado.ToString().ToUpperInvariant();
        _lblSubtotal.Text = $"USD {_pedidoSeleccionado.Subtotal.Valor:F2}";
        _lblDescuento.Text = $"- USD {_pedidoSeleccionado.Descuento.Valor:F2}";
        _lblEntrega.Text = $"USD {_pedidoSeleccionado.Tarifa.Valor:F2}";
        _lblTotal.Text = $"USD {_pedidoSeleccionado.Total.Valor:F2}";

        if (string.IsNullOrEmpty(_txtClave.Text))
            _txtClave.Text = $"KEY-{_pedidoSeleccionado.Id.ToString()[..8].ToUpperInvariant()}";

        _lblResultado.ForeColor = EstiloUi.ColorTextoSecundario;
        _lblResultado.Text = "Resultado / referencia: pendiente";
    }

    private void LimpiarVistaPedido()
    {
        _pedidoSeleccionado = null;
        _lblPedidoTitulo.Text = "Pedido —";
        _lblCliente.Text = "—";
        _lblEstado.Text = "—";
        _lblSubtotal.Text = "USD 0.00";
        _lblDescuento.Text = "USD 0.00";
        _lblEntrega.Text = "USD 0.00";
        _lblTotal.Text = "USD 0.00";
    }

    private async Task CobrarAsync()
    {
        if (_pedidoSeleccionado is null)
        {
            MostrarError("Selecciona un pedido pendiente.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_txtClave.Text))
        {
            MostrarError("Ingresa una clave de idempotencia.");
            return;
        }

        var confirmar = MessageBox.Show(this,
            $"¿Confirmas el cobro simulado por USD {_pedidoSeleccionado.Total.Valor:F2}?\nClave: {_txtClave.Text.Trim()}",
            "Confirmar cobro", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirmar != DialogResult.Yes) return;

        try
        {
            var solicitud = new ConfirmarPedidoSolicitud(_pedidoSeleccionado.Id, _txtClave.Text.Trim());
            var comando = new ConfirmarPedidoCommand(_confirmacion, solicitud);
            var resultado = await _invocador.EjecutarAsync(comando);

            if (!resultado.Exitoso)
            {
                MostrarError($"Cobro rechazado: {resultado.Error?.Mensaje ?? "Error desconocido."}");
                return;
            }

            _lblResultado.ForeColor = EstiloUi.ColorExito;
            var referencia = resultado.Pago?.Referencia ?? "REF-SIM";
            var outboxPendiente = false;
            try { await _outbox.ProcesarPendientesAsync(); }
            catch { outboxPendiente = true; }
            _lblResultado.Text = resultado.EsRepeticion
                ? $"Idempotencia detectada. Referencia: {referencia}"
                : $"Cobro exitoso. Referencia: {referencia}";
            if (outboxPendiente) _lblResultado.Text += " | Notificación pendiente de reintento.";

            MessageBox.Show(this,
                $"¡Cobro aprobado exitosamente!\n\nReferencia: {referencia}\nTotal: USD {_pedidoSeleccionado.Total.Valor:F2}\nEstado: PAGADO",
                "Cobro confirmado",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            // Refrescar y notificar al mediador
            _mediador.NotificarPedidoConfirmado(_pedidoSeleccionado.Id);
        }
        catch (Exception ex)
        {
            MostrarError($"Error al ejecutar cobro: {ex.Message}");
        }
    }

    private void MostrarError(string mensaje)
    {
        _lblResultado.ForeColor = EstiloUi.ColorPeligro;
        _lblResultado.Text = mensaje;
    }
}
