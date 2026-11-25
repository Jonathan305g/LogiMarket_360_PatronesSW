using LogiMarket.Application.Abstractions.Repositories;
using LogiMarket.Application.Abstractions.Services;
using LogiMarket.Application.UseCases.Despacho;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;

namespace LogiMarket.WinForms.Controles;

public sealed class PanelDespacho : UserControl
{
    private readonly IPedidoRepository _pedidos;
    private readonly IDespachoRepository _despachos;
    private readonly IGestionDespacho _despacho;
    private readonly IProcesadorOutbox _outbox;

    private readonly DataGridView _grilla = new() { Dock = DockStyle.Fill };
    private readonly TextBox _txtPedido = new() { Width = 260, ReadOnly = true, Font = EstiloUi.FuenteNormal };
    private readonly TextBox _txtGuia = new() { Width = 260, ReadOnly = true, Font = EstiloUi.FuenteMonocromatica };
    private readonly Button _btnDespachar = new() { Text = "Marcar como despachado", Width = 260, Height = 38 };
    private readonly Button _btnEntregar = new() { Text = "Registrar entrega", Width = 260, Height = 38 };
    private readonly Label _lblMensaje = new() { AutoSize = true, Font = EstiloUi.FuenteNormal, Margin = new Padding(0, 10, 0, 0) };

    private ResumenPedido? _seleccionado;

    public PanelDespacho(
        IPedidoRepository pedidos,
        IDespachoRepository despachos,
        IGestionDespacho despacho,
        IProcesadorOutbox outbox)
    {
        _pedidos = pedidos;
        _despachos = despachos;
        _despacho = despacho;
        _outbox = outbox;

        Dock = DockStyle.Fill;
        BackColor = Color.White;

        ConstruirInterfaz();
        Load += async (_, _) => await CargarAsync();
    }

    private void ConstruirInterfaz()
    {
        var encabezado = EstiloUi.CrearEncabezado("Despacho y entrega", "Preparación, guía interna y transición autorizada");

        // Área central dividida: Grilla pedidos (70%) + Detalle logístico (30%)
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

        // Tarjeta Grilla
        var tarjetaGrilla = EstiloUi.CrearTarjeta(0, 0, new Padding(12));
        tarjetaGrilla.Dock = DockStyle.Fill;

        var headerGrilla = new Panel { Dock = DockStyle.Top, Height = 36 };
        var lblTituloGrilla = new Label
        {
            Text = "Pedidos logísticos (PAGADO / DESPACHADO)",
            Font = EstiloUi.FuenteSeccion,
            ForeColor = EstiloUi.ColorTexto,
            Dock = DockStyle.Left,
            AutoSize = true
        };
        var btnRefrescar = new Button { Text = "Actualizar", Dock = DockStyle.Right, Width = 90 };
        EstiloUi.AplicarBotonSecundario(btnRefrescar);
        btnRefrescar.Click += async (_, _) => await CargarAsync();

        headerGrilla.Controls.Add(lblTituloGrilla);
        headerGrilla.Controls.Add(btnRefrescar);

        EstiloUi.AplicarGrilla(_grilla);
        _grilla.Columns.Add("Id", "Id");
        _grilla.Columns.Add("Codigo", "Pedido");
        _grilla.Columns.Add("Cliente", "Cliente");
        _grilla.Columns.Add("Estado", "Estado");
        _grilla.Columns.Add("Total", "Total");
        _grilla.Columns["Id"]!.Visible = false;

        _grilla.SelectionChanged += async (_, _) => await MostrarDetalleAsync();

        tarjetaGrilla.Controls.Add(_grilla);
        tarjetaGrilla.Controls.Add(headerGrilla);

        // Tarjeta Detalle Logístico (Wireframe 08)
        var tarjetaDetalle = EstiloUi.CrearTarjeta(0, 0, new Padding(20));
        tarjetaDetalle.Dock = DockStyle.Fill;

        var lblTituloLogistica = new Label
        {
            Text = "Detalle logístico",
            Font = EstiloUi.FuenteSeccion,
            ForeColor = EstiloUi.ColorTexto,
            Dock = DockStyle.Top,
            Height = 28
        };

        var flujoCampos = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(0, 10, 0, 0)
        };

        flujoCampos.Controls.Add(new Label { Text = "Pedido:", Font = EstiloUi.FuenteNegrita, AutoSize = true, Margin = new Padding(0, 4, 0, 4) });
        flujoCampos.Controls.Add(_txtPedido);

        flujoCampos.Controls.Add(new Label { Text = "Guía simulada:", Font = EstiloUi.FuenteNegrita, AutoSize = true, Margin = new Padding(0, 12, 0, 4) });
        flujoCampos.Controls.Add(_txtGuia);

