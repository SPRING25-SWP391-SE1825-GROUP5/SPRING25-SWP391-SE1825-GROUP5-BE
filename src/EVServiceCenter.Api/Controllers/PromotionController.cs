using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using EVServiceCenter.Application.Configurations;
using System.Text;
using System.IO;

namespace EVServiceCenter.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AuthenticatedUser")]
    public class PromotionController : ControllerBase
    {
        private readonly IPromotionService _promotionService;
        private readonly EVServiceCenter.Domain.Interfaces.IBookingRepository _bookingRepo;
        private readonly EVServiceCenter.Domain.Interfaces.IOrderRepository _orderRepo;
        private readonly EVServiceCenter.Domain.Interfaces.IPromotionRepository _promotionRepo;
        private readonly EVServiceCenter.Domain.Interfaces.ICustomerRepository _customerRepo;
        private readonly IOptions<ExportOptions> _exportOptions;


        public PromotionController(
            IPromotionService promotionService,
            EVServiceCenter.Domain.Interfaces.IBookingRepository bookingRepo,
            EVServiceCenter.Domain.Interfaces.IOrderRepository orderRepo,
            EVServiceCenter.Domain.Interfaces.IPromotionRepository promotionRepo,
            EVServiceCenter.Domain.Interfaces.ICustomerRepository customerRepo,
            IOptions<ExportOptions> exportOptions)
        {
            _promotionService = promotionService;
            _bookingRepo = bookingRepo;
            _orderRepo = orderRepo;
            _promotionRepo = promotionRepo;
            _customerRepo = customerRepo;
            _exportOptions = exportOptions;
        }

        private int? GetCustomerIdFromToken()
        {
            var customerIdClaim = User.FindFirst("customerId")?.Value;
            if (int.TryParse(customerIdClaim, out int customerId))
            {
                return customerId;
            }
            return null;
        }


        [HttpGet]
        public async Task<IActionResult> GetAllPromotions(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? status = null,
            [FromQuery] string? promotionType = null)
        {
            try
            {
                // Validate pagination parameters
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var result = await _promotionService.GetAllPromotionsAsync(pageNumber, pageSize, searchTerm, status, promotionType);

                return Ok(new {
                    success = true,
                    message = "Lấy danh sách khuyến mãi thành công",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new {
                    success = false,
                    message = "Lỗi hệ thống: " + ex.Message
                });
            }
        }

        public class BookingApplyPromotionRequest { public string? Code { get; set; } }

        [HttpPost("bookings/{bookingId:int}/apply")]
        public async Task<IActionResult> ApplyForBooking(int bookingId, [FromBody] BookingApplyPromotionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Code))
                return BadRequest(new { success = false, message = "Mã khuyến mãi không được để trống" });

            var booking = await _bookingRepo.GetBookingByIdAsync(bookingId);
            if (booking == null) return NotFound(new { success = false, message = "Booking không tồn tại" });

            var disallowedStatuses = new[] { "PAID", "COMPLETED", "DONE", "FINISHED", "CANCELLED" };
            if (!string.IsNullOrWhiteSpace(booking.Status) && disallowedStatuses.Contains(booking.Status.ToUpper()))
            {
                return BadRequest(new { success = false, message = "Booking đã hoàn tất/đã thanh toán/đã hủy, không thể áp dụng khuyến mãi." });
            }

            var validate = await _promotionService.ValidatePromotionAsync(new ValidatePromotionRequest
            {
                Code = request.Code.Trim().ToUpper(),
                OrderAmount = booking.Service?.BasePrice ?? 0,
                OrderType = "BOOKING"
            });
            if (!validate.IsValid)
                return BadRequest(new { success = false, message = validate.Message, data = validate });

            var promoEntity = await _promotionRepo.GetPromotionByCodeAsync(request.Code.Trim().ToUpper());
            if (promoEntity == null) return NotFound(new { success = false, message = "Mã khuyến mãi không tồn tại" });

            var existingOnBooking = await _promotionRepo.GetUserPromotionsByBookingAsync(booking.BookingId);
            if (existingOnBooking.Any())
            {
                return BadRequest(new { success = false, message = "Booking chỉ được áp dụng 1 khuyến mãi." });
            }

            var savedPromotions = await _promotionRepo.GetUserPromotionsByCustomerAsync(booking.CustomerId);
            var savedPromotion = savedPromotions.FirstOrDefault(x =>
                x.PromotionId == promoEntity.PromotionId &&
                x.Status == "SAVED" &&
                x.BookingId == null &&
                x.OrderId == null);

            if (savedPromotion == null)
            {
                return BadRequest(new {
                    success = false,
                    message = "Bạn cần lưu mã khuyến mãi này trước khi áp dụng. Vui lòng lưu mã khuyến mãi trước."
                });
            }

            savedPromotion.BookingId = booking.BookingId;
            savedPromotion.UsedAt = DateTime.UtcNow;
            savedPromotion.DiscountAmount = validate.DiscountAmount;
            savedPromotion.Status = "APPLIED";
            await _promotionRepo.UpdateUserPromotionAsync(savedPromotion);

            return Ok(new { success = true, message = "Áp dụng khuyến mãi thành công", data = validate });
        }

        [HttpDelete("bookings/{bookingId:int}/{promotionCode}")]
        public async Task<IActionResult> RemoveFromBooking(int bookingId, string promotionCode)
        {
            if (string.IsNullOrWhiteSpace(promotionCode))
                return BadRequest(new { success = false, message = "Mã khuyến mãi không được để trống" });

            var booking = await _bookingRepo.GetBookingByIdAsync(bookingId);
            if (booking == null) return NotFound(new { success = false, message = "Booking không tồn tại" });

            var userPromotions = await _promotionRepo.GetUserPromotionsByBookingAsync(bookingId);
            var userPromotion = userPromotions.FirstOrDefault(x =>
                x.Promotion?.Code?.Equals(promotionCode.Trim().ToUpper(), StringComparison.OrdinalIgnoreCase) == true &&
                x.Status == "APPLIED" &&
                x.BookingId == bookingId);

            if (userPromotion == null)
                return NotFound(new { success = false, message = "Không tìm thấy khuyến mãi trên booking" });

            userPromotion.BookingId = null;
            userPromotion.DiscountAmount = 0;
            userPromotion.Status = "SAVED";
            userPromotion.UsedAt = DateTime.UtcNow;
            await _promotionRepo.UpdateUserPromotionAsync(userPromotion);

            return Ok(new { success = true, message = "Đã gỡ khuyến mãi khỏi booking. Mã khuyến mãi đã được khôi phục vào danh sách đã lưu." });
        }

        [HttpGet("bookings/{bookingId:int}")]
        public async Task<IActionResult> ListByBooking(int bookingId)
        {
            var booking = await _bookingRepo.GetBookingByIdAsync(bookingId);
            if (booking == null) return NotFound(new { success = false, message = "Booking không tồn tại" });

            var items = await _promotionRepo.GetUserPromotionsByBookingAsync(bookingId);
            var result = items.Select(x => new
            {
                code = x.Promotion?.Code,
                description = x.Promotion?.Description,
                discountAmount = x.DiscountAmount,
                usedAt = x.UsedAt,
                status = x.Status
            });
            return Ok(new { success = true, data = result });
        }

        public class OrderApplyPromotionRequest { public string? Code { get; set; } }

        [HttpPost("orders/{orderId:int}/apply")]
        public async Task<IActionResult> ApplyForOrder(int orderId, [FromBody] OrderApplyPromotionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Code))
                return BadRequest(new { success = false, message = "Mã khuyến mãi không được để trống" });

            var order = await _orderRepo.GetByIdAsync(orderId);
            if (order == null) return NotFound(new { success = false, message = "Order không tồn tại" });

            var disallowedStatuses = new[] { "PAID", "COMPLETED", "DONE", "FINISHED", "CANCELLED" };
            if (!string.IsNullOrWhiteSpace(order.Status) && disallowedStatuses.Contains(order.Status.ToUpper()))
            {
                return BadRequest(new { success = false, message = "Order đã hoàn tất/đã thanh toán/đã hủy, không thể áp dụng khuyến mãi." });
            }

            var validate = await _promotionService.ValidatePromotionAsync(new ValidatePromotionRequest
            {
                Code = request.Code.Trim().ToUpper(),
                OrderAmount = order.OrderItems?.Sum(oi => oi.Quantity * oi.UnitPrice) ?? 0m,
                OrderType = "ORDER"
            });
            if (!validate.IsValid)
                return BadRequest(new { success = false, message = validate.Message, data = validate });

            var promoEntity = await _promotionRepo.GetPromotionByCodeAsync(request.Code.Trim().ToUpper());
            if (promoEntity == null) return NotFound(new { success = false, message = "Mã khuyến mãi không tồn tại" });

            var existingOnOrder = await _promotionRepo.GetUserPromotionsByOrderAsync(order.OrderId);
            if (existingOnOrder.Any())
            {
                return BadRequest(new { success = false, message = "Order chỉ được áp dụng 1 khuyến mãi." });
            }

            var savedPromotions = await _promotionRepo.GetUserPromotionsByCustomerAsync(order.CustomerId);
            var savedPromotion = savedPromotions.FirstOrDefault(x =>
                x.PromotionId == promoEntity.PromotionId &&
                x.Status == "SAVED" &&
                x.BookingId == null &&
                x.OrderId == null);

            if (savedPromotion == null)
            {
                return BadRequest(new {
                    success = false,
                    message = "Bạn cần lưu mã khuyến mãi này trước khi áp dụng. Vui lòng lưu mã khuyến mãi trước."
                });
            }

            savedPromotion.OrderId = order.OrderId;
            savedPromotion.UsedAt = DateTime.UtcNow;
            savedPromotion.DiscountAmount = validate.DiscountAmount;
            savedPromotion.Status = "APPLIED";
            await _promotionRepo.UpdateUserPromotionAsync(savedPromotion);

            return Ok(new { success = true, message = "Áp dụng khuyến mãi thành công", data = validate });
        }


        [HttpDelete("orders/{orderId:int}/{promotionCode}")]
        public async Task<IActionResult> RemoveFromOrder(int orderId, string promotionCode)
        {
            if (string.IsNullOrWhiteSpace(promotionCode))
                return BadRequest(new { success = false, message = "Mã khuyến mãi không được để trống" });

            var order = await _orderRepo.GetByIdAsync(orderId);
            if (order == null) return NotFound(new { success = false, message = "Order không tồn tại" });

            var userPromotions = await _promotionRepo.GetUserPromotionsByOrderAsync(orderId);
            var userPromotion = userPromotions.FirstOrDefault(x =>
                x.Promotion?.Code?.Equals(promotionCode.Trim().ToUpper(), StringComparison.OrdinalIgnoreCase) == true &&
                x.Status == "APPLIED" &&
                x.OrderId == orderId);

            if (userPromotion == null)
                return NotFound(new { success = false, message = "Không tìm thấy khuyến mãi trên đơn hàng" });

            userPromotion.OrderId = null;
            userPromotion.DiscountAmount = 0;
            userPromotion.Status = "SAVED";
            userPromotion.UsedAt = DateTime.UtcNow;
            await _promotionRepo.UpdateUserPromotionAsync(userPromotion);

            return Ok(new { success = true, message = "Đã gỡ khuyến mãi khỏi đơn hàng. Mã khuyến mãi đã được khôi phục vào danh sách đã lưu." });
        }

        [HttpGet("orders/{orderId:int}")]
        public async Task<IActionResult> ListByOrder(int orderId)
        {
            var order = await _orderRepo.GetByIdAsync(orderId);
            if (order == null) return NotFound(new { success = false, message = "Order không tồn tại" });

            var items = await _promotionRepo.GetUserPromotionsByOrderAsync(orderId);
            var result = items.Select(x => new
            {
                code = x.Promotion?.Code,
                description = x.Promotion?.Description,
                discountAmount = x.DiscountAmount,
                usedAt = x.UsedAt,
                status = x.Status
            });
            return Ok(new { success = true, data = result });
        }
        [HttpGet("export")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> ExportPromotions()
        {
            try
            {
                var opts = _exportOptions.Value;
                var promotions = await _promotionService.GetPromotionsForExportAsync(null, null, opts.MaxRecords);

                if (promotions.Count > opts.MaxRecords)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = $"Số bản ghi ({promotions.Count}) vượt quá giới hạn cho phép ({opts.MaxRecords}). Vui lòng thu hẹp bộ lọc."
                    });
                }

                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                var bytes = GenerateXlsx(promotions, opts.DateFormat);
                var fileName = $"promotions_{timestamp}.xlsx";
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetPromotionById(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { success = false, message = "ID khuyến mãi không hợp lệ" });

                var promotion = await _promotionService.GetPromotionByIdAsync(id);

                return Ok(new {
                    success = true,
                    message = "Lấy thông tin khuyến mãi thành công",
                    data = promotion
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



        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> CreatePromotion([FromBody] CreatePromotionRequest request)
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

                var promotion = await _promotionService.CreatePromotionAsync(request);

                return CreatedAtAction(nameof(GetPromotionById), new { id = promotion.PromotionId }, new {
                    success = true,
                    message = "Tạo khuyến mãi thành công",
                    data = promotion
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

        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> UpdatePromotion(int id, [FromBody] UpdatePromotionRequest request)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { success = false, message = "ID khuyến mãi không hợp lệ" });

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return BadRequest(new {
                        success = false,
                        message = "Dữ liệu không hợp lệ",
                        errors = errors
                    });
                }

                var promotion = await _promotionService.UpdatePromotionAsync(id, request);

                return Ok(new {
                    success = true,
                    message = "Cập nhật khuyến mãi thành công",
                    data = promotion
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



        [HttpGet("promotions")]
        public async Task<IActionResult> GetCustomerPromotions()
        {
            var customerIdNullable = GetCustomerIdFromToken();
            if (!customerIdNullable.HasValue)
            {
                return BadRequest(new { success = false, message = "Không xác định được khách hàng. Vui lòng đăng nhập lại." });
            }

            int customerId = customerIdNullable.Value;
            var items = await _promotionRepo.GetUserPromotionsByCustomerAsync(customerId);

            var today = DateOnly.FromDateTime(DateTime.Today);
            var result = items
                // Loại bỏ promotions đã USED (1 mã chỉ dùng 1 lần, không thể sử dụng lại)
                .Where(x => !string.Equals(x.Status, "USED", StringComparison.OrdinalIgnoreCase))
                .Select(x => {
                var promo = x.Promotion;
                if (promo == null) return null;

                var isExpired = promo.EndDate.HasValue && promo.EndDate.Value < today;
                var isUsageLimitReached = promo.UsageLimit.HasValue && promo.UsageCount >= promo.UsageLimit.Value;
                var isActive = promo.Status == "ACTIVE" && !isExpired && !isUsageLimitReached;
                var statusToReturn = promo.Status;
                if (promo.Status == "ACTIVE" && (isExpired || isUsageLimitReached))
                {
                    statusToReturn = "EXPIRED";
                }

                return new {
                    promotionId = promo.PromotionId,
                    code = promo.Code,
                    description = promo.Description,
                    discountValue = promo.DiscountValue,
                    discountType = promo.DiscountType,
                    minOrderAmount = promo.MinOrderAmount,
                    startDate = promo.StartDate,
                    endDate = promo.EndDate,
                    maxDiscount = promo.MaxDiscount,
                    status = statusToReturn,
                    createdAt = promo.CreatedAt,
                    updatedAt = promo.UpdatedAt,
                    usageLimit = promo.UsageLimit,
                    usageCount = promo.UsageCount,
                    isActive = isActive,
                    isExpired = isExpired,
                    isUsageLimitReached = isUsageLimitReached,
                    remainingUsage = promo.UsageLimit.HasValue
                        ? Math.Max(0, promo.UsageLimit.Value - promo.UsageCount)
                        : (int?)null,

                    bookingId = x.BookingId,
                    orderId = x.OrderId,
                    userPromotionStatus = x.Status,
                    discountAmount = x.DiscountAmount,
                    usedAt = x.UsedAt
                };
            }).Where(x => x != null);

            return Ok(new { success = true, data = result });
        }

        public class SaveCustomerPromotionRequest { public string Code { get; set; } = string.Empty; }

        [HttpPost("promotions")]
        public async Task<IActionResult> SaveCustomerPromotion([FromBody] SaveCustomerPromotionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Code)) return BadRequest(new { success = false, message = "Mã khuyến mãi không được để trống" });

            var customerIdNullable = GetCustomerIdFromToken();
            if (!customerIdNullable.HasValue)
            {
                return BadRequest(new { success = false, message = "Không xác định được khách hàng. Vui lòng đăng nhập lại." });
            }

            int customerId = customerIdNullable.Value;

            var customer = await _customerRepo.GetCustomerByIdAsync(customerId);
            if (customer == null)
            {
                return NotFound(new { success = false, message = $"Không tìm thấy khách hàng với ID {customerId}" });
            }

            var promoResponse = await _promotionService.GetPromotionByCodeAsync(request.Code.Trim().ToUpper());

            var promo = await _promotionRepo.GetPromotionByCodeAsync(request.Code.Trim().ToUpper());
            if (promo == null) return NotFound(new { success = false, message = "Mã khuyến mãi không tồn tại" });

            var existingForCustomer = await _promotionRepo.GetUserPromotionsByCustomerAsync(customerId);
            var existed = existingForCustomer.FirstOrDefault(x => x.Promotion?.PromotionId == promo.PromotionId);
            if (existed != null)
            {
                if (string.Equals(existed.Status, "APPLIED", StringComparison.OrdinalIgnoreCase) || string.Equals(existed.Status, "USED", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { success = false, message = "Mã này đã được áp dụng/đã sử dụng, không thể lưu lại." });
                }
                return BadRequest(new { success = false, message = "Bạn đã lưu mã khuyến mãi này rồi." });
            }

            var today = DateOnly.FromDateTime(DateTime.Today);
            var isInactive = !string.Equals(promoResponse.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase);
            var notStarted = promoResponse.StartDate > today;
            var expired = promoResponse.EndDate.HasValue && promoResponse.EndDate.Value < today;
            var usageExceeded = promoResponse.UsageLimit.HasValue && promoResponse.UsageCount >= promoResponse.UsageLimit.Value;
            if (isInactive || notStarted || expired || usageExceeded)
            {
                return BadRequest(new { success = false, message = "Mã khuyến mãi không còn hiệu lực để lưu." });
            }

            var up = new EVServiceCenter.Domain.Entities.UserPromotion
            {
                CustomerId = customerId,
                PromotionId = promo.PromotionId,
                BookingId = null,
                OrderId = null,
                ServiceId = null,
                UsedAt = DateTime.UtcNow,
                DiscountAmount = 0,
                Status = "SAVED"
            };
            await _promotionRepo.CreateUserPromotionAsync(up);

            return Ok(new { success = true, message = "Đã lưu khuyến mãi cho khách hàng" });
        }



        [HttpPost("admin/update-expired")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> UpdateExpiredPromotions()
        {
            try
            {
                var count = await _promotionService.UpdateExpiredPromotionsAsync();
                return Ok(new {
                    success = true,
                    message = $"Đã cập nhật {count} promotion(s) thành EXPIRED",
                    updatedCount = count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new {
                    success = false,
                    message = "Lỗi hệ thống: " + ex.Message
                });
            }
        }

        [HttpGet("active")]
        [AllowAnonymous]
        public async Task<IActionResult> GetActivePromotions(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? promotionType = null)
        {
            try
            {
                // Validate pagination parameters
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var result = await _promotionService.GetAllPromotionsAsync(pageNumber, pageSize, searchTerm, "ACTIVE", promotionType);

                return Ok(new {
                    success = true,
                    message = "Lấy danh sách khuyến mãi đang hoạt động thành công",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new {
                    success = false,
                    message = "Lỗi hệ thống: " + ex.Message
                });
            }
        }

        private static byte[] GenerateXlsx(System.Collections.Generic.IList<PromotionResponse> promotions, string dateFormat, object? filters = null)
        {
            using var wb = new ClosedXML.Excel.XLWorkbook();
            var ws = wb.AddWorksheet("Promotions");

            var headers = new[] {
                "PromotionId",
                "Code",
                "Description",
                "DiscountValue",
                "DiscountType",
                "MinOrderAmount",
                "MaxDiscount",
                "StartDate",
                "EndDate",
                "Status",
                "UsageLimit",
                "UsageCount",
                "CreatedAt",
                "UpdatedAt"
            };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
                ws.Cell(1, i + 1).Style.Font.Bold = true;
            }
            ws.SheetView.FreezeRows(1);

            int r = 2;
            foreach (var p in promotions)
            {
                ws.Cell(r, 1).Value = p.PromotionId;
                ws.Cell(r, 2).Value = p.Code ?? string.Empty;
                ws.Cell(r, 3).Value = p.Description ?? string.Empty;
                ws.Cell(r, 4).Value = p.DiscountValue;
                ws.Cell(r, 5).Value = p.DiscountType ?? string.Empty;
                ws.Cell(r, 6).Value = p.MinOrderAmount.HasValue ? p.MinOrderAmount.Value : (decimal?)null;
                ws.Cell(r, 7).Value = p.MaxDiscount.HasValue ? p.MaxDiscount.Value : (decimal?)null;
                ws.Cell(r, 8).Value = p.StartDate.ToDateTime(TimeOnly.MinValue);
                ws.Cell(r, 9).Value = p.EndDate.HasValue ? p.EndDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null;
                ws.Cell(r, 10).Value = p.Status ?? string.Empty;
                ws.Cell(r, 11).Value = p.UsageLimit.HasValue ? p.UsageLimit.Value : (int?)null;
                ws.Cell(r, 12).Value = p.UsageCount;
                ws.Cell(r, 13).Value = p.CreatedAt;
                ws.Cell(r, 14).Value = p.UpdatedAt;
                r++;
            }

            int lastRow = r - 1;
            int lastCol = headers.Length;

            ws.Range(2, 8, lastRow, 9).Style.DateFormat.Format = dateFormat;
            ws.Range(2, 13, lastRow, 14).Style.DateFormat.Format = dateFormat;

            ws.Range(2, 4, lastRow, 4).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Right;
            ws.Range(2, 6, lastRow, 7).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Right;
            ws.Range(2, 10, lastRow, 10).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
            ws.Range(2, 11, lastRow, 12).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
            ws.Range(2, 1, lastRow, lastCol).Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;

            var tableRange = ws.Range(1, 1, lastRow, lastCol);
            var table = tableRange.CreateTable();
            table.Theme = ClosedXML.Excel.XLTableTheme.TableStyleMedium9;
            table.ShowAutoFilter = true;

            ws.Range(1, 1, lastRow, lastCol).Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
            ws.Range(1, 1, lastRow, lastCol).Style.Border.InsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;

            ws.Columns().AdjustToContents();

            var wsFilters = wb.AddWorksheet("Filters");
            wsFilters.Cell(1, 1).Value = "Applied Filters";
            wsFilters.Cell(1, 1).Style.Font.Bold = true;
            int fr = 3;
            void WriteFilter(string key, string? value)
            {
                wsFilters.Cell(fr, 1).Value = key;
                wsFilters.Cell(fr, 2).Value = value ?? string.Empty;
                fr++;
            }
            var dict = new System.Collections.Generic.Dictionary<string, string?>();
            if (filters != null)
            {
                foreach (var prop in filters.GetType().GetProperties())
                {
                    var val = prop.GetValue(filters);
                    dict[prop.Name] = val switch
                    {
                        DateTime dt => dt.ToString(dateFormat),
                        bool b => b ? "TRUE" : "FALSE",
                        _ => val?.ToString()
                    };
                }
            }
            foreach (var kv in dict)
            {
                WriteFilter(kv.Key, kv.Value);
            }
            wsFilters.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }
    }
}
