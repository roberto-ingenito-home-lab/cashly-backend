using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace cashly.src.Data.Entities;

[Table("InvestmentAccounts")]
public class InvestmentAccount
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }
    public virtual User User { get; set; } = null!;

    [Required]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18, 2)")]
    public decimal CashTransferredNet { get; set; } = 0;

    [Column(TypeName = "decimal(18, 2)")]
    public decimal CashCurrentBalance { get; set; } = 0;

    [MaxLength(20)]
    public string ColorHex { get; set; } = "#4a7063";

    [MaxLength(50)]
    public string IconName { get; set; } = "account_balance";

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<InvestmentLog> Logs { get; set; } = [];
}
