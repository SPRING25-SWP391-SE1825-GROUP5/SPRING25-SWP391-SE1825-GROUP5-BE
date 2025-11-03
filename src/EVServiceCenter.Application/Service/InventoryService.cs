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

        public async Task<InventoryListResponse> GetInventoriesAsync(int pageNumber = 1, int pageSize = 10, int? centerId = null, string? searchTerm = null)
        {
            try
            {
                var inventories = await _inventoryRepository.GetAllInventoriesAsync();

                // Filtering by Center
                if (centerId.HasValue)
                {
                    inventories = inventories.Where(i => i.CenterId == centerId.Value).ToList();
                }

                // Filtering by search term (on CenterName or Part details within InventoryParts)
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    inventories = inventories.Where(i =>
                        i.Center.CenterName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        i.InventoryParts.Any(ip =>
                            ip.Part.PartNumber.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                            ip.Part.PartName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                            ip.Part.Brand.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                        )
                    ).ToList();
                }

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

        // Removed GetInventoriesByCenterAsync: each center has exactly one inventory now

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

        public async Task<InventoryResponse> GetInventoryByCenterIdAsync(int centerId)
        {
            try
            {
                var inventory = await _inventoryRepository.GetInventoryByCenterIdAsync(centerId);
                if (inventory == null)
                    throw new ArgumentException("Trung tâm chưa có kho.");

                return MapToInventoryResponse(inventory);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy tồn kho theo trung tâm: {ex.Message}");
            }
        }

        public async Task<InventoryResponse> CreateInventoryAsync(CreateInventoryRequest request)
        {
            try
            {
                // Validate center exists and is active
                var center = await _inventoryRepository.GetCenterByIdAsync(request.CenterId);
                if (center == null)
                    throw new ArgumentException($"Trung tâm với ID {request.CenterId} không tồn tại.");
                if (!center.IsActive)
                    throw new ArgumentException($"Trung tâm với ID {request.CenterId} đã bị vô hiệu hóa.");

                // Validate if center already has an inventory
                if (await _inventoryRepository.CenterHasInventoryAsync(request.CenterId))
                    throw new ArgumentException($"Trung tâm với ID {request.CenterId} đã có tồn kho. Mỗi trung tâm chỉ có thể có một tồn kho duy nhất.");

                var entity = new Inventory
                {
                    CenterId = request.CenterId,
                    LastUpdated = DateTime.UtcNow
                };

                entity = await _inventoryRepository.AddInventoryAsync(entity);
                return MapToInventoryResponse(entity);
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi tạo tồn kho: {ex.Message}");
            }
        }

        public async Task<InventoryResponse> UpdateInventoryAsync(int inventoryId, UpdateInventoryRequest request)
        {
            try
            {
                var inventory = await _inventoryRepository.GetInventoryByIdAsync(inventoryId);
                if (inventory == null)
                    throw new ArgumentException("Tồn kho không tồn tại.");

                // Only LastUpdated is updated for the main Inventory entity
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

        // ====================================================================================================
        // INVENTORY PART MANAGEMENT
        // ====================================================================================================

        public async Task<InventoryPartResponse> AddPartToInventoryAsync(int inventoryId, int partId, int currentStock, int minimumStock)
        {
            try
            {
                var inventory = await _inventoryRepository.GetInventoryByIdAsync(inventoryId);
                if (inventory == null)
                    throw new ArgumentException("Tồn kho không tồn tại.");

                var part = await _inventoryRepository.GetPartByIdAsync(partId);
                if (part == null)
                    throw new ArgumentException($"Phụ tùng với ID {partId} không tồn tại.");
                if (!part.IsActive)
                    throw new ArgumentException($"Phụ tùng với ID {partId} đã bị vô hiệu hóa.");

                if (await _inventoryRepository.InventoryPartExistsAsync(inventoryId, partId))
                    throw new ArgumentException($"Phụ tùng ID {partId} đã tồn tại trong tồn kho ID {inventoryId}.");

                var inventoryPart = new InventoryPart
                {
                    InventoryId = inventoryId,
                    PartId = partId,
                    CurrentStock = currentStock,
                    MinimumStock = minimumStock,
                    LastUpdated = DateTime.UtcNow
                };

                inventoryPart = await _inventoryRepository.AddInventoryPartAsync(inventoryPart);

                // Update parent inventory's LastUpdated
                inventory.LastUpdated = DateTime.UtcNow;
                await _inventoryRepository.UpdateInventoryAsync(inventory);

                return MapToInventoryPartResponse(inventoryPart);
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi thêm phụ tùng vào tồn kho: {ex.Message}");
            }
        }

        public async Task<InventoryPartResponse> UpdateInventoryPartAsync(int inventoryId, int partId, int currentStock, int minimumStock)
        {
            try
            {
                var inventoryPart = await _inventoryRepository.GetInventoryPartByInventoryAndPartAsync(inventoryId, partId);
                if (inventoryPart == null)
                    throw new ArgumentException($"Phụ tùng ID {partId} không tồn tại trong tồn kho ID {inventoryId}.");

                inventoryPart.CurrentStock = currentStock;
                inventoryPart.MinimumStock = minimumStock;
                inventoryPart.LastUpdated = DateTime.UtcNow;

                await _inventoryRepository.UpdateInventoryPartAsync(inventoryPart);

                // Update parent inventory's LastUpdated
                var inventory = await _inventoryRepository.GetInventoryByIdAsync(inventoryId);
                if (inventory != null)
                {
                    inventory.LastUpdated = DateTime.UtcNow;
                    await _inventoryRepository.UpdateInventoryAsync(inventory);
                }

                return MapToInventoryPartResponse(inventoryPart);
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi cập nhật phụ tùng trong tồn kho: {ex.Message}");
            }
        }

        public async Task<bool> RemovePartFromInventoryAsync(int inventoryId, int partId)
        {
            try
            {
                var inventoryPart = await _inventoryRepository.GetInventoryPartByInventoryAndPartAsync(inventoryId, partId);
                if (inventoryPart == null)
                    throw new ArgumentException($"Phụ tùng ID {partId} không tồn tại trong tồn kho ID {inventoryId}.");

                await _inventoryRepository.DeleteInventoryPartAsync(inventoryId, partId);

                // Update parent inventory's LastUpdated
                var inventory = await _inventoryRepository.GetInventoryByIdAsync(inventoryId);
                if (inventory != null)
                {
                    inventory.LastUpdated = DateTime.UtcNow;
                    await _inventoryRepository.UpdateInventoryAsync(inventory);
                }

                return true;
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi xóa phụ tùng khỏi tồn kho: {ex.Message}");
            }
        }

        // ====================================================================================================
        // AVAILABILITY
        // ====================================================================================================

        public async Task<List<InventoryPartResponse>> GetAvailabilityAsync(int centerId, List<int> partIds)
        {
            try
            {
                var inventory = await _inventoryRepository.GetInventoryByCenterIdAsync(centerId);
                if (inventory == null)
                    return new List<InventoryPartResponse>(); // Center has no inventory

                var availableParts = inventory.InventoryParts
                    .Where(ip => partIds.Contains(ip.PartId))
                    .Select(MapToInventoryPartResponse)
                    .ToList();

                return availableParts;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy thông tin khả dụng tồn kho: {ex.Message}");
            }
        }

        public async Task<List<InventoryAvailabilityResponse>> GetGlobalAvailabilityAsync(List<int> partIds)
        {
            try
            {
                var allInventories = await _inventoryRepository.GetAllInventoriesAsync();
                var relevantInventoryParts = allInventories
                    .SelectMany(inv => inv.InventoryParts)
                    .Where(ip => partIds.Contains(ip.PartId))
                    .ToList();

                var grouped = relevantInventoryParts
                    .GroupBy(ip => ip.PartId)
                    .Select(g => new InventoryAvailabilityResponse
                    {
                        PartId = g.Key,
                        PartNumber = g.FirstOrDefault()?.Part?.PartNumber ?? "N/A",
                        PartName = g.FirstOrDefault()?.Part?.PartName ?? "N/A",
                        Brand = g.FirstOrDefault()?.Part?.Brand ?? "N/A",
                        ImageUrl = g.FirstOrDefault()?.Part?.ImageUrl,
                        TotalStock = g.Sum(x => x.CurrentStock),
                        MinimumStock = g.Sum(x => x.MinimumStock),
                        IsLowStock = g.Sum(x => x.CurrentStock) <= g.Sum(x => x.MinimumStock),
                        IsOutOfStock = g.Sum(x => x.CurrentStock) == 0,
                        UnitPrice = g.FirstOrDefault()?.Part?.Price ?? 0,
                        Rating = g.FirstOrDefault()?.Part?.Rating,
                        LastUpdated = g.Max(x => x.LastUpdated)
                    })
                    .ToList();
                return grouped;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy thông tin khả dụng tồn kho toàn cầu: {ex.Message}");
            }
        }

        public async Task<List<InventoryAvailabilityResponse>> GetGlobalAvailabilityAllAsync()
        {
            try
            {
                var allInventories = await _inventoryRepository.GetAllInventoriesAsync();
                var allInventoryParts = allInventories.SelectMany(inv => inv.InventoryParts).ToList();

                var grouped = allInventoryParts
                    .GroupBy(ip => ip.PartId)
                    .Select(g => new InventoryAvailabilityResponse
                    {
                        PartId = g.Key,
                        PartNumber = g.FirstOrDefault()?.Part?.PartNumber ?? "N/A",
                        PartName = g.FirstOrDefault()?.Part?.PartName ?? "N/A",
                        Brand = g.FirstOrDefault()?.Part?.Brand ?? "N/A",
                        ImageUrl = g.FirstOrDefault()?.Part?.ImageUrl,
                        TotalStock = g.Sum(x => x.CurrentStock),
                        MinimumStock = g.Sum(x => x.MinimumStock),
                        IsLowStock = g.Sum(x => x.CurrentStock) <= g.Sum(x => x.MinimumStock),
                        IsOutOfStock = g.Sum(x => x.CurrentStock) == 0,
                        UnitPrice = g.FirstOrDefault()?.Part?.Price ?? 0,
                        Rating = g.FirstOrDefault()?.Part?.Rating,
                        LastUpdated = g.Max(x => x.LastUpdated)
                    })
                    .Where(r => r.TotalStock > 0)
                    .ToList();
                return grouped;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy thông tin khả dụng tồn kho toàn cầu: {ex.Message}");
            }
        }

        // ====================================================================================================
        // MAPPERS
        // ====================================================================================================

        private InventoryResponse MapToInventoryResponse(Inventory inventory, IEnumerable<InventoryPart>? inventoryParts = null)
        {
            inventoryParts ??= inventory.InventoryParts;

            return new InventoryResponse
            {
                InventoryId = inventory.InventoryId,
                CenterId = inventory.CenterId,
                CenterName = inventory.Center?.CenterName ?? "N/A",
                LastUpdated = inventory.LastUpdated,
                PartsCount = inventoryParts.Count(),
                InventoryParts = inventoryParts.Select(MapToInventoryPartResponse).ToList()
            };
        }

        private InventoryPartResponse MapToInventoryPartResponse(InventoryPart inventoryPart)
        {
            return new InventoryPartResponse
            {
                InventoryPartId = inventoryPart.InventoryPartId,
                InventoryId = inventoryPart.InventoryId,
                PartId = inventoryPart.PartId,
                PartNumber = inventoryPart.Part?.PartNumber ?? "N/A",
                PartName = inventoryPart.Part?.PartName ?? "N/A",
                Brand = inventoryPart.Part?.Brand ?? "N/A",
                UnitPrice = inventoryPart.Part?.Price ?? 0,
                CurrentStock = inventoryPart.CurrentStock,
                MinimumStock = inventoryPart.MinimumStock,
                LastUpdated = inventoryPart.LastUpdated,
                IsLowStock = inventoryPart.CurrentStock <= inventoryPart.MinimumStock,
                IsOutOfStock = inventoryPart.CurrentStock == 0
            };
        }
    }
}
