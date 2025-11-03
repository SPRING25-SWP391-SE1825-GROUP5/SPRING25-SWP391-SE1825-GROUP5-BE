using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;

namespace EVServiceCenter.Application.Service
{
    public class PromotionService : IPromotionService
    {
        private PromotionValidationResponse CreateInvalidResponse(string message, decimal orderAmount)
        {
            return new PromotionValidationResponse
            {
                IsValid = false,
                Message = message,
                DiscountAmount = 0,
                FinalAmount = orderAmount,
                Promotion = new PromotionResponse
                {
                    PromotionId = 0,
                    Code = "",
                    Description = "",
                    DiscountType = "",
                    DiscountValue = 0,
                    Status = "",
                    StartDate = DateOnly.FromDateTime(DateTime.MinValue),
                    EndDate = DateOnly.FromDateTime(DateTime.MinValue),
                    CreatedAt = DateTime.MinValue
                }
            };
        }
        private readonly IPromotionRepository _promotionRepository;
        private readonly ICustomerRepository _customerRepository;

        public PromotionService(IPromotionRepository promotionRepository, ICustomerRepository customerRepository)
        {
            _promotionRepository = promotionRepository;
            _customerRepository = customerRepository;
        }

        public async Task<int> MarkUsedByOrderAsync(int orderId)
        {
            var items = await _promotionRepository.GetUserPromotionsByOrderAsync(orderId);
            var count = 0;
            foreach (var up in items)
            {
                if (!string.Equals(up.Status, "USED", StringComparison.OrdinalIgnoreCase))
                {
                    up.Status = "USED";
                    up.UsedAt = DateTime.UtcNow;
                    await _promotionRepository.UpdateUserPromotionAsync(up);
                    // increase promotion usage
                    var promo = await _promotionRepository.GetPromotionByIdAsync(up.PromotionId);
                    if (promo != null)
                    {
                        promo.UsageCount = promo.UsageCount + 1;
                        await _promotionRepository.UpdatePromotionAsync(promo);
                    }
                    count++;
                }
            }
            return count;
        }

        public async Task<int> MarkUsedByBookingAsync(int bookingId)
        {
            var items = await _promotionRepository.GetUserPromotionsByBookingAsync(bookingId);
            var count = 0;
            foreach (var up in items)
            {
                if (!string.Equals(up.Status, "USED", StringComparison.OrdinalIgnoreCase))
                {
                    up.Status = "USED";
                    up.UsedAt = DateTime.UtcNow;
                    await _promotionRepository.UpdateUserPromotionAsync(up);
                    var promo = await _promotionRepository.GetPromotionByIdAsync(up.PromotionId);
                    if (promo != null)
                    {
                        promo.UsageCount = promo.UsageCount + 1;
                        await _promotionRepository.UpdatePromotionAsync(promo);
                    }
                    count++;
                }
            }
            return count;
        }

        public async Task<int> RemoveByBookingAsync(int bookingId)
        {
            var items = await _promotionRepository.GetUserPromotionsByBookingAsync(bookingId);
            var count = 0;
            foreach (var up in items)
            {
                var removed = await _promotionRepository.DeleteUserPromotionByBookingAndCodeAsync(bookingId, up.Promotion?.Code ?? string.Empty);
                if (removed) count++;
            }
            return count;
        }

