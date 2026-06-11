using System.ComponentModel.DataAnnotations;
using cashly.src.Data.Entities;

namespace cashly.src.DTOs;

public class SubscriptionCreateDto
{
    [Required]
    [MaxLength(150)]
    public required string Name { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "L'importo deve essere maggiore di zero.")]
    public decimal Amount { get; set; }

    [Required]
    public TransactionType Type { get; set; } = TransactionType.expense;

    [Required]
    public SubscriptionFrequency Frequency { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public int? CategoryId { get; set; }
}
