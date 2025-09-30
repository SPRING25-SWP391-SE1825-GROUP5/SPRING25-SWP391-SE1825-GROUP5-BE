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
    Task<List<OrderStatusHistoryResponse>> GetStatusHistoryAsync(int orderId);
}
