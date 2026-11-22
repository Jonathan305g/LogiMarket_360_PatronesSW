namespace LogiMarket.Application.UseCases.ConfirmarPedido.Chain;

// Arma la cadena en el orden que exige la guía y devuelve su primer eslabón.
public static class CadenaValidacionConfirmacion
{
    public static EslabonValidacion Crear()
    {
        var primero = new ValidarClienteActivo();

        primero.Siguiente(new ValidarLineas())
               .Siguiente(new ValidarStock())
               .Siguiente(new ValidarAutorizacionYCondiciones());

        return primero;
    }
}