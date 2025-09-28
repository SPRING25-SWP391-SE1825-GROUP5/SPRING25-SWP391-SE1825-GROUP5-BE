using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using EVServiceCenter.Domain.Configurations;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Infrastructure.Repositories;

public class OrderStatusHistoryRepository : IOrderStatusHistoryRepository
{
    private readonly EVDbContext _context;

    public OrderStatusHistoryRepository(EVDbContext context)
    {
        _context = context;
    }

    public async Task<List<OrderStatusHistory>> GetByOrderIdAsync(int orderId)
    {
        return await _context.OrderStatusHistories
            .Include(osh => osh.CreatedByUser)
            .Where(osh => osh.OrderId == orderId)
            .OrderBy(osh => osh.CreatedAt)
            .ToListAsync();
    }

    public async Task<OrderStatusHistory> AddAsync(OrderStatusHistory orderStatusHistory)
    {
        _context.OrderStatusHistories.Add(orderStatusHistory);
        await _context.SaveChangesAsync();
        return orderStatusHistory;
    }

    public async Task<List<OrderStatusHistory>> AddRangeAsync(List<OrderStatusHistory> orderStatusHistories)
    {
        _context.OrderStatusHistories.AddRange(orderStatusHistories);
        await _context.SaveChangesAsync();
        return orderStatusHistories;
    }
}
