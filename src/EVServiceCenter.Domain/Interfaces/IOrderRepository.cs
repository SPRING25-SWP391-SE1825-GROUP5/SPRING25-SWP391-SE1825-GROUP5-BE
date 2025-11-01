using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces;

public interface IOrderRepository
{
    Task<List<Order>> GetByCustomerIdAsync(int customerId);
    Task<Order?> GetByIdAsync(int orderId);
    Task<Order?> GetByOrderNumberAsync(string orderNumber);
    Task<List<Order>> GetAllAsync();
    Task<Order> AddAsync(Order order);
    Task<Order> UpdateAsync(Order order);
    Task DeleteAsync(int orderId);
    Task<bool> ExistsAsync(int orderId);
    Task<bool> ExistsByOrderNumberAsync(string orderNumber);
    Task<string> GenerateOrderNumberAsync();
    Task<List<Order>> GetOrdersByCustomerIdAsync(int customerId, int page = 1, int pageSize = 10, 
        string? status = null, DateTime? fromDate = null, DateTime? toDate = null, 
        string sortBy = "orderDate", string sortOrder = "desc");
    Task<int> CountOrdersByCustomerIdAsync(int customerId, string? status = null, 
        DateTime? fromDate = null, DateTime? toDate = null);
    Task<Order?> GetOrderWithDetailsByIdAsync(int orderId);

    // Cart helpers
    Task<Order?> GetCartByCustomerIdAsync(int customerId);
    Task<OrderItem?> FindItemAsync(int orderId, int partId);
}
