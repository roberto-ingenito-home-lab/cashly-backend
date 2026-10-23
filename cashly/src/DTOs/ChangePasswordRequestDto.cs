using System.ComponentModel.DataAnnotations;

namespace cashly.src.DTOs;

public class ChangePasswordRequestDto
{
    [Required]
    public required string CurrentPassword { get; set; }

    [Required]
    [MinLength(6)]
    public required string NewPassword { get; set; }
}
