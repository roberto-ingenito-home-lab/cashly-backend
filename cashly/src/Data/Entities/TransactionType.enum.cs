namespace cashly.src.Data.Entities;

public enum TransactionType
{
    income, // Entrata
    expense, // Uscita
    transfer_out, // Trasferimento in uscita (es. versamento su conto investimenti)
    transfer_in, // Trasferimento in entrata (es. prelievo/rientro da conto investimenti)
}
