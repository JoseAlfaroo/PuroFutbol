using Ecommerce_2024_1_NJD.Interfaces;
using System.Net;
using System.Net.Mail;

namespace Ecommerce_2024_1_NJD.Models
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string message, bool isHtml = false)
        {
            // Outlook (Establecer correo o correos aquí)
            string[] mails = { "purofutboloficial@outlook.com", "purofutboloficial1@outlook.com", "purofutboloficial2@outlook.com", "purofutboloficial3@outlook.com", "purofutboloficial4@outlook.com", "purofutboloficial5@outlook.com" };

            var rnd = new Random();

            // Agarra un mail random
            var mail = mails[rnd.Next(mails.Length)];

            //Establecer contraseña (En este caso es lo mismo para todos los correos)
            var pw = "E102Gamma";

            var client = new SmtpClient("smtp-mail.outlook.com", 587)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(mail, pw)
            };

            var mailMessage = new MailMessage(from: mail, to: email, subject, message);
            mailMessage.IsBodyHtml = isHtml;

            return client.SendMailAsync(mailMessage);

        }
    }
}
