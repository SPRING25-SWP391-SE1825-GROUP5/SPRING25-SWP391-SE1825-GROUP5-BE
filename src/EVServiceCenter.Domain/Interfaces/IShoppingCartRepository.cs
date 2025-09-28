using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces;

public interface IShoppingCartRepository
{
    Task<List<ShoppingCart>> GetByCustomerIdAsync(int customerId);
    Task<ShoppingCart?> GetByIdAsync(int cartId);
    Task<ShoppingCart?> GetByCustomerAndPartAsync(int customerId, int partId);
    Task<ShoppingCart> AddAsync(ShoppingCart shoppingCart);
    Task<ShoppingCart> UpdateAsync(ShoppingCart shoppingCart);
    Task DeleteAsync(int cartId);
    Task DeleteByCustomerIdAsync(int customerId);
    Task<bool> ExistsAsync(int cartId);
    Task<bool> ExistsByCustomerAndPartAsync(int customerId, int partId);
}
