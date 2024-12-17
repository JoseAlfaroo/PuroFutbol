using Ecommerce_2024_1_NJD.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Ecommerce_2024_1_NJD.Models
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public EmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string email, string subject, string message, bool isHtml = false)
        {
            // leer config de JSON
            var smtpConfig = _configuration.GetSection("Smtp");

            string smtpHost = smtpConfig["Host"];
            int smtpPort = int.Parse(smtpConfig["Port"]);
            string smtpUser = smtpConfig["User"];
            string smtpPassword = smtpConfig["Password"];

            var client = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(smtpUser, smtpPassword)
            };

            var mailMessage = new MailMessage(from: smtpUser, to: email, subject, message);
            mailMessage.IsBodyHtml = isHtml;

            await client.SendMailAsync(mailMessage);
        }
    }
}