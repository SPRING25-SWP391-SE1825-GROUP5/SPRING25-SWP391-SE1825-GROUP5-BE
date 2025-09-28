using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces;

public interface IWishlistRepository
{
    Task<List<Wishlist>> GetByCustomerIdAsync(int customerId);
    Task<Wishlist?> GetByIdAsync(int wishlistId);
    Task<Wishlist?> GetByCustomerAndPartAsync(int customerId, int partId);
    Task<Wishlist> AddAsync(Wishlist wishlist);
    Task DeleteAsync(int wishlistId);
    Task DeleteByCustomerAndPartAsync(int customerId, int partId);
    Task<bool> ExistsAsync(int wishlistId);
    Task<bool> ExistsByCustomerAndPartAsync(int customerId, int partId);
}
