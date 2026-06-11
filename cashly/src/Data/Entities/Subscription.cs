using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace cashly.src.Data.Entities;

public class Subscription
{
    [Key]
    public int SubscriptionId { get; set; }

    [Required]
    [MaxLength(150)]
    public required string Name { get; set; }

    [Required]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal Amount { get; set; }

    [Required]
    public TransactionType Type { get; set; } = TransactionType.expense;

    [Required]
    public SubscriptionFrequency Frequency { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public DateTime? LastPaymentDate { get; set; }

    // Foreign Key for Category
    public int? CategoryId { get; set; }
    public virtual Category? Category { get; set; }

    // Foreign Key for User
    [Required]
    public int UserId { get; set; }
    public virtual User User { get; set; } = null!;
}
