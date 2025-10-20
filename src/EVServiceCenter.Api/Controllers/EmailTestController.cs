using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EVServiceCenter.Application.Interfaces;

namespace EVServiceCenter.Api.Controllers;

[ApiController]
[Route("api/email")]
public class EmailTestController : ControllerBase
{
    private readonly IEmailService _emailService;

    public EmailTestController(IEmailService emailService)
    {
        _emailService = emailService;
    }

    /// <summary>
    /// Test gửi email cơ bản
    /// </summary>
    [HttpPost("test")]
    [Authorize]
    public async Task<IActionResult> TestSendEmail([FromBody] TestEmailRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new { success = false, message = "Email không được để trống" });
            }

            var subject = "Test Email - EV Service Center";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px;'>
                        <div style='background: linear-gradient(135deg, #007bff, #0056b3); color: white; padding: 20px; text-align: center; border-radius: 10px 10px 0 0;'>
                            <h1 style='margin: 0; font-size: 24px;'>EV Service Center</h1>
                            <p style='margin: 5px 0 0 0; font-size: 16px;'>Test Email</p>
                        </div>
                        <div style='padding: 30px;'>
                            <h2 style='color: #007bff; margin-top: 0;'>Chào mừng bạn!</h2>
                            <p>Đây là email test từ hệ thống EV Service Center.</p>
                            <p><strong>Thông tin test:</strong></p>
                            <ul>
                                <li>Email nhận: {request.Email}</li>
                                <li>Thời gian gửi: {DateTime.UtcNow:dd/MM/yyyy HH:mm:ss} UTC</li>
                                <li>Trạng thái: Thành công</li>
                            </ul>
                            <div style='background: #e3f2fd; border: 1px solid #2196f3; border-radius: 8px; padding: 15px; margin: 20px 0; text-align: center;'>
                                <p style='margin: 0; color: #1976d2;'><strong>✅ Email test đã được gửi thành công!</strong></p>
                            </div>
                            <p>Cảm ơn bạn đã sử dụng dịch vụ của chúng tôi!</p>
                        </div>
                        <div style='background-color: #f8f9fa; padding: 15px; text-align: center; border-radius: 0 0 10px 10px; font-size: 12px; color: #666;'>
                            <p style='margin: 0;'>© {DateTime.UtcNow.Year} EV Service Center</p>
                            <p style='margin: 5px 0 0 0;'>Hỗ trợ: support@evservicecenter.com | 1900-EVSERVICE</p>
                        </div>
                    </div>
                </body>
                </html>";

            await _emailService.SendEmailAsync(request.Email, subject, body);

            return Ok(new
            {
                success = true,
                message = "Email test đã được gửi thành công",
                data = new
                {
                    email = request.Email,
                    subject = subject,
                    sentAt = DateTime.UtcNow,
                    status = "sent"
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "Gửi email test thất bại",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Test gửi email hóa đơn
    /// </summary>
    [HttpPost("test-invoice")]
    [Authorize]
    public async Task<IActionResult> TestSendInvoiceEmail([FromBody] TestInvoiceEmailRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new { success = false, message = "Email không được để trống" });
            }

            // Sử dụng template email hóa đơn có sẵn
            var body = await _emailService.RenderInvoiceEmailTemplateAsync(
                customerName: request.CustomerName ?? "Khách hàng Test",
                invoiceId: request.InvoiceId ?? "TEST-001",
                bookingId: request.BookingId ?? "BK-001",
                createdDate: DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm"),
                customerEmail: request.Email,
                serviceName: request.ServiceName ?? "Dịch vụ Test",
                servicePrice: request.ServicePrice ?? "500,000",
                totalAmount: request.TotalAmount ?? "450,000",
                hasDiscount: request.HasDiscount,
                discountAmount: request.DiscountAmount ?? "50,000"
            );

            var subject = $"Hóa đơn Test #{request.InvoiceId ?? "TEST-001"} - EV Service Center";

            await _emailService.SendEmailAsync(request.Email, subject, body);

            return Ok(new
            {
                success = true,
                message = "Email hóa đơn test đã được gửi thành công",
                data = new
                {
                    email = request.Email,
                    subject = subject,
                    invoiceId = request.InvoiceId ?? "TEST-001",
                    sentAt = DateTime.UtcNow,
                    status = "sent"
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "Gửi email hóa đơn test thất bại",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Test gửi email với file đính kèm
    /// </summary>
    [HttpPost("test-with-attachment")]
    [Authorize]
    public async Task<IActionResult> TestSendEmailWithAttachment([FromBody] TestEmailRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new { success = false, message = "Email không được để trống" });
            }

            var subject = "Test Email với File Đính Kèm - EV Service Center";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px;'>
                        <div style='background: linear-gradient(135deg, #28a745, #20c997); color: white; padding: 20px; text-align: center; border-radius: 10px 10px 0 0;'>
                            <h1 style='margin: 0; font-size: 24px;'>EV Service Center</h1>
                            <p style='margin: 5px 0 0 0; font-size: 16px;'>Test Email với File Đính Kèm</p>
                        </div>
                        <div style='padding: 30px;'>
                            <h2 style='color: #28a745; margin-top: 0;'>Email Test với File PDF</h2>
                            <p>Đây là email test với file PDF đính kèm từ hệ thống EV Service Center.</p>
                            <div style='background: #e8f5e8; border: 1px solid #28a745; border-radius: 8px; padding: 15px; margin: 20px 0;'>
                                <p style='margin: 0; color: #155724;'><strong>📄 File đính kèm:</strong> test-document.pdf</p>
                                <p style='margin: 5px 0 0 0; color: #155724;'><strong>📧 Email nhận:</strong> {request.Email}</p>
                                <p style='margin: 5px 0 0 0; color: #155724;'><strong>⏰ Thời gian:</strong> {DateTime.UtcNow:dd/MM/yyyy HH:mm:ss} UTC</p>
                            </div>
                            <p>Cảm ơn bạn đã sử dụng dịch vụ của chúng tôi!</p>
                        </div>
                        <div style='background-color: #f8f9fa; padding: 15px; text-align: center; border-radius: 0 0 10px 10px; font-size: 12px; color: #666;'>
                            <p style='margin: 0;'>© {DateTime.UtcNow.Year} EV Service Center</p>
                            <p style='margin: 5px 0 0 0;'>Hỗ trợ: support@evservicecenter.com | 1900-EVSERVICE</p>
                        </div>
                    </div>
                </body>
                </html>";

            // Tạo file PDF test đơn giản
            var pdfContent = System.Text.Encoding.UTF8.GetBytes("Test PDF Content - EV Service Center");

            await _emailService.SendEmailWithAttachmentAsync(
                request.Email, 
                subject, 
                body, 
                "test-document.pdf", 
                pdfContent, 
                "application/pdf"
            );

            return Ok(new
            {
                success = true,
                message = "Email test với file đính kèm đã được gửi thành công",
                data = new
                {
                    email = request.Email,
                    subject = subject,
                    attachmentName = "test-document.pdf",
                    sentAt = DateTime.UtcNow,
                    status = "sent"
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "Gửi email test với file đính kèm thất bại",
                error = ex.Message
            });
        }
    }
}

/// <summary>
/// Request model cho test email cơ bản
/// </summary>
public class TestEmailRequest
{
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Request model cho test email hóa đơn
/// </summary>
public class TestInvoiceEmailRequest
{
    public string Email { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? InvoiceId { get; set; }
    public string? BookingId { get; set; }
    public string? ServiceName { get; set; }
    public string? ServicePrice { get; set; }
    public string? TotalAmount { get; set; }
    public bool HasDiscount { get; set; } = false;
    public string? DiscountAmount { get; set; }
}
