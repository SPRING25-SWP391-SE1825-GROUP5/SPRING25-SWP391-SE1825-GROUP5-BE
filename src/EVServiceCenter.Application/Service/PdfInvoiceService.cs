using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
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
        private readonly IConfiguration _configuration;

        // Company info từ configuration
        private string CompanyName => _configuration["Company:Name"] ?? "EV Service Center";
        private string CompanyEmail => _configuration["Company:Email"] ?? "support@evservicecenter.com";
        private string CompanyWebsite => _configuration["Company:Website"] ?? "www.evservicecenter.com";
        private string CompanyHotline => _configuration["Company:Hotline"] ?? _configuration["Support:Phone"] ?? "1900-EVSERVICE";
        private string WorkingHours => _configuration["Company:WorkingHours"] ?? "8:00 - 18:00 (T2-CN)";
        private string InvoiceSeries => _configuration["Company:InvoiceSeries"] ?? "EVS";

        public PdfInvoiceService(
            IBookingRepository bookingRepository,
            IWorkOrderPartRepository workOrderPartRepository,
            IInvoiceRepository invoiceRepository,
            IMaintenanceChecklistRepository maintenanceChecklistRepository,
            IMaintenanceChecklistResultRepository maintenanceChecklistResultRepository,
            ILogger<PdfInvoiceService> logger,
            IConfiguration configuration)
        {
            _bookingRepository = bookingRepository;
            _workOrderPartRepository = workOrderPartRepository;
            _invoiceRepository = invoiceRepository;
            _maintenanceChecklistRepository = maintenanceChecklistRepository;
            _maintenanceChecklistResultRepository = maintenanceChecklistResultRepository;
            _logger = logger;
            _configuration = configuration;
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

                // Set margins nhỏ hơn để fit trong 1 trang (10mm = ~28.35 points)
                document.SetMargins(28, 28, 28, 28);

                // Tạo font hỗ trợ tiếng Việt với encoding IDENTITY_H để hiển thị đúng ký tự có dấu
                PdfFont font;
                PdfFont boldFont;
                var fontsFolder = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);

                try
                {
                    // Ưu tiên 1: Arial (phổ biến và hỗ trợ tốt tiếng Việt)
                    var arialPath = Path.Combine(fontsFolder, "arial.ttf");
                    if (File.Exists(arialPath))
                    {
                        font = PdfFontFactory.CreateFont(arialPath, PdfEncodings.IDENTITY_H);
                        var arialBoldPath = Path.Combine(fontsFolder, "arialbd.ttf");
                        boldFont = File.Exists(arialBoldPath)
                            ? PdfFontFactory.CreateFont(arialBoldPath, PdfEncodings.IDENTITY_H)
                            : PdfFontFactory.CreateFont(arialPath, PdfEncodings.IDENTITY_H);
                    }
                    // Ưu tiên 2: Times New Roman
                    else
                    {
                        var timesPath = Path.Combine(fontsFolder, "times.ttf");
                        if (File.Exists(timesPath))
                        {
                            font = PdfFontFactory.CreateFont(timesPath, PdfEncodings.IDENTITY_H);
                            var timesBoldPath = Path.Combine(fontsFolder, "timesbd.ttf");
                            boldFont = File.Exists(timesBoldPath)
                                ? PdfFontFactory.CreateFont(timesBoldPath, PdfEncodings.IDENTITY_H)
                                : PdfFontFactory.CreateFont(timesPath, PdfEncodings.IDENTITY_H);
                        }
                        // Ưu tiên 3: Thử với tên font trực tiếp
                        else
                        {
                            font = PdfFontFactory.CreateFont("Arial", PdfEncodings.IDENTITY_H);
                            boldFont = PdfFontFactory.CreateFont("Arial-Bold", PdfEncodings.IDENTITY_H);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Không thể tải font từ hệ thống, thử font system font với IDENTITY_H");
                    try
                    {
                        // Thử với tên font hệ thống
                        font = PdfFontFactory.CreateFont("Arial", PdfEncodings.IDENTITY_H);
                        boldFont = PdfFontFactory.CreateFont("Arial-Bold", PdfEncodings.IDENTITY_H);
                    }
                    catch
                    {
                        try
                        {
                            font = PdfFontFactory.CreateFont("Times-Roman", PdfEncodings.IDENTITY_H);
                            boldFont = PdfFontFactory.CreateFont("Times-Bold", PdfEncodings.IDENTITY_H);
                        }
                        catch
                        {
                            // Fallback cuối cùng - vẫn dùng IDENTITY_H để hỗ trợ tiếng Việt
                            font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA, PdfEncodings.IDENTITY_H);
                            boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD, PdfEncodings.IDENTITY_H);
                        }
                    }
                }

                // Header - HÓA ĐƠN GIÁ TRỊ GIA TĂNG
                var headerTable = new Table(1)
                    .SetWidth(UnitValue.CreatePercentValue(100))
                    .SetMarginBottom(10);

                var headerCell = new Cell()
                    .SetBackgroundColor(new DeviceRgb(0, 123, 255))
                    .SetPadding(12)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .Add(new Paragraph("HÓA ĐƠN GIÁ TRỊ GIA TĂNG")
                        .SetFont(boldFont)
                        .SetFontSize(18)
                        .SetFontColor(DeviceRgb.WHITE))
                    .Add(new Paragraph("(VAT INVOICE)")
                        .SetFont(font)
                        .SetFontSize(12)
                        .SetFontColor(DeviceRgb.WHITE)
                        .SetMarginTop(3));

                headerTable.AddCell(headerCell);
                document.Add(headerTable);

                // Thông tin đơn vị bán hàng và người mua hàng
                var infoTable = new Table(2)
                    .SetWidth(UnitValue.CreatePercentValue(100))
                    .SetMarginBottom(10);

                // Đơn vị bán hàng
                var sellerCell = new Cell()
                    .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                    .SetPadding(6)
                    .Add(new Paragraph("Đơn vị bán hàng (Seller):").SetFont(boldFont).SetFontSize(10))
                    .Add(new Paragraph(booking.Center?.CenterName ?? CompanyName).SetFont(font).SetFontSize(9).SetMarginTop(2))
                    .Add(new Paragraph($"Mã số thuế: {booking.Center?.CenterName ?? ""} - {booking.CenterId:D6}").SetFont(font).SetFontSize(8).SetMarginTop(1))
                    .Add(new Paragraph($"Địa chỉ: {booking.Center?.Address ?? "Chưa cập nhật"}").SetFont(font).SetFontSize(8).SetMarginTop(1))
                    .Add(new Paragraph($"Điện thoại: {booking.Center?.PhoneNumber ?? "Chưa cập nhật"}").SetFont(font).SetFontSize(8).SetMarginTop(1))
                    .Add(new Paragraph($"Email: {CompanyEmail}").SetFont(font).SetFontSize(8).SetMarginTop(1))
                    .Add(new Paragraph($"Website: {CompanyWebsite}").SetFont(font).SetFontSize(8).SetMarginTop(1));

                // Người mua hàng
                var payment = invoice?.Payments?.OrderByDescending(p => p.CreatedAt).FirstOrDefault();
                var buyerCell = new Cell()
                    .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                    .SetPadding(6)
                    .Add(new Paragraph("Người mua hàng (Buyer):").SetFont(boldFont).SetFontSize(10))
                    .Add(new Paragraph(booking.Customer?.User?.FullName ?? "Chưa cập nhật").SetFont(font).SetFontSize(9).SetMarginTop(2))
                    .Add(new Paragraph($"Mã KH: {booking.CustomerId}").SetFont(font).SetFontSize(8).SetMarginTop(1))
                    .Add(new Paragraph($"Email: {invoice?.Email ?? booking.Customer?.User?.Email ?? "Chưa cập nhật"}").SetFont(font).SetFontSize(8).SetMarginTop(1))
                    .Add(new Paragraph($"Số điện thoại: {invoice?.Phone ?? booking.Customer?.User?.PhoneNumber ?? "Chưa cập nhật"}").SetFont(font).SetFontSize(8).SetMarginTop(1))
                    .Add(new Paragraph($"Địa chỉ: {booking.Customer?.User?.Address ?? "Chưa cập nhật"}").SetFont(font).SetFontSize(8).SetMarginTop(1));

                infoTable.AddCell(sellerCell);
                infoTable.AddCell(buyerCell);
                document.Add(infoTable);

                // Thông tin hóa đơn
                var invoiceInfoTable = new Table(3)
                    .SetWidth(UnitValue.CreatePercentValue(100))
                    .SetMarginBottom(8);

                invoiceInfoTable.AddCell(new Cell().Add(new Paragraph($"Ký hiệu (Series): {InvoiceSeries}").SetFont(font).SetFontSize(9)).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
                invoiceInfoTable.AddCell(new Cell().Add(new Paragraph($"Số (No.): {invoice?.InvoiceId:D8}").SetFont(font).SetFontSize(9)).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
                invoiceInfoTable.AddCell(new Cell().Add(new Paragraph($"Ngày: {invoice?.CreatedAt:dd} tháng {invoice?.CreatedAt:MM} năm {invoice?.CreatedAt:yyyy}").SetFont(font).SetFontSize(9)).SetBorder(iText.Layout.Borders.Border.NO_BORDER));

                document.Add(invoiceInfoTable);

                // Thông tin xe và dịch vụ
                var vehicleInfoTable = new Table(4)
                    .SetWidth(UnitValue.CreatePercentValue(100))
                    .SetMarginBottom(8);

                vehicleInfoTable.AddCell(new Cell().Add(new Paragraph("Thông tin xe:").SetFont(boldFont).SetFontSize(9)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(3));
                vehicleInfoTable.AddCell(new Cell().Add(new Paragraph($"Biển số: {booking.Vehicle?.LicensePlate ?? "N/A"}").SetFont(font).SetFontSize(8)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(3));
                vehicleInfoTable.AddCell(new Cell().Add(new Paragraph($"Loại xe: {booking.Vehicle?.VehicleModel?.ModelName ?? "N/A"}").SetFont(font).SetFontSize(8)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(3));
                vehicleInfoTable.AddCell(new Cell().Add(new Paragraph($"Màu: {booking.Vehicle?.Color ?? "N/A"}").SetFont(font).SetFontSize(8)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(3));

                vehicleInfoTable.AddCell(new Cell().Add(new Paragraph("Kỹ thuật viên:").SetFont(boldFont).SetFontSize(9)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(3));
                vehicleInfoTable.AddCell(new Cell().Add(new Paragraph(booking.TechnicianTimeSlot?.Technician?.User?.FullName ?? "Chưa phân công").SetFont(font).SetFontSize(8)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(3));
                vehicleInfoTable.AddCell(new Cell().Add(new Paragraph($"Ngày thực hiện: {booking.TechnicianTimeSlot?.WorkDate:dd/MM/yyyy}").SetFont(font).SetFontSize(8)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(3));
                vehicleInfoTable.AddCell(new Cell().Add(new Paragraph($"Thời gian: {booking.TechnicianTimeSlot?.Slot?.SlotLabel ?? booking.TechnicianTimeSlot?.Slot?.SlotTime.ToString("HH:mm") ?? "N/A"}").SetFont(font).SetFontSize(8)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(3));

                document.Add(vehicleInfoTable);

                // Bảng kê chi tiết hàng hóa, dịch vụ
                var itemsTable = new Table(6)
                    .SetWidth(UnitValue.CreatePercentValue(100))
                    .SetMarginBottom(10);

                // Header của bảng
                var headers = new[] { "STT", "Tên hàng hóa, dịch vụ", "ĐVT", "SL", "Đơn giá", "Thành tiền" };
                foreach (var header in headers)
                {
                    itemsTable.AddCell(new Cell()
                        .SetBackgroundColor(new DeviceRgb(240, 240, 240))
                        .SetPadding(5)
                        .Add(new Paragraph(header).SetFont(boldFont).SetFontSize(8))
                        .SetTextAlignment(TextAlignment.CENTER));
                }

                // Logic: Booking chỉ chọn được 1 trong 2: Service HOẶC Service Package
                var servicePrice = booking.Service?.BasePrice ?? 0m;
                var packageDiscountAmount = invoice?.PackageDiscountAmount ?? 0m;
                var promotionDiscountAmount = invoice?.PromotionDiscountAmount ?? 0m;
                var partsAmount = invoice?.PartsAmount ?? 0m;

                // Kiểm tra xem booking có sử dụng Service Package không
                var hasServicePackage = booking.AppliedCredit != null &&
                                       booking.AppliedCredit.ServicePackage != null;

                var packageName = booking.AppliedCredit?.ServicePackage?.PackageName;
                var packageCode = booking.AppliedCredit?.ServicePackage?.PackageCode;
                var packageDiscountPercent = booking.AppliedCredit?.ServicePackage?.DiscountPercent;

                int stt = 1;

                // Trường hợp 1: Booking sử dụng Service Package (có AppliedCredit)
                if (hasServicePackage && packageName != null)
                {
                    // Tính giá sau giảm giá của gói
                    var packagePrice = servicePrice - packageDiscountAmount;
                    var packagePriceText = packageDiscountAmount > 0 ? packagePrice : servicePrice;

                    // Hiển thị tên gói dịch vụ
                    var servicePackageDisplay = !string.IsNullOrEmpty(packageCode)
                        ? $"{packageName} ({packageCode})"
                        : packageName;

                    if (packageDiscountPercent.HasValue && packageDiscountPercent.Value > 0)
                    {
                        servicePackageDisplay += $" - Giảm {packageDiscountPercent.Value:N0}%";
                    }

                    itemsTable.AddCell(new Cell().Add(new Paragraph(stt.ToString()).SetFont(font).SetFontSize(8)).SetPadding(4).SetTextAlignment(TextAlignment.CENTER));
                    itemsTable.AddCell(new Cell().Add(new Paragraph(servicePackageDisplay).SetFont(font).SetFontSize(8)).SetPadding(4));
                    itemsTable.AddCell(new Cell().Add(new Paragraph("Gói").SetFont(font).SetFontSize(8)).SetPadding(4).SetTextAlignment(TextAlignment.CENTER));
                    itemsTable.AddCell(new Cell().Add(new Paragraph("1").SetFont(font).SetFontSize(8)).SetPadding(4).SetTextAlignment(TextAlignment.CENTER));
                    itemsTable.AddCell(new Cell().Add(new Paragraph($"{servicePrice:N0}").SetFont(font).SetFontSize(8)).SetPadding(4).SetTextAlignment(TextAlignment.RIGHT));

                    // Nếu có giảm giá, hiển thị giá sau giảm; nếu không, hiển thị giá gốc
                    itemsTable.AddCell(new Cell().Add(new Paragraph($"{packagePriceText:N0}").SetFont(font).SetFontSize(8)).SetPadding(4).SetTextAlignment(TextAlignment.RIGHT));

                    // Nếu có giảm giá, hiển thị dòng giảm giá riêng
                    if (packageDiscountAmount > 0)
                    {
                        itemsTable.AddCell(new Cell().Add(new Paragraph("").SetFont(font).SetFontSize(8)).SetPadding(4).SetTextAlignment(TextAlignment.CENTER));
                        itemsTable.AddCell(new Cell()
                            .Add(new Paragraph("Giảm giá gói dịch vụ").SetFont(font).SetFontSize(7))
                            .SetPadding(4)
                            .SetFontColor(new DeviceRgb(0, 128, 0)));
                        itemsTable.AddCell(new Cell().Add(new Paragraph("").SetFont(font).SetFontSize(8)).SetPadding(4).SetTextAlignment(TextAlignment.CENTER));
                        itemsTable.AddCell(new Cell().Add(new Paragraph("").SetFont(font).SetFontSize(8)).SetPadding(4).SetTextAlignment(TextAlignment.CENTER));
                        itemsTable.AddCell(new Cell().Add(new Paragraph("").SetFont(font).SetFontSize(8)).SetPadding(4).SetTextAlignment(TextAlignment.RIGHT));
                        itemsTable.AddCell(new Cell()
                            .Add(new Paragraph($"-{packageDiscountAmount:N0}").SetFont(font).SetFontSize(8))
                            .SetPadding(4)
                            .SetTextAlignment(TextAlignment.RIGHT)
                            .SetFontColor(new DeviceRgb(0, 128, 0)));
                    }
                }
                // Trường hợp 2: Booking chỉ sử dụng Service (không có gói dịch vụ)
                else
                {
                    // Chỉ hiển thị dịch vụ với giá gốc
                    itemsTable.AddCell(new Cell().Add(new Paragraph(stt.ToString()).SetFont(font).SetFontSize(8)).SetPadding(4).SetTextAlignment(TextAlignment.CENTER));
                    itemsTable.AddCell(new Cell().Add(new Paragraph(booking.Service?.ServiceName ?? "Dịch vụ").SetFont(font).SetFontSize(8)).SetPadding(4));
                    itemsTable.AddCell(new Cell().Add(new Paragraph("Lần").SetFont(font).SetFontSize(8)).SetPadding(4).SetTextAlignment(TextAlignment.CENTER));
                    itemsTable.AddCell(new Cell().Add(new Paragraph("1").SetFont(font).SetFontSize(8)).SetPadding(4).SetTextAlignment(TextAlignment.CENTER));
                    itemsTable.AddCell(new Cell().Add(new Paragraph($"{servicePrice:N0}").SetFont(font).SetFontSize(8)).SetPadding(4).SetTextAlignment(TextAlignment.RIGHT));
                    itemsTable.AddCell(new Cell().Add(new Paragraph($"{servicePrice:N0}").SetFont(font).SetFontSize(8)).SetPadding(4).SetTextAlignment(TextAlignment.RIGHT));
                }

                var finalServicePrice = hasServicePackage ? (servicePrice - packageDiscountAmount) : servicePrice;

                // Thêm các dòng phụ tùng (nếu có)
                if (workOrderParts.Any())
                {
                    foreach (var part in workOrderParts)
                    {
                        stt++;
                        var partTotal = (part.Part?.Price ?? 0) * part.QuantityUsed;
                        itemsTable.AddCell(new Cell().Add(new Paragraph(stt.ToString()).SetFont(font).SetFontSize(8)).SetPadding(4).SetTextAlignment(TextAlignment.CENTER));
                        itemsTable.AddCell(new Cell().Add(new Paragraph(part.Part?.PartName ?? "Phụ tùng").SetFont(font).SetFontSize(8)).SetPadding(4));
                        itemsTable.AddCell(new Cell().Add(new Paragraph("Cái").SetFont(font).SetFontSize(8)).SetPadding(4).SetTextAlignment(TextAlignment.CENTER));
                        itemsTable.AddCell(new Cell().Add(new Paragraph(part.QuantityUsed.ToString()).SetFont(font).SetFontSize(8)).SetPadding(4).SetTextAlignment(TextAlignment.CENTER));
                        itemsTable.AddCell(new Cell().Add(new Paragraph($"{(part.Part?.Price ?? 0):N0}").SetFont(font).SetFontSize(8)).SetPadding(4).SetTextAlignment(TextAlignment.RIGHT));
                        itemsTable.AddCell(new Cell().Add(new Paragraph($"{partTotal:N0}").SetFont(font).SetFontSize(8)).SetPadding(4).SetTextAlignment(TextAlignment.RIGHT));
                    }
                }

                document.Add(itemsTable);

                // Phần tóm tắt thanh toán
                var totalAmount = finalServicePrice + partsAmount;
                var finalTotal = totalAmount - promotionDiscountAmount;

                var summaryTable = new Table(2)
                    .SetWidth(UnitValue.CreatePercentValue(60))
                    .SetHorizontalAlignment(HorizontalAlignment.RIGHT)
                    .SetMarginTop(10);

                summaryTable.AddCell(new Cell().Add(new Paragraph("Cộng tiền hàng:").SetFont(font).SetFontSize(9)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(3));
                summaryTable.AddCell(new Cell().Add(new Paragraph($"{totalAmount:N0}").SetFont(font).SetFontSize(9)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(3).SetTextAlignment(TextAlignment.RIGHT));

                // Hiển thị giảm giá gói dịch vụ trong phần tóm tắt (chỉ khi có Service Package thật sự)
                if (hasServicePackage && packageDiscountAmount > 0 && packageName != null)
                {
                    var packageDiscountText = !string.IsNullOrEmpty(packageCode)
                        ? $"Giảm giá gói ({packageName}):"
                        : $"Giảm giá gói ({packageName}):";
                    summaryTable.AddCell(new Cell().Add(new Paragraph(packageDiscountText).SetFont(font).SetFontSize(9)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(3));
                    summaryTable.AddCell(new Cell().Add(new Paragraph($"-{packageDiscountAmount:N0}").SetFont(font).SetFontSize(9)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(3).SetTextAlignment(TextAlignment.RIGHT));
                }

                if (promotionDiscountAmount > 0)
                {
                    summaryTable.AddCell(new Cell().Add(new Paragraph("Giảm giá khuyến mãi:").SetFont(font).SetFontSize(9)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(3));
                    summaryTable.AddCell(new Cell().Add(new Paragraph($"-{promotionDiscountAmount:N0}").SetFont(font).SetFontSize(9)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(3).SetTextAlignment(TextAlignment.RIGHT));
                }

                summaryTable.AddCell(new Cell().Add(new Paragraph("Tổng cộng thanh toán:").SetFont(boldFont).SetFontSize(10)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(5));
                summaryTable.AddCell(new Cell().Add(new Paragraph($"{finalTotal:N0}").SetFont(boldFont).SetFontSize(10)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(5).SetTextAlignment(TextAlignment.RIGHT));

                document.Add(summaryTable);

                // Số tiền bằng chữ
                var amountInWords = ConvertNumberToWords((int)finalTotal);
                var amountInWordsParagraph = new Paragraph($"Số tiền bằng chữ: {amountInWords}")
                    .SetFont(font)
                    .SetFontSize(8)
                    .SetMarginTop(5);
                document.Add(amountInWordsParagraph);

                // Thông tin thanh toán chi tiết (nếu có)
                if (payment != null)
                {
                    var paymentInfoTable = new Table(2)
                        .SetWidth(UnitValue.CreatePercentValue(100))
                        .SetMarginTop(5)
                        .SetMarginBottom(3);

                    paymentInfoTable.AddCell(new Cell().Add(new Paragraph("Thông tin thanh toán:").SetFont(boldFont).SetFontSize(9)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(3));
                    paymentInfoTable.AddCell(new Cell().Add(new Paragraph("").SetFont(font).SetFontSize(8)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(3));

                    paymentInfoTable.AddCell(new Cell().Add(new Paragraph("Mã thanh toán:").SetFont(font).SetFontSize(8)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(3));
                    paymentInfoTable.AddCell(new Cell().Add(new Paragraph(payment.PaymentCode ?? "N/A").SetFont(font).SetFontSize(8)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(3));

                    paymentInfoTable.AddCell(new Cell().Add(new Paragraph("Ngày thanh toán:").SetFont(font).SetFontSize(8)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(3));
                    paymentInfoTable.AddCell(new Cell().Add(new Paragraph(payment.PaidAt?.ToString("dd/MM/yyyy HH:mm") ?? payment.CreatedAt.ToString("dd/MM/yyyy HH:mm")).SetFont(font).SetFontSize(8)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(3));

                    paymentInfoTable.AddCell(new Cell().Add(new Paragraph("Trạng thái:").SetFont(font).SetFontSize(8)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(3));
                    var statusText = payment.Status?.ToUpper() switch
                    {
                        "SUCCESS" or "COMPLETED" or "PAID" => "Đã thanh toán",
                        "PENDING" => "Đang chờ thanh toán",
                        "FAILED" => "Thất bại",
                        _ => payment.Status ?? "N/A"
                    };
                    paymentInfoTable.AddCell(new Cell().Add(new Paragraph(statusText).SetFont(font).SetFontSize(8)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(3));

                    document.Add(paymentInfoTable);
                }

                // Chữ ký - giảm margin để tiết kiệm không gian
                var signatureTable = new Table(2)
                    .SetWidth(UnitValue.CreatePercentValue(100))
                    .SetMarginTop(10);

                signatureTable.AddCell(new Cell()
                    .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                    .SetPadding(8)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .Add(new Paragraph("NGƯỜI MUA HÀNG").SetFont(boldFont).SetFontSize(9))
                    .Add(new Paragraph("").SetFont(font).SetFontSize(8).SetMarginTop(10))
                    .Add(new Paragraph("Ký tên").SetFont(font).SetFontSize(8)));

                signatureTable.AddCell(new Cell()
                    .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                    .SetPadding(8)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .Add(new Paragraph("NGƯỜI BÁN HÀNG").SetFont(boldFont).SetFontSize(9))
                    .Add(new Paragraph("").SetFont(font).SetFontSize(8).SetMarginTop(10))
                    .Add(new Paragraph("Ký tên").SetFont(font).SetFontSize(8)));

                document.Add(signatureTable);

                // Footer - đẩy xuống cuối trang nhưng vẫn trong 1 trang
                // Tăng khoảng cách vừa đủ để footer ở cuối trang nhưng không tràn sang trang mới
                var footerTable = new Table(1)
                    .SetWidth(UnitValue.CreatePercentValue(100))
                    .SetMarginTop(20)
                    .SetKeepTogether(true);

                footerTable.AddCell(new Cell()
                    .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                    .SetPadding(5)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .Add(new Paragraph("(Cần kiểm tra, đối chiếu khi lập, giao, nhận hóa đơn)").SetFont(font).SetFontSize(7))
                    .Add(new Paragraph("").SetFont(font).SetFontSize(3).SetMarginTop(2))
                    .Add(new Paragraph($"Hotline: {CompanyHotline} | Email: {CompanyEmail}").SetFont(font).SetFontSize(7))
                    .Add(new Paragraph($"Website: {CompanyWebsite} | Giờ làm việc: {WorkingHours}").SetFont(font).SetFontSize(7))
                    .Add(new Paragraph($"© {CompanyName} - Dịch vụ bảo dưỡng và sửa chữa xe điện chuyên nghiệp").SetFont(font).SetFontSize(7).SetMarginTop(2)));

                document.Add(footerTable);

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

                // Tạo font hỗ trợ tiếng Việt với encoding IDENTITY_H để hiển thị đúng ký tự có dấu
                PdfFont font;
                PdfFont boldFont;
                var fontsFolder = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);

                try
                {
                    // Ưu tiên 1: Arial (phổ biến và hỗ trợ tốt tiếng Việt)
                    var arialPath = Path.Combine(fontsFolder, "arial.ttf");
                    if (File.Exists(arialPath))
                    {
                        font = PdfFontFactory.CreateFont(arialPath, PdfEncodings.IDENTITY_H);
                        var arialBoldPath = Path.Combine(fontsFolder, "arialbd.ttf");
                        boldFont = File.Exists(arialBoldPath)
                            ? PdfFontFactory.CreateFont(arialBoldPath, PdfEncodings.IDENTITY_H)
                            : PdfFontFactory.CreateFont(arialPath, PdfEncodings.IDENTITY_H);
                    }
                    // Ưu tiên 2: Times New Roman
                    else
                    {
                        var timesPath = Path.Combine(fontsFolder, "times.ttf");
                        if (File.Exists(timesPath))
                        {
                            font = PdfFontFactory.CreateFont(timesPath, PdfEncodings.IDENTITY_H);
                            var timesBoldPath = Path.Combine(fontsFolder, "timesbd.ttf");
                            boldFont = File.Exists(timesBoldPath)
                                ? PdfFontFactory.CreateFont(timesBoldPath, PdfEncodings.IDENTITY_H)
                                : PdfFontFactory.CreateFont(timesPath, PdfEncodings.IDENTITY_H);
                        }
                        // Ưu tiên 3: Thử với tên font trực tiếp
                        else
                        {
                            font = PdfFontFactory.CreateFont("Arial", PdfEncodings.IDENTITY_H);
                            boldFont = PdfFontFactory.CreateFont("Arial-Bold", PdfEncodings.IDENTITY_H);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Không thể tải font từ hệ thống, thử font system font với IDENTITY_H");
                    try
                    {
                        // Thử với tên font hệ thống
                        font = PdfFontFactory.CreateFont("Arial", PdfEncodings.IDENTITY_H);
                        boldFont = PdfFontFactory.CreateFont("Arial-Bold", PdfEncodings.IDENTITY_H);
                    }
                    catch
                    {
                        try
                        {
                            font = PdfFontFactory.CreateFont("Times-Roman", PdfEncodings.IDENTITY_H);
                            boldFont = PdfFontFactory.CreateFont("Times-Bold", PdfEncodings.IDENTITY_H);
                        }
                        catch
                        {
                            // Fallback cuối cùng - vẫn dùng IDENTITY_H để hỗ trợ tiếng Việt
                            font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA, PdfEncodings.IDENTITY_H);
                            boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD, PdfEncodings.IDENTITY_H);
                        }
                    }
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

        private string GetPaymentMethodText(string? paymentMethod)
        {
            return paymentMethod?.ToUpper() switch
            {
                "PAYOS" => "Chuyển khoản",
                "CASH" => "Tiền mặt",
                "BANK_TRANSFER" => "Chuyển khoản ngân hàng",
                _ => "Chưa xác định"
            };
        }
    }
}
