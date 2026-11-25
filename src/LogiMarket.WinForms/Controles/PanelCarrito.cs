using LogiMarket.Application.Abstractions.Repositories;
using LogiMarket.Application.UseCases.Pedidos;
using LogiMarket.Domain.Catalog.Composite;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Exceptions;
using LogiMarket.Domain.Orders.Builder;
using LogiMarket.Domain.Orders.Memento;
using LogiMarket.WinForms.Mediador;

namespace LogiMarket.WinForms.Controles;

public sealed class PanelCarrito : UserControl
{
    private readonly IClienteRepository _clientes;
    private readonly ICatalogoRepository _catalogo;
    private readonly IValidarPedidoCasoUso _validarPedido;
    private readonly ICotizadorPedido _cotizador;
    private readonly IMediadorPrincipal _mediador;

    private readonly HistorialEdiciones _historial = new();
    private Pedido? _pedido;
    private Cliente? _clienteActual;

    private readonly ComboBox _cmbCliente = new() { Width = 170, DropDownStyle = ComboBoxStyle.DropDownList, Font = EstiloUi.FuenteNormal };
    private readonly ComboBox _cmbEntrega = new() { Width = 95, DropDownStyle = ComboBoxStyle.DropDownList, Font = EstiloUi.FuenteNormal };
    private readonly ComboBox _cmbElementos = new() { Width = 185, DropDownStyle = ComboBoxStyle.DropDownList, Font = EstiloUi.FuenteNormal };
    private readonly NumericUpDown _numCantidad = new() { Width = 55, Minimum = 1, Maximum = 999, Value = 1, Font = EstiloUi.FuenteNormal };
    private readonly Button _btnAgregar = new() { Text = "Añadir", Width = 90 };

    private readonly DataGridView _grilla = new() { Dock = DockStyle.Fill };

    private readonly Button _btnDeshacer = new() { Text = "Deshacer", Width = 95 };
    private readonly Button _btnRehacer = new() { Text = "Rehacer", Width = 95 };
    private readonly Button _btnQuitar = new() { Text = "Quitar línea", Width = 110 };

    private readonly Label _lblSubtotal = new() { Text = "USD 0.00", Font = EstiloUi.FuenteNegrita, ForeColor = EstiloUi.ColorTexto, AutoSize = true };
    private readonly Label _lblDescuento = new() { Text = "USD 0.00", Font = EstiloUi.FuenteNegrita, ForeColor = EstiloUi.ColorPeligro, AutoSize = true };
    private readonly Label _lblDescuentoTitulo = new() { Text = "Descuento NORMAL:", Font = EstiloUi.FuenteNormal, ForeColor = EstiloUi.ColorTextoSecundario, AutoSize = true };
    private readonly Label _lblEntrega = new() { Text = "USD 0.00", Font = EstiloUi.FuenteNegrita, ForeColor = EstiloUi.ColorTexto, AutoSize = true };
    private readonly Label _lblTotal = new() { Text = "USD 0.00", Font = new Font("Segoe UI", 14f, FontStyle.Bold), ForeColor = EstiloUi.ColorPrimarioOscuro, AutoSize = true };
    private readonly Button _btnValidar = new() { Text = "Validar pedido", Width = 190, Height = 38 };
    private readonly Label _lblMensaje = new() { AutoSize = true, Font = EstiloUi.FuenteNormal, Margin = new Padding(0, 8, 0, 0) };

    public PanelCarrito(
        IClienteRepository clientes,
        ICatalogoRepository catalogo,
        IValidarPedidoCasoUso validarPedido,
        ICotizadorPedido cotizador,
        IMediadorPrincipal mediador)
    {
        _clientes = clientes;
        _catalogo = catalogo;
        _validarPedido = validarPedido;
        _cotizador = cotizador;
        _mediador = mediador;

        Dock = DockStyle.Fill;
        BackColor = Color.White;

        ConstruirInterfaz();
        Load += async (_, _) => await CargarDatosAsync();
    }

    private void ConstruirInterfaz()
    {
        var encabezado = EstiloUi.CrearEncabezado("Carrito y borrador", "Selección de cliente, líneas de pedido y control de cambios");

        // Barra superior de selección
        var barraSuperior = new Panel { Dock = DockStyle.Top, Height = 56, Padding = new Padding(16, 8, 16, 8), BackColor = Color.White };
        var flujoTop = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };

