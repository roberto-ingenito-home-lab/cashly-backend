namespace cashly.src.DTOs;

public class UserResponseDto
{
    public int UserId { get; set; }
    public required string Email { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}