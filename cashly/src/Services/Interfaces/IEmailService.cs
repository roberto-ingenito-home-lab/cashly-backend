namespace cashly.src.Services.Interfaces;

public interface IEmailService
{
    Task SendPasswordResetEmail(string toEmail, string resetLink);
}
