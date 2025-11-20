using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using EVServiceCenter.Infrastructure.Configurations;
using Microsoft.Extensions.Configuration;

namespace EVServiceCenter.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AuthenticatedUser")]
    public class TestingController : ControllerBase
    {
        private readonly IPdfInvoiceService _pdfInvoiceService;
        private readonly EVDbContext _db;
        private readonly IBookingRepository _bookingRepository;
        private readonly IConfiguration _configuration;

        public TestingController(
            IPdfInvoiceService pdfInvoiceService, 
            EVDbContext db,
            IBookingRepository bookingRepository,
            IConfiguration configuration)
        {
            _pdfInvoiceService = pdfInvoiceService;
            _db = db;
            _bookingRepository = bookingRepository;
            _configuration = configuration;
        }

        [HttpGet("invoices/booking/{bookingId:int}/pdf")]
        public async Task<IActionResult> GetInvoicePdf(int bookingId)
        {
            try
            {
                var bookingExists = await _db.Bookings.AnyAsync(b => b.BookingId == bookingId);
                if (!bookingExists)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy booking" });
                }

                var pdfBytes = await _pdfInvoiceService.GenerateInvoicePdfAsync(bookingId);

                return File(pdfBytes, "application/pdf", $"Invoice_Booking_{bookingId}_{DateTime.Now:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi khi tạo PDF hóa đơn: " + ex.Message });
            }
        }

        [HttpGet("bookings/{bookingId:int}/result-pdf")]
        public async Task<IActionResult> GetBookingResultPdf(int bookingId)
        {
            try
            {
                var bookingExists = await _db.Bookings.AnyAsync(b => b.BookingId == bookingId);
                if (!bookingExists)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy booking" });
                }

                var pdfBytes = await _pdfInvoiceService.GenerateMaintenanceReportPdfAsync(bookingId);

                return File(pdfBytes, "application/pdf", $"WorkOrderResult_Booking_{bookingId}_{DateTime.Now:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi khi tạo PDF phiếu kết quả: " + ex.Message });
            }
        }

        /// <summary>
        /// Tạo QR code từ bookingId để test
        /// </summary>
        /// <param name="bookingId">ID của booking</param>
        /// <param name="size">Kích thước QR code (mặc định: 300x300)</param>
        /// <returns>URL của QR code image hoặc JSON chứa QR code data</returns>
        [HttpGet("bookings/{bookingId:int}/qr-code")]
        public async Task<IActionResult> GenerateBookingQRCode(int bookingId, [FromQuery] int size = 300)
        {
            try
            {
                // Kiểm tra booking có tồn tại không
                var booking = await _bookingRepository.GetBookingByIdAsync(bookingId);
                if (booking == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy booking" });
                }

                // Tạo QR code data object (giống format trong frontend)
                var qrCodeDataObject = new
                {
                    bookingId = bookingId,
                    timestamp = DateTime.UtcNow.ToString("O"),
                    type = "CHECK_IN",
                    expiry = DateTime.UtcNow.AddHours(24).ToString("O") // 24 hours expiry
                };

                // Chuẩn bị chuỗi JSON để encode vào QR
                var qrCodeDataString = System.Text.Json.JsonSerializer.Serialize(qrCodeDataObject);

                // Lấy QR code API URL từ config
                var qrCodeApiUrl = _configuration["App:QrCodeApiUrl"] ?? "https://api.qrserver.com/v1/create-qr-code/";
                
                // Tạo QR code image URL (dữ liệu bên trong QR là JSON)
                var qrCodeImageUrl = $"{qrCodeApiUrl}?size={size}x{size}&data={Uri.EscapeDataString(qrCodeDataString)}";

                // Lấy ngày booking từ TechnicianTimeSlot.WorkDate hoặc CreatedAt
                var bookingDate = booking.TechnicianTimeSlot?.WorkDate != null
                    ? DateOnly.FromDateTime(booking.TechnicianTimeSlot.WorkDate)
                    : DateOnly.FromDateTime(booking.CreatedAt);

                return Ok(new
                {
                    success = true,
                    message = "Tạo QR code thành công",
                    data = new
                    {
                        bookingId = bookingId,
                        qrCodeImageUrl = qrCodeImageUrl,
                        qrCodeData = qrCodeDataObject,
                        qrCodeDataString = qrCodeDataString,
                        size = size,
                        bookingStatus = booking.Status,
                        bookingDate = bookingDate.ToString("yyyy-MM-dd")
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi khi tạo QR code: " + ex.Message });
            }
        }
    }
}