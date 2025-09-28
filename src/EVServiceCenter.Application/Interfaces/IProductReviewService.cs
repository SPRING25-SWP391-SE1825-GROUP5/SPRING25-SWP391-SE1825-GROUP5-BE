using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;

namespace EVServiceCenter.Application.Interfaces;

public interface IProductReviewService
{
    Task<List<ProductReviewResponse>> GetByPartIdAsync(int partId);
    Task<List<ProductReviewResponse>> GetByCustomerIdAsync(int customerId);
    Task<List<ProductReviewResponse>> GetByOrderIdAsync(int orderId);
    Task<ProductReviewResponse?> GetByIdAsync(int reviewId);
    Task<ProductReviewResponse> CreateReviewAsync(CreateProductReviewRequest request);
    Task<ProductReviewResponse> UpdateReviewAsync(int reviewId, UpdateProductReviewRequest request);
    Task DeleteReviewAsync(int reviewId);
    Task<bool> ExistsAsync(int reviewId);
    Task<bool> ExistsByCustomerAndPartAsync(int customerId, int partId);
    Task<bool> ExistsByCustomerAndOrderAsync(int customerId, int orderId);
}
