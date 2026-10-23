using System.ComponentModel.DataAnnotations;

namespace cashly.src.DTOs;

public class UpdateInvestmentAccountDto
{
    [Required]
    public string Name { get; set; } = string.Empty;
}
