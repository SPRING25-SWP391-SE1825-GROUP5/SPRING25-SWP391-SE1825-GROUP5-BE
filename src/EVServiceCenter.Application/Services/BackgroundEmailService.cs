using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace EVServiceCenter.Application.Services
{
    public interface IBackgroundEmailService
    {
        Task SendBookingPaymentEmailAsync(int bookingId);
    }

    public class BackgroundEmailService : IBackgroundEmailService
    {
        private readonly IEmailService _emailService;
        private readonly IPdfInvoiceService _pdfInvoiceService;
        private readonly IBookingRepository _bookingRepository;
        private readonly IWorkOrderPartRepository _workOrderPartRepository;
        private readonly IPromotionRepository _promotionRepository;
        private readonly ILogger<BackgroundEmailService> _logger;

        public BackgroundEmailService(
            IEmailService emailService,
            IPdfInvoiceService pdfInvoiceService,
            IBookingRepository bookingRepository,
            IWorkOrderPartRepository workOrderPartRepository,
            IPromotionRepository promotionRepository,
            ILogger<BackgroundEmailService> logger)
        {
            _emailService = emailService;
            _pdfInvoiceService = pdfInvoiceService;
            _bookingRepository = bookingRepository;
            _workOrderPartRepository = workOrderPartRepository;
            _promotionRepository = promotionRepository;
            _logger = logger;
        }

        public async Task SendBookingPaymentEmailAsync(int bookingId)
        {
            try
            {
                _logger.LogInformation($"Bắt đầu gửi email thanh toán cho booking {bookingId}");

                var booking = await _bookingRepository.GetBookingByIdAsync(bookingId);
                if (booking?.Customer?.User?.Email == null)
                {
                    _logger.LogWarning($"Không tìm thấy email cho booking {bookingId}");
                    return;
                }

                var customerEmail = booking.Customer.User.Email;

                // Lấy thông tin phụ tùng phát sinh
                var workOrderParts = await _workOrderPartRepository.GetByBookingIdAsync(booking.BookingId);
                var parts = workOrderParts
                    .Where(p => p.Status == "CONSUMED" && !p.IsCustomerSupplied)
                    .Select(p => new EVServiceCenter.Application.Service.InvoicePartItem
                    {
                        Name = p.Part?.PartName ?? $"Phụ tùng #{p.PartId}",
                        Quantity = p.QuantityUsed,
                        Amount = p.QuantityUsed * (p.Part?.Price ?? 0)
                    }).ToList();

                // Lấy thông tin promotion đã áp dụng
                var userPromotions = await _promotionRepository.GetUserPromotionsByBookingAsync(booking.BookingId);
                var promotions = userPromotions?
                    .Where(up => string.Equals(up.Status, "APPLIED", StringComparison.OrdinalIgnoreCase))
                    .Select(up => new EVServiceCenter.Application.Service.InvoicePromotionItem
                    {
                        Code = up.Promotion?.Code ?? "N/A",
                        Description = up.Promotion?.Description ?? "Khuyến mãi",
                        DiscountAmount = up.DiscountAmount
                    }).ToList() ?? new List<EVServiceCenter.Application.Service.InvoicePromotionItem>();

                // Tính package discount
                decimal packageDiscountAmount = 0m;
                if (booking.AppliedCreditId.HasValue)
                {
                    // Logic tính package discount (copy từ PaymentService)
                    var serviceBasePrice = booking.Service?.BasePrice ?? 0m;
                    packageDiscountAmount = serviceBasePrice * 0.1m; // Simplified calculation
                }

                // Tính tổng tiền
                var servicePrice = booking.Service?.BasePrice ?? 0m;
                var partsTotal = parts.Sum(p => p.Amount);
                var promotionTotal = promotions.Sum(p => p.DiscountAmount);
                var totalAmount = servicePrice + partsTotal - packageDiscountAmount - promotionTotal;

                var subject = $"Hóa đơn thanh toán - Booking #{booking.BookingId}";
                var body = await _emailService.RenderInvoiceEmailTemplateAsync(
                    booking.Customer?.User?.FullName ?? "Khách hàng",
                    $"INV-{booking.BookingId:D6}",
                    booking.BookingId.ToString(),
                    DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm"),
                    customerEmail,
                    booking.Service?.ServiceName ?? "N/A",
                    servicePrice.ToString("N0"),
                    totalAmount.ToString("N0"),
                    booking.AppliedCreditId.HasValue,
                    packageDiscountAmount.ToString("N0")
                );

                // Tạo PDF
                var invoicePdfContent = await _pdfInvoiceService.GenerateInvoicePdfAsync(booking.BookingId);

                byte[]? maintenancePdfContent = null;
                try
                {
                    maintenancePdfContent = await _pdfInvoiceService.GenerateMaintenanceReportPdfAsync(booking.BookingId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Không thể tạo maintenance report PDF cho booking {bookingId}: {ex.Message}");
                }

                // Gửi email
                if (maintenancePdfContent != null)
                {
                    var attachments = new List<(string fileName, byte[] content, string mimeType)>
                    {
                        ($"Invoice_Booking_{booking.BookingId}.pdf", invoicePdfContent, "application/pdf"),
                        ($"MaintenanceReport_Booking_{booking.BookingId}.pdf", maintenancePdfContent, "application/pdf")
                    };

                    await _emailService.SendEmailWithMultipleAttachmentsAsync(customerEmail, subject, body, attachments);
                }
                else
                {
                    await _emailService.SendEmailWithAttachmentAsync(
                        customerEmail,
                        subject,
                        body,
                        $"Invoice_Booking_{booking.BookingId}.pdf",
                        invoicePdfContent,
                        "application/pdf");
                }

                _logger.LogInformation($"Đã gửi email thanh toán thành công cho booking {bookingId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi gửi email thanh toán cho booking {bookingId}");
                // Không throw exception để không làm crash background job
            }
        }
    }
}
