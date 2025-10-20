using System;
using System.IO;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.IO.Font.Constants;

namespace EVServiceCenter.Application.Service
{
    public class PdfInvoiceService : IPdfInvoiceService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly ILogger<PdfInvoiceService> _logger;

        public PdfInvoiceService(IBookingRepository bookingRepository, ILogger<PdfInvoiceService> logger)
        {
            _bookingRepository = bookingRepository;
            _logger = logger;
        }

        public async Task<byte[]> GenerateInvoicePdfAsync(int bookingId)
        {
            try
            {
                var booking = await _bookingRepository.GetBookingByIdAsync(bookingId);
                if (booking == null)
                    throw new ArgumentException($"Booking {bookingId} không tồn tại");

                using var memoryStream = new MemoryStream();
                using var writer = new PdfWriter(memoryStream);
                using var pdf = new PdfDocument(writer);
                using var document = new Document(pdf);

                // Tạo font
                var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

                // Header
                var header = new Paragraph("HÓA ĐƠN DỊCH VỤ")
                    .SetFont(boldFont)
                    .SetFontSize(20)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(20);
                document.Add(header);

                // Thông tin công ty
                var companyInfo = new Paragraph()
                    .SetFont(font)
                    .SetFontSize(12)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .Add("EV SERVICE CENTER\n")
                    .Add("Địa chỉ: 123 Đường ABC, Quận XYZ, TP.HCM\n")
                    .Add("Điện thoại: 1900-EVSERVICE\n")
                    .Add("Email: support@evservice.com")
                    .SetMarginBottom(30);
                document.Add(companyInfo);

                // Thông tin hóa đơn
                var invoiceInfo = new Table(2)
                    .SetWidth(UnitValue.CreatePercentValue(100))
                    .SetMarginBottom(20);

                invoiceInfo.AddCell(new Cell().Add(new Paragraph("Mã hóa đơn:").SetFont(boldFont)).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
                invoiceInfo.AddCell(new Cell().Add(new Paragraph($"INV-{bookingId:D6}").SetFont(font)).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
                
                invoiceInfo.AddCell(new Cell().Add(new Paragraph("Ngày tạo:").SetFont(boldFont)).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
                invoiceInfo.AddCell(new Cell().Add(new Paragraph(DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm")).SetFont(font)).SetBorder(iText.Layout.Borders.Border.NO_BORDER));

                document.Add(invoiceInfo);

                // Thông tin khách hàng
                var customerInfo = new Paragraph("THÔNG TIN KHÁCH HÀNG")
                    .SetFont(boldFont)
                    .SetFontSize(14)
                    .SetMarginTop(20)
                    .SetMarginBottom(10);
                document.Add(customerInfo);

                var customerTable = new Table(2)
                    .SetWidth(UnitValue.CreatePercentValue(100))
                    .SetMarginBottom(20);

                customerTable.AddCell(new Cell().Add(new Paragraph("Tên khách hàng:").SetFont(boldFont)).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
                customerTable.AddCell(new Cell().Add(new Paragraph(booking.Customer?.User?.FullName ?? "N/A").SetFont(font)).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
                
                customerTable.AddCell(new Cell().Add(new Paragraph("Email:").SetFont(boldFont)).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
                customerTable.AddCell(new Cell().Add(new Paragraph(booking.Customer?.User?.Email ?? "N/A").SetFont(font)).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
                
                customerTable.AddCell(new Cell().Add(new Paragraph("Số điện thoại:").SetFont(boldFont)).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
                customerTable.AddCell(new Cell().Add(new Paragraph(booking.Customer?.User?.PhoneNumber ?? "N/A").SetFont(font)).SetBorder(iText.Layout.Borders.Border.NO_BORDER));

                document.Add(customerTable);

                // Thông tin dịch vụ
                var serviceInfo = new Paragraph("CHI TIẾT DỊCH VỤ")
                    .SetFont(boldFont)
                    .SetFontSize(14)
                    .SetMarginTop(20)
                    .SetMarginBottom(10);
                document.Add(serviceInfo);

                var serviceTable = new Table(4)
                    .SetWidth(UnitValue.CreatePercentValue(100))
                    .SetMarginBottom(20);

                // Header của bảng dịch vụ
                serviceTable.AddCell(new Cell().Add(new Paragraph("Dịch vụ").SetFont(boldFont)).SetTextAlignment(TextAlignment.CENTER).SetBackgroundColor(ColorConstants.LIGHT_GRAY));
                serviceTable.AddCell(new Cell().Add(new Paragraph("Số lượng").SetFont(boldFont)).SetTextAlignment(TextAlignment.CENTER).SetBackgroundColor(ColorConstants.LIGHT_GRAY));
                serviceTable.AddCell(new Cell().Add(new Paragraph("Đơn giá").SetFont(boldFont)).SetTextAlignment(TextAlignment.CENTER).SetBackgroundColor(ColorConstants.LIGHT_GRAY));
                serviceTable.AddCell(new Cell().Add(new Paragraph("Thành tiền").SetFont(boldFont)).SetTextAlignment(TextAlignment.CENTER).SetBackgroundColor(ColorConstants.LIGHT_GRAY));

                // Tính toán giá
                decimal servicePrice = booking.Service?.BasePrice ?? 0m;
                decimal discountAmount = 0m;
                decimal finalAmount = servicePrice;

                // Nếu có package discount
                if (booking.AppliedCreditId.HasValue)
                {
                    // Logic tính discount sẽ được implement sau
                    // Tạm thời sử dụng 10% discount
                    discountAmount = servicePrice * 0.1m;
                    finalAmount = servicePrice - discountAmount;
                }

                // Dòng dịch vụ
                serviceTable.AddCell(new Cell().Add(new Paragraph(booking.Service?.ServiceName ?? "N/A").SetFont(font)));
                serviceTable.AddCell(new Cell().Add(new Paragraph("1").SetFont(font)).SetTextAlignment(TextAlignment.CENTER));
                serviceTable.AddCell(new Cell().Add(new Paragraph($"{servicePrice:N0} VNĐ").SetFont(font)).SetTextAlignment(TextAlignment.RIGHT));
                serviceTable.AddCell(new Cell().Add(new Paragraph($"{finalAmount:N0} VNĐ").SetFont(font)).SetTextAlignment(TextAlignment.RIGHT));

                document.Add(serviceTable);

                // Tổng kết
                var summaryTable = new Table(2)
                    .SetWidth(UnitValue.CreatePercentValue(50))
                    .SetHorizontalAlignment(HorizontalAlignment.RIGHT)
                    .SetMarginTop(20);

                summaryTable.AddCell(new Cell().Add(new Paragraph("Tạm tính:").SetFont(font)).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
                summaryTable.AddCell(new Cell().Add(new Paragraph($"{servicePrice:N0} VNĐ").SetFont(font)).SetTextAlignment(TextAlignment.RIGHT).SetBorder(iText.Layout.Borders.Border.NO_BORDER));

                if (discountAmount > 0)
                {
                    summaryTable.AddCell(new Cell().Add(new Paragraph("Giảm giá:").SetFont(font)).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
                    summaryTable.AddCell(new Cell().Add(new Paragraph($"-{discountAmount:N0} VNĐ").SetFont(font)).SetTextAlignment(TextAlignment.RIGHT).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
                }

                summaryTable.AddCell(new Cell().Add(new Paragraph("TỔNG CỘNG:").SetFont(boldFont)).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
                summaryTable.AddCell(new Cell().Add(new Paragraph($"{finalAmount:N0} VNĐ").SetFont(boldFont)).SetTextAlignment(TextAlignment.RIGHT).SetBorder(iText.Layout.Borders.Border.NO_BORDER));

                document.Add(summaryTable);

                // Footer
                var footer = new Paragraph()
                    .SetFont(font)
                    .SetFontSize(10)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginTop(50)
                    .Add("Cảm ơn bạn đã sử dụng dịch vụ của chúng tôi!\n")
                    .Add("Hóa đơn này được tạo tự động bởi hệ thống EV Service Center");
                document.Add(footer);

                document.Close();

                var pdfBytes = memoryStream.ToArray();
                _logger.LogInformation("Generated PDF invoice for booking {BookingId}, size: {Size} bytes", bookingId, pdfBytes.Length);
                
                return pdfBytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate PDF invoice for booking {BookingId}", bookingId);
                throw new Exception($"Không thể tạo hóa đơn PDF: {ex.Message}", ex);
            }
        }
    }
}
