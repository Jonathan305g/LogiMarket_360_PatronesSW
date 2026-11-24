using LogiMarket.Application.UseCases.Acceso;
using LogiMarket.WinForms.Mediador;
using LogiMarket.WinForms.Sesion;

namespace LogiMarket.WinForms.Controles;

public sealed class PanelAcceso : UserControl
{
    private readonly IInicioSesionCasoUso _inicioSesion;
    private readonly UsuarioActualEnSesion _sesion;
    private readonly IMediadorPrincipal _mediador;
    private readonly TextBox _txtUsuario = new() { Width = 220 };
    private readonly TextBox _txtContrasena = new() { Width = 220, UseSystemPasswordChar = true };
    private readonly Label _lblError = new() { ForeColor = Color.Firebrick, AutoSize = true };

    public PanelAcceso(IInicioSesionCasoUso inicioSesion, UsuarioActualEnSesion sesion, IMediadorPrincipal mediador)
    {
        _inicioSesion = inicioSesion;
        _sesion = sesion;
        _mediador = mediador;

        var tabla = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, Padding = new Padding(24), Location = new Point(40, 40) };
        tabla.Controls.Add(new Label { Text = "Usuario:", AutoSize = true }, 0, 0);
        tabla.Controls.Add(_txtUsuario, 1, 0);
        tabla.Controls.Add(new Label { Text = "Contraseña:", AutoSize = true }, 0, 1);
        tabla.Controls.Add(_txtContrasena, 1, 1);

        var boton = new Button { Text = "Ingresar", Width = 100 };
        boton.Click += async (_, _) => await IntentarIngresarAsync();
        tabla.Controls.Add(boton, 1, 2);
        tabla.Controls.Add(_lblError, 1, 3);
        Controls.Add(tabla);
    }

    private async Task IntentarIngresarAsync()
    {
        _lblError.Text = string.Empty;
        try
        {
            var resultado = await _inicioSesion.IniciarSesionAsync(_txtUsuario.Text, _txtContrasena.Text);
            if (!resultado.Exitoso)
            {
                _lblError.Text = resultado.Error ?? "No se pudo iniciar sesión.";
                return;
            }

            _sesion.IniciarSesion(resultado.Usuario!);
            _mediador.MostrarPanel(TipoPanel.PanelGeneral);
        }
        catch (Exception ex)
        {
            _lblError.Text = $"No se pudo iniciar sesión: {ex.Message}";
        }
    }
}
