using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace VinhuniEvent.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;

        public EmailService(IOptions<EmailSettings> options)
        {
            _settings = options.Value;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                return;
            }

            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.EnableSsl,
                Credentials = new NetworkCredential(_settings.UserName, _settings.Password)
            };

            var fromAddress = new MailAddress(
                string.IsNullOrWhiteSpace(_settings.From) ? _settings.UserName : _settings.From,
                string.IsNullOrWhiteSpace(_settings.FromName) ? _settings.UserName : _settings.FromName);

            var message = new MailMessage
            {
                From = fromAddress,
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            message.To.Add(toEmail);

            await client.SendMailAsync(message);
        }
    }
}
