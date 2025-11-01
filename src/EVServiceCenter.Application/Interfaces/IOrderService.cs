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

    // Cart operations (Order with Status = "CART")
    Task<OrderResponse> GetOrCreateCartAsync(int customerId);
    Task<List<OrderItemSimpleResponse>> GetCartItemsAsync(int cartOrderId);
    Task<OrderResponse> AddItemToCartAsync(int cartOrderId, int partId, int quantity);
    Task<OrderResponse> UpdateCartItemQuantityAsync(int cartOrderId, int orderItemId, int quantity);
    Task<OrderResponse> RemoveCartItemAsync(int cartOrderId, int orderItemId);
    Task<OrderResponse> ClearCartAsync(int cartOrderId);
    Task<OrderResponse> CheckoutCartAsync(int cartOrderId);
}
