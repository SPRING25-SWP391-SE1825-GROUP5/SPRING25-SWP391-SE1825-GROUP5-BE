using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using EVServiceCenter.Domain.Configurations;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly EVDbContext _context;

    public OrderRepository(EVDbContext context)
    {
        _context = context;
    }

    public async Task<List<Order>> GetByCustomerIdAsync(int customerId)
    {
        return await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Part)
            .Include(o => o.OrderStatusHistories)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<Order?> GetByIdAsync(int orderId)
    {
        return await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Part)
            .Include(o => o.OrderStatusHistories)
                .ThenInclude(osh => osh.CreatedByUser)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);
    }

    public async Task<Order?> GetByOrderNumberAsync(string orderNumber)
    {
        return await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Part)
            .Include(o => o.OrderStatusHistories)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
    }

    public async Task<List<Order>> GetAllAsync()
    {
        return await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Part)
            .Include(o => o.OrderStatusHistories)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<Order> AddAsync(Order order)
    {
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        return order;
    }

    public async Task<Order> UpdateAsync(Order order)
    {
        _context.Orders.Update(order);
        await _context.SaveChangesAsync();
        return order;
    }

    public async Task DeleteAsync(int orderId)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order != null)
        {
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(int orderId)
    {
        return await _context.Orders.AnyAsync(o => o.OrderId == orderId);
    }

    public async Task<bool> ExistsByOrderNumberAsync(string orderNumber)
    {
        return await _context.Orders.AnyAsync(o => o.OrderNumber == orderNumber);
    }

    public async Task<string> GenerateOrderNumberAsync()
    {
        var today = DateTime.Now.ToString("yyyyMMdd");
        var count = await _context.Orders
            .Where(o => o.OrderNumber.StartsWith($"ORD-{today}"))
            .CountAsync();
        
        return $"ORD-{today}-{(count + 1):D3}";
    }
}
