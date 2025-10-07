using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;

namespace EVServiceCenter.Application.Service
{
    public class MaintenanceChecklistItemService : IMaintenanceChecklistItemService
    {
        private readonly IMaintenanceChecklistItemRepository _itemRepository;

        public MaintenanceChecklistItemService(IMaintenanceChecklistItemRepository itemRepository)
        {
            _itemRepository = itemRepository;
        }

        public async Task<MaintenanceChecklistItemListResponse> GetAllItemsAsync(int pageNumber = 1, int pageSize = 10, string searchTerm = null)
        {
            try
            {
                var items = await _itemRepository.GetAllItemsAsync();

                // Filtering
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    items = items.Where(i =>
                        i.ItemName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        i.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }

                // Pagination
                var totalCount = items.Count;
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                var paginatedItems = items.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

                var itemResponses = paginatedItems.Select(i => MapToItemResponse(i)).ToList();

                return new MaintenanceChecklistItemListResponse
                {
                    Items = itemResponses,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                    TotalCount = totalCount
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy danh sách mục kiểm tra: {ex.Message}");
            }
        }

        public async Task<MaintenanceChecklistItemResponse> GetItemByIdAsync(int itemId)
        {
            try
            {
                var item = await _itemRepository.GetItemByIdAsync(itemId);
                if (item == null)
                    throw new ArgumentException("Mục kiểm tra không tồn tại.");

                return MapToItemResponse(item);
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy thông tin mục kiểm tra: {ex.Message}");
            }
        }

        public async Task<MaintenanceChecklistItemResponse> CreateItemAsync(CreateMaintenanceChecklistItemRequest request)
        {
            try
            {
                // Create new item entity
                var item = new MaintenanceChecklistItem
                {
                    ItemName = request.ItemName.Trim(),
                    Description = request.Description.Trim()
                };

                // Save to repository
                var createdItem = await _itemRepository.CreateItemAsync(item);

                return MapToItemResponse(createdItem);
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi tạo mục kiểm tra: {ex.Message}");
            }
        }

        public async Task<MaintenanceChecklistItemResponse> UpdateItemAsync(int itemId, UpdateMaintenanceChecklistItemRequest request)
        {
            try
            {
                var existingItem = await _itemRepository.GetItemByIdAsync(itemId);
                if (existingItem == null)
                    throw new ArgumentException("Mục kiểm tra không tồn tại.");

                existingItem.ItemName = request.ItemName.Trim();
                existingItem.Description = request.Description.Trim();

                await _itemRepository.UpdateItemAsync(existingItem);

                return MapToItemResponse(existingItem);
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi cập nhật mục kiểm tra: {ex.Message}");
            }
        }

        public async Task<bool> DeleteItemAsync(int itemId)
        {
            try
            {
                var exists = await _itemRepository.ItemExistsAsync(itemId);
                if (!exists)
                    return false;

                await _itemRepository.DeleteItemAsync(itemId);
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi xóa mục kiểm tra: {ex.Message}");
            }
        }

        public async Task<MaintenanceChecklistItemListResponse> GetTemplateByServiceIdAsync(int serviceId)
        {
            try
            {
                var items = await _itemRepository.GetTemplateByServiceIdAsync(serviceId);
                var itemResponses = items.Select(i => MapToItemResponse(i)).ToList();

                return new MaintenanceChecklistItemListResponse
                {
                    Items = itemResponses,
                    PageNumber = 1,
                    PageSize = itemResponses.Count,
                    TotalPages = 1,
                    TotalCount = itemResponses.Count
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy template mục kiểm tra theo dịch vụ: {ex.Message}");
            }
        }

        private MaintenanceChecklistItemResponse MapToItemResponse(MaintenanceChecklistItem item)
        {
            return new MaintenanceChecklistItemResponse
            {
                ItemId = item.ItemId,
                ItemName = item.ItemName,
                Description = item.Description,
                CreatedAt = DateTime.UtcNow // Note: MaintenanceChecklistItem entity doesn't have CreatedAt, using current time
            };
        }
    }
}





