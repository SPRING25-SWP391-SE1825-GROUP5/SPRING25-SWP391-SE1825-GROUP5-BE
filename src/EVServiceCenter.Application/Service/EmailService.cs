using System.Net.Mail;
using System.Net;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace EVServiceCenter.Application.Service
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var host = _config["Email:Host"];
            var port = int.Parse(_config["Email:Port"]);
            var user = _config["Email:User"];
            var password = _config["Email:Password"];
            var from = _config["Email:From"];
            var fromName = _config["Email:FromName"];

            using var smtp = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(user, password),
                EnableSsl = true
            };

            var mail = new MailMessage
            {
                From = new MailAddress(from, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mail.To.Add(to);

            await smtp.SendMailAsync(mail);
        }
    }
}