        var lblCliente = new Label { Text = "Cliente:", Font = EstiloUi.FuenteNegrita, AutoSize = true, Margin = new Padding(0, 6, 4, 0) };
        var lblEntrega = new Label { Text = "Entrega:", Font = EstiloUi.FuenteNegrita, AutoSize = true, Margin = new Padding(12, 6, 4, 0) };
        var lblItem = new Label { Text = "Artículo:", Font = EstiloUi.FuenteNegrita, AutoSize = true, Margin = new Padding(12, 6, 4, 0) };
        var lblCant = new Label { Text = "Cant.:", Font = EstiloUi.FuenteNegrita, AutoSize = true, Margin = new Padding(8, 6, 4, 0) };

        _cmbEntrega.Items.AddRange(Enum.GetNames<TipoEntrega>());
        _cmbEntrega.SelectedIndex = 0;
        _cmbEntrega.SelectedIndexChanged += (_, _) => AlCambiarEntrega();

        EstiloUi.AplicarBotonPrimario(_btnAgregar);
        _btnAgregar.Click += async (_, _) => await AgregarLineaAsync();

        flujoTop.Controls.Add(lblCliente);
        flujoTop.Controls.Add(_cmbCliente);
        flujoTop.Controls.Add(lblEntrega);
        flujoTop.Controls.Add(_cmbEntrega);
        flujoTop.Controls.Add(lblItem);
        flujoTop.Controls.Add(_cmbElementos);
        flujoTop.Controls.Add(lblCant);
        flujoTop.Controls.Add(_numCantidad);
        flujoTop.Controls.Add(_btnAgregar);

        barraSuperior.Controls.Add(flujoTop);

        // Área central dividida: Tabla de líneas con Deshacer/Rehacer (70%) + Resumen (30%)
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

        // Tarjeta Tabla de Líneas
        var tarjetaLineas = EstiloUi.CrearTarjeta(0, 0, new Padding(12));
        tarjetaLineas.Dock = DockStyle.Fill;

        EstiloUi.AplicarGrilla(_grilla);
        _grilla.Columns.Add("Sku", "SKU");
        _grilla.Columns.Add("Articulo", "Artículo");
        _grilla.Columns.Add("Cantidad", "Cantidad");
        _grilla.Columns.Add("PrecioUnitario", "P. unitario");
        _grilla.Columns.Add("Importe", "Importe");

