namespace LogiMarket.Application.Abstractions.Services;

public interface IHasherContrasena
{
    string Hashear(string contrasena);
    bool Verificar(string contrasena, string hashAlmacenado);
}
