using LogiMarket.Application.Abstractions.Repositories;
using LogiMarket.Application.UseCases.Despacho;
using LogiMarket.Domain.Enums;

namespace LogiMarket.WinForms.Controles;

public sealed class PanelDespacho : UserControl
{
    private readonly IPedidoRepository _pedidos;
    private readonly IGestionDespacho _despacho;
    private readonly ComboBox _cmbPagados = new() { Width = 430, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _cmbDespachados = new() { Width = 430, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Label _lblMensaje = new() { AutoSize = true };

    public PanelDespacho(IPedidoRepository pedidos, IGestionDespacho despacho)
    {
        _pedidos = pedidos;
        _despacho = despacho;
        var panel = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(8), FlowDirection = FlowDirection.TopDown, WrapContents = false };

        var filaDespacho = new FlowLayoutPanel { AutoSize = true };
        filaDespacho.Controls.Add(new Label { Text = "Pedido pagado:", AutoSize = true, Margin = new Padding(0, 6, 4, 0) });
        filaDespacho.Controls.Add(_cmbPagados);
        var botonDespachar = new Button { Text = "Registrar despacho", Width = 150 };
        botonDespachar.Click += async (_, _) => await DespacharAsync();
        filaDespacho.Controls.Add(botonDespachar);

        var filaEntrega = new FlowLayoutPanel { AutoSize = true };
        filaEntrega.Controls.Add(new Label { Text = "Pedido despachado:", AutoSize = true, Margin = new Padding(0, 6, 4, 0) });
        filaEntrega.Controls.Add(_cmbDespachados);
        var botonEntregar = new Button { Text = "Marcar entregado", Width = 150 };
        botonEntregar.Click += async (_, _) => await EntregarAsync();
        filaEntrega.Controls.Add(botonEntregar);

        panel.Controls.Add(filaDespacho);
        panel.Controls.Add(filaEntrega);
        panel.Controls.Add(_lblMensaje);
        Controls.Add(panel);
        Load += async (_, _) => await CargarAsync();
    }

    private async Task CargarAsync()
    {
        var resumenes = await _pedidos.ListarResumenAsync();
        _cmbPagados.Items.Clear();
        _cmbDespachados.Items.Clear();
        foreach (var resumen in resumenes)
        {
            if (resumen.Estado == EstadoPedido.Pagado) _cmbPagados.Items.Add(resumen);
            if (resumen.Estado == EstadoPedido.Despachado) _cmbDespachados.Items.Add(resumen);
        }
        if (_cmbPagados.Items.Count > 0) _cmbPagados.SelectedIndex = 0;
        if (_cmbDespachados.Items.Count > 0) _cmbDespachados.SelectedIndex = 0;
    }

    private async Task DespacharAsync()
    {
        if (_cmbPagados.SelectedItem is not ResumenPedido pedido)
        {
            MostrarError("Selecciona un pedido pagado.");
            return;
        }

        var resultado = await _despacho.DespacharAsync(pedido.Id);
        if (!resultado.Exitoso)
        {
            MostrarError(resultado.Error?.Mensaje ?? "No se pudo despachar el pedido.");
            return;
        }

        _lblMensaje.ForeColor = Color.ForestGreen;
        _lblMensaje.Text = $"Pedido despachado. Guía: {resultado.Guia}";
        await CargarAsync();
    }

    private async Task EntregarAsync()
    {
        if (_cmbDespachados.SelectedItem is not ResumenPedido pedido)
        {
            MostrarError("Selecciona un pedido despachado.");
            return;
        }

        var resultado = await _despacho.EntregarAsync(pedido.Id);
        if (!resultado.Exitoso)
        {
            MostrarError(resultado.Error?.Mensaje ?? "No se pudo registrar la entrega.");
            return;
        }

        _lblMensaje.ForeColor = Color.ForestGreen;
        _lblMensaje.Text = "Pedido marcado como entregado.";
        await CargarAsync();
    }

    private void MostrarError(string mensaje)
    {
        _lblMensaje.ForeColor = Color.Firebrick;
        _lblMensaje.Text = mensaje;
    }
}
