using cashly.src.Data.Entities;

namespace cashly.src.DTOs;

public class SubscriptionResponseDto
{
    public int SubscriptionId { get; set; }
    public required string Name { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public SubscriptionFrequency Frequency { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public DateTime? LastPaymentDate { get; set; }
    public int? CategoryId { get; set; }
    public CategoryResponseDto? Category { get; set; }
}
