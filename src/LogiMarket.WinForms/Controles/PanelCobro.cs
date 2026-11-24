using LogiMarket.Application.Abstractions.Repositories;
using LogiMarket.Application.UseCases.ConfirmarPedido.Facade;
using LogiMarket.Domain.Enums;

namespace LogiMarket.WinForms.Controles;

public sealed class PanelCobro : UserControl
{
    private readonly IPedidoRepository _pedidos;
    private readonly IConfirmacionPedido _confirmacion;
    private readonly ComboBox _cmbPedido = new() { Width = 430, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _txtClave = new() { Width = 220 };
    private readonly Label _lblMensaje = new() { AutoSize = true };

    public PanelCobro(IPedidoRepository pedidos, IConfirmacionPedido confirmacion)
    {
        _pedidos = pedidos;
        _confirmacion = confirmacion;
        var panel = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(8) };
        panel.Controls.Add(new Label { Text = "Pedido por cobrar:", AutoSize = true, Margin = new Padding(0, 6, 4, 0) });
        panel.Controls.Add(_cmbPedido);
        panel.Controls.Add(new Label { Text = "Clave idempotencia:", AutoSize = true, Margin = new Padding(12, 6, 4, 0) });
        panel.Controls.Add(_txtClave);
        var boton = new Button { Text = "Confirmar y cobrar", Width = 150 };
        boton.Click += async (_, _) => await CobrarAsync();
        panel.Controls.Add(boton);
        panel.Controls.Add(_lblMensaje);
        Controls.Add(panel);
        Load += async (_, _) => await CargarAsync();
    }

    private async Task CargarAsync()
    {
        _cmbPedido.Items.Clear();
        foreach (var resumen in await _pedidos.ListarResumenAsync())
            if (resumen.Estado is EstadoPedido.Borrador or EstadoPedido.Validado)
                _cmbPedido.Items.Add(resumen);
        if (_cmbPedido.Items.Count > 0)
            _cmbPedido.SelectedIndex = 0;
    }

    private async Task CobrarAsync()
    {
        _lblMensaje.Text = string.Empty;
        if (_cmbPedido.SelectedItem is not ResumenPedido seleccionado)
        {
            MostrarError("Selecciona un pedido pendiente.");
            return;
        }
        if (string.IsNullOrWhiteSpace(_txtClave.Text))
        {
            MostrarError("Ingresa una clave de idempotencia.");
            return;
        }

        try
        {
            var resultado = await _confirmacion.ConfirmarAsync(
                new ConfirmarPedidoSolicitud(seleccionado.Id, _txtClave.Text.Trim()));
            if (!resultado.Exitoso)
            {
                MostrarError(resultado.Error?.Mensaje ?? "No se pudo cobrar el pedido.");
                return;
            }

            _lblMensaje.ForeColor = Color.ForestGreen;
            _lblMensaje.Text = resultado.EsRepeticion
                ? $"La clave ya se procesó. Referencia: {resultado.Pago?.Referencia}"
                : $"Pedido cobrado. Referencia: {resultado.Pago?.Referencia}";
            _txtClave.Clear();
            await CargarAsync();
        }
        catch (Exception ex)
        {
            MostrarError($"No se pudo completar el cobro: {ex.Message}");
        }
    }

    private void MostrarError(string mensaje)
    {
        _lblMensaje.ForeColor = Color.Firebrick;
        _lblMensaje.Text = mensaje;
    }
}
