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

public class WishlistService : IWishlistService
{
    private readonly IWishlistRepository _wishlistRepository;
    private readonly IPartRepository _partRepository;
    private readonly ICustomerRepository _customerRepository;

    public WishlistService(
        IWishlistRepository wishlistRepository,
        IPartRepository partRepository,
        ICustomerRepository customerRepository)
    {
        _wishlistRepository = wishlistRepository;
        _partRepository = partRepository;
        _customerRepository = customerRepository;
    }

    public async Task<List<WishlistResponse>> GetByCustomerIdAsync(int customerId)
    {
        var wishlists = await _wishlistRepository.GetByCustomerIdAsync(customerId);
        return wishlists.Select(MapToResponse).ToList();
    }

    public async Task<WishlistResponse?> GetByIdAsync(int wishlistId)
    {
        var wishlist = await _wishlistRepository.GetByIdAsync(wishlistId);
        return wishlist != null ? MapToResponse(wishlist) : null;
    }

    public async Task<WishlistResponse> AddToWishlistAsync(AddToWishlistRequest request)
    {
        // Kiểm tra sản phẩm có tồn tại không
        var part = await _partRepository.GetPartByIdAsync(request.PartId);
        if (part == null)
            throw new ArgumentException("Sản phẩm không tồn tại");

        if (!part.IsActive)
            throw new ArgumentException("Sản phẩm không còn hoạt động");

        // Kiểm tra khách hàng có tồn tại không
        var customer = await _customerRepository.GetCustomerByIdAsync(request.CustomerId);
        if (customer == null)
            throw new ArgumentException("Khách hàng không tồn tại");

        // Kiểm tra sản phẩm đã có trong wishlist chưa
        var existingWishlist = await _wishlistRepository.GetByCustomerAndPartAsync(request.CustomerId, request.PartId);
        if (existingWishlist != null)
            throw new ArgumentException("Sản phẩm đã có trong danh sách yêu thích");

        // Tạo mới
        var wishlist = new Wishlist
        {
            CustomerId = request.CustomerId,
            PartId = request.PartId,
            CreatedAt = DateTime.UtcNow
        };

        var addedWishlist = await _wishlistRepository.AddAsync(wishlist);
        return MapToResponse(addedWishlist);
    }

    public async Task DeleteFromWishlistAsync(int wishlistId)
    {
        if (!await _wishlistRepository.ExistsAsync(wishlistId))
            throw new ArgumentException("Mục yêu thích không tồn tại");

        await _wishlistRepository.DeleteAsync(wishlistId);
    }

    public async Task DeleteByCustomerAndPartAsync(int customerId, int partId)
    {
        if (!await _wishlistRepository.ExistsByCustomerAndPartAsync(customerId, partId))
            throw new ArgumentException("Mục yêu thích không tồn tại");

        await _wishlistRepository.DeleteByCustomerAndPartAsync(customerId, partId);
    }

    public async Task<bool> ExistsAsync(int wishlistId)
    {
        return await _wishlistRepository.ExistsAsync(wishlistId);
    }

    public async Task<bool> ExistsByCustomerAndPartAsync(int customerId, int partId)
    {
        return await _wishlistRepository.ExistsByCustomerAndPartAsync(customerId, partId);
    }

    private WishlistResponse MapToResponse(Wishlist wishlist)
    {
        return new WishlistResponse
        {
            WishlistId = wishlist.WishlistId,
            CustomerId = wishlist.CustomerId,
            CustomerName = wishlist.Customer?.User?.FullName ?? "Khách hàng",
            PartId = wishlist.PartId,
            PartName = wishlist.Part?.PartName ?? "",
            PartNumber = wishlist.Part?.PartNumber ?? "",
            Brand = wishlist.Part?.Brand ?? "",
            UnitPrice = wishlist.Part?.UnitPrice ?? 0,
            IsActive = wishlist.Part?.IsActive ?? false,
            CreatedAt = wishlist.CreatedAt
        };
    }
}
