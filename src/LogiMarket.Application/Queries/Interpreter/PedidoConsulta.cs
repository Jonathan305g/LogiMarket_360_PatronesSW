namespace LogiMarket.Application.Queries.Interpreter;

// Proyección plana de un pedido para consultarlo. No es una entidad de Domain:
// es solo lo que el reporte necesita leer (Infrastructure la construirá con una consulta real).
public sealed record PedidoConsulta(Guid PedidoId, string Estado, decimal Total, string Cliente);