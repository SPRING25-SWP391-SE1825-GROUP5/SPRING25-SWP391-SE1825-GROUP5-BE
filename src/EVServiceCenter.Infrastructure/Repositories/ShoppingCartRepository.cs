using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using EVServiceCenter.Domain.Configurations;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Infrastructure.Repositories;

public class ShoppingCartRepository : IShoppingCartRepository
{
    private readonly EVDbContext _context;

    public ShoppingCartRepository(EVDbContext context)
    {
        _context = context;
    }

    public async Task<List<ShoppingCart>> GetByCustomerIdAsync(int customerId)
    {
        return await _context.ShoppingCarts
            .Include(sc => sc.Part)
            .Where(sc => sc.CustomerId == customerId)
            .OrderBy(sc => sc.CreatedAt)
            .ToListAsync();
    }

    public async Task<ShoppingCart?> GetByIdAsync(int cartId)
    {
        return await _context.ShoppingCarts
            .Include(sc => sc.Part)
            .Include(sc => sc.Customer)
            .FirstOrDefaultAsync(sc => sc.CartId == cartId);
    }

    public async Task<ShoppingCart?> GetByCustomerAndPartAsync(int customerId, int partId)
    {
        return await _context.ShoppingCarts
            .Include(sc => sc.Part)
            .FirstOrDefaultAsync(sc => sc.CustomerId == customerId && sc.PartId == partId);
    }

    public async Task<ShoppingCart> AddAsync(ShoppingCart shoppingCart)
    {
        _context.ShoppingCarts.Add(shoppingCart);
        await _context.SaveChangesAsync();
        return shoppingCart;
    }

    public async Task<ShoppingCart> UpdateAsync(ShoppingCart shoppingCart)
    {
        _context.ShoppingCarts.Update(shoppingCart);
        await _context.SaveChangesAsync();
        return shoppingCart;
    }

    public async Task DeleteAsync(int cartId)
    {
        var shoppingCart = await _context.ShoppingCarts.FindAsync(cartId);
        if (shoppingCart != null)
        {
            _context.ShoppingCarts.Remove(shoppingCart);
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteByCustomerIdAsync(int customerId)
    {
        var shoppingCarts = await _context.ShoppingCarts
            .Where(sc => sc.CustomerId == customerId)
            .ToListAsync();
        
        if (shoppingCarts.Any())
        {
            _context.ShoppingCarts.RemoveRange(shoppingCarts);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(int cartId)
    {
        return await _context.ShoppingCarts.AnyAsync(sc => sc.CartId == cartId);
    }

    public async Task<bool> ExistsByCustomerAndPartAsync(int customerId, int partId)
    {
        return await _context.ShoppingCarts.AnyAsync(sc => sc.CustomerId == customerId && sc.PartId == partId);
    }
}
