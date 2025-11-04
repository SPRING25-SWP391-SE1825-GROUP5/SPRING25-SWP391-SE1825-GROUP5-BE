using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;

namespace EVServiceCenter.Application.Interfaces;

public interface IOrderService
{
    Task<List<OrderResponse>> GetByCustomerIdAsync(int customerId);
    Task<OrderResponse?> GetByIdAsync(int orderId);
    Task<List<OrderResponse>> GetAllAsync();
    Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request);
    Task<OrderResponse> CreateQuickOrderAsync(QuickOrderRequest request);
    Task<OrderResponse> UpdateOrderStatusAsync(int orderId, UpdateOrderStatusRequest request);
    Task DeleteOrderAsync(int orderId);
    Task<bool> ExistsAsync(int orderId);
    Task<List<OrderItemSimpleResponse>> GetItemsAsync(int orderId);

    [Obsolete("Use ICartService for cart operations. This method is kept for backward compatibility.")]
    Task<OrderResponse> GetOrCreateCartAsync(int customerId);
    [Obsolete("Use ICartService for cart operations.")]
    Task<List<OrderItemSimpleResponse>> GetCartItemsAsync(int cartOrderId);
    [Obsolete("Use ICartService for cart operations.")]
    Task<OrderResponse> AddItemToCartAsync(int cartOrderId, int partId, int quantity);
    [Obsolete("Use ICartService for cart operations.")]
    Task<OrderResponse> UpdateCartItemQuantityAsync(int cartOrderId, int orderItemId, int quantity);
    [Obsolete("Use ICartService for cart operations.")]
    Task<OrderResponse> RemoveCartItemAsync(int cartOrderId, int orderItemId);
    [Obsolete("Use ICartService for cart operations.")]
    Task<OrderResponse> ClearCartAsync(int cartOrderId);
    [Obsolete("Use ICartService for cart operations.")]
    Task<OrderResponse> CheckoutCartAsync(int cartOrderId);

    Task<OrderResponse> CheckoutCartFromRedisAsync(int customerId);
}
