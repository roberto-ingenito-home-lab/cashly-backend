using System.ComponentModel.DataAnnotations;
using cashly.src.Data.Entities;

namespace cashly.src.DTOs;

public class TransactionCreateDto
{
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "L'importo deve essere positivo.")]
    public decimal Amount { get; set; }

    [Required]
    public TransactionType Type { get; set; }

    public int? CategoryId { get; set; }

    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

    [MaxLength(500)]
    public string? Description { get; set; }
}