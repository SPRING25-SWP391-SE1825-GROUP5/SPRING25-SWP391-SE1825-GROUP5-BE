using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using EVServiceCenter.Application.Service;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AuthenticatedUser")]
    public class BookingController : ControllerBase
    {
        private readonly IBookingService _bookingService;
        private readonly IBookingHistoryService _bookingHistoryService;
        private readonly IGuestBookingService _guestBookingService;
        private static readonly string[] AllowedBookingStatuses = new[] { "PENDING", "CONFIRMED", "CANCELLED", "COMPLETED" };

        private readonly EVServiceCenter.Application.Interfaces.IHoldStore _holdStore;
        private readonly Microsoft.AspNetCore.SignalR.IHubContext<EVServiceCenter.Api.BookingHub> _hub;
        private readonly int _ttlMinutes;

        private readonly EVServiceCenter.Application.Service.PaymentService _paymentService;
        private readonly EVServiceCenter.Domain.Interfaces.IInvoiceRepository _invoiceRepository;
        private readonly EVServiceCenter.Domain.Interfaces.IPaymentRepository _paymentRepository;
        private readonly EVServiceCenter.Domain.Interfaces.IBookingRepository _bookingRepository;
        private readonly EVServiceCenter.Domain.Interfaces.ITechnicianRepository _technicianRepository;

    public BookingController(IBookingService bookingService, IBookingHistoryService bookingHistoryService, EVServiceCenter.Application.Interfaces.IHoldStore holdStore, Microsoft.AspNetCore.SignalR.IHubContext<EVServiceCenter.Api.BookingHub> hub, Microsoft.Extensions.Options.IOptions<EVServiceCenter.Application.Configurations.BookingRealtimeOptions> realtimeOptions, IGuestBookingService guestBookingService, EVServiceCenter.Application.Service.PaymentService paymentService, EVServiceCenter.Domain.Interfaces.IInvoiceRepository invoiceRepository, EVServiceCenter.Domain.Interfaces.IPaymentRepository paymentRepository, EVServiceCenter.Domain.Interfaces.IBookingRepository bookingRepository, EVServiceCenter.Domain.Interfaces.ITechnicianRepository technicianRepository)
        {
        _bookingService = bookingService;
        _bookingHistoryService = bookingHistoryService;
        _holdStore = holdStore;
        _hub = hub;
        _ttlMinutes = realtimeOptions?.Value?.HoldTtlMinutes ?? 5;
        _guestBookingService = guestBookingService;
        _paymentService = paymentService;
        _invoiceRepository = invoiceRepository;
        _paymentRepository = paymentRepository;
        _bookingRepository = bookingRepository;
        _technicianRepository = technicianRepository;
        }

        [HttpGet("availability")]
        public async Task<IActionResult> GetAvailability(
            [FromQuery] int centerId,
            [FromQuery] string date,
            [FromQuery] string? serviceIds = null)
        {
            try
            {
                if (centerId <= 0)
                    return BadRequest(new { success = false, message = "ID trung tâm không hợp lệ" });

                if (!DateOnly.TryParse(date, out var bookingDate))
                    return BadRequest(new { success = false, message = "Ngày không đúng định dạng YYYY-MM-DD" });

                var serviceIdList = new List<int>();
                if (!string.IsNullOrWhiteSpace(serviceIds))
                {
                    var serviceIdStrings = serviceIds.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var serviceIdString in serviceIdStrings)
                    {
                        if (int.TryParse(serviceIdString.Trim(), out var serviceId))
                        {
                            serviceIdList.Add(serviceId);
                        }
                    }
                }

                var availability = await _bookingService.GetAvailabilityAsync(centerId, bookingDate, serviceIdList);
                
                return Ok(new { 
                    success = true, 
                    message = "Lấy thông tin khả dụng thành công",
                    data = availability
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "Lỗi hệ thống: " + ex.Message 
                });
            }
        }

        [AllowAnonymous]
        [HttpPost("guest")]
        public async Task<IActionResult> CreateGuestBooking([FromBody] GuestBookingRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors = ModelState });
            }

            try
            {
                var result = await _guestBookingService.CreateGuestBookingAsync(request);
                return Ok(new { success = true, message = "Tạo đặt lịch thành công. Vui lòng thanh toán để xác nhận.", data = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPost("intent")]
        public IActionResult Intent([FromQuery] int centerId, [FromQuery] string date, [FromQuery] int? slotId = null)
        {
            if (centerId <= 0) return BadRequest(new { success = false, message = "ID trung tâm không hợp lệ" });
            if (!DateOnly.TryParse(date, out _)) return BadRequest(new { success = false, message = "Ngày không đúng định dạng YYYY-MM-DD" });
            return Ok(new { success = true, message = "Intent recorded" });
        }

        [HttpGet("available-times")]
        public async Task<IActionResult> GetAvailableTimes(
            [FromQuery] int centerId,
            [FromQuery] string date,
            [FromQuery] int? technicianId = null,
            [FromQuery] string? serviceIds = null)
        {
            try
            {
                if (centerId <= 0)
                    return BadRequest(new { success = false, message = "ID trung tâm không hợp lệ" });

                if (!DateOnly.TryParse(date, out var bookingDate))
                    return BadRequest(new { success = false, message = "Ngày không đúng định dạng YYYY-MM-DD" });

                if (bookingDate < DateOnly.FromDateTime(DateTime.Today))
                    return BadRequest(new { success = false, message = "Không thể đặt lịch cho ngày trong quá khứ" });

                var serviceIdList = new List<int>();
                if (!string.IsNullOrWhiteSpace(serviceIds))
                {
                    var serviceIdStrings = serviceIds.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var serviceIdString in serviceIdStrings)
                    {
                        if (int.TryParse(serviceIdString.Trim(), out var serviceId))
                        {
                            serviceIdList.Add(serviceId);
                        }
                    }
                }

                var availableTimes = await _bookingService.GetAvailableTimesAsync(centerId, bookingDate, technicianId, serviceIdList);

                return Ok(new { 
                    success = true, 
                    message = "Lấy danh sách thời gian khả dụng thành công",
                    data = availableTimes
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "Lỗi hệ thống: " + ex.Message 
                });
            }
        }

        [HttpPost("reserve-slot")]
        public async Task<IActionResult> ReserveTimeSlot(
            [FromQuery] int technicianId,
            [FromQuery] string date,
            [FromQuery] int slotId,
            [FromQuery] int centerId,
            [FromQuery] int? bookingId = null)
        {
            try
            {
                if (technicianId <= 0)
                    return BadRequest(new { success = false, message = "ID kỹ thuật viên không hợp lệ" });

                if (slotId <= 0)
                    return BadRequest(new { success = false, message = "ID slot không hợp lệ" });

                if (!DateOnly.TryParse(date, out var bookingDate))
                    return BadRequest(new { success = false, message = "Ngày không đúng định dạng YYYY-MM-DD" });

                var customerId = 0;
                var ttl = System.TimeSpan.FromMinutes(_ttlMinutes);
                if (_holdStore == null)
                {
                    var result = await _bookingService.ReserveTimeSlotAsync(technicianId, bookingDate, slotId, bookingId);
                    if (result)
                        return Ok(new { success = true, message = "Tạm giữ time slot thành công", data = new { technicianId, date = bookingDate, slotId, bookingId } });
                    return BadRequest(new { success = false, message = "Không thể tạm giữ time slot. Slot có thể đã được đặt hoặc không khả dụng." });
                }
                else
                {
                    if (_holdStore.IsHeld(centerId, bookingDate, slotId, technicianId))
                        return BadRequest(new { success = false, message = "Slot đang được giữ bởi người khác" });
                    if (_holdStore.TryHold(centerId, bookingDate, slotId, technicianId, customerId, ttl, out var expiresAt))
                    {
                        var group = $"center:{centerId}:date:{bookingDate:yyyy-MM-dd}";
                        await _hub.Clients.Group(group).SendCoreAsync("slotHeld", new object[] { new { technicianId, slotId, expiresAt } });
                        return Ok(new { success = true, message = "Tạm giữ time slot thành công", data = new { technicianId, date = bookingDate, slotId, bookingId, expiresAt } });
                    }
                    return BadRequest(new { success = false, message = "Không thể tạm giữ time slot" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "Lỗi hệ thống: " + ex.Message 
                });
            }
        }

        public class UpdateBookingStatusRequest { public string Status { get; set; } = string.Empty; }

        [HttpPut("{id:int}/status")]
        public async Task<IActionResult> ChangeStatus(int id, [FromBody] UpdateBookingStatusRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Status))
                return BadRequest(new { success = false, message = "Trạng thái không được để trống" });

            var status = request.Status.Trim().ToUpper();
            if (!AllowedBookingStatuses.Contains(status))
                return BadRequest(new { success = false, message = "Trạng thái booking không hợp lệ" });

            var updated = await _bookingService.UpdateBookingStatusAsync(id, new EVServiceCenter.Application.Models.Requests.UpdateBookingStatusRequest { Status = status });
            return Ok(new { success = true, message = "Cập nhật trạng thái booking thành công", data = new { bookingId = updated.BookingId, status = updated.Status } });
        }

        [HttpPost("release-slot")]
        public async Task<IActionResult> ReleaseTimeSlot(
            [FromQuery] int technicianId,
            [FromQuery] string date,
            [FromQuery] int slotId,
            [FromQuery] int centerId)
        {
            try
            {
                if (technicianId <= 0)
                    return BadRequest(new { success = false, message = "ID kỹ thuật viên không hợp lệ" });

                if (slotId <= 0)
                    return BadRequest(new { success = false, message = "ID slot không hợp lệ" });

                if (!DateOnly.TryParse(date, out var bookingDate))
                    return BadRequest(new { success = false, message = "Ngày không đúng định dạng YYYY-MM-DD" });

                if (_holdStore != null)
                {
                    var customerId = 0;
                    var ok = _holdStore.Release(centerId, bookingDate, slotId, technicianId, customerId);
                    if (ok)
                    {
                        var group = $"center:{centerId}:date:{bookingDate:yyyy-MM-dd}";
                        await _hub.Clients.Group(group).SendCoreAsync("slotReleased", new object[] { new { technicianId, slotId } });
                        return Ok(new { success = true, message = "Giải phóng time slot thành công", data = new { technicianId, date = bookingDate, slotId } });
                    }
                    return BadRequest(new { success = false, message = "Không thể giải phóng time slot." });
                }
                else
                {
                    var result = await _bookingService.ReleaseTimeSlotAsync(technicianId, bookingDate, slotId);
                    if (result) return Ok(new { success = true, message = "Giải phóng time slot thành công", data = new { technicianId, date = bookingDate, slotId } });
                    return BadRequest(new { success = false, message = "Không thể giải phóng time slot." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "Lỗi hệ thống: " + ex.Message 
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return BadRequest(new { 
                        success = false, 
                        message = "Dữ liệu không hợp lệ", 
                        errors = errors 
                    });
                }

                var booking = await _bookingService.CreateBookingAsync(request);
                
                var message = (!string.IsNullOrWhiteSpace(request.PackageCode))
                    ? $"Tạo đặt lịch thành công và đã áp dụng gói '{request.PackageCode}'"
                    : "Tạo đặt lịch thành công";
                
                return CreatedAtAction(nameof(GetBookingById), new { id = booking.BookingId }, new { 
                    success = true, 
                    message = message,
                    data = booking
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "Lỗi hệ thống: " + ex.Message 
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetBookingById(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { success = false, message = "ID đặt lịch không hợp lệ" });

                var booking = await _bookingService.GetBookingByIdAsync(id);
                
                return Ok(new { 
                    success = true, 
                    message = "Lấy thông tin đặt lịch thành công",
                    data = booking
                });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "Lỗi hệ thống: " + ex.Message 
                });
            }
        }

        [HttpPost("{id}/auto-assign-technician")]
        public async Task<IActionResult> AutoAssignTechnician(int id)
        {
            try
            {
                var booking = await _bookingService.GetBookingByIdAsync(id);
                if (booking == null)
                    return NotFound(new { success = false, message = "Đặt lịch không tồn tại." });

                var available = await _bookingService.GetAvailableTimesAsync(booking.CenterId, booking.BookingDate, null, null);
                var pick = available.AvailableTimeSlots
                    .FirstOrDefault(t => t.SlotId == booking.SlotId && t.IsRealtimeAvailable);

                if (pick == null || pick.TechnicianId == null)
                    return BadRequest(new { success = false, message = "Không có kỹ thuật viên khả dụng cho slot đã chọn." });

                return Ok(new { success = true, message = "Kỹ thuật viên đã được gán tự động", data = booking });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpPatch("{id}/cancel")]
        public async Task<IActionResult> CancelBooking(int id)
        {
            try
            {
                var req = new EVServiceCenter.Application.Models.Requests.UpdateBookingStatusRequest { Status = "CANCELLED" };
                var updated = await _bookingService.UpdateBookingStatusAsync(id, req);
                return Ok(new { success = true, message = "Hủy đặt lịch thành công", data = updated });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpGet("Customer/{customerId}/booking-history")]
        public async Task<ActionResult<BookingHistoryListResponse>> GetBookingHistory(
            [FromRoute] int customerId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string sortBy = "bookingDate",
            [FromQuery] string sortOrder = "desc")
        {
            var result = await _bookingHistoryService.GetBookingHistoryAsync(
                customerId, page, pageSize, status, fromDate, toDate, sortBy, sortOrder);
            return Ok(result);
        }

        [HttpGet("Customer/{customerId}/booking-history/{bookingId}")]
        public async Task<ActionResult<BookingHistoryResponse>> GetBookingHistoryById(
            [FromRoute] int customerId,
            [FromRoute] int bookingId)
        {
            var result = await _bookingHistoryService.GetBookingHistoryByIdAsync(customerId, bookingId);
            return Ok(result);
        }

        [HttpGet("Customer/{customerId}/booking-history/stats")]
        public async Task<ActionResult<BookingHistoryStatsResponse>> GetBookingHistoryStats(
            [FromRoute] int customerId,
            [FromQuery] string period = "all")
        {
            var result = await _bookingHistoryService.GetBookingHistoryStatsAsync(customerId, period);
            return Ok(result);
        }
        
        [HttpGet("{bookingId:int}/invoice")]
        public async Task<IActionResult> GetBookingInvoice([FromRoute] int bookingId)
        {
            var invoice = await _invoiceRepository.GetByBookingIdAsync(bookingId);
            if (invoice == null) return NotFound(new { success = false, message = "Chưa có hóa đơn cho booking" });
            return Ok(new { success = true, data = new { invoice.InvoiceId, invoice.BookingId, invoice.Status, invoice.Email, invoice.Phone, invoice.CreatedAt } });
        }

        [HttpPost("{bookingId:int}/invoice/link")]
        public async Task<IActionResult> CreateBookingInvoiceLink([FromRoute] int bookingId)
        {
            var checkoutUrl = await _paymentService.CreateBookingPaymentLinkAsync(bookingId);
            return Ok(new { success = true, checkoutUrl });
        }

        [HttpPost("{bookingId:int}/invoice/confirm")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmBookingInvoice([FromRoute] int bookingId, [FromQuery] string orderCode)
        {
            if (string.IsNullOrWhiteSpace(orderCode)) orderCode = bookingId.ToString();
            var ok = await _paymentService.ConfirmPaymentAsync(orderCode);
            if (!ok) return BadRequest(new { success = false, message = "Xác nhận thanh toán không thành công" });
            return Ok(new { success = true });
        }

        public class CreateOfflinePaymentForBookingRequest
        {
            public int Amount { get; set; }
            public int PaidByUserId { get; set; }
            public string Note { get; set; } = string.Empty;
        }

        [HttpPost("{bookingId:int}/invoice/offline")]
        public async Task<IActionResult> CreateOfflinePaymentForBooking([FromRoute] int bookingId, [FromBody] CreateOfflinePaymentForBookingRequest req)
        {
            if (req == null || req.Amount <= 0 || req.PaidByUserId <= 0)
                return BadRequest(new { success = false, message = "amount và paidByUserId là bắt buộc" });

            var booking = await _bookingRepository.GetBookingByIdAsync(bookingId);
            if (booking == null) return NotFound(new { success = false, message = "Không tìm thấy booking" });

            var invoice = await _invoiceRepository.GetByBookingIdAsync(booking.BookingId);
            if (invoice == null)
            {
                invoice = await _invoiceRepository.CreateMinimalAsync(new EVServiceCenter.Domain.Entities.Invoice
                {
                    BookingId = booking.BookingId,
                    CustomerId = booking.CustomerId,
                    Email = booking.Customer?.User?.Email,
                    Phone = booking.Customer?.User?.PhoneNumber,
                    Status = "PAID",
                    CreatedAt = DateTime.UtcNow
                });
            }

            var payment = await _paymentRepository.CreateAsync(new EVServiceCenter.Domain.Entities.Payment
            {
                PaymentCode = $"PAYCASH{DateTime.UtcNow:yyyyMMddHHmmss}{bookingId}",
                InvoiceId = invoice.InvoiceId,
                PaymentMethod = "CASH",
                Amount = req.Amount,
                Status = "PAID",
                PaidAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                PaidByUserID = req.PaidByUserId
            });

            return Ok(new { success = true, paymentId = payment.PaymentId, paymentCode = payment.PaymentCode, amount = payment.Amount });
        }

        [HttpPost("{bookingId:int}/apply-package")]
        public async Task<IActionResult> ApplyPackageToBooking([FromRoute] int bookingId, [FromBody] ApplyPackageRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return BadRequest(new { 
                        success = false, 
                        message = "Dữ liệu không hợp lệ", 
                        errors = errors 
                    });
                }

                var result = await _bookingService.ApplyPackageToBookingAsync(bookingId, request);
                
                return Ok(new { 
                    success = true, 
                    message = "Áp dụng gói dịch vụ thành công",
                    data = result
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "Lỗi hệ thống: " + ex.Message 
                });
            }
        }

        [HttpDelete("{bookingId:int}/remove-package")]
        public async Task<IActionResult> RemovePackageFromBooking([FromRoute] int bookingId)
        {
            try
            {
                var result = await _bookingService.RemovePackageFromBookingAsync(bookingId);
                
                return Ok(new { 
                    success = true, 
                    message = "Gỡ gói dịch vụ thành công",
                    data = result
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "Lỗi hệ thống: " + ex.Message 
                });
            }
        }

        [HttpPost("{bookingId:int}/create-package")]
        [Authorize(Roles = "CUSTOMER,STAFF,ADMIN")]
        public async Task<IActionResult> CreatePackageAfterPayment(int bookingId, [FromBody] CreatePackageAfterPaymentRequest request)
        {
            try
            {
                if (bookingId <= 0)
                    return BadRequest(new { success = false, message = "ID booking không hợp lệ" });

                if (string.IsNullOrWhiteSpace(request.PackageCode))
                    return BadRequest(new { success = false, message = "Mã gói dịch vụ là bắt buộc" });

                var result = await _bookingService.CreatePackageAfterPaymentAsync(bookingId, request.PackageCode);
                
                return Ok(new { 
                    success = true, 
                    message = "Tạo gói dịch vụ thành công",
                    data = result
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "Lỗi hệ thống: " + ex.Message 
                });
            }
        }

        [HttpGet("center/{centerId}")]
        [Authorize(Roles = "STAFF,ADMIN,MANAGER")]
        public async Task<IActionResult> GetBookingsByCenter(
            [FromRoute] int centerId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string sortBy = "createdAt",
            [FromQuery] string sortOrder = "desc")
        {
            try
            {
                if (centerId <= 0)
                {
                    return BadRequest(new { success = false, message = "Center ID must be greater than 0." });
                }

                if (page < 1)
                {
                    return BadRequest(new { success = false, message = "Page must be greater than 0." });
                }

                if (pageSize < 1 || pageSize > 50)
                {
                    return BadRequest(new { success = false, message = "Page size must be between 1 and 50." });
                }

                var bookings = await _bookingRepository.GetBookingsByCenterIdAsync(
                    centerId, page, pageSize, status, fromDate, toDate, sortBy, sortOrder);

                var totalItems = await _bookingRepository.CountBookingsByCenterIdAsync(
                    centerId, status, fromDate, toDate);
                var bookingSummaries = bookings.Select(MapToBookingSummary).ToList();
                var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
                var pagination = new
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    HasNextPage = page < totalPages,
                    HasPreviousPage = page > 1
                };

                var filters = new
                {
                    Status = status,
                    FromDate = fromDate,
                    ToDate = toDate,
                    SortBy = sortBy,
                    SortOrder = sortOrder
                };

                return Ok(new
                {
                    success = true,
                    message = "Lấy danh sách booking theo center thành công",
                    data = new
                    {
                        Bookings = bookingSummaries,
                        Pagination = pagination,
                        Filters = filters
                    }
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }

        private object MapToBookingSummary(Booking booking)
        {
            return new
            {
                BookingId = booking.BookingId,
                BookingDate = DateOnly.FromDateTime(booking.CreatedAt),
                Status = booking.Status ?? "",
                CenterInfo = new
                {
                    CenterId = booking.Center?.CenterId ?? 0,
                    CenterName = booking.Center?.CenterName ?? "",
                    CenterAddress = booking.Center?.Address ?? "",
                    PhoneNumber = booking.Center?.PhoneNumber
                },
                VehicleInfo = new
                {
                    VehicleId = booking.Vehicle?.VehicleId ?? 0,
                    LicensePlate = booking.Vehicle?.LicensePlate ?? "",
                    Vin = booking.Vehicle?.Vin ?? "",
                    ModelName = booking.Vehicle?.VehicleModel?.ModelName ?? "",
                    Version = booking.Vehicle?.VehicleModel?.Version ?? "",
                    CurrentMileage = booking.CurrentMileage ?? 0
                },
                ServiceInfo = new
                {
                    ServiceId = booking.Service?.ServiceId ?? 0,
                    ServiceName = booking.Service?.ServiceName ?? "",
                    Description = booking.Service?.Description ?? "",
                    BasePrice = booking.Service?.BasePrice ?? 0
                },
                TechnicianInfo = new
                {
                    TechnicianId = booking.TechnicianTimeSlot?.TechnicianId ?? 0,
                    TechnicianName = booking.TechnicianTimeSlot?.Technician?.User?.FullName ?? "Chưa gán",
                    PhoneNumber = booking.TechnicianTimeSlot?.Technician?.User?.PhoneNumber ?? "",
                    Position = booking.TechnicianTimeSlot?.Technician?.Position ?? ""
                },
                TimeSlotInfo = new
                {
                    SlotId = booking.TechnicianTimeSlot?.SlotId ?? 0,
                    StartTime = booking.TechnicianTimeSlot?.Slot?.SlotTime.ToString("HH:mm") ?? "Chưa xác định",
                    EndTime = booking.TechnicianTimeSlot?.Slot?.SlotTime.AddMinutes(30).ToString("HH:mm") ?? "Chưa xác định",
                    SlotLabel = booking.TechnicianTimeSlot?.Slot?.SlotLabel ?? "Chưa xác định",
                    WorkDate = booking.TechnicianTimeSlot?.WorkDate.ToString("yyyy-MM-dd") ?? "Chưa xác định",
                    Notes = booking.TechnicianTimeSlot?.Notes ?? ""
                },
                CustomerInfo = new
                {
                    CustomerId = booking.Customer?.CustomerId ?? 0,
                    FullName = booking.Customer?.User?.FullName ?? "",
                    Email = booking.Customer?.User?.Email ?? "",
                    PhoneNumber = booking.Customer?.User?.PhoneNumber ?? ""
                },
                SpecialRequests = booking.SpecialRequests,
                AppliedCreditId = booking.AppliedCreditId,
                CreatedAt = booking.CreatedAt,
                UpdatedAt = booking.UpdatedAt
            };
        }
    }

    public class CreatePackageAfterPaymentRequest
    {
        public required string PackageCode { get; set; }
    }
}