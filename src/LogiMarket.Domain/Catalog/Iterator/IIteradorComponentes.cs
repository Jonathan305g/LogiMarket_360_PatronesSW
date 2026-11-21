using LogiMarket.Domain.Catalog.Composite;

namespace LogiMarket.Domain.Catalog.Iterator;

// Contrato del iterador: quien recorre solo sabe "¿hay más?" y "dame el siguiente".
// No sabe cómo está guardado el kit por dentro.
public interface IIteradorComponentes
{
    bool TieneSiguiente();
    ComponenteFisico Siguiente();
}