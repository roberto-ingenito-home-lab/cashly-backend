using System.ComponentModel.DataAnnotations;

namespace cashly.src.DTOs;

public class ForgotPasswordRequestDto
{
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public required string Email { get; set; }
}
