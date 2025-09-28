using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using EVServiceCenter.Domain.Configurations;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Infrastructure.Repositories;

public class WishlistRepository : IWishlistRepository
{
    private readonly EVDbContext _context;

    public WishlistRepository(EVDbContext context)
    {
        _context = context;
    }

    public async Task<List<Wishlist>> GetByCustomerIdAsync(int customerId)
    {
        return await _context.Wishlists
            .Include(w => w.Part)
            .Where(w => w.CustomerId == customerId)
            .OrderBy(w => w.CreatedAt)
            .ToListAsync();
    }

    public async Task<Wishlist?> GetByIdAsync(int wishlistId)
    {
        return await _context.Wishlists
            .Include(w => w.Part)
            .Include(w => w.Customer)
            .FirstOrDefaultAsync(w => w.WishlistId == wishlistId);
    }

    public async Task<Wishlist?> GetByCustomerAndPartAsync(int customerId, int partId)
    {
        return await _context.Wishlists
            .Include(w => w.Part)
            .FirstOrDefaultAsync(w => w.CustomerId == customerId && w.PartId == partId);
    }

    public async Task<Wishlist> AddAsync(Wishlist wishlist)
    {
        _context.Wishlists.Add(wishlist);
        await _context.SaveChangesAsync();
        return wishlist;
    }

    public async Task DeleteAsync(int wishlistId)
    {
        var wishlist = await _context.Wishlists.FindAsync(wishlistId);
        if (wishlist != null)
        {
            _context.Wishlists.Remove(wishlist);
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteByCustomerAndPartAsync(int customerId, int partId)
    {
        var wishlist = await _context.Wishlists
            .FirstOrDefaultAsync(w => w.CustomerId == customerId && w.PartId == partId);
        
        if (wishlist != null)
        {
            _context.Wishlists.Remove(wishlist);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(int wishlistId)
    {
        return await _context.Wishlists.AnyAsync(w => w.WishlistId == wishlistId);
    }

    public async Task<bool> ExistsByCustomerAndPartAsync(int customerId, int partId)
    {
        return await _context.Wishlists.AnyAsync(w => w.CustomerId == customerId && w.PartId == partId);
    }
}
