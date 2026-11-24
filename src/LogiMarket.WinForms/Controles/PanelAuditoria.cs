using LogiMarket.Application.Abstractions.Services;
using LogiMarket.Application.Metrics.Singleton;

namespace LogiMarket.WinForms.Controles;

public sealed class PanelAuditoria : UserControl
{
    private readonly IBitacoraAuditoria _bitacora;
    private readonly IAuditoriaSeguridad _auditoriaSeguridad;
    private readonly MetricasSesion _metricas;
    private readonly DataGridView _grillaNegocio = new()
    {
        Dock = DockStyle.Top, Height = 220, ReadOnly = true,
        AllowUserToAddRows = false, AllowUserToDeleteRows = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
    };
    private readonly DataGridView _grillaSeguridad = new()
    {
        Dock = DockStyle.Fill, ReadOnly = true,
        AllowUserToAddRows = false, AllowUserToDeleteRows = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
    };
    private readonly Label _lblMetricas = new() { Dock = DockStyle.Bottom, Height = 36, AutoSize = false };

    public PanelAuditoria(IBitacoraAuditoria bitacora, IAuditoriaSeguridad auditoriaSeguridad, MetricasSesion metricas)
    {
        _bitacora = bitacora;
        _auditoriaSeguridad = auditoriaSeguridad;
        _metricas = metricas;
        _grillaNegocio.Columns.Add("Tipo", "Tipo de evento (negocio)");
        _grillaNegocio.Columns.Add("Detalle", "Detalle");
        _grillaNegocio.Columns.Add("Fecha", "Fecha");
        _grillaSeguridad.Columns.Add("Usuario", "Usuario");
        _grillaSeguridad.Columns.Add("Rol", "Rol");
        _grillaSeguridad.Columns.Add("Operacion", "Operación rechazada");
        _grillaSeguridad.Columns.Add("Fecha", "Fecha");

        var actualizar = new Button { Text = "Actualizar auditorías", Dock = DockStyle.Top, Height = 32 };
        actualizar.Click += async (_, _) => await CargarAsync();
        Controls.Add(_grillaSeguridad);
        Controls.Add(_grillaNegocio);
        Controls.Add(_lblMetricas);
        Controls.Add(actualizar);
        Load += async (_, _) => await CargarAsync();
    }

    private async Task CargarAsync()
    {
        _grillaNegocio.Rows.Clear();
        foreach (var registro in await _bitacora.ListarRecientesAsync())
            _grillaNegocio.Rows.Add(registro.TipoEvento, registro.Detalle, registro.OcurrioUtc.ToLocalTime());

        _grillaSeguridad.Rows.Clear();
        foreach (var registro in await _auditoriaSeguridad.ListarRecientesAsync())
            _grillaSeguridad.Rows.Add(registro.Usuario, registro.Rol, registro.Operacion, registro.OcurrioUtc.ToLocalTime());

        var propiedades = typeof(MetricasSesion).GetProperties()
            .Select(p => $"{p.Name}: {p.GetValue(_metricas)}");
        _lblMetricas.Text = string.Join("    |    ", propiedades);
    }
}
