using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces;

public interface IProductReviewRepository
{
    Task<List<ProductReview>> GetByPartIdAsync(int partId);
    Task<List<ProductReview>> GetByCustomerIdAsync(int customerId);
    Task<List<ProductReview>> GetByOrderIdAsync(int orderId);
    Task<ProductReview?> GetByIdAsync(int reviewId);
    Task<ProductReview> AddAsync(ProductReview productReview);
    Task<ProductReview> UpdateAsync(ProductReview productReview);
    Task DeleteAsync(int reviewId);
    Task<bool> ExistsAsync(int reviewId);
    Task<bool> ExistsByCustomerAndPartAsync(int customerId, int partId);
    Task<bool> ExistsByCustomerAndOrderAsync(int customerId, int orderId);
}
