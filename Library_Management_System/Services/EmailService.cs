using System.Net;
using System.Net.Mail;
using Library_Management_System.Models;
using Microsoft.Extensions.Options;

namespace Library_Management_System.Services
{
    /// <summary>
    /// Sends transactional email via SMTP using the strongly-typed
    /// EmailSettings already registered in Program.cs via
    /// `builder.Services.Configure&lt;EmailSettings&gt;(...)`.
    /// Reading from IOptions avoids string-key typos and makes the
    /// service unit-testable by passing a plain settings instance.
    /// </summary>
    public class EmailService
    {
        private readonly EmailSettings _settings;

        public EmailService(IOptions<EmailSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            // If credentials aren't configured (e.g., local dev without user-secrets),
            if (string.IsNullOrWhiteSpace(_settings.SenderEmail) ||
                string.IsNullOrWhiteSpace(_settings.Password))
            {
                throw new InvalidOperationException(
                    "EmailSettings:SenderEmail / Password are not configured. " +
                    "Set them via `dotnet user-secrets` for dev or via environment " +
                    "variables / Azure Key Vault in production.");
            }

            using var smtp = new SmtpClient(_settings.SmtpServer)
            {
                Port = _settings.Port,
                Credentials = new NetworkCredential(
                    string.IsNullOrWhiteSpace(_settings.Username)
                        ? _settings.SenderEmail
                        : _settings.Username,
                    _settings.Password),
                EnableSsl = true
            };

            using var mail = new MailMessage
            {
                From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mail.To.Add(toEmail);

            await smtp.SendMailAsync(mail);
        }
    }
}
