using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
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
using iText.IO.Font;

namespace EVServiceCenter.Application.Service
{
    public class PdfInvoiceService : IPdfInvoiceService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IWorkOrderPartRepository _workOrderPartRepository;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IMaintenanceChecklistRepository _maintenanceChecklistRepository;
        private readonly IMaintenanceChecklistResultRepository _maintenanceChecklistResultRepository;
        private readonly ILogger<PdfInvoiceService> _logger;

        public PdfInvoiceService(
            IBookingRepository bookingRepository, 
            IWorkOrderPartRepository workOrderPartRepository,
            IInvoiceRepository invoiceRepository,
            IMaintenanceChecklistRepository maintenanceChecklistRepository,
            IMaintenanceChecklistResultRepository maintenanceChecklistResultRepository,
            ILogger<PdfInvoiceService> logger)
        {
            _bookingRepository = bookingRepository;
            _workOrderPartRepository = workOrderPartRepository;
            _invoiceRepository = invoiceRepository;
            _maintenanceChecklistRepository = maintenanceChecklistRepository;
            _maintenanceChecklistResultRepository = maintenanceChecklistResultRepository;
            _logger = logger;
        }

        public async Task<byte[]> GenerateInvoicePdfAsync(int bookingId)
        {
            try
            {
                var booking = await _bookingRepository.GetBookingByIdAsync(bookingId);
                if (booking == null)
                    throw new ArgumentException($"Booking {bookingId} không tồn tại");

                var invoice = await _invoiceRepository.GetByBookingIdAsync(bookingId);
                var workOrderParts = await _workOrderPartRepository.GetByBookingIdAsync(bookingId);
                
                // Lấy dữ liệu kết quả kiểm tra bảo dưỡng
                var maintenanceChecklist = await _maintenanceChecklistRepository.GetByBookingIdAsync(bookingId);
                var maintenanceResults = maintenanceChecklist != null 
                    ? await _maintenanceChecklistResultRepository.GetByChecklistIdAsync(maintenanceChecklist.ChecklistId)
                    : new List<Domain.Entities.MaintenanceChecklistResult>();

                using var memoryStream = new MemoryStream();
                using var writer = new PdfWriter(memoryStream);
                using var pdf = new PdfDocument(writer);
                using var document = new Document(pdf);

                // Tạo font hỗ trợ tiếng Việt
                PdfFont font;
                PdfFont boldFont;
                try
                {
                    // Thử sử dụng font hệ thống hỗ trợ tiếng Việt
                    font = PdfFontFactory.CreateFont("Arial", PdfEncodings.IDENTITY_H);
                    boldFont = PdfFontFactory.CreateFont("Arial-Bold", PdfEncodings.IDENTITY_H);
                }
                catch
                {
                    // Fallback về font mặc định nếu không tìm thấy Arial
                    font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                    boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                }

                // Header - HÓA ĐƠN GIÁ TRỊ GIA TĂNG
                var headerTable = new Table(1)
                    .SetWidth(UnitValue.CreatePercentValue(100))
                    .SetMarginBottom(20);
                
                var headerCell = new Cell()
                    .SetBackgroundColor(new DeviceRgb(0, 123, 255))
                    .SetPadding(20)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .Add(new Paragraph("HÓA ĐƠN GIÁ TRỊ GIA TĂNG")
                        .SetFont(boldFont)
                        .SetFontSize(20)
                        .SetFontColor(DeviceRgb.WHITE))
                    .Add(new Paragraph("(VAT INVOICE)")
                        .SetFont(font)
                        .SetFontSize(14)
                        .SetFontColor(DeviceRgb.WHITE)
                        .SetMarginTop(5));
                
                headerTable.AddCell(headerCell);
                document.Add(headerTable);

                // Thông tin đơn vị bán hàng và người mua hàng
                var infoTable = new Table(2)
                    .SetWidth(UnitValue.CreatePercentValue(100))
                    .SetMarginBottom(20);

                // Đơn vị bán hàng
                var sellerCell = new Cell()
                    .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                    .SetPadding(10)
                    .Add(new Paragraph("Đơn vị bán hàng (Seller):").SetFont(boldFont).SetFontSize(12))
                    .Add(new Paragraph("EV SERVICE CENTER").SetFont(font).SetFontSize(11).SetMarginTop(5))
                    .Add(new Paragraph("Mã số thuế: 0123456789").SetFont(font).SetFontSize(10).SetMarginTop(3))
                    .Add(new Paragraph("Địa chỉ: 123 Đường ABC, Quận XYZ, TP.HCM").SetFont(font).SetFontSize(10).SetMarginTop(3))
                    .Add(new Paragraph("Điện thoại: 1900-EVSERVICE").SetFont(font).SetFontSize(10).SetMarginTop(3));

                // Người mua hàng
                var buyerCell = new Cell()
                    .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                    .SetPadding(10)
                    .Add(new Paragraph("Người mua hàng (Buyer):").SetFont(boldFont).SetFontSize(12))
                    .Add(new Paragraph(booking.Customer?.User?.FullName ?? "N/A").SetFont(font).SetFontSize(11).SetMarginTop(5))
                    .Add(new Paragraph($"Mã KH: {booking.CustomerId}").SetFont(font).SetFontSize(10).SetMarginTop(3))
                    .Add(new Paragraph("Địa chỉ: " + (booking.Customer?.User?.Address ?? "N/A")).SetFont(font).SetFontSize(10).SetMarginTop(3))
                    .Add(new Paragraph("Hình thức thanh toán: Chuyển khoản").SetFont(font).SetFontSize(10).SetMarginTop(3));

                infoTable.AddCell(sellerCell);
                infoTable.AddCell(buyerCell);
                document.Add(infoTable);

                // Thông tin hóa đơn
                var invoiceInfoTable = new Table(3)
                    .SetWidth(UnitValue.CreatePercentValue(100))
                    .SetMarginBottom(20);

                invoiceInfoTable.AddCell(new Cell().Add(new Paragraph("Ký hiệu (Series): 1K25TAA").SetFont(font).SetFontSize(10)).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
                invoiceInfoTable.AddCell(new Cell().Add(new Paragraph($"Số (No.): {invoice?.InvoiceId:D8}").SetFont(font).SetFontSize(10)).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
                invoiceInfoTable.AddCell(new Cell().Add(new Paragraph($"Ngày: {DateTime.UtcNow:dd} tháng {DateTime.UtcNow:MM} năm {DateTime.UtcNow:yyyy}").SetFont(font).SetFontSize(10)).SetBorder(iText.Layout.Borders.Border.NO_BORDER));

                document.Add(invoiceInfoTable);

                // Bảng kê chi tiết hàng hóa, dịch vụ
                var itemsTable = new Table(6)
                    .SetWidth(UnitValue.CreatePercentValue(100))
                    .SetMarginBottom(20);

                // Header của bảng
                var headers = new[] { "STT (No.)", "Tên hàng hóa, dịch vụ (Description)", "Đơn vị tính (Unit)", "Số lượng (Quantity)", "Đơn giá (Unit price)", "Thành tiền (Amount)" };
                foreach (var header in headers)
                {
                    itemsTable.AddCell(new Cell()
                        .SetBackgroundColor(new DeviceRgb(240, 240, 240))
                        .SetPadding(8)
                        .Add(new Paragraph(header).SetFont(boldFont).SetFontSize(10))
                        .SetTextAlignment(TextAlignment.CENTER));
                }

                // Thêm dòng dịch vụ
                var servicePrice = booking.Service?.BasePrice ?? 0m;
                var packageDiscountAmount = invoice?.PackageDiscountAmount ?? 0m;
                var promotionDiscountAmount = invoice?.PromotionDiscountAmount ?? 0m;
                var partsAmount = invoice?.PartsAmount ?? 0m;
                var finalServicePrice = servicePrice - packageDiscountAmount;

                int stt = 1;
                itemsTable.AddCell(new Cell().Add(new Paragraph(stt.ToString()).SetFont(font).SetFontSize(10)).SetPadding(8).SetTextAlignment(TextAlignment.CENTER));
                itemsTable.AddCell(new Cell().Add(new Paragraph(booking.Service?.ServiceName ?? "Dịch vụ").SetFont(font).SetFontSize(10)).SetPadding(8));
                itemsTable.AddCell(new Cell().Add(new Paragraph("Lần").SetFont(font).SetFontSize(10)).SetPadding(8).SetTextAlignment(TextAlignment.CENTER));
                itemsTable.AddCell(new Cell().Add(new Paragraph("1").SetFont(font).SetFontSize(10)).SetPadding(8).SetTextAlignment(TextAlignment.CENTER));
                itemsTable.AddCell(new Cell().Add(new Paragraph($"{finalServicePrice:N0}").SetFont(font).SetFontSize(10)).SetPadding(8).SetTextAlignment(TextAlignment.RIGHT));
                itemsTable.AddCell(new Cell().Add(new Paragraph($"{finalServicePrice:N0}").SetFont(font).SetFontSize(10)).SetPadding(8).SetTextAlignment(TextAlignment.RIGHT));

                // Thêm các dòng phụ tùng
                foreach (var part in workOrderParts)
                {
                    stt++;
                    var partTotal = (part.Part?.Price ?? 0) * part.QuantityUsed;
                    itemsTable.AddCell(new Cell().Add(new Paragraph(stt.ToString()).SetFont(font).SetFontSize(10)).SetPadding(8).SetTextAlignment(TextAlignment.CENTER));
                    itemsTable.AddCell(new Cell().Add(new Paragraph(part.Part?.PartName ?? "Phụ tùng").SetFont(font).SetFontSize(10)).SetPadding(8));
                    itemsTable.AddCell(new Cell().Add(new Paragraph("Cái").SetFont(font).SetFontSize(10)).SetPadding(8).SetTextAlignment(TextAlignment.CENTER));
                    itemsTable.AddCell(new Cell().Add(new Paragraph(part.QuantityUsed.ToString()).SetFont(font).SetFontSize(10)).SetPadding(8).SetTextAlignment(TextAlignment.CENTER));
                    itemsTable.AddCell(new Cell().Add(new Paragraph($"{(part.Part?.Price ?? 0):N0}").SetFont(font).SetFontSize(10)).SetPadding(8).SetTextAlignment(TextAlignment.RIGHT));
                    itemsTable.AddCell(new Cell().Add(new Paragraph($"{partTotal:N0}").SetFont(font).SetFontSize(10)).SetPadding(8).SetTextAlignment(TextAlignment.RIGHT));
                }

                document.Add(itemsTable);

                // Phần tóm tắt thanh toán
                var totalAmount = finalServicePrice + partsAmount;
                var vatAmount = 0m; // Không có VAT
                var finalTotal = totalAmount - promotionDiscountAmount;

                var summaryTable = new Table(2)
                    .SetWidth(UnitValue.CreatePercentValue(60))
                    .SetHorizontalAlignment(HorizontalAlignment.RIGHT)
                    .SetMarginTop(20);

                summaryTable.AddCell(new Cell().Add(new Paragraph("Thuế suất GTGT (VAT rate): KCT").SetFont(font).SetFontSize(10)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(5));
                summaryTable.AddCell(new Cell().Add(new Paragraph("").SetFont(font).SetFontSize(10)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(5));

                summaryTable.AddCell(new Cell().Add(new Paragraph("Cộng tiền hàng (Total amount):").SetFont(font).SetFontSize(10)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(5));
                summaryTable.AddCell(new Cell().Add(new Paragraph($"{totalAmount:N0}").SetFont(font).SetFontSize(10)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(5).SetTextAlignment(TextAlignment.RIGHT));

                summaryTable.AddCell(new Cell().Add(new Paragraph("Tiền thuế GTGT (VAT amount):").SetFont(font).SetFontSize(10)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(5));
                summaryTable.AddCell(new Cell().Add(new Paragraph($"{vatAmount:N0}").SetFont(font).SetFontSize(10)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(5).SetTextAlignment(TextAlignment.RIGHT));

                if (promotionDiscountAmount > 0)
                {
                    summaryTable.AddCell(new Cell().Add(new Paragraph("Giảm giá khuyến mãi:").SetFont(font).SetFontSize(10)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(5));
                    summaryTable.AddCell(new Cell().Add(new Paragraph($"-{promotionDiscountAmount:N0}").SetFont(font).SetFontSize(10)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(5).SetTextAlignment(TextAlignment.RIGHT));
                }

                summaryTable.AddCell(new Cell().Add(new Paragraph("Tổng cộng tiền thanh toán (Total payment):").SetFont(boldFont).SetFontSize(12)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(8));
                summaryTable.AddCell(new Cell().Add(new Paragraph($"{finalTotal:N0}").SetFont(boldFont).SetFontSize(12)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(8).SetTextAlignment(TextAlignment.RIGHT));

                document.Add(summaryTable);

                // Số tiền bằng chữ
                var amountInWords = ConvertNumberToWords((int)finalTotal);
                var amountInWordsParagraph = new Paragraph($"Số tiền viết bằng chữ (Amount in words): {amountInWords}")
                    .SetFont(font)
                    .SetFontSize(10)
                    .SetMarginTop(15);
                document.Add(amountInWordsParagraph);

                // Chữ ký
                var signatureTable = new Table(2)
                    .SetWidth(UnitValue.CreatePercentValue(100))
                    .SetMarginTop(50);

                signatureTable.AddCell(new Cell()
                    .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                    .SetPadding(20)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .Add(new Paragraph("NGƯỜI MUA HÀNG (Buyer)").SetFont(boldFont).SetFontSize(12))
                    .Add(new Paragraph("").SetFont(font).SetFontSize(10).SetMarginTop(30))
                    .Add(new Paragraph("Ký tên").SetFont(font).SetFontSize(10)));

                signatureTable.AddCell(new Cell()
                    .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                    .SetPadding(20)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .Add(new Paragraph("NGƯỜI BÁN HÀNG (Seller)").SetFont(boldFont).SetFontSize(12))
                    .Add(new Paragraph("").SetFont(font).SetFontSize(10).SetMarginTop(30))
                    .Add(new Paragraph("Ký tên").SetFont(font).SetFontSize(10)));

                document.Add(signatureTable);

                // Footer
                var footerParagraph = new Paragraph("(Cần kiểm tra, đối chiếu khi lập, giao, nhận hóa đơn)")
                    .SetFont(font)
                    .SetFontSize(9)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginTop(30);
                document.Add(footerParagraph);

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

        private string ConvertNumberToWords(int number)
        {
            if (number == 0) return "Không đồng";

            string[] ones = { "", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín" };
            string[] tens = { "", "mười", "hai mươi", "ba mươi", "bốn mươi", "năm mươi", "sáu mươi", "bảy mươi", "tám mươi", "chín mươi" };
            string[] hundreds = { "", "một trăm", "hai trăm", "ba trăm", "bốn trăm", "năm trăm", "sáu trăm", "bảy trăm", "tám trăm", "chín trăm" };

            if (number < 10) return ones[number] + " đồng";
            if (number < 100)
            {
                if (number % 10 == 0) return tens[number / 10] + " đồng";
                return tens[number / 10] + " " + ones[number % 10] + " đồng";
            }
            if (number < 1000)
            {
                if (number % 100 == 0) return hundreds[number / 100] + " đồng";
                if (number % 10 == 0) return hundreds[number / 100] + " " + tens[(number % 100) / 10] + " đồng";
                return hundreds[number / 100] + " " + tens[(number % 100) / 10] + " " + ones[number % 10] + " đồng";
            }

            // Đơn giản hóa cho số lớn
            if (number >= 1000000)
            {
                int millions = number / 1000000;
                int remainder = number % 1000000;
                if (remainder == 0) return ConvertNumberToWords(millions) + " triệu đồng";
                return ConvertNumberToWords(millions) + " triệu " + ConvertNumberToWords(remainder);
            }
            if (number >= 1000)
            {
                int thousands = number / 1000;
                int remainder = number % 1000;
                if (remainder == 0) return ConvertNumberToWords(thousands) + " nghìn đồng";
                return ConvertNumberToWords(thousands) + " nghìn " + ConvertNumberToWords(remainder);
            }

            return number.ToString() + " đồng";
        }

        public async Task<byte[]> GenerateMaintenanceReportPdfAsync(int bookingId)
        {
            try
            {
                var booking = await _bookingRepository.GetBookingByIdAsync(bookingId);
                if (booking == null)
                    throw new ArgumentException($"Booking {bookingId} không tồn tại");

                // Lấy dữ liệu kết quả kiểm tra bảo dưỡng
                var maintenanceChecklist = await _maintenanceChecklistRepository.GetByBookingIdAsync(bookingId);
                var maintenanceResults = maintenanceChecklist != null 
                    ? await _maintenanceChecklistResultRepository.GetByChecklistIdAsync(maintenanceChecklist.ChecklistId)
                    : new List<Domain.Entities.MaintenanceChecklistResult>();

                if (!maintenanceResults.Any())
                    throw new ArgumentException($"Không có kết quả kiểm tra bảo dưỡng cho booking {bookingId}");

                using var memoryStream = new MemoryStream();
                using var writer = new PdfWriter(memoryStream);
                using var pdf = new PdfDocument(writer);
                using var document = new Document(pdf);

                // Tạo font hỗ trợ tiếng Việt
                PdfFont font;
                PdfFont boldFont;
                try
                {
                    font = PdfFontFactory.CreateFont("Arial", PdfEncodings.IDENTITY_H);
                    boldFont = PdfFontFactory.CreateFont("Arial-Bold", PdfEncodings.IDENTITY_H);
                }
                catch
                {
                    font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                    boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                }

                // Header - PHIẾU KIỂM TRA BẢO DƯỠNG ĐỊNH KỲ
                var headerTable = new Table(1)
                    .SetWidth(UnitValue.CreatePercentValue(100))
                    .SetMarginBottom(20);
                
                var headerCell = new Cell()
                    .SetBackgroundColor(new DeviceRgb(0, 123, 255))
                    .SetPadding(20)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .Add(new Paragraph("PHIẾU KIỂM TRA BẢO DƯỠNG ĐỊNH KỲ")
                        .SetFont(boldFont)
                        .SetFontSize(20)
                        .SetFontColor(DeviceRgb.WHITE))
                    .Add(new Paragraph("BIỂU MẪU 09")
                        .SetFont(font)
                        .SetFontSize(14)
                        .SetFontColor(DeviceRgb.WHITE)
                        .SetMarginTop(5));
                
                headerTable.AddCell(headerCell);
                document.Add(headerTable);

                // Thông tin booking và xe
                var infoTable = new Table(4)
                    .SetWidth(UnitValue.CreatePercentValue(100))
                    .SetMarginBottom(20);

                infoTable.AddCell(new Cell().Add(new Paragraph("Biển số:").SetFont(boldFont).SetFontSize(10)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(5));
                infoTable.AddCell(new Cell().Add(new Paragraph(booking.Vehicle?.LicensePlate ?? "N/A").SetFont(font).SetFontSize(10)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(5));
                infoTable.AddCell(new Cell().Add(new Paragraph("Ngày kiểm tra:").SetFont(boldFont).SetFontSize(10)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(5));
                infoTable.AddCell(new Cell().Add(new Paragraph(DateTime.UtcNow.ToString("dd/MM/yyyy")).SetFont(font).SetFontSize(10)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(5));

                infoTable.AddCell(new Cell().Add(new Paragraph("Loại xe:").SetFont(boldFont).SetFontSize(10)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(5));
                infoTable.AddCell(new Cell().Add(new Paragraph(booking.Vehicle?.VehicleModel?.ModelName ?? "N/A").SetFont(font).SetFontSize(10)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(5));
                infoTable.AddCell(new Cell().Add(new Paragraph("Kỹ thuật viên:").SetFont(boldFont).SetFontSize(10)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(5));
                infoTable.AddCell(new Cell().Add(new Paragraph(booking.TechnicianTimeSlot?.Technician?.User?.FullName ?? "N/A").SetFont(font).SetFontSize(10)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(5));

                document.Add(infoTable);

                // Ghi chú hướng dẫn
                var instructionParagraph = new Paragraph("Ghi chú: khoanh tròn các hạng mục đã thực hiện")
                    .SetFont(font)
                    .SetFontSize(10)
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .SetMarginBottom(15);
                document.Add(instructionParagraph);

                // Bảng kết quả kiểm tra bảo dưỡng
                var maintenanceTable = new Table(5)
                    .SetWidth(UnitValue.CreatePercentValue(100))
                    .SetMarginBottom(20);

                // Header của bảng
                var headers = new[] { "STT", "Hình minh họa", "Nội dung kiểm tra Bảo dưỡng", "Cấp bảo dưỡng", "Kết quả kiểm tra" };
                foreach (var header in headers)
                {
                    maintenanceTable.AddCell(new Cell()
                        .SetBackgroundColor(new DeviceRgb(240, 240, 240))
                        .SetPadding(8)
                        .Add(new Paragraph(header).SetFont(boldFont).SetFontSize(10))
                        .SetTextAlignment(TextAlignment.CENTER));
                }

                // Thêm dữ liệu kết quả kiểm tra
                int stt = 1;
                foreach (var result in maintenanceResults.OrderBy(r => r.PartId))
                {
                    // STT
                    maintenanceTable.AddCell(new Cell()
                        .Add(new Paragraph(stt.ToString()).SetFont(font).SetFontSize(10))
                        .SetPadding(8)
                        .SetTextAlignment(TextAlignment.CENTER));

                    // Hình minh họa (để trống vì không có hình)
                    maintenanceTable.AddCell(new Cell()
                        .Add(new Paragraph("-").SetFont(font).SetFontSize(10))
                        .SetPadding(8)
                        .SetTextAlignment(TextAlignment.CENTER));

                    // Nội dung kiểm tra
                    var partName = result.Part?.PartName ?? result.Description ?? "N/A";
                    maintenanceTable.AddCell(new Cell()
                        .Add(new Paragraph(partName).SetFont(font).SetFontSize(10))
                        .SetPadding(8));

                    // Cấp bảo dưỡng (để trống vì không có thông tin)
                    maintenanceTable.AddCell(new Cell()
                        .Add(new Paragraph("-").SetFont(font).SetFontSize(10))
                        .SetPadding(8)
                        .SetTextAlignment(TextAlignment.CENTER));

                    // Kết quả kiểm tra với màu sắc
                    var resultText = result.Result?.ToUpper() ?? "PENDING";
                    var resultColor = GetResultColor(resultText);
                    maintenanceTable.AddCell(new Cell()
                        .Add(new Paragraph(resultText).SetFont(boldFont).SetFontSize(10).SetFontColor(resultColor))
                        .SetPadding(8)
                        .SetTextAlignment(TextAlignment.CENTER));

                    stt++;
                }

                document.Add(maintenanceTable);

                // Thống kê kết quả kiểm tra
                var passCount = maintenanceResults.Count(r => string.Equals(r.Result, "PASS", StringComparison.OrdinalIgnoreCase));
                var failCount = maintenanceResults.Count(r => string.Equals(r.Result, "FAIL", StringComparison.OrdinalIgnoreCase));
                var pendingCount = maintenanceResults.Count(r => string.Equals(r.Result, "PENDING", StringComparison.OrdinalIgnoreCase));
                var naCount = maintenanceResults.Count(r => string.Equals(r.Result, "NA", StringComparison.OrdinalIgnoreCase));

                var summaryTable = new Table(2)
                    .SetWidth(UnitValue.CreatePercentValue(80))
                    .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                    .SetMarginTop(20);

                summaryTable.AddCell(new Cell().Add(new Paragraph("Tổng số hạng mục kiểm tra:").SetFont(boldFont).SetFontSize(12)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(8));
                summaryTable.AddCell(new Cell().Add(new Paragraph(maintenanceResults.Count().ToString()).SetFont(boldFont).SetFontSize(12)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(8).SetTextAlignment(TextAlignment.RIGHT));

                summaryTable.AddCell(new Cell().Add(new Paragraph("Đạt (PASS):").SetFont(font).SetFontSize(11)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(6));
                summaryTable.AddCell(new Cell().Add(new Paragraph(passCount.ToString()).SetFont(font).SetFontSize(11)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(6).SetTextAlignment(TextAlignment.RIGHT));

                summaryTable.AddCell(new Cell().Add(new Paragraph("Không đạt (FAIL):").SetFont(font).SetFontSize(11)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(6));
                summaryTable.AddCell(new Cell().Add(new Paragraph(failCount.ToString()).SetFont(font).SetFontSize(11)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(6).SetTextAlignment(TextAlignment.RIGHT));

                summaryTable.AddCell(new Cell().Add(new Paragraph("Chưa kiểm tra (PENDING):").SetFont(font).SetFontSize(11)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(6));
                summaryTable.AddCell(new Cell().Add(new Paragraph(pendingCount.ToString()).SetFont(font).SetFontSize(11)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(6).SetTextAlignment(TextAlignment.RIGHT));

                summaryTable.AddCell(new Cell().Add(new Paragraph("Không áp dụng (N/A):").SetFont(font).SetFontSize(11)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(6));
                summaryTable.AddCell(new Cell().Add(new Paragraph(naCount.ToString()).SetFont(font).SetFontSize(11)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(6).SetTextAlignment(TextAlignment.RIGHT));

                document.Add(summaryTable);

                // Chữ ký
                var signatureTable = new Table(3)
                    .SetWidth(UnitValue.CreatePercentValue(100))
                    .SetMarginTop(50);

                signatureTable.AddCell(new Cell()
                    .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                    .SetPadding(20)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .Add(new Paragraph("KỸ THUẬT VIÊN").SetFont(boldFont).SetFontSize(12))
                    .Add(new Paragraph("").SetFont(font).SetFontSize(10).SetMarginTop(30))
                    .Add(new Paragraph("Ký tên").SetFont(font).SetFontSize(10)));

                signatureTable.AddCell(new Cell()
                    .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                    .SetPadding(20)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .Add(new Paragraph("QUẢN ĐỐC").SetFont(boldFont).SetFontSize(12))
                    .Add(new Paragraph("").SetFont(font).SetFontSize(10).SetMarginTop(30))
                    .Add(new Paragraph("Ký tên").SetFont(font).SetFontSize(10)));

                signatureTable.AddCell(new Cell()
                    .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                    .SetPadding(20)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .Add(new Paragraph("TRƯỞNG PHÒNG DỊCH VỤ").SetFont(boldFont).SetFontSize(12))
                    .Add(new Paragraph("").SetFont(font).SetFontSize(10).SetMarginTop(30))
                    .Add(new Paragraph("Ký tên").SetFont(font).SetFontSize(10)));

                document.Add(signatureTable);

                // Footer
                var footerParagraph = new Paragraph("(Cần kiểm tra, đối chiếu khi lập, giao, nhận phiếu kiểm tra)")
                    .SetFont(font)
                    .SetFontSize(9)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginTop(30);
                document.Add(footerParagraph);

                document.Close();

                var pdfBytes = memoryStream.ToArray();
                _logger.LogInformation("Generated maintenance report PDF for booking {BookingId}, size: {Size} bytes", bookingId, pdfBytes.Length);
                
                return pdfBytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate maintenance report PDF for booking {BookingId}", bookingId);
                throw new Exception($"Không thể tạo phiếu kết quả bảo dưỡng PDF: {ex.Message}", ex);
            }
        }

        private DeviceRgb GetResultColor(string result)
        {
            return result switch
            {
                "PASS" => new DeviceRgb(40, 167, 69), // Xanh lá
                "FAIL" => new DeviceRgb(220, 53, 69), // Đỏ
                "PENDING" => new DeviceRgb(255, 193, 7), // Vàng
                "NA" => new DeviceRgb(108, 117, 125), // Xám
                _ => new DeviceRgb(0, 0, 0) // Đen mặc định
            };
        }
    }
}
