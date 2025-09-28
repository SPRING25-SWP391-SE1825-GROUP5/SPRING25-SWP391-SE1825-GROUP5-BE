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
}
