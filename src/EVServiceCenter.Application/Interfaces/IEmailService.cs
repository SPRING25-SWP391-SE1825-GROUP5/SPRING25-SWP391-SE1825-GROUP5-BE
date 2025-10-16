using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVServiceCenter.Application.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
        Task SendVerificationEmailAsync(string toEmail, string fullName, string otpCode);
        Task SendWelcomeEmailAsync(string toEmail, string fullName);
        Task SendResetPasswordEmailAsync(string toEmail, string fullName, string otpCode);
        Task SendEmailWithAttachmentAsync(string to, string subject, string body, string attachmentName, byte[] attachmentContent, string contentType = "application/pdf");
        Task SendWelcomeCustomerWithPasswordAsync(string toEmail, string fullName, string tempPassword);
    }
}