        // Barra inferior de acciones (Deshacer, Rehacer, Quitar línea)
        var barraAcciones = new Panel { Dock = DockStyle.Bottom, Height = 44, Padding = new Padding(0, 8, 0, 0) };
        var flujoAcciones = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };

        EstiloUi.AplicarBotonSecundario(_btnDeshacer);
        EstiloUi.AplicarBotonSecundario(_btnRehacer);
        EstiloUi.AplicarBotonSecundario(_btnQuitar);

        _btnDeshacer.Click += (_, _) => Deshacer();
        _btnRehacer.Click += (_, _) => Rehacer();
        _btnQuitar.Click += (_, _) => QuitarLinea();

        flujoAcciones.Controls.Add(_btnDeshacer);
        flujoAcciones.Controls.Add(_btnRehacer);
        flujoAcciones.Controls.Add(_btnQuitar);
        barraAcciones.Controls.Add(flujoAcciones);

        tarjetaLineas.Controls.Add(_grilla);
        tarjetaLineas.Controls.Add(barraAcciones);

        // Tarjeta Resumen
        var tarjetaResumen = EstiloUi.CrearTarjeta(0, 0, new Padding(16));
        tarjetaResumen.Dock = DockStyle.Fill;

        var lblTituloResumen = new Label
        {
            Text = "Resumen",
            Font = EstiloUi.FuenteSeccion,
            ForeColor = EstiloUi.ColorTexto,
            Dock = DockStyle.Top,
            Height = 32
        };

        var tablaTotales = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 5,
            AutoSize = true,
            Padding = new Padding(0, 8, 0, 16)
        };
        tablaTotales.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52f));
        tablaTotales.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48f));

        tablaTotales.Controls.Add(new Label { Text = "Subtotal:", Font = EstiloUi.FuenteNormal, ForeColor = EstiloUi.ColorTextoSecundario, AutoSize = true }, 0, 0);
        tablaTotales.Controls.Add(_lblSubtotal, 1, 0);

        tablaTotales.Controls.Add(_lblDescuentoTitulo, 0, 1);
        tablaTotales.Controls.Add(_lblDescuento, 1, 1);

        tablaTotales.Controls.Add(new Label { Text = "Entrega:", Font = EstiloUi.FuenteNormal, ForeColor = EstiloUi.ColorTextoSecundario, AutoSize = true }, 0, 2);
        tablaTotales.Controls.Add(_lblEntrega, 1, 2);

        tablaTotales.Controls.Add(new Label { Text = "TOTAL:", Font = EstiloUi.FuenteNegrita, ForeColor = EstiloUi.ColorPrimarioOscuro, AutoSize = true, Margin = new Padding(0, 10, 0, 0) }, 0, 3);
        tablaTotales.Controls.Add(_lblTotal, 1, 3);

        EstiloUi.AplicarBotonPrimario(_btnValidar);
        _btnValidar.Click += async (_, _) => await GuardarYValidarPedidoAsync();

        var panelBotonValidar = new Panel { Dock = DockStyle.Bottom, Height = 90 };
        _btnValidar.Dock = DockStyle.Top;
        _lblMensaje.Dock = DockStyle.Fill;
        panelBotonValidar.Controls.Add(_lblMensaje);
        panelBotonValidar.Controls.Add(_btnValidar);

        tarjetaResumen.Controls.Add(panelBotonValidar);
        tarjetaResumen.Controls.Add(tablaTotales);
        tarjetaResumen.Controls.Add(lblTituloResumen);

        panelCentral.Controls.Add(tarjetaLineas, 0, 0);
        panelCentral.Controls.Add(tarjetaResumen, 1, 0);

        Controls.Add(panelCentral);
        Controls.Add(barraSuperior);
        Controls.Add(encabezado);

        ActualizarBotonesMemento();
    }

    private async Task CargarDatosAsync()
    {
        _cmbCliente.Items.Clear();
        foreach (var c in await _clientes.ListarAsync())
            _cmbCliente.Items.Add(new ItemCliente(c));

        if (_cmbCliente.Items.Count > 0)
        {
            _cmbCliente.SelectedIndex = 0;
            _clienteActual = ((ItemCliente)_cmbCliente.SelectedItem!).Cliente;
        }

        _cmbCliente.SelectedIndexChanged += (_, _) =>
        {
            if (_cmbCliente.SelectedItem is ItemCliente it)
            {
                _clienteActual = it.Cliente;
                RecalcularTotales();
            }
        };

        _cmbElementos.Items.Clear();
        foreach (var r in await _catalogo.ListarResumenAsync())
            _cmbElementos.Items.Add(new ItemElemento(r.Sku, r.Nombre, r.EsKit));

        if (_cmbElementos.Items.Count > 0)
            _cmbElementos.SelectedIndex = 0;

        InicializarPedido();
    }

    private void InicializarPedido()
    {
        if (_clienteActual is null) return;
        var entrega = Enum.Parse<TipoEntrega>((string)_cmbEntrega.SelectedItem!);
        _pedido = new PedidoBuilder().ConCliente(_clienteActual).ConEntrega(entrega).ConstruirVacio();
        _historial.Limpiar();
        ActualizarGrilla();
        RecalcularTotales();
        ActualizarBotonesMemento();
    }

    private void AlCambiarEntrega()
    {
        if (_pedido is null) return;
        var entrega = Enum.Parse<TipoEntrega>((string)_cmbEntrega.SelectedItem!);
        _pedido.CambiarEntrega(entrega);
        RecalcularTotales();
    }

    private async Task AgregarLineaAsync()
    {
        _lblMensaje.Text = string.Empty;
        if (_clienteActual is null)
        {
            MostrarError("Primero crea o selecciona un cliente.");
            return;
        }

        if (_cmbElementos.SelectedItem is not ItemElemento seleccionado)
        {
            MostrarError("Selecciona un producto o kit.");
            return;
        }

        var elemento = await _catalogo.ObtenerPorSkuAsync(seleccionado.Sku);
        if (elemento is null)
        {
            MostrarError($"No se encontró el elemento con SKU {seleccionado.Sku}.");
            return;
        }

        try
        {
            if (_pedido is null)
            {
                var entrega = Enum.Parse<TipoEntrega>((string)_cmbEntrega.SelectedItem!);
                _pedido = new PedidoBuilder().ConCliente(_clienteActual).ConEntrega(entrega).ConstruirVacio();
            }

            // Memento: guardar antes de modificar
            _historial.Guardar(_pedido);

            var cantidad = (int)_numCantidad.Value;
            _pedido.AgregarLinea(elemento, cantidad);

            ActualizarGrilla();
            RecalcularTotales();
            ActualizarBotonesMemento();
            _lblMensaje.ForeColor = EstiloUi.ColorExito;
            _lblMensaje.Text = $"Se agregó {seleccionado.Sku} x{cantidad}";
        }
        catch (DomainException ex)
        {
            MostrarError(ex.Message);
        }
    }

    private void QuitarLinea()
    {
        if (_pedido is null || _grilla.SelectedRows.Count == 0) return;
        var sku = _grilla.SelectedRows[0].Cells["Sku"].Value?.ToString();
        if (string.IsNullOrEmpty(sku)) return;

        try
        {
            _historial.Guardar(_pedido);
            _pedido.QuitarLinea(sku);
            ActualizarGrilla();
            RecalcularTotales();
            ActualizarBotonesMemento();
            _lblMensaje.ForeColor = EstiloUi.ColorTexto;
            _lblMensaje.Text = $"Se quitó la línea {sku}.";
        }
        catch (Exception ex)
        {
            MostrarError(ex.Message);
        }
    }

    private void Deshacer()
    {
        if (_pedido is null || !_historial.PuedeDeshacer) return;
        try
        {
            _historial.Deshacer(_pedido);
            ActualizarGrilla();
            RecalcularTotales();
            ActualizarBotonesMemento();
            _lblMensaje.ForeColor = EstiloUi.ColorTexto;
            _lblMensaje.Text = "Cambio deshecho.";
        }
        catch (Exception ex)
        {
            MostrarError(ex.Message);
        }
    }

    private void Rehacer()
    {
        if (_pedido is null || !_historial.PuedeRehacer) return;
        try
        {
            _historial.Rehacer(_pedido);
            ActualizarGrilla();
            RecalcularTotales();
            ActualizarBotonesMemento();
            _lblMensaje.ForeColor = EstiloUi.ColorTexto;
            _lblMensaje.Text = "Cambio rehecho.";
        }
        catch (Exception ex)
        {
            MostrarError(ex.Message);
        }
    }

    private void ActualizarBotonesMemento()
    {
        _btnDeshacer.Enabled = _historial.PuedeDeshacer;
        _btnRehacer.Enabled = _historial.PuedeRehacer;
    }

    private void ActualizarGrilla()
    {
        _grilla.Rows.Clear();
        if (_pedido is null) return;

        foreach (var d in _pedido.Detalles)
        {
            _grilla.Rows.Add(
                d.Sku,
                d.Nombre,
                d.Cantidad,
                $"USD {d.PrecioUnitario.Valor:F2}",
                $"USD {d.Subtotal.Valor:F2}");
        }
    }

    private void RecalcularTotales()
    {
        if (_pedido is null || _clienteActual is null) return;

        var cotizacion = _cotizador.Cotizar(_pedido, _clienteActual.Categoria);
        _lblSubtotal.Text = $"USD {cotizacion.Subtotal.Valor:F2}";
        _lblDescuentoTitulo.Text = $"Descuento ({cotizacion.Politica}):";
        _lblDescuento.Text = $"- USD {cotizacion.Descuento.Valor:F2}";
        _lblEntrega.Text = $"USD {cotizacion.Tarifa.Valor:F2}";
        _lblTotal.Text = $"USD {cotizacion.Total.Valor:F2}";
    }

    private async Task GuardarYValidarPedidoAsync()
    {
        _lblMensaje.Text = string.Empty;
        if (_pedido is null || _pedido.Detalles.Count == 0)
        {
            MostrarError("El pedido requiere al menos una línea.");
            return;
        }

        try
        {
            var resultado = await _validarPedido.EjecutarAsync(_pedido, _clienteActual!.Categoria);
            if (!resultado.Exitoso || resultado.Pedido is null)
            {
                MostrarError(resultado.Error ?? "No se pudo validar el pedido.");
                return;
            }
            _pedido = resultado.Pedido;

            _historial.Limpiar();
            ActualizarBotonesMemento();

            MessageBox.Show(this,
                $"¡Pedido validado y guardado con éxito!\n\nID: {_pedido.Id}\nTotal: USD {_pedido.Total.Valor:F2}\nEstado: {_pedido.Estado}",
                "Pedido guardado",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            // Transicionar al panel de Cobro mediante el Mediador
            _mediador.MostrarPanel(TipoPanel.Cobro);
        }
        catch (DomainException ex)
        {
            MostrarError(ex.Message);
        }
        catch (Exception ex)
        {
            MostrarError($"Error al guardar: {ex.Message}");
        }
    }

    private void MostrarError(string mensaje)
    {
        _lblMensaje.ForeColor = EstiloUi.ColorPeligro;
        _lblMensaje.Text = mensaje;
    }

    private sealed record ItemCliente(Cliente Cliente)
    {
        public override string ToString() => $"{Cliente.Nombre} · {Cliente.Categoria}";
    }

    private sealed record ItemElemento(string Sku, string Nombre, bool EsKit)
    {
        public override string ToString() => $"{(EsKit ? "[KIT] " : "")}{Sku} - {Nombre}";
    }
}
