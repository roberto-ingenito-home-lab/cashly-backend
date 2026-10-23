using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace cashly.src.Data.Entities;

[Table("InvestmentLogs")]
public class InvestmentLog
{
    [Key]
    public int Id { get; set; }

    public int AccountId { get; set; }
    public virtual InvestmentAccount Account { get; set; } = null!;

    public int UserId { get; set; }
    public virtual User User { get; set; } = null!;

    public InvestmentLogType ActionType { get; set; } = InvestmentLogType.DEPOSIT;

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Amount { get; set; }

    public DateTime LogDate { get; set; } = DateTime.UtcNow;

    [MaxLength(300)]
    public string? Note { get; set; }
}
