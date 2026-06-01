using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace cashly.src.Data.Entities;

public class Transaction
{
    [Key]
    public int TransactionId { get; set; }

    [Required]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal Amount { get; set; }

    [Required]
    public TransactionType Type { get; set; } // Uso dell'enum

    public DateTime TransactionDate { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; } // Nullable, quindi opzionale

    // Foreign Key per la categoria
    public int? CategoryId { get; set; }
    public virtual Category? Category { get; set; }

    // Foreign Key per l'utente
    public int UserId { get; set; }
    public virtual User User { get; set; } = null!;
}