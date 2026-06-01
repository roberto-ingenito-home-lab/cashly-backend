using System.ComponentModel.DataAnnotations;

namespace cashly.src.DTOs;

public class UserLoginDto
{
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public required string Email { get; set; }

    [Required]
    public required string Password { get; set; }
}