        public async Task<PromotionListResponse> GetAllPromotionsAsync(int pageNumber = 1, int pageSize = 10, string? searchTerm = null, string? status = null, string? promotionType = null)
        {
            try
            {
                var promotions = await _promotionRepository.GetAllPromotionsAsync();

                // Auto-update expired promotions before filtering
                foreach (var promotion in promotions.Where(p => p.Status == "ACTIVE"))
                {
                    await CheckAndUpdateExpiredStatusAsync(promotion);
                }

                // Filtering
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    promotions = promotions.Where(p =>
                        p.Code.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        p.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }

                if (!string.IsNullOrWhiteSpace(status))
                {
                    promotions = promotions.Where(p => p.Status == status.ToUpper()).ToList();
                }

                // PromotionType removed

                // Pagination
                var totalCount = promotions.Count;
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                var paginatedPromotions = promotions.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

                var promotionResponses = paginatedPromotions.Select(p => MapToPromotionResponse(p)).ToList();

                return new PromotionListResponse
                {
                    Promotions = promotionResponses,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                    TotalCount = totalCount
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy danh sách khuyến mãi: {ex.Message}");
            }
        }

        public async Task<PromotionResponse> GetPromotionByIdAsync(int promotionId)
        {
            try
            {
                var promotion = await _promotionRepository.GetPromotionByIdAsync(promotionId);
                if (promotion == null)
                    throw new ArgumentException("Khuyến mãi không tồn tại.");

                // Auto-update expired status
                await CheckAndUpdateExpiredStatusAsync(promotion);

                return MapToPromotionResponse(promotion);
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy thông tin khuyến mãi: {ex.Message}");
            }
        }

        public async Task<PromotionResponse> GetPromotionByCodeAsync(string code)
        {
            try
            {
                var promotion = await _promotionRepository.GetPromotionByCodeAsync(code);
                if (promotion == null)
                    throw new ArgumentException("Mã khuyến mãi không tồn tại.");

                // Auto-update expired status
                await CheckAndUpdateExpiredStatusAsync(promotion);

                return MapToPromotionResponse(promotion);
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy thông tin khuyến mãi: {ex.Message}");
            }
        }

        public async Task<PromotionResponse> CreatePromotionAsync(CreatePromotionRequest request)
        {
            try
            {
                // Validate request
                await ValidateCreatePromotionRequestAsync(request);

                // Create promotion entity
                var promotion = new Promotion
                {
                    Code = request.Code.Trim().ToUpper(),
                    Description = request.Description.Trim(),
                    DiscountValue = request.DiscountValue,
                    DiscountType = request.DiscountType.ToUpper(),
                    MinOrderAmount = request.MinOrderAmount,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    MaxDiscount = request.MaxDiscount,
                    Status = request.Status.ToUpper(),
                    UsageLimit = request.UsageLimit,
                    UsageCount = 0,


                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Save promotion
                var createdPromotion = await _promotionRepository.CreatePromotionAsync(promotion);

                return MapToPromotionResponse(createdPromotion);
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi tạo khuyến mãi: {ex.Message}");
            }
        }

        public async Task<PromotionResponse> UpdatePromotionAsync(int promotionId, UpdatePromotionRequest request)
        {
            try
            {
                // Validate promotion exists
                var promotion = await _promotionRepository.GetPromotionByIdAsync(promotionId);
                if (promotion == null)
                    throw new ArgumentException("Khuyến mãi không tồn tại.");

                // Validate request
                await ValidateUpdatePromotionRequestAsync(request, promotionId);

                // Update promotion
                promotion.Code = request.Code.Trim().ToUpper();
                promotion.Description = request.Description.Trim();
                promotion.DiscountValue = request.DiscountValue;
                promotion.DiscountType = request.DiscountType.ToUpper();
                promotion.MinOrderAmount = request.MinOrderAmount;
                promotion.StartDate = request.StartDate;
                promotion.EndDate = request.EndDate;
                promotion.MaxDiscount = request.MaxDiscount;
                promotion.Status = request.Status.ToUpper();
                promotion.UsageLimit = request.UsageLimit;


                promotion.UpdatedAt = DateTime.UtcNow;

                await _promotionRepository.UpdatePromotionAsync(promotion);

                return MapToPromotionResponse(promotion);
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi cập nhật khuyến mãi: {ex.Message}");
            }
        }

        public async Task<bool> DeletePromotionAsync(int promotionId)
        {
            try
            {
                // Validate promotion exists
                if (!await _promotionRepository.PromotionExistsAsync(promotionId))
                    throw new ArgumentException("Khuyến mãi không tồn tại.");

                await _promotionRepository.DeletePromotionAsync(promotionId);
                return true;
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi xóa khuyến mãi: {ex.Message}");
            }
        }

        public async Task<PromotionValidationResponse> ValidatePromotionAsync(ValidatePromotionRequest request)
        {
            try
            {
                // Get promotion by code
                var promotion = await _promotionRepository.GetPromotionByCodeAsync(request.Code.Trim().ToUpper());
                if (promotion == null)
                {
                    return CreateInvalidResponse("Mã khuyến mãi không tồn tại.", request.OrderAmount);
                }

                // Auto-update expired status before validation
                await CheckAndUpdateExpiredStatusAsync(promotion);

                // Reload promotion to get updated status
                var reloadedPromotion = await _promotionRepository.GetPromotionByIdAsync(promotion.PromotionId);
                if (reloadedPromotion == null)
                {
                    return CreateInvalidResponse("Mã khuyến mãi không tồn tại.", request.OrderAmount);
                }
                promotion = reloadedPromotion;

                // Check if promotion is active
                if (promotion.Status != "ACTIVE")
                {
                    return CreateInvalidResponse("Mã khuyến mãi không còn hoạt động.", request.OrderAmount);
                }

                // Check date validity
                var today = DateOnly.FromDateTime(DateTime.Today);
                if (promotion.StartDate > today)
                {
                    return CreateInvalidResponse("Mã khuyến mãi chưa có hiệu lực.", request.OrderAmount);
                }

                if (promotion.EndDate.HasValue && promotion.EndDate.Value < today)
                {
                    return CreateInvalidResponse("Mã khuyến mãi đã hết hạn.", request.OrderAmount);
                }

                // Check minimum order amount
                if (promotion.MinOrderAmount.HasValue && request.OrderAmount < promotion.MinOrderAmount.Value)
                {
                    return CreateInvalidResponse($"Đơn hàng phải có giá trị tối thiểu {promotion.MinOrderAmount.Value:N0} VNĐ.", request.OrderAmount);
                }

                // Check usage limit
                if (promotion.UsageLimit.HasValue && promotion.UsageCount >= promotion.UsageLimit.Value)
                {
                    return CreateInvalidResponse("Mã khuyến mãi đã hết lượt sử dụng.", request.OrderAmount);
                }

                // ApplyFor removed: scope is inferred by where the promotion is linked (booking/order)

                // Calculate discount amount
                decimal discountAmount = 0;
                if (promotion.DiscountType == "PERCENT")
                {
                    discountAmount = request.OrderAmount * (promotion.DiscountValue / 100);
                    if (promotion.MaxDiscount.HasValue && discountAmount > promotion.MaxDiscount.Value)
                    {
                        discountAmount = promotion.MaxDiscount.Value;
                    }
                }
                else if (promotion.DiscountType == "FIXED")
                {
                    discountAmount = promotion.DiscountValue;
                    if (discountAmount > request.OrderAmount)
                    {
                        discountAmount = request.OrderAmount;
                    }
                }

                var finalAmount = request.OrderAmount - discountAmount;

                return new PromotionValidationResponse
                {
                    IsValid = true,
                    Message = "Mã khuyến mãi hợp lệ.",
                    DiscountAmount = discountAmount,
                    FinalAmount = finalAmount,
                    Promotion = MapToPromotionResponse(promotion)
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi xác thực mã khuyến mãi: {ex.Message}");
            }
        }

        public async Task<bool> ActivatePromotionAsync(int promotionId)
        {
            try
            {
                var promotion = await _promotionRepository.GetPromotionByIdAsync(promotionId);
                if (promotion == null)
                    throw new ArgumentException("Khuyến mãi không tồn tại.");

                promotion.Status = "ACTIVE";
                promotion.UpdatedAt = DateTime.UtcNow;

                await _promotionRepository.UpdatePromotionAsync(promotion);
                return true;
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi kích hoạt khuyến mãi: {ex.Message}");
            }
        }

        public async Task<bool> DeactivatePromotionAsync(int promotionId)
        {
            try
            {
                var promotion = await _promotionRepository.GetPromotionByIdAsync(promotionId);
                if (promotion == null)
                    throw new ArgumentException("Khuyến mãi không tồn tại.");

                promotion.Status = "CANCELLED";
                promotion.UpdatedAt = DateTime.UtcNow;

                await _promotionRepository.UpdatePromotionAsync(promotion);
                return true;
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi vô hiệu hóa khuyến mãi: {ex.Message}");
            }
        }

        private async Task CheckAndUpdateExpiredStatusAsync(Promotion promotion)
        {
            if (promotion.Status == "ACTIVE")
            {
                var today = DateOnly.FromDateTime(DateTime.Today);
                var isExpired = promotion.EndDate.HasValue && promotion.EndDate.Value < today;
                var isUsageLimitReached = promotion.UsageLimit.HasValue && promotion.UsageCount >= promotion.UsageLimit.Value;

                if (isExpired || isUsageLimitReached)
                {
                    promotion.Status = "EXPIRED";
                    promotion.UpdatedAt = DateTime.UtcNow;
                    await _promotionRepository.UpdatePromotionAsync(promotion);
                }
            }
        }

        public async Task<int> UpdateExpiredPromotionsAsync()
        {
            try
            {
                var allPromotions = await _promotionRepository.GetAllPromotionsAsync();
                var today = DateOnly.FromDateTime(DateTime.Today);
                var count = 0;

                foreach (var promotion in allPromotions.Where(p => p.Status == "ACTIVE"))
                {
                    var isExpired = promotion.EndDate.HasValue && promotion.EndDate.Value < today;
                    var isUsageLimitReached = promotion.UsageLimit.HasValue && promotion.UsageCount >= promotion.UsageLimit.Value;

                    if (isExpired || isUsageLimitReached)
                    {
                        promotion.Status = "EXPIRED";
                        promotion.UpdatedAt = DateTime.UtcNow;
                        await _promotionRepository.UpdatePromotionAsync(promotion);
                        count++;
                    }
                }

                return count;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi cập nhật promotions hết hạn: {ex.Message}");
            }
        }

        public async Task<IList<PromotionResponse>> GetPromotionsForExportAsync(string? searchTerm = null, string? status = null, int maxRecords = 100000)
        {
            var promotions = await _promotionRepository.GetAllPromotionsAsync();

            // Auto-update expired promotions before filtering
            foreach (var promotion in promotions.Where(p => p.Status == "ACTIVE"))
            {
                await CheckAndUpdateExpiredStatusAsync(promotion);
            }

            // Reload after auto-update to get latest status
            promotions = await _promotionRepository.GetAllPromotionsAsync();

            // Filtering
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                promotions = promotions.Where(p =>
                    p.Code.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                promotions = promotions.Where(p => p.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            var limited = promotions
                .OrderBy(p => p.PromotionId)
                .Take(maxRecords)
                .Select(MapToPromotionResponse)
                .ToList();

            return limited;
        }

        private PromotionResponse MapToPromotionResponse(Promotion promotion)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var isExpired = promotion.EndDate.HasValue && promotion.EndDate.Value < today;
            var isUsageLimitReached = promotion.UsageLimit.HasValue && promotion.UsageCount >= promotion.UsageLimit.Value;
            var isActive = promotion.Status == "ACTIVE" && !isExpired && !isUsageLimitReached;

            // Return status with updated value if it was just expired
            var statusToReturn = promotion.Status;
            if (promotion.Status == "ACTIVE" && (isExpired || isUsageLimitReached))
            {
                statusToReturn = "EXPIRED";
            }

            return new PromotionResponse
            {
                PromotionId = promotion.PromotionId,
                Code = promotion.Code,
                Description = promotion.Description,
                DiscountValue = promotion.DiscountValue,
                DiscountType = promotion.DiscountType,
                MinOrderAmount = promotion.MinOrderAmount,
                StartDate = promotion.StartDate,
                EndDate = promotion.EndDate,
                MaxDiscount = promotion.MaxDiscount,
                Status = statusToReturn,
                CreatedAt = promotion.CreatedAt,
                UpdatedAt = promotion.UpdatedAt,
                UsageLimit = promotion.UsageLimit,
                UsageCount = promotion.UsageCount,


                IsActive = isActive,
                IsExpired = isExpired,
                IsUsageLimitReached = isUsageLimitReached,
                // Nếu không giới hạn lượt (UsageLimit null) thì để 0 để tránh giá trị rất lớn gây hiểu nhầm
                RemainingUsage = promotion.UsageLimit.HasValue ? Math.Max(0, promotion.UsageLimit.Value - promotion.UsageCount) : 0
            };
        }

        private async Task ValidateCreatePromotionRequestAsync(CreatePromotionRequest request)
        {
            var errors = new List<string>();

            // Check for duplicate code
            if (!await _promotionRepository.IsPromotionCodeUniqueAsync(request.Code.Trim().ToUpper()))
            {
                errors.Add("Mã khuyến mãi này đã tồn tại. Vui lòng chọn mã khác.");
            }

            // Validate dates
            if (request.EndDate.HasValue && request.EndDate.Value <= request.StartDate)
            {
                errors.Add("Ngày kết thúc phải sau ngày bắt đầu.");
            }

            if (request.StartDate < DateOnly.FromDateTime(DateTime.Today))
            {
                errors.Add("Ngày bắt đầu không được là ngày trong quá khứ.");
            }

            // Validate discount logic
            if (string.IsNullOrWhiteSpace(request.DiscountType))
                errors.Add("Loại giảm giá không được trống.");

            var discountType = (request.DiscountType ?? string.Empty).ToUpper();
            if (discountType == "PERCENT")
            {
                if (request.DiscountValue <= 0 || request.DiscountValue > 100)
                    errors.Add("Giá trị giảm giá phần trăm phải trong khoảng 0 < giá trị ≤ 100.");
                if (request.MaxDiscount.HasValue && request.MaxDiscount.Value < 0)
                    errors.Add("Giảm tối đa (maxDiscount) phải ≥ 0.");
            }
            else if (discountType == "FIXED")
            {
                if (request.DiscountValue <= 0)
                    errors.Add("Giá trị giảm giá cố định phải > 0.");
                if (request.MaxDiscount.HasValue && request.MaxDiscount.Value < 0)
                    errors.Add("Giảm tối đa (maxDiscount) phải ≥ 0.");
            }
            else
            {
                errors.Add("Loại giảm giá không hợp lệ. Chỉ hỗ trợ PERCENT hoặc FIXED.");
            }

            // Validate min order amount
            if (request.MinOrderAmount.HasValue && request.MinOrderAmount.Value < 0)
                errors.Add("Giá trị đơn hàng tối thiểu (minOrderAmount) phải ≥ 0.");

            // Validate usage limit
            if (request.UsageLimit.HasValue && request.UsageLimit.Value <= 0)
                errors.Add("Giới hạn lượt sử dụng (usageLimit) phải > 0 nếu được cung cấp.");

            if (errors.Any())
                throw new ArgumentException(string.Join(" ", errors));
        }

        private async Task ValidateUpdatePromotionRequestAsync(UpdatePromotionRequest request, int promotionId)
        {
            var errors = new List<string>();

            // Check for duplicate code
            if (!await _promotionRepository.IsPromotionCodeUniqueAsync(request.Code.Trim().ToUpper(), promotionId))
            {
                errors.Add("Mã khuyến mãi này đã tồn tại. Vui lòng chọn mã khác.");
            }

            // Validate dates
            if (request.EndDate.HasValue && request.EndDate.Value <= request.StartDate)
            {
                errors.Add("Ngày kết thúc phải sau ngày bắt đầu.");
            }

            // Validate discount logic
            var discountType = (request.DiscountType ?? string.Empty).ToUpper();
            if (discountType == "PERCENT")
            {
                if (request.DiscountValue <= 0 || request.DiscountValue > 100)
                    errors.Add("Giá trị giảm giá phần trăm phải trong khoảng 0 < giá trị ≤ 100.");
                if (request.MaxDiscount.HasValue && request.MaxDiscount.Value < 0)
                    errors.Add("Giảm tối đa (maxDiscount) phải ≥ 0.");
            }
            else if (discountType == "FIXED")
            {
                if (request.DiscountValue <= 0)
                    errors.Add("Giá trị giảm giá cố định phải > 0.");
                if (request.MaxDiscount.HasValue && request.MaxDiscount.Value < 0)
                    errors.Add("Giảm tối đa (maxDiscount) phải ≥ 0.");
            }
            else
            {
                errors.Add("Loại giảm giá không hợp lệ. Chỉ hỗ trợ PERCENT hoặc FIXED.");
            }

            // Validate min order amount
            if (request.MinOrderAmount.HasValue && request.MinOrderAmount.Value < 0)
                errors.Add("Giá trị đơn hàng tối thiểu (minOrderAmount) phải ≥ 0.");

            // Validate usage limit không nhỏ hơn usageCount hiện tại
            if (request.UsageLimit.HasValue)
            {
                var existing = await _promotionRepository.GetPromotionByIdAsync(promotionId);
                if (existing != null && request.UsageLimit.Value < existing.UsageCount)
                    errors.Add("Giới hạn lượt sử dụng (usageLimit) không được nhỏ hơn số lượt đã dùng (usageCount) hiện tại.");
                if (request.UsageLimit.Value <= 0)
                    errors.Add("Giới hạn lượt sử dụng (usageLimit) phải > 0 nếu được cung cấp.");
            }

            if (errors.Any())
                throw new ArgumentException(string.Join(" ", errors));
        }
    }
}
