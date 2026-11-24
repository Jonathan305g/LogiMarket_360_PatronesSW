using LogiMarket.Domain.Catalog.Composite;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.ValueObjects;

namespace LogiMarket.Domain.Entities;

public class Devolucion
{
    public Guid Id { get; private set; }
    public Guid PedidoId { get; private set; }
    public MotivoDevolucion Motivo { get; private set; }
    public Dinero Reembolso { get; private set; }
    public DateTime FechaUtc { get; private set; }

    // SKU y cantidades que deben volver al inventario (una sola vez).
    public IReadOnlyList<ComponenteFisico> Reintegros { get; private set; }

    private Devolucion() { Reintegros = new List<ComponenteFisico>(); } // EF Core

    // "internal": solo el proceso de devolución (dentro de Domain) puede crear una.
    internal Devolucion(Guid pedidoId, MotivoDevolucion motivo, Dinero reembolso,
        DateTime fechaUtc, IEnumerable<ComponenteFisico> reintegros)
    {
        Id = Guid.NewGuid();
        PedidoId = pedidoId;
        Motivo = motivo;
        Reembolso = reembolso;
        FechaUtc = fechaUtc;
        Reintegros = reintegros.ToList();
    }
}
