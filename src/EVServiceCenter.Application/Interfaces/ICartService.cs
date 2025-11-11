using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Application.Models;

namespace EVServiceCenter.Application.Interfaces;

public interface ICartService
{
    Task<Cart> GetOrCreateCartAsync(int customerId);
    Task<Cart?> GetCartAsync(int customerId);
    Task<List<CartItem>> GetCartItemsAsync(int customerId);
    Task<Cart> AddItemToCartAsync(int customerId, int partId, int quantity, int? fulfillmentCenterId = null);
    Task<Cart> UpdateCartItemQuantityAsync(int customerId, int partId, int quantity);
    Task<Cart> RemoveCartItemAsync(int customerId, int partId);
    Task<Cart> ClearCartAsync(int customerId);
    Task<bool> CartExistsAsync(int customerId);
    Task<Cart> UpdateFulfillmentCenterAsync(int customerId, int? fulfillmentCenterId);
}

