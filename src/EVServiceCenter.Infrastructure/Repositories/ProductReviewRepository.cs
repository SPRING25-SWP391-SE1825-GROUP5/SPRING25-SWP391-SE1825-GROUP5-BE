using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using EVServiceCenter.Domain.Configurations;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Infrastructure.Repositories;

public class ProductReviewRepository : IProductReviewRepository
{
    private readonly EVDbContext _context;

    public ProductReviewRepository(EVDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProductReview>> GetByPartIdAsync(int partId)
    {
        return await _context.ProductReviews
            .Include(pr => pr.Customer)
                .ThenInclude(c => c.User)
            .Include(pr => pr.Part)
            .Where(pr => pr.PartId == partId)
            .OrderByDescending(pr => pr.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<ProductReview>> GetByCustomerIdAsync(int customerId)
    {
        return await _context.ProductReviews
            .Include(pr => pr.Part)
            .Include(pr => pr.Order)
            .Where(pr => pr.CustomerId == customerId)
            .OrderByDescending(pr => pr.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<ProductReview>> GetByOrderIdAsync(int orderId)
    {
        return await _context.ProductReviews
            .Include(pr => pr.Customer)
            .Include(pr => pr.Part)
            .Where(pr => pr.OrderId == orderId)
            .OrderByDescending(pr => pr.CreatedAt)
            .ToListAsync();
    }

    public async Task<ProductReview?> GetByIdAsync(int reviewId)
    {
        return await _context.ProductReviews
            .Include(pr => pr.Customer)
                .ThenInclude(c => c.User)
            .Include(pr => pr.Part)
            .Include(pr => pr.Order)
            .FirstOrDefaultAsync(pr => pr.ReviewId == reviewId);
    }

    public async Task<ProductReview> AddAsync(ProductReview productReview)
    {
        _context.ProductReviews.Add(productReview);
        await _context.SaveChangesAsync();
        return productReview;
    }

    public async Task<ProductReview> UpdateAsync(ProductReview productReview)
    {
        _context.ProductReviews.Update(productReview);
        await _context.SaveChangesAsync();
        return productReview;
    }

    public async Task DeleteAsync(int reviewId)
    {
        var productReview = await _context.ProductReviews.FindAsync(reviewId);
        if (productReview != null)
        {
            _context.ProductReviews.Remove(productReview);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(int reviewId)
    {
        return await _context.ProductReviews.AnyAsync(pr => pr.ReviewId == reviewId);
    }

    public async Task<bool> ExistsByCustomerAndPartAsync(int customerId, int partId)
    {
        return await _context.ProductReviews.AnyAsync(pr => pr.CustomerId == customerId && pr.PartId == partId);
    }

    public async Task<bool> ExistsByCustomerAndOrderAsync(int customerId, int orderId)
    {
        return await _context.ProductReviews.AnyAsync(pr => pr.CustomerId == customerId && pr.OrderId == orderId);
    }
}
