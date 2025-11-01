using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace EVServiceCenter.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AuthenticatedUser")] // Tất cả user đã đăng nhập
    public class PromotionController : ControllerBase
    {
        private readonly IPromotionService _promotionService;
        private readonly EVServiceCenter.Domain.Interfaces.IBookingRepository _bookingRepo;
        private readonly EVServiceCenter.Domain.Interfaces.IOrderRepository _orderRepo;
        private readonly EVServiceCenter.Domain.Interfaces.IPromotionRepository _promotionRepo;
        private readonly EVServiceCenter.Domain.Interfaces.ICustomerRepository _customerRepo;
        

        public PromotionController(
            IPromotionService promotionService,
            EVServiceCenter.Domain.Interfaces.IBookingRepository bookingRepo,
            EVServiceCenter.Domain.Interfaces.IOrderRepository orderRepo,
            EVServiceCenter.Domain.Interfaces.IPromotionRepository promotionRepo,
            EVServiceCenter.Domain.Interfaces.ICustomerRepository customerRepo)
        {
            _promotionService = promotionService;
            _bookingRepo = bookingRepo;
            _orderRepo = orderRepo;
            _promotionRepo = promotionRepo;
            _customerRepo = customerRepo;
        }

        private int? GetCustomerIdFromToken()
        {
            // Lấy customerId từ JWT token claim
            var customerIdClaim = User.FindFirst("customerId")?.Value;
            if (int.TryParse(customerIdClaim, out int customerId))
            {
                return customerId;
            }
            return null;
        }


        /// <summary>
        /// Lấy danh sách tất cả khuyến mãi với phân trang và tìm kiếm
        /// </summary>
        /// <param name="pageNumber">Số trang (mặc định: 1)</param>
        /// <param name="pageSize">Kích thước trang (mặc định: 10)</param>
        /// <param name="searchTerm">Từ khóa tìm kiếm (mã, mô tả)</param>
        /// <param name="status">Lọc theo trạng thái (ACTIVE, CANCELLED, EXPIRED)</param>
        /// <param name="promotionType">Lọc theo loại khuyến mãi (GENERAL, FIRST_TIME, BIRTHDAY, LOYALTY)</param>
        /// <returns>Danh sách khuyến mãi</returns>
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

        // ===== APPLY/REMOVE/LIST for BOOKINGS and ORDERS (unified under PromotionController) =====
        public class BookingApplyPromotionRequest { public string? Code { get; set; } }

        [HttpPost("bookings/{bookingId:int}/apply")]
        public async Task<IActionResult> ApplyForBooking(int bookingId, [FromBody] BookingApplyPromotionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Code))
                return BadRequest(new { success = false, message = "Mã khuyến mãi không được để trống" });

            var booking = await _bookingRepo.GetBookingByIdAsync(bookingId);
            if (booking == null) return NotFound(new { success = false, message = "Booking không tồn tại" });

            // Chỉ cho áp dụng nếu booking chưa thanh toán/chưa hoàn tất
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

            // Chỉ cho 1 promotion cho mỗi booking (ngăn nhiều code)
            var existingOnBooking = await _promotionRepo.GetUserPromotionsByBookingAsync(booking.BookingId);
            if (existingOnBooking.Any())
            {
                return BadRequest(new { success = false, message = "Booking chỉ được áp dụng 1 khuyến mãi." });
            }

            var userPromotion = new EVServiceCenter.Domain.Entities.UserPromotion
            {
                CustomerId = booking.CustomerId,
                PromotionId = promoEntity.PromotionId,
                BookingId = booking.BookingId,
                UsedAt = DateTime.UtcNow,
                DiscountAmount = validate.DiscountAmount,
                Status = "APPLIED"
            };
            await _promotionRepo.CreateUserPromotionAsync(userPromotion);

            return Ok(new { success = true, message = "Áp dụng khuyến mãi thành công", data = validate });
        }

        [HttpDelete("bookings/{bookingId:int}/{promotionCode}")]
        public async Task<IActionResult> RemoveFromBooking(int bookingId, string promotionCode)
        {
            if (string.IsNullOrWhiteSpace(promotionCode))
                return BadRequest(new { success = false, message = "Mã khuyến mãi không được để trống" });

            var booking = await _bookingRepo.GetBookingByIdAsync(bookingId);
            if (booking == null) return NotFound(new { success = false, message = "Booking không tồn tại" });

            var removed = await _promotionRepo.DeleteUserPromotionByBookingAndCodeAsync(bookingId, promotionCode.Trim().ToUpper());
            if (!removed) return NotFound(new { success = false, message = "Không tìm thấy khuyến mãi trên booking" });

            return Ok(new { success = true, message = "Đã gỡ khuyến mãi khỏi booking" });
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

            // Chỉ cho áp dụng nếu order chưa thanh toán/chưa hoàn tất
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

            // Chỉ cho 1 promotion cho mỗi order (ngăn nhiều code)
            var existingOnOrder = await _promotionRepo.GetUserPromotionsByOrderAsync(order.OrderId);
            if (existingOnOrder.Any())
            {
                return BadRequest(new { success = false, message = "Order chỉ được áp dụng 1 khuyến mãi." });
            }

            var userPromotion = new EVServiceCenter.Domain.Entities.UserPromotion
            {
                CustomerId = order.CustomerId,
                PromotionId = promoEntity.PromotionId,
                OrderId = order.OrderId,
                UsedAt = DateTime.UtcNow,
                DiscountAmount = validate.DiscountAmount,
                Status = "APPLIED"
            };
            await _promotionRepo.CreateUserPromotionAsync(userPromotion);

            return Ok(new { success = true, message = "Áp dụng khuyến mãi thành công", data = validate });
        }


        [HttpDelete("orders/{orderId:int}/{promotionCode}")]
        public async Task<IActionResult> RemoveFromOrder(int orderId, string promotionCode)
        {
            if (string.IsNullOrWhiteSpace(promotionCode))
                return BadRequest(new { success = false, message = "Mã khuyến mãi không được để trống" });

            var order = await _orderRepo.GetByIdAsync(orderId);
            if (order == null) return NotFound(new { success = false, message = "Order không tồn tại" });

            var removed = await _promotionRepo.DeleteUserPromotionByOrderAndCodeAsync(orderId, promotionCode.Trim().ToUpper());
            if (!removed) return NotFound(new { success = false, message = "Không tìm thấy khuyến mãi trên đơn hàng" });

            return Ok(new { success = true, message = "Đã gỡ khuyến mãi khỏi đơn hàng" });
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
        /// <summary>
        /// Lấy thông tin khuyến mãi theo ID
        /// </summary>
        /// <param name="id">ID khuyến mãi</param>
        /// <returns>Thông tin khuyến mãi</returns>
        [HttpGet("{id}")]
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

        

        /// <summary>
        /// Tạo khuyến mãi mới (chỉ ADMIN)
        /// </summary>
        /// <param name="request">Thông tin khuyến mãi mới</param>
        /// <returns>Thông tin khuyến mãi đã tạo</returns>
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

        /// <summary>
        /// Cập nhật thông tin khuyến mãi (chỉ ADMIN)
        /// </summary>
        /// <param name="id">ID khuyến mãi</param>
        /// <param name="request">Thông tin cập nhật</param>
        /// <returns>Kết quả cập nhật</returns>
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


        
        // ===== Customer promotions: list & save =====
        [HttpGet("promotions")]
        public async Task<IActionResult> GetCustomerPromotions()
        {
            // Lấy customerId từ JWT token
            var customerIdNullable = GetCustomerIdFromToken();
            if (!customerIdNullable.HasValue)
            {
                return BadRequest(new { success = false, message = "Không xác định được khách hàng. Vui lòng đăng nhập lại." });
            }

            int customerId = customerIdNullable.Value;
            var items = await _promotionRepo.GetUserPromotionsByCustomerAsync(customerId);
            
            // Map đầy đủ thông tin promotion và user promotion
            var today = DateOnly.FromDateTime(DateTime.Today);
            var result = items.Select(x => {
                var promo = x.Promotion;
                if (promo == null) return null;
                
                // Auto-update expired status nếu cần
                var isExpired = promo.EndDate.HasValue && promo.EndDate.Value < today;
                var isUsageLimitReached = promo.UsageLimit.HasValue && promo.UsageCount >= promo.UsageLimit.Value;
                var isActive = promo.Status == "ACTIVE" && !isExpired && !isUsageLimitReached;
                var statusToReturn = promo.Status;
                if (promo.Status == "ACTIVE" && (isExpired || isUsageLimitReached))
                {
                    statusToReturn = "EXPIRED";
                }
                
                return new {
                    // Thông tin từ Promotion
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
                    
                    // Thông tin từ UserPromotion
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

            // Lấy customerId từ JWT token
            var customerIdNullable = GetCustomerIdFromToken();
            if (!customerIdNullable.HasValue)
            {
                return BadRequest(new { success = false, message = "Không xác định được khách hàng. Vui lòng đăng nhập lại." });
            }

            int customerId = customerIdNullable.Value;

            // Kiểm tra customer có tồn tại không
            var customer = await _customerRepo.GetCustomerByIdAsync(customerId);
            if (customer == null)
            {
                return NotFound(new { success = false, message = $"Không tìm thấy khách hàng với ID {customerId}" });
            }

            // Tìm promotion theo code (dùng service để có auto-update expired status)
            var promoResponse = await _promotionService.GetPromotionByCodeAsync(request.Code.Trim().ToUpper());
            
            // Reload từ repo để lấy entity mới nhất sau khi auto-update
            var promo = await _promotionRepo.GetPromotionByCodeAsync(request.Code.Trim().ToUpper());
            if (promo == null) return NotFound(new { success = false, message = "Mã khuyến mãi không tồn tại" });

            // Không cho lưu nếu khách đã có bản ghi cho mã này (dù ở bất kỳ trạng thái nào: SAVED, APPLIED, USED)
            var existingForCustomer = await _promotionRepo.GetUserPromotionsByCustomerAsync(customerId);
            var existed = existingForCustomer.FirstOrDefault(x => x.Promotion?.PromotionId == promo.PromotionId);
            if (existed != null)
            {
                if (string.Equals(existed.Status, "APPLIED", StringComparison.OrdinalIgnoreCase) || string.Equals(existed.Status, "USED", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { success = false, message = "Mã này đã được áp dụng/đã sử dụng, không thể lưu lại." });
                }
                // Đã có bản ghi SAVED rồi, không cho lưu lại
                return BadRequest(new { success = false, message = "Bạn đã lưu mã khuyến mãi này rồi." });
            }

            // Không cho lưu nếu promotion không còn hiệu lực: Inactive, chưa hiệu lực, đã hết hạn, hoặc hết lượt
            // Dùng status từ response (đã được auto-update) để check
            var today = DateOnly.FromDateTime(DateTime.Today);
            var isInactive = !string.Equals(promoResponse.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase);
            var notStarted = promoResponse.StartDate > today;
            var expired = promoResponse.EndDate.HasValue && promoResponse.EndDate.Value < today;
            var usageExceeded = promoResponse.UsageLimit.HasValue && promoResponse.UsageCount >= promoResponse.UsageLimit.Value;
            if (isInactive || notStarted || expired || usageExceeded)
            {
                return BadRequest(new { success = false, message = "Mã khuyến mãi không còn hiệu lực để lưu." });
            }

            // Lưu vào UserPromotions ở trạng thái SAVED (chưa áp dụng booking/order)
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



        /// <summary>
        /// Force update tất cả expired promotions (chỉ ADMIN)
        /// </summary>
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

        /// <summary>
        /// Lấy danh sách khuyến mãi đang hoạt động cho user (Public - không cần đăng nhập)
        /// </summary>
        /// <param name="pageNumber">Số trang (mặc định: 1)</param>
        /// <param name="pageSize">Kích thước trang (mặc định: 10)</param>
        /// <param name="searchTerm">Từ khóa tìm kiếm (mã, mô tả)</param>
        /// <param name="promotionType">Lọc theo loại khuyến mãi (GENERAL, FIRST_TIME, BIRTHDAY, LOYALTY)</param>
        /// <returns>Danh sách khuyến mãi đang hoạt động</returns>
        [HttpGet("active")]
        [AllowAnonymous] // ✅ Cho phép người chưa đăng nhập xem
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
    }
}
