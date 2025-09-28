using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces;

public interface IOrderStatusHistoryRepository
{
    Task<List<OrderStatusHistory>> GetByOrderIdAsync(int orderId);
    Task<OrderStatusHistory> AddAsync(OrderStatusHistory orderStatusHistory);
    Task<List<OrderStatusHistory>> AddRangeAsync(List<OrderStatusHistory> orderStatusHistories);
}
