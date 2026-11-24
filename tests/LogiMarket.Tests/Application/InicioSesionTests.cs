using LogiMarket.Application.UseCases.Acceso;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Infrastructure.Seguridad;
using LogiMarket.Tests.Fakes;

namespace LogiMarket.Tests.Application;

public sealed class InicioSesionTests
{
    private readonly HasherContrasenaPbkdf2 _hasher = new();

    private Usuario CrearUsuario(string nombre, string contrasena, RolUsuario rol) =>
        new(nombre, _hasher.Hashear(contrasena), rol);

    [Fact]
    public async Task CredencialesCorrectas_DevuelveUsuarioSinAuditar()
    {
        var repo = new UsuarioRepositorioDePrueba();
        repo.Sembrar(CrearUsuario("admin", "Admin123!", RolUsuario.Admin));
        var auditoria = new AuditoriaSeguridadDePrueba();

        var resultado = await new InicioSesionCasoUso(repo, _hasher, auditoria)
            .IniciarSesionAsync("admin", "Admin123!");

        Assert.True(resultado.Exitoso);
        Assert.Equal("admin", resultado.Usuario!.NombreUsuario);
        Assert.Empty(auditoria.Registros);
    }

    [Fact]
    public async Task ContrasenaIncorrecta_RechazaYAuditaElIntento()
    {
        var repo = new UsuarioRepositorioDePrueba();
        repo.Sembrar(CrearUsuario("admin", "Admin123!", RolUsuario.Admin));
        var auditoria = new AuditoriaSeguridadDePrueba();

        var resultado = await new InicioSesionCasoUso(repo, _hasher, auditoria)
            .IniciarSesionAsync("admin", "ClaveMala");

        Assert.False(resultado.Exitoso);
        Assert.Equal("Usuario o contraseña incorrectos.", resultado.Error);
        Assert.Single(auditoria.Registros);
    }

    [Fact]
    public async Task UsuarioInexistente_RechazaConElMismoMensajeYAudita()
    {
        var auditoria = new AuditoriaSeguridadDePrueba();
        var resultado = await new InicioSesionCasoUso(new UsuarioRepositorioDePrueba(), _hasher, auditoria)
            .IniciarSesionAsync("fantasma", "loquesea");

        Assert.False(resultado.Exitoso);
        Assert.Equal("Usuario o contraseña incorrectos.", resultado.Error);
        Assert.Equal("Desconocido", Assert.Single(auditoria.Registros).Rol);
    }

    [Fact]
    public async Task UsuarioDesactivado_RechazaElAcceso()
    {
        var repo = new UsuarioRepositorioDePrueba();
        var usuario = CrearUsuario("exempleado", "Clave123!", RolUsuario.Ventas);
        usuario.Desactivar();
        repo.Sembrar(usuario);

        var resultado = await new InicioSesionCasoUso(repo, _hasher, new AuditoriaSeguridadDePrueba())
            .IniciarSesionAsync("exempleado", "Clave123!");

        Assert.False(resultado.Exitoso);
    }
}
