using LogiMarket.Application.Abstractions.Repositories;
using LogiMarket.Application.UseCases.Devoluciones;
using LogiMarket.Domain.Enums;

namespace LogiMarket.WinForms.Controles;

public sealed class PanelDevoluciones : UserControl
{
    private readonly IPedidoRepository _pedidos;
    private readonly IProcesoDevolucion _devoluciones;
    private readonly ComboBox _cmbPedido = new() { Width = 430, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _cmbMotivo = new() { Width = 160, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly CheckBox _chkSinUso = new() { Text = "Producto sin uso", Checked = true, AutoSize = true };
    private readonly Label _lblMensaje = new() { AutoSize = true };

    public PanelDevoluciones(IPedidoRepository pedidos, IProcesoDevolucion devoluciones)
    {
        _pedidos = pedidos;
        _devoluciones = devoluciones;
        _cmbMotivo.Items.AddRange(Enum.GetNames<MotivoDevolucion>());
        _cmbMotivo.SelectedIndex = 0;
        var panel = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(8) };
        panel.Controls.Add(new Label { Text = "Pedido entregado:", AutoSize = true, Margin = new Padding(0, 6, 4, 0) });
        panel.Controls.Add(_cmbPedido);
        panel.Controls.Add(new Label { Text = "Motivo:", AutoSize = true, Margin = new Padding(12, 6, 4, 0) });
        panel.Controls.Add(_cmbMotivo);
        panel.Controls.Add(_chkSinUso);
        var boton = new Button { Text = "Procesar devolución", Width = 160 };
        boton.Click += async (_, _) => await ProcesarAsync();
        panel.Controls.Add(boton);
        panel.Controls.Add(_lblMensaje);
        Controls.Add(panel);
        Load += async (_, _) => await CargarAsync();
    }

    private async Task CargarAsync()
    {
        _cmbPedido.Items.Clear();
        foreach (var resumen in await _pedidos.ListarResumenAsync())
            if (resumen.Estado == EstadoPedido.Entregado)
                _cmbPedido.Items.Add(resumen);
        if (_cmbPedido.Items.Count > 0)
            _cmbPedido.SelectedIndex = 0;
    }

    private async Task ProcesarAsync()
    {
        if (_cmbPedido.SelectedItem is not ResumenPedido seleccionado)
        {
            MostrarError("Selecciona un pedido entregado.");
            return;
        }

        try
        {
            var motivo = Enum.Parse<MotivoDevolucion>((string)_cmbMotivo.SelectedItem!);
            var solicitud = new SolicitudDevolucionDto(seleccionado.Id, motivo, DateTime.UtcNow, _chkSinUso.Checked);
            var resultado = await _devoluciones.SolicitarAsync(solicitud);
            if (!resultado.Exitoso)
            {
                MostrarError(resultado.Error?.Mensaje ?? "No se pudo procesar la devolución.");
                return;
            }

            _lblMensaje.ForeColor = Color.ForestGreen;
            _lblMensaje.Text = "Devolución procesada correctamente.";
            await CargarAsync();
        }
        catch (Exception ex)
        {
            MostrarError($"No se pudo procesar la devolución: {ex.Message}");
        }
    }

    private void MostrarError(string mensaje)
    {
        _lblMensaje.ForeColor = Color.Firebrick;
        _lblMensaje.Text = mensaje;
    }
}
