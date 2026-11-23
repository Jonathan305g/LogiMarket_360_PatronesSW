namespace LogiMarket.Application.Queries.Interpreter;

public enum TipoToken
{
    Campo,          // ESTADO, TOTAL, CLIENTE
    Operador,       // =, !=, >=, <=, >, <
    Valor,          // PAGADO, 50, Ana
    Y,              // AND
    O,              // OR
    ParentesisIzq,
    ParentesisDer,
    Fin
}