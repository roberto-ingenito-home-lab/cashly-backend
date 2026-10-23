namespace cashly.src.Data.Entities;

public enum InvestmentLogType
{
    DEPOSIT = 0,        // Versamento soldi nel conto (bonifico / rata PAC / acquisto)
    WITHDRAWAL = 1,     // Prelievo soldi dal conto (rientro su conto corrente)
    INTEREST = 2,       // Interesse accreditato (aumenta il valore attuale senza aumentare i soldi trasferiti)
    VALUE_SNAPSHOT = 3  // Rilevazione manuale del valore attuale del conto (profitto/perdita non realizzata)
}
