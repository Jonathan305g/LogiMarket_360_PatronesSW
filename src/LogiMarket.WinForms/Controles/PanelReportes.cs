using LogiMarket.Application.Abstractions.Repositories;
using LogiMarket.Application.Comprobantes.Bridge;
using LogiMarket.Domain.Entities;
using LogiMarket.Infrastructure.Comprobantes;
using LogiMarket.Infrastructure.Outbox;

namespace LogiMarket.WinForms.Controles;

public sealed class PanelReportes : UserControl
{
    private readonly IPedidoRepository _pedidos;
    private readonly ProcesadorOutbox _procesadorOutbox;
    private readonly ComboBox _cmbPedido = new() { Width = 430, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _txtComprobante = new()
    {
        Dock = DockStyle.Fill, Multiline = true, ReadOnly = true,
        ScrollBars = ScrollBars.Vertical, Font = new Font("Consolas", 10)
    };
    private readonly Label _lblMensaje = new() { AutoSize = true };

    public PanelReportes(IPedidoRepository pedidos, ProcesadorOutbox procesadorOutbox)
    {
        _pedidos = pedidos;
        _procesadorOutbox = procesadorOutbox;
        var panel = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(8) };
        panel.Controls.Add(new Label { Text = "Pedido:", AutoSize = true, Margin = new Padding(0, 6, 4, 0) });
        panel.Controls.Add(_cmbPedido);

        var vistaPrevia = new Button { Text = "Ver comprobante", Width = 140 };
        vistaPrevia.Click += async (_, _) => await VerComprobanteAsync();
        panel.Controls.Add(vistaPrevia);
        var guardarTxt = new Button { Text = "Guardar TXT", Width = 110 };
        guardarTxt.Click += async (_, _) => await GuardarTxtAsync();
        panel.Controls.Add(guardarTxt);
        var procesar = new Button { Text = "Procesar notificaciones pendientes", Width = 240 };
        procesar.Click += async (_, _) => await ProcesarOutboxAsync();
        panel.Controls.Add(procesar);
        panel.Controls.Add(_lblMensaje);

        Controls.Add(_txtComprobante);
        Controls.Add(panel);
        Load += async (_, _) => await CargarAsync();
    }

    private async Task CargarAsync()
    {
        _cmbPedido.Items.Clear();
        foreach (var resumen in await _pedidos.ListarResumenAsync())
            _cmbPedido.Items.Add(resumen);
        if (_cmbPedido.Items.Count > 0)
            _cmbPedido.SelectedIndex = 0;
    }

    private async Task<Pedido?> PedidoSeleccionadoAsync() =>
        _cmbPedido.SelectedItem is ResumenPedido resumen
            ? await _pedidos.ObtenerPorIdAsync(resumen.Id)
            : null;

    private async Task VerComprobanteAsync()
    {
        var pedido = await PedidoSeleccionadoAsync();
        if (pedido is null)
        {
            MostrarError("Selecciona un pedido.");
            return;
        }

        _txtComprobante.Text = new ComprobanteVenta(pedido, new VistaPrevia()).Generar();
        MostrarExito("Vista previa generada.");
    }

    private async Task GuardarTxtAsync()
    {
        var pedido = await PedidoSeleccionadoAsync();
        if (pedido is null)
        {
            MostrarError("Selecciona un pedido.");
            return;
        }

        var carpeta = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LogiMarket360", "Comprobantes");
        var ruta = new ComprobanteVenta(pedido, new ArchivoTXT(carpeta)).Generar();
        _txtComprobante.Text = File.ReadAllText(ruta);
        MostrarExito($"Comprobante guardado: {ruta}");
    }

    private async Task ProcesarOutboxAsync()
    {
        _lblMensaje.Text = "Procesando...";
        try
        {
            await _procesadorOutbox.ProcesarPendientesAsync();
            MostrarExito("Notificaciones pendientes procesadas.");
        }
        catch (Exception ex)
        {
            MostrarError($"Error al procesar notificaciones: {ex.Message}");
        }
    }

    private void MostrarExito(string mensaje)
    {
        _lblMensaje.ForeColor = Color.ForestGreen;
        _lblMensaje.Text = mensaje;
    }

    private void MostrarError(string mensaje)
    {
        _lblMensaje.ForeColor = Color.Firebrick;
        _lblMensaje.Text = mensaje;
    }
}
