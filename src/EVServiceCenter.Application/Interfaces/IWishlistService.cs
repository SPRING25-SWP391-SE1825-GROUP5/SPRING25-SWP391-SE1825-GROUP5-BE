using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;

namespace EVServiceCenter.Application.Interfaces;

public interface IWishlistService
{
    Task<List<WishlistResponse>> GetByCustomerIdAsync(int customerId);
    Task<WishlistResponse?> GetByIdAsync(int wishlistId);
    Task<WishlistResponse> AddToWishlistAsync(AddToWishlistRequest request);
    Task DeleteFromWishlistAsync(int wishlistId);
    Task DeleteByCustomerAndPartAsync(int customerId, int partId);
    Task<bool> ExistsAsync(int wishlistId);
    Task<bool> ExistsByCustomerAndPartAsync(int customerId, int partId);
}
