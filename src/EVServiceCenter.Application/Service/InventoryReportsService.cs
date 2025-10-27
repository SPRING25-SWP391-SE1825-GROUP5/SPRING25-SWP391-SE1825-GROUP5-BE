using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Responses;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace EVServiceCenter.Application.Service
{
    public class InventoryReportsService : IInventoryReportsService
    {
        private readonly IInventoryRepository _inventoryRepository;
        private readonly IWorkOrderPartRepository _workOrderPartRepository;
        private readonly ILogger<InventoryReportsService> _logger;

        public InventoryReportsService(
            IInventoryRepository inventoryRepository,
            IWorkOrderPartRepository workOrderPartRepository,
            ILogger<InventoryReportsService> logger)
        {
            _inventoryRepository = inventoryRepository;
            _workOrderPartRepository = workOrderPartRepository;
            _logger = logger;
        }

        public async Task<InventoryUsageResponse> GetInventoryUsageAsync(int centerId, string period = "month")
        {
            try
            {
                var startDate = GetPeriodStartDate(period);
                var endDate = DateTime.Now;

                // Lấy inventory của center
                var inventory = await _inventoryRepository.GetInventoryByCenterIdAsync(centerId);
                if (inventory == null)
                {
                    return new InventoryUsageResponse();
                }

                // Lấy inventory parts
                var inventoryParts = await _inventoryRepository.GetInventoryPartsByInventoryIdAsync(inventory.InventoryId);

                // Lấy work order parts trong khoảng thời gian
                var workOrderParts = await _workOrderPartRepository.GetByCenterAndDateRangeAsync(centerId, startDate, endDate);

                var partUsageItems = new List<PartUsageItem>();

                foreach (var inventoryPart in inventoryParts)
                {
                    var part = inventoryPart.Part;
                    var usageInPeriod = workOrderParts.Where(wop => wop.PartId == part.PartId).ToList();
                    
                    var usageCount = usageInPeriod.Sum(wop => wop.QuantityUsed);
                    var usageValue = usageInPeriod.Sum(wop => wop.QuantityUsed * wop.Part.Price);
                    var usageRate = inventoryPart.CurrentStock > 0 ? (double)usageCount / inventoryPart.CurrentStock : 0;

                    var stockStatus = inventoryPart.CurrentStock switch
                    {
                        0 => "OUT",
                        var stock when stock <= inventoryPart.MinimumStock => "LOW",
                        _ => "NORMAL"
                    };

                    var lastUsedDate = usageInPeriod.Any() ? usageInPeriod.Max(wop => wop.Booking.UpdatedAt) : DateTime.MinValue;

                    partUsageItems.Add(new PartUsageItem
                    {
                        PartId = part.PartId,
                        PartNumber = part.PartNumber,
                        PartName = part.PartName,
                        Brand = part.Brand,
                        CurrentStock = inventoryPart.CurrentStock,
                        MinimumStock = inventoryPart.MinimumStock,
                        UsageCount = usageCount,
                        UsageValue = usageValue,
                        UnitPrice = part.Price,
                        UsageRate = usageRate,
                        StockStatus = stockStatus,
                        LastUsedDate = lastUsedDate,
                        Category = "General"
                    });
                }

                // Phân loại parts
                var hotParts = partUsageItems
                    .Where(p => p.UsageCount > 0)
                    .OrderByDescending(p => p.UsageCount)
                    .Take((int)(partUsageItems.Count * 0.2)) // Top 20%
                    .ToList();

                var notHotParts = partUsageItems
                    .Where(p => p.UsageCount > 0)
                    .OrderBy(p => p.UsageCount)
                    .Take((int)(partUsageItems.Count * 0.3)) // Bottom 30%
                    .ToList();

                var unusedParts = partUsageItems
                    .Where(p => p.UsageCount == 0)
                    .ToList();

                var summary = new InventoryUsageSummary
                {
                    TotalParts = partUsageItems.Count,
                    HotPartsCount = hotParts.Count,
                    NotHotPartsCount = notHotParts.Count,
                    UnusedPartsCount = unusedParts.Count,
                    LowStockPartsCount = partUsageItems.Count(p => p.StockStatus == "LOW"),
                    OutOfStockPartsCount = partUsageItems.Count(p => p.StockStatus == "OUT"),
                    TotalInventoryValue = partUsageItems.Sum(p => p.CurrentStock * p.UnitPrice),
                    TotalUsageValue = partUsageItems.Sum(p => p.UsageValue),
                    AverageUsageRate = partUsageItems.Any() ? partUsageItems.Average(p => p.UsageRate) : 0
                };

                return new InventoryUsageResponse
                {
                    HotParts = hotParts,
                    NotHotParts = notHotParts,
                    UnusedParts = unusedParts,
                    Summary = summary
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy báo cáo sử dụng kho cho center {CenterId}", centerId);
                throw;
            }
        }

        private DateTime GetPeriodStartDate(string period)
        {
            return period.ToLower() switch
            {
                "week" => DateTime.Now.AddDays(-7),
                "month" => DateTime.Now.AddMonths(-1),
                "quarter" => DateTime.Now.AddMonths(-3),
                "year" => DateTime.Now.AddYears(-1),
                _ => DateTime.Now.AddMonths(-1)
            };
        }
    }
}
