using System.ComponentModel.DataAnnotations;

namespace cashly.src.Data.Entities;

public class Category
{
    [Key]
    public int CategoryId { get; set; }

    [Required]
    [MaxLength(100)]
    public required string CategoryName { get; set; }

    [MaxLength(50)]
    public string? IconName { get; set; }

    [MaxLength(7)]
    public string? ColorHex { get; set; }

    // Foreign Key per l'utente
    public int UserId { get; set; }
    public virtual User User { get; set; } = null!;

    // Navigation property: una categoria può essere associata a molte transazioni
    public virtual ICollection<Transaction> Transactions { get; set; } = [];
}