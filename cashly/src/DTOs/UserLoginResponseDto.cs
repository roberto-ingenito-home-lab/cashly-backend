namespace cashly.src.DTOs;

public class UserLoginResponseDto
{
    public required string Token { get; set; }
    public required string RefreshToken { get; set; }
    public required UserResponseDto User { get; set; }
}