        EstiloUi.AplicarBotonPrimario(_btnDespachar);
        _btnDespachar.Margin = new Padding(0, 20, 0, 8);
        _btnDespachar.Click += async (_, _) => await DespacharAsync();
        flujoCampos.Controls.Add(_btnDespachar);

        EstiloUi.AplicarBotonSecundario(_btnEntregar);
        _btnEntregar.Margin = new Padding(0, 0, 0, 8);
        _btnEntregar.Click += async (_, _) => await EntregarAsync();
        flujoCampos.Controls.Add(_btnEntregar);

        flujoCampos.Controls.Add(_lblMensaje);

        tarjetaDetalle.Controls.Add(flujoCampos);
        tarjetaDetalle.Controls.Add(lblTituloLogistica);

        panelCentral.Controls.Add(tarjetaGrilla, 0, 0);
        panelCentral.Controls.Add(tarjetaDetalle, 1, 0);

        Controls.Add(panelCentral);
        Controls.Add(encabezado);
    }

    private async Task CargarAsync()
    {
        _grilla.Rows.Clear();
        var resumenes = await _pedidos.ListarResumenAsync();
        var relevantes = resumenes
            .Where(r => r.Estado is EstadoPedido.Pagado or EstadoPedido.Despachado or EstadoPedido.Entregado)
            .OrderByDescending(r => r.Estado == EstadoPedido.Pagado)
            .ThenByDescending(r => r.Estado == EstadoPedido.Despachado);

        foreach (var r in relevantes)
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
            Limpiar();
    }

    private async Task MostrarDetalleAsync()
    {
        _lblMensaje.Text = string.Empty;
        if (_grilla.SelectedRows.Count == 0)
        {
            Limpiar();
            return;
        }

        var id = (Guid)_grilla.SelectedRows[0].Cells["Id"].Value;
        var resumenes = await _pedidos.ListarResumenAsync();
        _seleccionado = resumenes.FirstOrDefault(r => r.Id == id);
        if (_seleccionado is null) return;

        _txtPedido.Text = $"PED-{_seleccionado.Id.ToString()[..6].ToUpperInvariant()} ({_seleccionado.Cliente})";

        // Buscar guía si ya fue despachado
        var despachos = await _despachos.ListarAsync();
        var despacho = despachos.FirstOrDefault(d => d.PedidoId == _seleccionado.Id);
        _txtGuia.Text = despacho?.Guia ?? "Pendiente de despacho";

        _btnDespachar.Enabled = _seleccionado.Estado == EstadoPedido.Pagado;
        _btnEntregar.Enabled = _seleccionado.Estado == EstadoPedido.Despachado;
    }

    private void Limpiar()
    {
        _seleccionado = null;
        _txtPedido.Text = string.Empty;
        _txtGuia.Text = string.Empty;
        _btnDespachar.Enabled = false;
        _btnEntregar.Enabled = false;
    }

    private async Task DespacharAsync()
    {
        if (_seleccionado is null) return;
        if (MessageBox.Show(this, "¿Confirmas registrar el despacho de este pedido?", "Confirmar despacho",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        try
        {
            var resultado = await _despacho.DespacharAsync(_seleccionado.Id);
            if (!resultado.Exitoso)
            {
                MostrarError(resultado.Error?.Mensaje ?? "No se pudo despachar el pedido.");
                return;
            }

            _lblMensaje.ForeColor = EstiloUi.ColorExito;
            _lblMensaje.Text = $"Despacho registrado. Guía: {resultado.Guia}";
            try { await _outbox.ProcesarPendientesAsync(); }
            catch { _lblMensaje.Text += " | Evento pendiente de reintento."; }
            await CargarAsync();
        }
        catch (Exception ex)
        {
            MostrarError(ex.Message);
        }
    }

    private async Task EntregarAsync()
    {
        if (_seleccionado is null) return;
        if (MessageBox.Show(this, "¿Confirmas que el pedido fue entregado?", "Confirmar entrega",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        try
        {
            var resultado = await _despacho.EntregarAsync(_seleccionado.Id);
            if (!resultado.Exitoso)
            {
                MostrarError(resultado.Error?.Mensaje ?? "No se pudo marcar como entregado.");
                return;
            }

            _lblMensaje.ForeColor = EstiloUi.ColorExito;
            _lblMensaje.Text = "Pedido marcado como ENTREGADO.";
            try { await _outbox.ProcesarPendientesAsync(); }
            catch { _lblMensaje.Text += " | Evento pendiente de reintento."; }
            await CargarAsync();
        }
        catch (Exception ex)
        {
            MostrarError(ex.Message);
        }
    }

    private void MostrarError(string mensaje)
    {
        _lblMensaje.ForeColor = EstiloUi.ColorPeligro;
        _lblMensaje.Text = mensaje;
    }
}
