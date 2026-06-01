using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using cashly.src.Data;
using cashly.src.Data.Entities;
using cashly.src.DTOs;
using cashly.src.Exceptions;
using cashly.src.Extensions;
using cashly.src.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace cashly.src.Services.Implementations;

public class UserService(AppDbContext dbContext, IConfiguration configuration, IEmailService emailService) : IUserService
{
    public async Task Delete(int userId)
    {
        await dbContext.Users.Where(u => u.UserId == userId).ExecuteDeleteAsync();
    }

    public async Task<User> SignIn(UserLoginDto dto)
    {
        // Trova l'utente tramite email
        User? user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

        // Se l'utente non esiste o la password è errata, restituisci null
        if (user == null)
        {
            throw new AppException("wrong-credentials", HttpStatusCode.Unauthorized);
        }
        // controlla se la password è errata
        else if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.HashedPassword))
        {
            throw new AppException("wrong-credentials", HttpStatusCode.Unauthorized);
        }

        return user;
    }

    public async Task<User> SignUp(UserCreateDto dto)
    {
        // Controlla se l'email è già in uso
        if (await dbContext.Users.AnyAsync(u => u.Email == dto.Email))
        {
            throw new AppException("user-already-exists", HttpStatusCode.Conflict);
        }

        // Crea un nuovo utente e esegui l'hashing della password
        User newUser = new() { Email = dto.Email, HashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password) };

        // Salva l'utente nel database
        dbContext.Users.Add(newUser);
        await dbContext.SaveChangesAsync();

        return newUser;
    }

    public async Task<UserLoginResponseDto> GenerateTokens(User user)
    {
        string token = GenerateJwtToken(user);
        string refreshToken = GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddMonths(1);

        await dbContext.SaveChangesAsync();

        return new UserLoginResponseDto
        {
            Token = token,
            RefreshToken = refreshToken,
            User = user.ToDto(),
        };
    }

    public async Task<UserLoginResponseDto> RefreshToken(string refreshToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

        if (user == null || user.RefreshTokenExpiry < DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("invalid-refresh-token");
        }

        // Refresh token rotation: generate new tokens and invalidate old one
        return await GenerateTokens(user);
    }

    private string GenerateJwtToken(User user)
    {
        // Ottieni la chiave segreta e altre impostazioni da appsettings.json
        string? jwtKey = configuration["Jwt:Key"];
        if (string.IsNullOrEmpty(jwtKey))
        {
            throw new InvalidOperationException("La chiave JWT non è configurata in appsettings.json");
        }
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        // Definisci le "claims" (informazioni sull'utente da inserire nel token)
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()), // 'Subject' -> ID dell'utente
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // ID univoco per il token
        };

        // Access token lifespan: 15 minutes for better security
        var expires = DateTime.UtcNow.AddMinutes(15);

        // Crea il token
        var token = new JwtSecurityToken(
            claims: claims,
            expires: expires,
            signingCredentials: credentials,
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"]
        );

        // Scrivi il token come stringa
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public async Task ForgotPassword(ForgotPasswordRequestDto dto, string origin)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        
        if (user == null)
        {
            return;
        }

        var resetToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        
        user.PasswordResetToken = resetToken;
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
        await dbContext.SaveChangesAsync();

        string baseUrl = "http://localhost:3000";
        if (Uri.TryCreate(origin, UriKind.Absolute, out var uri))
        {
            baseUrl = uri.GetLeftPart(UriPartial.Authority);
        }
        string resetLink = $"{baseUrl}/cashly/reset-password?token={resetToken}";

        await emailService.SendPasswordResetEmail(user.Email, resetLink);
    }

    public async Task ResetPassword(ResetPasswordRequestDto dto)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == dto.Token);

        if (user == null || user.PasswordResetTokenExpiry < DateTime.UtcNow)
        {
            throw new AppException("invalid-or-expired-token", HttpStatusCode.BadRequest);
        }

        user.HashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;

        await dbContext.SaveChangesAsync();
    }
}
