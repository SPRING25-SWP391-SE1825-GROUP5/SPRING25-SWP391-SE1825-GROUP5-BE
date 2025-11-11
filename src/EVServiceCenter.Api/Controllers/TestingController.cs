using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using EVServiceCenter.Infrastructure.Configurations;

namespace EVServiceCenter.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AuthenticatedUser")]
    public class TestingController : ControllerBase
    {
        private readonly IPdfInvoiceService _pdfInvoiceService;
        private readonly EVDbContext _db;

        public TestingController(IPdfInvoiceService pdfInvoiceService, EVDbContext db)
        {
            _pdfInvoiceService = pdfInvoiceService;
            _db = db;
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
    }
}