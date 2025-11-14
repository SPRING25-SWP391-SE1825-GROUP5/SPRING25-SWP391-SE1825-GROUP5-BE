using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using EVServiceCenter.Application.Configurations;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace EVServiceCenter.Application.Service;

public class CartService : ICartService
{
    private readonly IDistributedCache _cache;
    private readonly IPartRepository _partRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly CartOptions _options;

    public CartService(
        IDistributedCache cache,
        IPartRepository partRepository,
        ICustomerRepository customerRepository,
        IOptions<CartOptions> options)
    {
        _cache = cache;
        _partRepository = partRepository;
        _customerRepository = customerRepository;
        _options = options.Value;
    }

    private string GetCartKey(int customerId) => $"{_options.KeyPrefix}{customerId}";

    public async Task<Cart> GetOrCreateCartAsync(int customerId)
    {
        var customer = await _customerRepository.GetCustomerByIdAsync(customerId);
        if (customer == null)
            throw new ArgumentException("Khách hàng không tồn tại");

        var cart = await GetCartAsync(customerId);
        if (cart != null)
            return cart;

        cart = new Cart
        {
            CustomerId = customerId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Items = new List<CartItem>()
        };

        await SaveCartAsync(cart);
        return cart;
    }

    public async Task<Cart?> GetCartAsync(int customerId)
    {
        var cacheKey = GetCartKey(customerId);

        for (int attempt = 1; attempt <= _options.RetryAttempts; attempt++)
        {
            try
            {
                var cartJson = await _cache.GetStringAsync(cacheKey);
                if (string.IsNullOrEmpty(cartJson))
                {
                    return null;
                }

                var cart = JsonSerializer.Deserialize<Cart>(cartJson);
                return cart;
            }
            catch (Exception)
            {
                if (attempt == _options.RetryAttempts)
                {
                    return null;
                }

                await Task.Delay(_options.RetryDelayMs * attempt);
            }
        }

        return null;
    }

    public async Task<List<CartItem>> GetCartItemsAsync(int customerId)
    {
        var cart = await GetCartAsync(customerId);
        return cart?.Items ?? new List<CartItem>();
    }

    public async Task<Cart> AddItemToCartAsync(int customerId, int partId, int quantity, int? fulfillmentCenterId = null)
    {
        if (quantity <= 0)
            throw new ArgumentException("Số lượng phải lớn hơn 0");

        var part = await _partRepository.GetPartByIdAsync(partId);
        if (part == null)
            throw new ArgumentException("Phụ tùng không tồn tại");
        if (!part.IsActive)
            throw new ArgumentException($"Sản phẩm {part.PartName} đã ngưng hoạt động");

        var cart = await GetOrCreateCartAsync(customerId);

        if (fulfillmentCenterId.HasValue)
        {
            cart.FulfillmentCenterId = fulfillmentCenterId.Value;
        }

        var existingItem = cart.Items.FirstOrDefault(item => item.PartId == partId);
        if (existingItem != null)
        {
            existingItem.Quantity += quantity;
        }
        else
        {
            cart.Items.Add(new CartItem
            {
                PartId = part.PartId,
                PartName = part.PartName,
                UnitPrice = part.Price,
                Quantity = quantity
            });
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await SaveCartAsync(cart);
        return cart;
    }

    public async Task<Cart> UpdateCartItemQuantityAsync(int customerId, int partId, int quantity)
    {
        if (quantity < 0)
            throw new ArgumentException("Số lượng không hợp lệ");

        var cart = await GetCartAsync(customerId);
        if (cart == null)
            throw new ArgumentException("Cart không tồn tại");

        var item = cart.Items.FirstOrDefault(i => i.PartId == partId);
        if (item == null)
            throw new ArgumentException("Item không tồn tại trong cart");

        if (quantity == 0)
        {
            cart.Items.Remove(item);
        }
        else
        {
            item.Quantity = quantity;
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await SaveCartAsync(cart);
        return cart;
    }

    public async Task<Cart> RemoveCartItemAsync(int customerId, int partId)
    {
        var cart = await GetCartAsync(customerId);
        if (cart == null)
            throw new ArgumentException("Cart không tồn tại");

        var item = cart.Items.FirstOrDefault(i => i.PartId == partId);
        if (item == null)
            throw new ArgumentException("Item không tồn tại trong cart");

        cart.Items.Remove(item);
        cart.UpdatedAt = DateTime.UtcNow;
        await SaveCartAsync(cart);
        return cart;
    }

    public async Task<Cart> ClearCartAsync(int customerId)
    {
        var cart = await GetCartAsync(customerId);
        if (cart == null)
            throw new ArgumentException("Cart không tồn tại");

        cart.Items.Clear();
        cart.UpdatedAt = DateTime.UtcNow;
        await SaveCartAsync(cart);
        return cart;
    }

    public async Task<bool> CartExistsAsync(int customerId)
    {
        var cart = await GetCartAsync(customerId);
        return cart != null;
    }

    public async Task<Cart> UpdateFulfillmentCenterAsync(int customerId, int? fulfillmentCenterId)
    {
        var cart = await GetCartAsync(customerId);
        if (cart == null)
            throw new ArgumentException("Giỏ hàng không tồn tại");

        cart.FulfillmentCenterId = fulfillmentCenterId;
        cart.UpdatedAt = DateTime.UtcNow;
        await SaveCartAsync(cart);
        return cart;
    }

    private async Task SaveCartAsync(Cart cart)
    {
        var cacheKey = GetCartKey(cart.CustomerId);
        var cartJson = JsonSerializer.Serialize(cart);

        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(_options.TtlDays)
        };

        for (int attempt = 1; attempt <= _options.RetryAttempts; attempt++)
        {
            try
            {
                await _cache.SetStringAsync(cacheKey, cartJson, cacheOptions);
                return;
            }
            catch (Exception ex)
            {
                if (attempt == _options.RetryAttempts)
                {
                    throw new InvalidOperationException($"Không thể lưu cart vào cache: {ex.Message}", ex);
                }

                await Task.Delay(_options.RetryDelayMs * attempt);
            }
        }
    }
}

