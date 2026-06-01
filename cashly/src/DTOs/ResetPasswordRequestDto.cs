using System.ComponentModel.DataAnnotations;

namespace cashly.src.DTOs;

public class ResetPasswordRequestDto
{
    [Required]
    public required string Token { get; set; }

    [Required]
    [MinLength(6)]
    public required string NewPassword { get; set; }
}
