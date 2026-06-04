using System.Net;
using System.Net.Mail;
using Library_Management_System.Models;
using Microsoft.Extensions.Options;

namespace LibraryManagementSystem.Services
{
    /// <summary>
    /// Admin-app SMTP sender. Same implementation as the user-app copy —
    /// kept duplicated rather than moved to ClassLibrary because the
    /// EmailSettings model already lives in Library_Management_System.Models
    /// and adding a project reference there would be a bigger refactor.
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
            if (string.IsNullOrWhiteSpace(_settings.SenderEmail) ||
                string.IsNullOrWhiteSpace(_settings.Password))
            {
                throw new InvalidOperationException(
                    "EmailSettings:SenderEmail / Password are not configured. " +
                    "Set them in the admin app's appsettings.json or via " +
                    "environment variables in production.");
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
