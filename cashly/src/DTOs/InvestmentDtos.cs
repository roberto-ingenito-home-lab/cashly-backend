using System.ComponentModel.DataAnnotations;
using cashly.src.Data.Entities;

namespace cashly.src.DTOs;

public class InvestmentLogDto
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public InvestmentLogType ActionType { get; set; }
    public decimal Amount { get; set; }
    public DateTime LogDate { get; set; }
    public string? Note { get; set; }
}

public class PagedInvestmentLogsDto
{
    public List<InvestmentLogDto> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? Math.Max(1, (int)Math.Ceiling((double)TotalCount / PageSize)) : 1;
}

public class InvestmentAccountDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ColorHex { get; set; } = "#4a7063";
    public string IconName { get; set; } = "account_balance";
    public DateTime UpdatedAt { get; set; }

    // Soldi netti trasferiti (Versamenti - Prelievi)
    public decimal TotalTransferred { get; set; }

    // Valore attuale complessivo segnato sul conto
    public decimal TotalCurrentValue { get; set; }

    // Somma di tutti gli interessi accreditati nel tempo
    public decimal TotalInterestCredited { get; set; }

    // Numero totale di operazioni nello storico (senza scaricarle tutte subito)
    public int TotalLogsCount { get; set; }

    // Profitto/Perdita totale (Valore Attuale - Soldi Trasferiti)
    public decimal ProfitLoss => TotalCurrentValue - TotalTransferred;

    // Profitto/Perdita non realizzato da oscillazione mercato (al netto degli interessi accreditati)
    public decimal UnrealizedMarketProfitLoss => ProfitLoss - TotalInterestCredited;

    public decimal ReturnPercentage => TotalTransferred > 0
        ? ((TotalCurrentValue - TotalTransferred) / TotalTransferred) * 100
        : 0;
}

public class CreateInvestmentAccountDto
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public decimal InitialDeposit { get; set; } = 0;
    public decimal? InitialCurrentValue { get; set; }
    public string ColorHex { get; set; } = "#4a7063";
    public string IconName { get; set; } = "account_balance";
}

public class InvestmentCashOperationDto
{
    [Required]
    public int AccountId { get; set; }
    public InvestmentLogType OperationType { get; set; } = InvestmentLogType.DEPOSIT;
    [Required]
    public decimal Amount { get; set; }
    public DateTime? LogDate { get; set; }
    public string? Note { get; set; }
}

public class UpdateInvestmentAccountValueDto
{
    [Required]
    public decimal CurrentValue { get; set; }
    public DateTime? LogDate { get; set; }
    public string? Note { get; set; }
}

public class UpdateInvestmentLogDto
{
    [Required]
    public InvestmentLogType ActionType { get; set; }
    [Required]
    public decimal Amount { get; set; }
    public DateTime? LogDate { get; set; }
    public string? Note { get; set; }
}
