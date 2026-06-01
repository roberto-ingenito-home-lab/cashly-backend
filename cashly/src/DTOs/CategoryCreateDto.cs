using System.ComponentModel.DataAnnotations;

namespace cashly.src.DTOs;

public class CategoryCreateDto
{
    [Required]
    [MaxLength(100)]
    public required string CategoryName { get; set; }

    [MaxLength(50)]
    public string? IconName { get; set; }

    [MaxLength(7)]
    public string? ColorHex { get; set; }
}