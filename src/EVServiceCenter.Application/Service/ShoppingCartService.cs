using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;

namespace EVServiceCenter.Application.Service;

public class ShoppingCartService : IShoppingCartService
{
    private readonly IShoppingCartRepository _shoppingCartRepository;
    private readonly IPartRepository _partRepository;
    private readonly ICustomerRepository _customerRepository;

    public ShoppingCartService(
        IShoppingCartRepository shoppingCartRepository,
        IPartRepository partRepository,
        ICustomerRepository customerRepository)
    {
        _shoppingCartRepository = shoppingCartRepository;
        _partRepository = partRepository;
        _customerRepository = customerRepository;
    }

    public async Task<List<ShoppingCartResponse>> GetByCustomerIdAsync(int customerId)
    {
        var shoppingCarts = await _shoppingCartRepository.GetByCustomerIdAsync(customerId);
        return shoppingCarts.Select(MapToResponse).ToList();
    }

    public async Task<ShoppingCartResponse?> GetByIdAsync(int cartId)
    {
        var shoppingCart = await _shoppingCartRepository.GetByIdAsync(cartId);
        return shoppingCart != null ? MapToResponse(shoppingCart) : null;
    }

    public async Task<ShoppingCartResponse> AddToCartAsync(AddToCartRequest request)
    {
        // Kiểm tra sản phẩm có tồn tại không
        var part = await _partRepository.GetPartLiteByIdAsync(request.PartId);
        if (part == null)
            throw new ArgumentException("Sản phẩm không tồn tại");

        if (!part.IsActive)
            throw new ArgumentException("Sản phẩm không còn hoạt động");

        // Kiểm tra khách hàng có tồn tại không
        var customer = await _customerRepository.GetCustomerByIdAsync(request.CustomerId);
        if (customer == null)
            throw new ArgumentException("Khách hàng không tồn tại");

        // Kiểm tra sản phẩm đã có trong giỏ hàng chưa
        var existingCartItem = await _shoppingCartRepository.GetByCustomerAndPartAsync(request.CustomerId, request.PartId);
        
        if (existingCartItem != null)
        {
            // Cập nhật số lượng
            existingCartItem.Quantity += request.Quantity;
            existingCartItem.UpdatedAt = DateTime.UtcNow;
            var updatedCartItem = await _shoppingCartRepository.UpdateAsync(existingCartItem);
            return MapToResponse(updatedCartItem);
        }
        else
        {
            // Tạo mới
            var shoppingCart = new ShoppingCart
            {
                CustomerId = request.CustomerId,
                PartId = request.PartId,
                Quantity = request.Quantity,
                UnitPrice = part.UnitPrice,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var addedCartItem = await _shoppingCartRepository.AddAsync(shoppingCart);
            return MapToResponse(addedCartItem);
        }
    }

    public async Task<ShoppingCartResponse> UpdateCartItemAsync(int cartId, UpdateCartItemRequest request)
    {
        var shoppingCart = await _shoppingCartRepository.GetByIdAsync(cartId);
        if (shoppingCart == null)
            throw new ArgumentException("Mục giỏ hàng không tồn tại");

        shoppingCart.Quantity = request.Quantity;
        shoppingCart.UpdatedAt = DateTime.UtcNow;

        var updatedCartItem = await _shoppingCartRepository.UpdateAsync(shoppingCart);
        return MapToResponse(updatedCartItem);
    }

    public async Task<ShoppingCartResponse> UpdateCartItemByCustomerAndPartAsync(int customerId, int partId, int quantity)
    {
        if (customerId <= 0 || partId <= 0) throw new ArgumentException("Thiếu customerId/partId");
        if (quantity <= 0) throw new ArgumentException("Số lượng phải > 0");

        var cart = await _shoppingCartRepository.GetByCustomerAndPartAsync(customerId, partId);
        if (cart == null) throw new ArgumentException("Mục giỏ hàng không tồn tại");

        cart.Quantity = quantity;
        cart.UpdatedAt = DateTime.UtcNow;
        cart = await _shoppingCartRepository.UpdateAsync(cart);
        return MapToResponse(cart);
    }

    public async Task DeleteCartItemAsync(int cartId)
    {
        if (!await _shoppingCartRepository.ExistsAsync(cartId))
            throw new ArgumentException("Mục giỏ hàng không tồn tại");

        await _shoppingCartRepository.DeleteAsync(cartId);
    }

    public async Task DeleteCartItemByCustomerAndPartAsync(int customerId, int partId)
    {
        var cart = await _shoppingCartRepository.GetByCustomerAndPartAsync(customerId, partId);
        if (cart == null) return;
        await _shoppingCartRepository.DeleteAsync(cart.CartId);
    }

    public async Task ClearCartAsync(int customerId)
    {
        await _shoppingCartRepository.DeleteByCustomerIdAsync(customerId);
    }

    public async Task<bool> ExistsAsync(int cartId)
    {
        return await _shoppingCartRepository.ExistsAsync(cartId);
    }

    private ShoppingCartResponse MapToResponse(ShoppingCart shoppingCart)
    {
        return new ShoppingCartResponse
        {
            CartId = shoppingCart.CartId,
            CustomerId = shoppingCart.CustomerId,
            CustomerName = shoppingCart.Customer?.User?.FullName ?? "Khách hàng",
            PartId = shoppingCart.PartId,
            PartName = shoppingCart.Part?.PartName ?? "",
            PartNumber = shoppingCart.Part?.PartNumber ?? "",
            Brand = shoppingCart.Part?.Brand ?? "",
            Quantity = shoppingCart.Quantity,
            UnitPrice = shoppingCart.UnitPrice,
            LineTotal = shoppingCart.Quantity * shoppingCart.UnitPrice,
            CreatedAt = shoppingCart.CreatedAt,
            UpdatedAt = shoppingCart.UpdatedAt
        };
    }
}
