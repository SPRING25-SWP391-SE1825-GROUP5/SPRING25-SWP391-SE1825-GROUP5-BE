using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EVServiceCenter.Application.Interfaces;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using System.IO;

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
    /// Test gửi email phiếu kết quả bảo dưỡng với PDF attachment
    /// </summary>
    [HttpPost("test-maintenance-report-pdf")]
    [Authorize]
    public async Task<IActionResult> TestSendMaintenanceReportPdfEmail([FromBody] TestMaintenanceReportRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new { success = false, message = "Email không được để trống" });
            }

            var subject = $"Phiếu kết quả bảo dưỡng #{request.BookingId ?? "TEST-001"} - EV Service Center";
            
            // Tạo PDF phiếu kiểm tra bảo dưỡng
            var pdfBytes = await GenerateMaintenanceChecklistPdfAsync(request);
            
            // Tạo email body đơn giản
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px;'>
                        <div style='background: linear-gradient(135deg, #17a2b8, #138496); color: white; padding: 20px; text-align: center; border-radius: 10px 10px 0 0;'>
                            <h1 style='margin: 0; font-size: 24px;'>EV Service Center</h1>
                            <p style='margin: 5px 0 0 0; font-size: 16px;'>PHIẾU KẾT QUẢ BẢO DƯỠNG</p>
                        </div>
                        <div style='padding: 30px;'>
                            <h2 style='color: #17a2b8; margin-top: 0;'>Phiếu kiểm tra bảo dưỡng đã được tạo</h2>
                            <p>Kính gửi <strong>{request.CustomerName ?? "Khách hàng"}</strong>,</p>
                            <p>Phiếu kiểm tra bảo dưỡng cho xe <strong>{request.LicensePlate ?? "29A-12345"}</strong> đã được hoàn thành.</p>
                            <div style='background: #e8f5e8; border: 1px solid #28a745; border-radius: 8px; padding: 15px; margin: 20px 0; text-align: center;'>
                                <p style='margin: 0; color: #155724;'><strong>📄 Phiếu kiểm tra chi tiết được đính kèm trong file PDF</strong></p>
                            </div>
                            <p>Cảm ơn bạn đã tin tưởng dịch vụ của chúng tôi!</p>
                        </div>
                        <div style='background-color: #f8f9fa; padding: 15px; text-align: center; border-radius: 0 0 10px 10px; font-size: 12px; color: #666;'>
                            <p style='margin: 0;'>© {DateTime.UtcNow.Year} EV Service Center</p>
                            <p style='margin: 5px 0 0 0;'>Hỗ trợ: support@evservicecenter.com | 1900-EVSERVICE</p>
                        </div>
                    </div>
                </body>
                </html>";

            await _emailService.SendEmailWithAttachmentAsync(
                request.Email, 
                subject, 
                body, 
                $"PhieuKiemTraBaoDuong_{request.BookingId ?? "TEST-001"}.pdf", 
                pdfBytes, 
                "application/pdf"
            );

            return Ok(new
            {
                success = true,
                message = "Email phiếu kết quả bảo dưỡng với PDF đã được gửi thành công",
                data = new
                {
                    email = request.Email,
                    subject = subject,
                    pdfFileName = $"PhieuKiemTraBaoDuong_{request.BookingId ?? "TEST-001"}.pdf",
                    pdfSize = pdfBytes.Length,
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
                message = "Gửi email phiếu kết quả bảo dưỡng với PDF thất bại",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Test gửi email phiếu kết quả bảo dưỡng
    /// </summary>
    [HttpPost("test-maintenance-report")]
    [Authorize]
    public async Task<IActionResult> TestSendMaintenanceReportEmail([FromBody] TestMaintenanceReportRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new { success = false, message = "Email không được để trống" });
            }

            var subject = $"Phiếu kết quả bảo dưỡng #{request.BookingId ?? "TEST-001"} - EV Service Center";
            
            // Tạo template email cho phiếu kết quả bảo dưỡng
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <div style='max-width: 800px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px;'>
                        <div style='background: linear-gradient(135deg, #17a2b8, #138496); color: white; padding: 20px; text-align: center; border-radius: 10px 10px 0 0;'>
                            <h1 style='margin: 0; font-size: 24px;'>EV Service Center</h1>
                            <p style='margin: 5px 0 0 0; font-size: 16px;'>PHIẾU KẾT QUẢ BẢO DƯỠNG</p>
                        </div>
                        <div style='padding: 30px;'>
                            <div style='background: #e3f2fd; border: 1px solid #2196f3; border-radius: 8px; padding: 20px; margin-bottom: 20px;'>
                                <h2 style='color: #1976d2; margin-top: 0; text-align: center;'>📋 THÔNG TIN BOOKING</h2>
                                <div style='display: grid; grid-template-columns: 1fr 1fr; gap: 15px;'>
                                    <div>
                                        <p><strong>Mã Booking:</strong> {request.BookingId ?? "TEST-001"}</p>
                                        <p><strong>Khách hàng:</strong> {request.CustomerName ?? "Nguyễn Văn A"}</p>
                                        <p><strong>Email:</strong> {request.Email}</p>
                                        <p><strong>Điện thoại:</strong> {request.PhoneNumber ?? "0123456789"}</p>
                                    </div>
                                    <div>
                                        <p><strong>Ngày thực hiện:</strong> {request.ServiceDate ?? DateTime.UtcNow.ToString("dd/MM/yyyy")}</p>
                                        <p><strong>Trung tâm:</strong> {request.CenterName ?? "EV Lê Văn Việt"}</p>
                                        <p><strong>Kỹ thuật viên:</strong> {request.TechnicianName ?? "Trần Văn B"}</p>
                                        <p><strong>Trạng thái:</strong> <span style='color: #28a745; font-weight: bold;'>HOÀN THÀNH</span></p>
                                    </div>
                                </div>
                            </div>

                            <div style='background: #f8f9fa; border: 1px solid #dee2e6; border-radius: 8px; padding: 20px; margin-bottom: 20px;'>
                                <h2 style='color: #495057; margin-top: 0; text-align: center;'>🚗 THÔNG TIN XE</h2>
                                <div style='display: grid; grid-template-columns: 1fr 1fr; gap: 15px;'>
                                    <div>
                                        <p><strong>Biển số:</strong> {request.LicensePlate ?? "29A-12345"}</p>
                                        <p><strong>Model:</strong> {request.VehicleModel ?? "VinFast VF8"}</p>
                                        <p><strong>VIN:</strong> {request.Vin ?? "VF8XXXXXXXXXXXXX"}</p>
                                    </div>
                                    <div>
                                        <p><strong>Dịch vụ:</strong> {request.ServiceName ?? "Bảo dưỡng định kỳ"}</p>
                                        <p><strong>Số km hiện tại:</strong> {request.CurrentMileage ?? "15,000"} km</p>
                                        <p><strong>Ghi chú:</strong> {request.Notes ?? "Không có"}</p>
                                    </div>
                                </div>
                            </div>

                            <div style='background: #fff3cd; border: 1px solid #ffeaa7; border-radius: 8px; padding: 20px; margin-bottom: 20px;'>
                                <h2 style='color: #856404; margin-top: 0; text-align: center;'>🔧 KẾT QUẢ KIỂM TRA CHI TIẾT</h2>
                                <div style='overflow-x: auto;'>
                                    <table style='width: 100%; border-collapse: collapse; border: 1px solid #ddd;'>
                                        <thead>
                                            <tr style='background-color: #f8f9fa;'>
                                                <th style='border: 1px solid #ddd; padding: 12px; text-align: left;'>STT</th>
                                                <th style='border: 1px solid #ddd; padding: 12px; text-align: left;'>Tên Phụ Tùng</th>
                                                <th style='border: 1px solid #ddd; padding: 12px; text-align: left;'>Mã Phụ Tùng</th>
                                                <th style='border: 1px solid #ddd; padding: 12px; text-align: center;'>Kết Quả</th>
                                                <th style='border: 1px solid #ddd; padding: 12px; text-align: left;'>Ghi Chú</th>
                                            </tr>
                                        </thead>
                                        <tbody>
                                            <tr>
                                                <td style='border: 1px solid #ddd; padding: 12px;'>1</td>
                                                <td style='border: 1px solid #ddd; padding: 12px;'>Lọc gió động cơ</td>
                                                <td style='border: 1px solid #ddd; padding: 12px;'>PT001</td>
                                                <td style='border: 1px solid #ddd; padding: 12px; text-align: center;'><span style='background: #d4edda; color: #155724; padding: 4px 8px; border-radius: 4px; font-weight: bold;'>PASS</span></td>
                                                <td style='border: 1px solid #ddd; padding: 12px;'>Tốt</td>
                                            </tr>
                                            <tr>
                                                <td style='border: 1px solid #ddd; padding: 12px;'>2</td>
                                                <td style='border: 1px solid #ddd; padding: 12px;'>Dầu động cơ</td>
                                                <td style='border: 1px solid #ddd; padding: 12px;'>PT002</td>
                                                <td style='border: 1px solid #ddd; padding: 12px; text-align: center;'><span style='background: #d4edda; color: #155724; padding: 4px 8px; border-radius: 4px; font-weight: bold;'>PASS</span></td>
                                                <td style='border: 1px solid #ddd; padding: 12px;'>Cần thay mới</td>
                                            </tr>
                                            <tr>
                                                <td style='border: 1px solid #ddd; padding: 12px;'>3</td>
                                                <td style='border: 1px solid #ddd; padding: 12px;'>Phanh trước</td>
                                                <td style='border: 1px solid #ddd; padding: 12px;'>PT003</td>
                                                <td style='border: 1px solid #ddd; padding: 12px; text-align: center;'><span style='background: #f8d7da; color: #721c24; padding: 4px 8px; border-radius: 4px; font-weight: bold;'>FAIL</span></td>
                                                <td style='border: 1px solid #ddd; padding: 12px;'>Cần thay thế</td>
                                            </tr>
                                            <tr>
                                                <td style='border: 1px solid #ddd; padding: 12px;'>4</td>
                                                <td style='border: 1px solid #ddd; padding: 12px;'>Pin xe điện</td>
                                                <td style='border: 1px solid #ddd; padding: 12px;'>PT004</td>
                                                <td style='border: 1px solid #ddd; padding: 12px; text-align: center;'><span style='background: #d4edda; color: #155724; padding: 4px 8px; border-radius: 4px; font-weight: bold;'>PASS</span></td>
                                                <td style='border: 1px solid #ddd; padding: 12px;'>Tốt</td>
                                            </tr>
                                        </tbody>
                                    </table>
                                </div>
                            </div>

                            <div style='background: #d1ecf1; border: 1px solid #bee5eb; border-radius: 8px; padding: 20px; margin-bottom: 20px;'>
                                <h2 style='color: #0c5460; margin-top: 0; text-align: center;'>📊 TỔNG KẾT</h2>
                                <div style='display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 15px; text-align: center;'>
                                    <div style='background: #d4edda; padding: 15px; border-radius: 8px;'>
                                        <h3 style='margin: 0; color: #155724;'>✅ PASS</h3>
                                        <p style='margin: 5px 0 0 0; font-size: 24px; font-weight: bold; color: #155724;'>3</p>
                                    </div>
                                    <div style='background: #f8d7da; padding: 15px; border-radius: 8px;'>
                                        <h3 style='margin: 0; color: #721c24;'>❌ FAIL</h3>
                                        <p style='margin: 5px 0 0 0; font-size: 24px; font-weight: bold; color: #721c24;'>1</p>
                                    </div>
                                    <div style='background: #e2e3e5; padding: 15px; border-radius: 8px;'>
                                        <h3 style='margin: 0; color: #383d41;'>📋 TỔNG</h3>
                                        <p style='margin: 5px 0 0 0; font-size: 24px; font-weight: bold; color: #383d41;'>4</p>
                                    </div>
                                </div>
                            </div>

                            <div style='background: #e8f5e8; border: 1px solid #28a745; border-radius: 8px; padding: 20px; text-align: center;'>
                                <h3 style='margin: 0; color: #155724;'>🎉 BẢO DƯỠNG HOÀN THÀNH</h3>
                                <p style='margin: 10px 0 0 0; color: #155724; font-size: 16px;'>
                                    Xe của bạn đã được bảo dưỡng thành công. Cảm ơn bạn đã tin tưởng dịch vụ của chúng tôi!
                                </p>
                            </div>

                            <div style='margin-top: 20px; padding: 15px; background-color: #f8f9fa; border-radius: 8px; text-align: center;'>
                                <p style='margin: 0; color: #6c757d; font-size: 14px;'>
                                    <strong>📞 Hỗ trợ:</strong> 1900-EVSERVICE | <strong>📧 Email:</strong> support@evservicecenter.com
                                </p>
                                <p style='margin: 5px 0 0 0; color: #6c757d; font-size: 12px;'>
                                    © {DateTime.UtcNow.Year} EV Service Center - Phiếu kết quả được tạo tự động
                                </p>
                            </div>
                        </div>
                    </div>
                </body>
                </html>";

            await _emailService.SendEmailAsync(request.Email, subject, body);

            return Ok(new
            {
                success = true,
                message = "Email phiếu kết quả bảo dưỡng test đã được gửi thành công",
                data = new
                {
                    email = request.Email,
                    subject = subject,
                    bookingId = request.BookingId ?? "TEST-001",
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
                message = "Gửi email phiếu kết quả bảo dưỡng test thất bại",
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

    /// <summary>
    /// Tạo PDF phiếu kiểm tra bảo dưỡng với cấu trúc giống form thực tế
    /// </summary>
    private async Task<byte[]> GenerateMaintenanceChecklistPdfAsync(TestMaintenanceReportRequest request)
    {
        await Task.Yield();
        using var memoryStream = new MemoryStream();
        using var writer = new PdfWriter(memoryStream);
        using var pdf = new PdfDocument(writer);
        using var document = new Document(pdf);

        // Tạo font cho tiếng Việt
        var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
        var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

        // Header
        var headerTable = new Table(3).UseAllAvailableWidth();
        headerTable.SetMarginBottom(20);

        // Biển số
        var licensePlateCell = new Cell().Add(new Paragraph("Biển số: " + (request.LicensePlate ?? "29A-12345"))
            .SetFont(font).SetFontSize(12));
        licensePlateCell.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
        headerTable.AddCell(licensePlateCell);

        // Tiêu đề chính
        var titleCell = new Cell().Add(new Paragraph("PHIẾU KIỂM TRA BẢO DƯỠNG ĐỊNH KỲ")
            .SetFont(boldFont).SetFontSize(16).SetTextAlignment(TextAlignment.CENTER));
        titleCell.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
        headerTable.AddCell(titleCell);

        // Loại xe
        var vehicleTypeCell = new Cell().Add(new Paragraph("Loại xe: " + (request.VehicleModel ?? "VinFast VF8"))
            .SetFont(font).SetFontSize(12));
        vehicleTypeCell.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
        headerTable.AddCell(vehicleTypeCell);

        document.Add(headerTable);

        // Thông tin bổ sung
        var infoTable = new Table(2).UseAllAvailableWidth();
        infoTable.SetMarginBottom(20);

        var dateCell = new Cell().Add(new Paragraph("Ngày kiểm tra: " + (request.ServiceDate ?? DateTime.UtcNow.ToString("dd/MM/yyyy")))
            .SetFont(font).SetFontSize(12));
        dateCell.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
        infoTable.AddCell(dateCell);

        var noteCell = new Cell().Add(new Paragraph("Ghi chú: khoanh tròn các hạng mục đã thực hiện")
            .SetFont(font).SetFontSize(12));
        noteCell.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
        infoTable.AddCell(noteCell);

        document.Add(infoTable);

        // Bảng kiểm tra chính
        var mainTable = new Table(6).UseAllAvailableWidth();
        mainTable.SetMarginBottom(20);

        // Header của bảng
        var headers = new[] { "STT", "Hình minh họa", "Nội dung kiểm tra Bảo dưỡng", "Cấp bảo dưỡng", "Kết quả kiểm tra" };
        foreach (var header in headers)
        {
            var headerCell = new Cell().Add(new Paragraph(header).SetFont(boldFont).SetFontSize(10))
                .SetTextAlignment(TextAlignment.CENTER)
                .SetBackgroundColor(iText.Kernel.Colors.ColorConstants.LIGHT_GRAY);
            mainTable.AddCell(headerCell);
        }

        // Dữ liệu mẫu cho bảng kiểm tra
        var checklistItems = new[]
        {
            new { STT = "1", Name = "Hệ thống phanh tay", Result = "PASS", Note = "Tốt" },
            new { STT = "2", Name = "Đèn còi Mặt đồng hồ", Result = "PASS", Note = "Hoạt động bình thường" },
            new { STT = "3", Name = "Vỏ bọc và tay ga", Result = "FAIL", Note = "Cần thay thế" },
            new { STT = "4", Name = "Chân chống cạnh/chân chống đứng", Result = "PASS", Note = "Tốt" },
            new { STT = "5", Name = "Cơ cấu mở khóa cốp", Result = "PASS", Note = "Hoạt động bình thường" },
            new { STT = "6", Name = "Nắp dầu phanh", Result = "PASS", Note = "Tốt" },
            new { STT = "7", Name = "Cổ phốt", Result = "PASS", Note = "Tốt" },
            new { STT = "8", Name = "Giảm xóc trước", Result = "FAIL", Note = "Cần kiểm tra" },
            new { STT = "9", Name = "Phanh sau", Result = "PASS", Note = "Tốt" },
            new { STT = "10", Name = "Ống dầu phanh sau", Result = "PASS", Note = "Tốt" },
            new { STT = "11", Name = "Vành xe sau", Result = "PASS", Note = "Tốt" },
            new { STT = "12", Name = "Lốp xe sau", Result = "PASS", Note = "Tốt" }
        };

        foreach (var item in checklistItems)
        {
            // STT
            mainTable.AddCell(new Cell().Add(new Paragraph(item.STT).SetFont(font).SetFontSize(10))
                .SetTextAlignment(TextAlignment.CENTER));

            // Hình minh họa (placeholder)
            mainTable.AddCell(new Cell().Add(new Paragraph("📷").SetFont(font).SetFontSize(16))
                .SetTextAlignment(TextAlignment.CENTER));

            // Nội dung kiểm tra
            mainTable.AddCell(new Cell().Add(new Paragraph(item.Name).SetFont(font).SetFontSize(10)));

            // Cấp bảo dưỡng (sub-table)
            var maintenanceLevelCell = new Cell();
            var subTable = new Table(5).UseAllAvailableWidth();
            
            var levels = new[] { "1K", "3K", "Nhỏ", "TB", "Lớn" };
            foreach (var level in levels)
            {
                subTable.AddCell(new Cell().Add(new Paragraph(level).SetFont(font).SetFontSize(8))
                    .SetTextAlignment(TextAlignment.CENTER));
            }
            
            var actions = new[] { "K", "K", "K/T", "K", "K" };
            foreach (var action in actions)
            {
                subTable.AddCell(new Cell().Add(new Paragraph(action).SetFont(font).SetFontSize(8))
                    .SetTextAlignment(TextAlignment.CENTER));
            }
            
            maintenanceLevelCell.Add(subTable);
            mainTable.AddCell(maintenanceLevelCell);

            // Kết quả kiểm tra
            var resultColor = item.Result == "PASS" ? iText.Kernel.Colors.ColorConstants.GREEN : iText.Kernel.Colors.ColorConstants.RED;
            var resultCell = new Cell().Add(new Paragraph($"{item.Result}\n{item.Note}")
                .SetFont(font).SetFontSize(9))
                .SetTextAlignment(TextAlignment.CENTER)
                .SetBackgroundColor(resultColor);
            mainTable.AddCell(resultCell);
        }

        document.Add(mainTable);

        // Chú thích
        var legendTable = new Table(1).UseAllAvailableWidth();
        legendTable.SetMarginBottom(20);

        var legendText = "Chú thích: KT - kiểm tra, BT - bôi trơn, TT - thay thế, 1K - Bảo dưỡng 1.000 km/1 tháng, TB - Trung bình, ĐC - Điều chỉnh";
        var legendCell = new Cell().Add(new Paragraph(legendText).SetFont(font).SetFontSize(10))
            .SetTextAlignment(TextAlignment.CENTER)
            .SetBackgroundColor(iText.Kernel.Colors.ColorConstants.LIGHT_GRAY);
        legendTable.AddCell(legendCell);

        document.Add(legendTable);

        // Những vấn đề cần lưu ý khác
        var notesTable = new Table(1).UseAllAvailableWidth();
        notesTable.SetMarginBottom(20);

        var notesCell = new Cell().Add(new Paragraph("Những vấn đề cần lưu ý khác:\n\n\n\n")
            .SetFont(font).SetFontSize(12))
            .SetMinHeight(100);
        notesTable.AddCell(notesCell);

        document.Add(notesTable);

        // Chữ ký
        var signatureTable = new Table(3).UseAllAvailableWidth();
        signatureTable.SetMarginBottom(20);

        var signatures = new[] { "Kỹ thuật viên", "Quản đốc", "Trưởng phòng dịch vụ" };
        foreach (var signature in signatures)
        {
            var sigCell = new Cell().Add(new Paragraph($"{signature}\n\n\n\n")
                .SetFont(font).SetFontSize(12))
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMinHeight(80);
            signatureTable.AddCell(sigCell);
        }

        document.Add(signatureTable);

        // Footer với lịch bảo dưỡng
        var footerTable = new Table(1).UseAllAvailableWidth();

        var footerText = "Cấp bảo dưỡng Nhỏ: 6 tháng, 18 tháng, 30 tháng ... hoặc 5.000 km, 15.000 km, 25.000 km ...\n" +
                        "Cấp bảo dưỡng TB: 12 tháng, 36 tháng, 50 tháng ... hoặc 10.000 km, 30.000 km, 50.000 km ...\n" +
                        "Cấp bảo dưỡng Lớn: 24 tháng, 48 tháng, 72 tháng... hoặc 20.000 km, 40.000 km, 60.000 km ...";

        var footerCell = new Cell().Add(new Paragraph(footerText).SetFont(font).SetFontSize(9))
            .SetTextAlignment(TextAlignment.CENTER)
            .SetBackgroundColor(iText.Kernel.Colors.ColorConstants.LIGHT_GRAY);
        footerTable.AddCell(footerCell);

        document.Add(footerTable);

        document.Close();

        return memoryStream.ToArray();
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

/// <summary>
/// Request model cho test email phiếu kết quả bảo dưỡng
/// </summary>
public class TestMaintenanceReportRequest
{
    public string Email { get; set; } = string.Empty;
    public string? BookingId { get; set; }
    public string? CustomerName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? ServiceDate { get; set; }
    public string? CenterName { get; set; }
    public string? TechnicianName { get; set; }
    public string? LicensePlate { get; set; }
    public string? VehicleModel { get; set; }
    public string? Vin { get; set; }
    public string? ServiceName { get; set; }
    public string? CurrentMileage { get; set; }
    public string? Notes { get; set; }
}
