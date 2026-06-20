using cashly.src.Data.Entities;

namespace cashly.src.DTOs;

public class TransactionListResponseDto
{
    public IEnumerable<TransactionResponseDto> Transactions { get; set; } = [];
    public int TotalCount { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal Balance { get; set; }
}

public class TransactionFilterDto
{
    public TransactionType? Type { get; set; }
    public int? CategoryId { get; set; }
    public string? DateFrom { get; set; }
    public string? DateTo { get; set; }
    public int Page { get; set; } = 0;
    public int PageSize { get; set; } = 20;
}
