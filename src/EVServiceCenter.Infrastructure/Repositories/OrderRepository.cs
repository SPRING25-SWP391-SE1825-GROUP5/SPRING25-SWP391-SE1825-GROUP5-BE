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
        // OrderNumber is deprecated; try parse id fallback
        if (int.TryParse(orderNumber?.Replace("ORD-#", string.Empty), out var id))
        {
            return await GetByIdAsync(id);
        }
        return await GetByIdAsync(-1);
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
        if (int.TryParse(orderNumber?.Replace("ORD-#", string.Empty), out var id))
        {
            return await _context.Orders.AnyAsync(o => o.OrderId == id);
        }
        return false;
    }

    public async Task<string> GenerateOrderNumberAsync()
    {
        // Deprecated: return placeholder using OrderId sequence semantics
        var nextId = await _context.Orders.MaxAsync(o => (int?)o.OrderId) ?? 0;
        return $"ORD-#{nextId + 1}";
    }

    public async Task<List<Order>> GetOrdersByCustomerIdAsync(int customerId, int page = 1, int pageSize = 10, 
        string? status = null, DateTime? fromDate = null, DateTime? toDate = null, 
        string sortBy = "orderDate", string sortOrder = "desc")
    {
        var query = _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Part)
            .Where(o => o.CustomerId == customerId);

        // Apply filters
        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(o => o.Status == status);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt <= toDate.Value);
        }

        // Apply sorting
        switch (sortBy.ToLower())
        {
            case "orderdate":
                query = sortOrder.ToLower() == "asc" 
                    ? query.OrderBy(o => o.CreatedAt)
                    : query.OrderByDescending(o => o.CreatedAt);
                break;
            case "createdat":
                query = sortOrder.ToLower() == "asc" 
                    ? query.OrderBy(o => o.CreatedAt)
                    : query.OrderByDescending(o => o.CreatedAt);
                break;
            case "totalamount":
                query = sortOrder.ToLower() == "asc" 
                    ? query.OrderBy(o => o.OrderItems.Sum(oi => oi.Quantity * oi.UnitPrice))
                    : query.OrderByDescending(o => o.OrderItems.Sum(oi => oi.Quantity * oi.UnitPrice));
                break;
            default:
                query = query.OrderByDescending(o => o.CreatedAt);
                break;
        }

        // Apply pagination
        return await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountOrdersByCustomerIdAsync(int customerId, string? status = null, 
        DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.Orders.Where(o => o.CustomerId == customerId);

        // Apply filters
        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(o => o.Status == status);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt <= toDate.Value);
        }

        return await query.CountAsync();
    }

    public async Task<Order?> GetOrderWithDetailsByIdAsync(int orderId)
    {
        return await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Part)
            .Include(o => o.OrderStatusHistories)
            .Include(o => o.Invoices)
                .ThenInclude(i => i.Payments)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);
    }
}
