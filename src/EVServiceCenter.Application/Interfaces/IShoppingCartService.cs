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
    Task<ShoppingCartResponse> UpdateCartItemByCustomerAndPartAsync(int customerId, int partId, int quantity);
    Task DeleteCartItemByCustomerAndPartAsync(int customerId, int partId);
    Task ClearCartAsync(int customerId);
    Task<bool> ExistsAsync(int cartId);
}
