using cashly.src.Data.Entities;
using cashly.src.DTOs;

namespace cashly.src.Services.Interfaces;

public interface IUserService
{
    Task<User> SignUp(UserCreateDto dto);
    Task<User> SignIn(UserLoginDto dto);
    Task Delete(int userId);
    Task<UserLoginResponseDto> GenerateTokens(User user);
    Task<UserLoginResponseDto> RefreshToken(string refreshToken);
    Task ForgotPassword(ForgotPasswordRequestDto dto, string origin);
    Task ResetPassword(ResetPasswordRequestDto dto);
}
