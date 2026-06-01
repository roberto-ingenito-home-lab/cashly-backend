using System.Net;
using System.Net.Mail;
using cashly.src.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace cashly.src.Services.Implementations;

public class EmailService(ILogger<EmailService> logger) : IEmailService
{
    public async Task SendPasswordResetEmail(string toEmail, string resetLink)
    {
        string? host = Environment.GetEnvironmentVariable("SMTP_HOST");
        string? portStr = Environment.GetEnvironmentVariable("SMTP_PORT");
        string? username = Environment.GetEnvironmentVariable("SMTP_USERNAME");
        string? password = Environment.GetEnvironmentVariable("SMTP_PASSWORD");
        string fromEmail = Environment.GetEnvironmentVariable("SMTP_FROM_EMAIL") ?? "robe.ingenito@gmail.com";
        string fromName = Environment.GetEnvironmentVariable("SMTP_FROM_NAME") ?? "Cashly";

        int port = 587;
        if (!string.IsNullOrEmpty(portStr))
        {
            int.TryParse(portStr, out port);
        }

        string subject = "Ripristino Password - Cashly";
        string body = $@"
            <h3>Richiesta di ripristino password</h3>
            <p>Abbiamo ricevuto una richiesta di ripristino della password per il tuo account Cashly.</p>
            <p>Per reimpostare la tua password, clicca sul link seguente:</p>
            <p><a href='{resetLink}' style='padding: 10px 20px; background-color: #007bff; color: white; text-decoration: none; border-radius: 4px; display: inline-block;'>Reimposta Password</a></p>
            <p>Se il pulsante non funziona, copia e incolla il seguente link nel tuo browser:</p>
            <p>{resetLink}</p>
            <br>
            <p>Il link scadrà tra 1 ora. Se non hai richiesto tu il ripristino, puoi ignorare questa email.</p>
            <br>
            <p>Un saluto,<br>Il team di Cashly</p>";

        // Controlla se i parametri SMTP minimi sono configurati
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("==== CONFIGURAZIONE SMTP ASSENTE O INCOMPLETA ====");
            logger.LogWarning("Fallback: Visualizzazione dell'email nei log per lo sviluppo.");
            logger.LogWarning($"A: {toEmail}");
            logger.LogWarning($"Oggetto: {subject}");
            logger.LogWarning($"Link di ripristino: {resetLink}");
            logger.LogWarning("==================================================");
            return;
        }

        try
        {
            using var mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(fromEmail, fromName);
            mailMessage.To.Add(new MailAddress(toEmail));
            mailMessage.Subject = subject;
            mailMessage.Body = body;
            mailMessage.IsBodyHtml = true;

            using var smtpClient = new SmtpClient(host, port);
            smtpClient.Credentials = new NetworkCredential(username, password);
            smtpClient.EnableSsl = true;

            await smtpClient.SendMailAsync(mailMessage);
            logger.LogInformation($"Email di ripristino inviata con successo a {toEmail} via SMTP.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Errore durante l'invio dell'email di ripristino a {toEmail} via SMTP.");
            throw;
        }
    }
}
