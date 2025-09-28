using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;

namespace EVServiceCenter.Application.Interfaces;

public interface IShoppingCartService
{
    Task<List<ShoppingCartResponse>> GetByCustomerIdAsync(int customerId);
    Task<ShoppingCartResponse?> GetByIdAsync(int cartId);
    Task<ShoppingCartResponse> AddToCartAsync(AddToCartRequest request);
    Task<ShoppingCartResponse> UpdateCartItemAsync(int cartId, UpdateCartItemRequest request);
    Task DeleteCartItemAsync(int cartId);
    Task ClearCartAsync(int customerId);
    Task<bool> ExistsAsync(int cartId);
}
