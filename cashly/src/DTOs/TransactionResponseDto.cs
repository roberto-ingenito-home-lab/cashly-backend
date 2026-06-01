using cashly.src.Data.Entities;

namespace cashly.src.DTOs;

public class TransactionResponseDto
{
    public int TransactionId { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public DateTime TransactionDate { get; set; }
    public string? Description { get; set; }
    public int? CategoryId { get; set; }
}