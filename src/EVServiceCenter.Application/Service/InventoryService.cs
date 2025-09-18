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
    public class InventoryService : IInventoryService
    {
        private readonly IInventoryRepository _inventoryRepository;

        public InventoryService(IInventoryRepository inventoryRepository)
        {
            _inventoryRepository = inventoryRepository;
        }

        public async Task<InventoryListResponse> GetInventoriesAsync(int pageNumber = 1, int pageSize = 10, int? centerId = null, int? partId = null, string searchTerm = null)
        {
            try
            {
                var inventories = await _inventoryRepository.GetAllInventoriesAsync();

                // Filtering
                if (centerId.HasValue)
                {
                    inventories = inventories.Where(i => i.CenterId == centerId.Value).ToList();
                }

                if (partId.HasValue)
                {
                    inventories = inventories.Where(i => i.PartId == partId.Value).ToList();
                }

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    inventories = inventories.Where(i =>
                        i.Part.PartNumber.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        i.Part.PartName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        i.Part.Brand.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        i.Center.CenterName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }

                // Pagination
                var totalCount = inventories.Count;
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                var paginatedInventories = inventories.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

                var inventoryResponses = paginatedInventories.Select(i => MapToInventoryResponse(i)).ToList();

                return new InventoryListResponse
                {
                    Inventories = inventoryResponses,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                    TotalCount = totalCount
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy danh sách tồn kho: {ex.Message}");
            }
        }

        public async Task<InventoryResponse> GetInventoryByIdAsync(int inventoryId)
        {
            try
            {
                var inventory = await _inventoryRepository.GetInventoryByIdAsync(inventoryId);
                if (inventory == null)
                    throw new ArgumentException("Tồn kho không tồn tại.");

                return MapToInventoryResponse(inventory);
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy thông tin tồn kho: {ex.Message}");
            }
        }

        public async Task<InventoryResponse> UpdateInventoryAsync(int inventoryId, UpdateInventoryRequest request)
        {
            try
            {
                // Validate inventory exists
                var inventory = await _inventoryRepository.GetInventoryByIdAsync(inventoryId);
                if (inventory == null)
                    throw new ArgumentException("Tồn kho không tồn tại.");

                // Update inventory
                inventory.CurrentStock = request.CurrentStock;
                inventory.MinimumStock = request.MinimumStock;
                inventory.LastUpdated = DateTime.UtcNow;

                await _inventoryRepository.UpdateInventoryAsync(inventory);

                return MapToInventoryResponse(inventory);
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi cập nhật tồn kho: {ex.Message}");
            }
        }

        private InventoryResponse MapToInventoryResponse(Inventory inventory)
        {
            return new InventoryResponse
            {
                InventoryId = inventory.InventoryId,
                CenterId = inventory.CenterId,
                CenterName = inventory.Center?.CenterName ?? "N/A",
                PartId = inventory.PartId,
                PartNumber = inventory.Part?.PartNumber ?? "N/A",
                PartName = inventory.Part?.PartName ?? "N/A",
                Brand = inventory.Part?.Brand ?? "N/A",
                UnitPrice = inventory.Part?.UnitPrice ?? 0,
                Unit = inventory.Part?.Unit ?? "N/A",
                CurrentStock = inventory.CurrentStock,
                MinimumStock = inventory.MinimumStock,
                LastUpdated = inventory.LastUpdated,
                IsLowStock = inventory.CurrentStock <= inventory.MinimumStock,
                IsOutOfStock = inventory.CurrentStock == 0
            };
        }
    }
}
