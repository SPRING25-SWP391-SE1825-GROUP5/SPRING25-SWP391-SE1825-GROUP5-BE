using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;

namespace EVServiceCenter.Application.Service;

public class ProductReviewService : IProductReviewService
{
    private readonly IProductReviewRepository _productReviewRepository;
    private readonly IPartRepository _partRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IOrderRepository _orderRepository;

    public ProductReviewService(
        IProductReviewRepository productReviewRepository,
        IPartRepository partRepository,
        ICustomerRepository customerRepository,
        IOrderRepository orderRepository)
    {
        _productReviewRepository = productReviewRepository;
        _partRepository = partRepository;
        _customerRepository = customerRepository;
        _orderRepository = orderRepository;
    }

    public async Task<List<ProductReviewResponse>> GetByPartIdAsync(int partId)
    {
        var reviews = await _productReviewRepository.GetByPartIdAsync(partId);
        return reviews.Select(MapToResponse).ToList();
    }

    public async Task<List<ProductReviewResponse>> GetByCustomerIdAsync(int customerId)
    {
        var reviews = await _productReviewRepository.GetByCustomerIdAsync(customerId);
        return reviews.Select(MapToResponse).ToList();
    }

    public async Task<List<ProductReviewResponse>> GetByOrderIdAsync(int orderId)
    {
        var reviews = await _productReviewRepository.GetByOrderIdAsync(orderId);
        return reviews.Select(MapToResponse).ToList();
    }

    public async Task<ProductReviewResponse?> GetByIdAsync(int reviewId)
    {
        var review = await _productReviewRepository.GetByIdAsync(reviewId);
        return review != null ? MapToResponse(review) : null;
    }

    public async Task<ProductReviewResponse> CreateReviewAsync(CreateProductReviewRequest request)
    {
        // Kiểm tra sản phẩm có tồn tại không
        var part = await _partRepository.GetPartByIdAsync(request.PartId);
        if (part == null)
            throw new ArgumentException("Sản phẩm không tồn tại");

        if (!part.IsActive)
            throw new ArgumentException("Sản phẩm không còn hoạt động");

        // Kiểm tra khách hàng có tồn tại không
        var customer = await _customerRepository.GetCustomerByIdAsync(request.CustomerId);
        if (customer == null)
            throw new ArgumentException("Khách hàng không tồn tại");

        // Kiểm tra đơn hàng có tồn tại không (nếu có)
        if (request.OrderId.HasValue)
        {
            var order = await _orderRepository.GetByIdAsync(request.OrderId.Value);
            if (order == null)
                throw new ArgumentException("Đơn hàng không tồn tại");

            if (order.CustomerId != request.CustomerId)
                throw new ArgumentException("Đơn hàng không thuộc về khách hàng này");

            // Kiểm tra đã đánh giá sản phẩm trong đơn hàng này chưa
            if (await _productReviewRepository.ExistsByCustomerAndOrderAsync(request.CustomerId, request.OrderId.Value))
                throw new ArgumentException("Bạn đã đánh giá sản phẩm trong đơn hàng này");
        }

        // Kiểm tra đã đánh giá sản phẩm này chưa (nếu không có orderId)
        if (!request.OrderId.HasValue)
        {
            if (await _productReviewRepository.ExistsByCustomerAndPartAsync(request.CustomerId, request.PartId))
                throw new ArgumentException("Bạn đã đánh giá sản phẩm này");
        }

        // Tạo đánh giá
        var productReview = new ProductReview
        {
            PartId = request.PartId,
            CustomerId = request.CustomerId,
            OrderId = request.OrderId,
            Rating = request.Rating,
            Comment = request.Comment,
            IsVerified = request.OrderId.HasValue, // Đánh giá có đơn hàng được coi là verified
            CreatedAt = DateTime.UtcNow
        };

        var createdReview = await _productReviewRepository.AddAsync(productReview);
        return MapToResponse(createdReview);
    }

    public async Task<ProductReviewResponse> UpdateReviewAsync(int reviewId, UpdateProductReviewRequest request)
    {
        var review = await _productReviewRepository.GetByIdAsync(reviewId);
        if (review == null)
            throw new ArgumentException("Đánh giá không tồn tại");

        review.Rating = request.Rating;
        review.Comment = request.Comment;

        var updatedReview = await _productReviewRepository.UpdateAsync(review);
        return MapToResponse(updatedReview);
    }

    public async Task DeleteReviewAsync(int reviewId)
    {
        if (!await _productReviewRepository.ExistsAsync(reviewId))
            throw new ArgumentException("Đánh giá không tồn tại");

        await _productReviewRepository.DeleteAsync(reviewId);
    }

    public async Task<bool> ExistsAsync(int reviewId)
    {
        return await _productReviewRepository.ExistsAsync(reviewId);
    }

    public async Task<bool> ExistsByCustomerAndPartAsync(int customerId, int partId)
    {
        return await _productReviewRepository.ExistsByCustomerAndPartAsync(customerId, partId);
    }

    public async Task<bool> ExistsByCustomerAndOrderAsync(int customerId, int orderId)
    {
        return await _productReviewRepository.ExistsByCustomerAndOrderAsync(customerId, orderId);
    }

    private ProductReviewResponse MapToResponse(ProductReview review)
    {
        return new ProductReviewResponse
        {
            ReviewId = review.ReviewId,
            PartId = review.PartId,
            PartName = review.Part?.PartName ?? "",
            PartNumber = review.Part?.PartNumber ?? "",
            CustomerId = review.CustomerId,
            CustomerName = review.Customer?.User?.FullName ?? "Khách hàng",
            OrderId = review.OrderId,
            OrderNumber = review.Order?.OrderNumber,
            Rating = review.Rating,
            Comment = review.Comment,
            IsVerified = review.IsVerified,
            CreatedAt = review.CreatedAt
        };
    }
}
