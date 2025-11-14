using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
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
        private readonly IConfiguration _configuration;
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
            _ = logger;
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

                var maintenanceChecklist = await _maintenanceChecklistRepository.GetByBookingIdAsync(bookingId);
                var maintenanceResults = maintenanceChecklist != null
                    ? await _maintenanceChecklistResultRepository.GetByChecklistIdAsync(maintenanceChecklist.ChecklistId)
                    : new List<Domain.Entities.MaintenanceChecklistResult>();

                using var memoryStream = new MemoryStream();
                using var writer = new PdfWriter(memoryStream);
                using var pdf = new PdfDocument(writer);
                using var document = new Document(pdf);

                document.SetMargins(28, 28, 28, 28);

                PdfFont font;
                PdfFont boldFont;
                var fontsFolder = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);

                try
                {
                    var arialPath = Path.Combine(fontsFolder, "arial.ttf");
                    if (File.Exists(arialPath))
                    {
                        font = PdfFontFactory.CreateFont(arialPath, PdfEncodings.IDENTITY_H);
                        var arialBoldPath = Path.Combine(fontsFolder, "arialbd.ttf");
                        boldFont = File.Exists(arialBoldPath)
                            ? PdfFontFactory.CreateFont(arialBoldPath, PdfEncodings.IDENTITY_H)
                            : PdfFontFactory.CreateFont(arialPath, PdfEncodings.IDENTITY_H);
                    }
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
                        else
                        {
                            font = PdfFontFactory.CreateFont("Arial", PdfEncodings.IDENTITY_H);
                            boldFont = PdfFontFactory.CreateFont("Arial-Bold", PdfEncodings.IDENTITY_H);
                        }
                    }
                }
                catch (Exception)
                {
                    try
                    {
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
                            font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA, PdfEncodings.IDENTITY_H);
                            boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD, PdfEncodings.IDENTITY_H);
                        }
                    }
                }

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

                var infoTable = new Table(2)
                    .SetWidth(UnitValue.CreatePercentValue(100))
                    .SetMarginBottom(10);

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

                var invoiceInfoTable = new Table(3)
                    .SetWidth(UnitValue.CreatePercentValue(100))
                    .SetMarginBottom(8);

                invoiceInfoTable.AddCell(new Cell().Add(new Paragraph($"Ký hiệu (Series): {InvoiceSeries}").SetFont(font).SetFontSize(9)).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
                invoiceInfoTable.AddCell(new Cell().Add(new Paragraph($"Số (No.): {invoice?.InvoiceId:D8}").SetFont(font).SetFontSize(9)).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
                invoiceInfoTable.AddCell(new Cell().Add(new Paragraph($"Ngày: {invoice?.CreatedAt:dd} tháng {invoice?.CreatedAt:MM} năm {invoice?.CreatedAt:yyyy}").SetFont(font).SetFontSize(9)).SetBorder(iText.Layout.Borders.Border.NO_BORDER));

                document.Add(invoiceInfoTable);

                var vehicleInfoTable = new Table(4)
                    .SetWidth(UnitValue.CreatePercentValue(100))
                    .SetMarginBottom(8);

                vehicleInfoTable.AddCell(new Cell().Add(new Paragraph("Thông tin xe:").SetFont(boldFont).SetFontSize(9)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(3));
                vehicleInfoTable.AddCell(new Cell().Add(new Paragraph($"Biển số: {booking.Vehicle?.LicensePlate ?? "Chưa cập nhật"}").SetFont(font).SetFontSize(8)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(3));
                vehicleInfoTable.AddCell(new Cell().Add(new Paragraph($"Loại xe: {booking.Vehicle?.VehicleModel?.ModelName ?? "Chưa cập nhật"}").SetFont(font).SetFontSize(8)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(3));
                vehicleInfoTable.AddCell(new Cell().Add(new Paragraph($"Màu: {booking.Vehicle?.Color ?? "Chưa cập nhật"}").SetFont(font).SetFontSize(8)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(3));

                vehicleInfoTable.AddCell(new Cell().Add(new Paragraph("Kỹ thuật viên:").SetFont(boldFont).SetFontSize(9)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(3));
                vehicleInfoTable.AddCell(new Cell().Add(new Paragraph(booking.TechnicianTimeSlot?.Technician?.User?.FullName ?? "Chưa phân công").SetFont(font).SetFontSize(8)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(3));
                vehicleInfoTable.AddCell(new Cell().Add(new Paragraph($"Ngày thực hiện: {booking.TechnicianTimeSlot?.WorkDate:dd/MM/yyyy}").SetFont(font).SetFontSize(8)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(3));
                vehicleInfoTable.AddCell(new Cell().Add(new Paragraph($"Thời gian: {booking.TechnicianTimeSlot?.Slot?.SlotLabel ?? booking.TechnicianTimeSlot?.Slot?.SlotTime.ToString("HH:mm") ?? "Chưa cập nhật"}").SetFont(font).SetFontSize(8)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(3));

                document.Add(vehicleInfoTable);

                var itemsTable = new Table(6)
                    .SetWidth(UnitValue.CreatePercentValue(100))
                    .SetMarginBottom(10);

                var headers = new[] { "STT", "Tên hàng hóa, dịch vụ", "ĐVT", "SL", "Đơn giá", "Thành tiền" };
                foreach (var header in headers)
                {
                    itemsTable.AddCell(new Cell()
                        .SetBackgroundColor(new DeviceRgb(240, 240, 240))
                        .SetPadding(5)
                        .Add(new Paragraph(header).SetFont(boldFont).SetFontSize(8))
                        .SetTextAlignment(TextAlignment.CENTER));
                }

                var servicePrice = booking.Service?.BasePrice ?? 0m;
                var packageDiscountAmount = invoice?.PackageDiscountAmount ?? 0m;
                var promotionDiscountAmount = invoice?.PromotionDiscountAmount ?? 0m;
                
                var partsAmount = workOrderParts
                    .Where(p => p.Status == "CONSUMED" && !p.IsCustomerSupplied)
                    .Sum(p => (p.Part?.Price ?? 0m) * p.QuantityUsed);

                var hasServicePackage = booking.AppliedCredit != null &&
                                       booking.AppliedCredit.ServicePackage != null;

                var packageName = booking.AppliedCredit?.ServicePackage?.PackageName;
                var packageCode = booking.AppliedCredit?.ServicePackage?.PackageCode;
                var packageDiscountPercent = booking.AppliedCredit?.ServicePackage?.DiscountPercent;

                int stt = 1;

                if (hasServicePackage && packageName != null)
                {
                    var packagePrice = servicePrice - packageDiscountAmount;
                    var packagePriceText = packageDiscountAmount > 0 ? packagePrice : servicePrice;

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

                    itemsTable.AddCell(new Cell().Add(new Paragraph($"{packagePriceText:N0}").SetFont(font).SetFontSize(8)).SetPadding(4).SetTextAlignment(TextAlignment.RIGHT));

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
                else
                {
                    itemsTable.AddCell(new Cell().Add(new Paragraph(stt.ToString()).SetFont(font).SetFontSize(8)).SetPadding(4).SetTextAlignment(TextAlignment.CENTER));
                    itemsTable.AddCell(new Cell().Add(new Paragraph(booking.Service?.ServiceName ?? "Dịch vụ").SetFont(font).SetFontSize(8)).SetPadding(4));
                    itemsTable.AddCell(new Cell().Add(new Paragraph("Lần").SetFont(font).SetFontSize(8)).SetPadding(4).SetTextAlignment(TextAlignment.CENTER));
                    itemsTable.AddCell(new Cell().Add(new Paragraph("1").SetFont(font).SetFontSize(8)).SetPadding(4).SetTextAlignment(TextAlignment.CENTER));
                    itemsTable.AddCell(new Cell().Add(new Paragraph($"{servicePrice:N0}").SetFont(font).SetFontSize(8)).SetPadding(4).SetTextAlignment(TextAlignment.RIGHT));
                    itemsTable.AddCell(new Cell().Add(new Paragraph($"{servicePrice:N0}").SetFont(font).SetFontSize(8)).SetPadding(4).SetTextAlignment(TextAlignment.RIGHT));
                }

                var finalServicePrice = hasServicePackage ? (servicePrice - packageDiscountAmount) : servicePrice;

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

                var totalAmount = finalServicePrice + partsAmount;
                var finalTotal = totalAmount - promotionDiscountAmount;

                var summaryTable = new Table(2)
                    .SetWidth(UnitValue.CreatePercentValue(60))
                    .SetHorizontalAlignment(HorizontalAlignment.RIGHT)
                    .SetMarginTop(10);

                summaryTable.AddCell(new Cell().Add(new Paragraph("Cộng tiền hàng:").SetFont(font).SetFontSize(9)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(3));
                summaryTable.AddCell(new Cell().Add(new Paragraph($"{totalAmount:N0}").SetFont(font).SetFontSize(9)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(3).SetTextAlignment(TextAlignment.RIGHT));

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

                var amountInWords = ConvertNumberToWords((int)finalTotal);
                var amountInWordsParagraph = new Paragraph($"Số tiền bằng chữ: {amountInWords}")
                    .SetFont(font)
                    .SetFontSize(8)
                    .SetMarginTop(5);
                document.Add(amountInWordsParagraph);

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

                return pdfBytes;
            }
            catch (Exception ex)
            {
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

                PdfFont font;
                PdfFont boldFont;
                var fontsFolder = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);

                try
                {
                    var arialPath = Path.Combine(fontsFolder, "arial.ttf");
                    if (File.Exists(arialPath))
                    {
                        font = PdfFontFactory.CreateFont(arialPath, PdfEncodings.IDENTITY_H);
                        var arialBoldPath = Path.Combine(fontsFolder, "arialbd.ttf");
                        boldFont = File.Exists(arialBoldPath)
                            ? PdfFontFactory.CreateFont(arialBoldPath, PdfEncodings.IDENTITY_H)
                            : PdfFontFactory.CreateFont(arialPath, PdfEncodings.IDENTITY_H);
                    }
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
                        else
                        {
                            font = PdfFontFactory.CreateFont("Arial", PdfEncodings.IDENTITY_H);
                            boldFont = PdfFontFactory.CreateFont("Arial-Bold", PdfEncodings.IDENTITY_H);
                        }
                    }
                }
                catch (Exception)
                {
                    try
                    {
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
                            font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA, PdfEncodings.IDENTITY_H);
                            boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD, PdfEncodings.IDENTITY_H);
                        }
                    }
                }

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

                var infoTable = new Table(4)
                    .SetWidth(UnitValue.CreatePercentValue(100))
                    .SetMarginBottom(20);

                infoTable.AddCell(new Cell().Add(new Paragraph("Biển số:").SetFont(boldFont).SetFontSize(10)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(5));
                infoTable.AddCell(new Cell().Add(new Paragraph(booking.Vehicle?.LicensePlate ?? "Chưa cập nhật").SetFont(font).SetFontSize(10)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(5));
                infoTable.AddCell(new Cell().Add(new Paragraph("Ngày kiểm tra:").SetFont(boldFont).SetFontSize(10)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(5));
                infoTable.AddCell(new Cell().Add(new Paragraph(DateTime.UtcNow.ToString("dd/MM/yyyy")).SetFont(font).SetFontSize(10)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(5));

                infoTable.AddCell(new Cell().Add(new Paragraph("Loại xe:").SetFont(boldFont).SetFontSize(10)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(5));
                infoTable.AddCell(new Cell().Add(new Paragraph(booking.Vehicle?.VehicleModel?.ModelName ?? "Chưa cập nhật").SetFont(font).SetFontSize(10)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(5));
                infoTable.AddCell(new Cell().Add(new Paragraph("Kỹ thuật viên:").SetFont(boldFont).SetFontSize(10)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(5));
                infoTable.AddCell(new Cell().Add(new Paragraph(booking.TechnicianTimeSlot?.Technician?.User?.FullName ?? "Chưa phân công").SetFont(font).SetFontSize(10)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(5));

                document.Add(infoTable);

                var instructionParagraph = new Paragraph("Ghi chú: khoanh tròn các hạng mục đã thực hiện")
                    .SetFont(font)
                    .SetFontSize(10)
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .SetMarginBottom(15);
                document.Add(instructionParagraph);

                var maintenanceTable = new Table(5)
                    .SetWidth(UnitValue.CreatePercentValue(100))
                    .SetMarginBottom(20);

                var headers = new[] { "STT", "Hình minh họa", "Nội dung kiểm tra Bảo dưỡng", "Ghi chú", "Kết quả kiểm tra" };
                foreach (var header in headers)
                {
                    maintenanceTable.AddCell(new Cell()
                        .SetBackgroundColor(new DeviceRgb(240, 240, 240))
                        .SetPadding(8)
                        .Add(new Paragraph(header).SetFont(boldFont).SetFontSize(10))
                        .SetTextAlignment(TextAlignment.CENTER));
                }

                int stt = 1;
                foreach (var result in maintenanceResults.OrderBy(r => r.CategoryId ?? int.MaxValue))
                {
                    maintenanceTable.AddCell(new Cell()
                        .Add(new Paragraph(stt.ToString()).SetFont(font).SetFontSize(10))
                        .SetPadding(8)
                        .SetTextAlignment(TextAlignment.CENTER));

                    maintenanceTable.AddCell(new Cell()
                        .Add(new Paragraph("-").SetFont(font).SetFontSize(10))
                        .SetPadding(8)
                        .SetTextAlignment(TextAlignment.CENTER));

                    var categoryName = result.Category?.CategoryName ?? result.Description ?? "N/A";
                    maintenanceTable.AddCell(new Cell()
                        .Add(new Paragraph(categoryName).SetFont(font).SetFontSize(10))
                        .SetPadding(8));

                    var notes = result.Description ?? "-";
                    maintenanceTable.AddCell(new Cell()
                        .Add(new Paragraph(notes).SetFont(font).SetFontSize(10))
                        .SetPadding(8)
                        .SetTextAlignment(TextAlignment.CENTER));

                    var resultText = result.Result?.ToUpper() ?? "PENDING";
                    var resultTextVi = resultText switch
                    {
                        "PASS" => "Đạt",
                        "FAIL" => "Không đạt",
                        "PENDING" => "Chưa kiểm tra",
                        "NA" => "Không áp dụng",
                        _ => resultText
                    };
                    var resultColor = GetResultColor(resultText);
                    maintenanceTable.AddCell(new Cell()
                        .Add(new Paragraph(resultTextVi).SetFont(boldFont).SetFontSize(10).SetFontColor(resultColor))
                        .SetPadding(8)
                        .SetTextAlignment(TextAlignment.CENTER));

                    stt++;
                }

                document.Add(maintenanceTable);

                var passCount = maintenanceResults.Count(r => string.Equals(r.Result, "PASS", StringComparison.OrdinalIgnoreCase));
                var failCount = maintenanceResults.Count(r => string.Equals(r.Result, "FAIL", StringComparison.OrdinalIgnoreCase));
                var pendingCount = maintenanceResults.Count(r => string.Equals(r.Result, "PENDING", StringComparison.OrdinalIgnoreCase));

                var summaryTable = new Table(2)
                    .SetWidth(UnitValue.CreatePercentValue(80))
                    .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                    .SetMarginTop(20);

                summaryTable.AddCell(new Cell().Add(new Paragraph("Tổng số hạng mục kiểm tra:").SetFont(boldFont).SetFontSize(12)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(8));
                summaryTable.AddCell(new Cell().Add(new Paragraph(maintenanceResults.Count().ToString()).SetFont(boldFont).SetFontSize(12)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(8).SetTextAlignment(TextAlignment.RIGHT));

                summaryTable.AddCell(new Cell().Add(new Paragraph("Đạt:").SetFont(font).SetFontSize(11)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(6));
                summaryTable.AddCell(new Cell().Add(new Paragraph(passCount.ToString()).SetFont(font).SetFontSize(11)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(6).SetTextAlignment(TextAlignment.RIGHT));

                summaryTable.AddCell(new Cell().Add(new Paragraph("Không đạt:").SetFont(font).SetFontSize(11)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(6));
                summaryTable.AddCell(new Cell().Add(new Paragraph(failCount.ToString()).SetFont(font).SetFontSize(11)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(6).SetTextAlignment(TextAlignment.RIGHT));

                summaryTable.AddCell(new Cell().Add(new Paragraph("Chưa kiểm tra:").SetFont(font).SetFontSize(11)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(6));
                summaryTable.AddCell(new Cell().Add(new Paragraph(pendingCount.ToString()).SetFont(font).SetFontSize(11)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(6).SetTextAlignment(TextAlignment.RIGHT));

                document.Add(summaryTable);

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

                var footerParagraph = new Paragraph("(Cần kiểm tra, đối chiếu khi lập, giao, nhận phiếu kiểm tra)")
                    .SetFont(font)
                    .SetFontSize(9)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginTop(30);
                document.Add(footerParagraph);

                document.Close();

                var pdfBytes = memoryStream.ToArray();

                return pdfBytes;
            }
            catch (Exception ex)
            {
                throw new Exception($"Không thể tạo phiếu kết quả bảo dưỡng PDF: {ex.Message}", ex);
            }
        }

        private DeviceRgb GetResultColor(string result)
        {
            return result switch
            {
                "PASS" => new DeviceRgb(40, 167, 69),
                "FAIL" => new DeviceRgb(220, 53, 69),
                "PENDING" => new DeviceRgb(255, 193, 7),
                "NA" => new DeviceRgb(108, 117, 125),
                _ => new DeviceRgb(0, 0, 0)
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
