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
        Task SendEmailWithMultipleAttachmentsAsync(string to, string subject, string body, List<(string fileName, byte[] content, string mimeType)> attachments);
        Task SendWelcomeCustomerWithPasswordAsync(string toEmail, string fullName, string tempPassword);
        Task<string> RenderInvoiceEmailTemplateAsync(string customerName, string invoiceId, string bookingId, string createdDate, string customerEmail, string serviceName, string servicePrice, string totalAmount, bool hasDiscount, string discountAmount);
        
        // Thêm các email templates mới
        Task SendBookingConfirmationEmailAsync(string toEmail, string customerName, string bookingCode, string bookingDate, string serviceName, string centerName);
        Task SendBookingReminderEmailAsync(string toEmail, string customerName, string bookingCode, string bookingDateTime, string centerName);
        Task SendBookingCancellationEmailAsync(string toEmail, string customerName, string bookingCode, string reason);
        Task SendBookingCompletedEmailAsync(string toEmail, string customerName, string bookingCode, string serviceName, string totalAmount);
        Task SendPaymentConfirmationEmailAsync(string toEmail, string customerName, string invoiceId, string amount, string paymentMethod);
        Task SendMaintenanceReminderEmailAsync(string toEmail, string customerName, string vehicleInfo, string recommendedDate);
        Task SendPromotionEmailAsync(string toEmail, string customerName, string promotionTitle, string promotionDescription, string discountAmount, string validUntil);
    }
}
