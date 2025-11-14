using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace EVServiceCenter.Application.Service
{
    public class PartsUsageReportService : IPartsUsageReportService
    {
        private readonly IWorkOrderPartRepository _workOrderPartRepository;
        private readonly IInventoryRepository _inventoryRepository;
        private readonly ILogger<PartsUsageReportService> _logger;

        public PartsUsageReportService(
            IWorkOrderPartRepository workOrderPartRepository,
            IInventoryRepository inventoryRepository,
            ILogger<PartsUsageReportService> logger)
        {
            _workOrderPartRepository = workOrderPartRepository;
            _inventoryRepository = inventoryRepository;
            _logger = logger;
        }

        public async Task<PartsUsageReportResponse> GetPartsUsageReportAsync(int centerId, PartsUsageReportRequest request)
        {
            try
            {
                // Lấy dữ liệu WorkOrderParts trong khoảng thời gian
                var workOrderParts = await _workOrderPartRepository.GetByCenterAndDateRangeAsync(
                    centerId, request.StartDate, request.EndDate);

                // Tính toán các chỉ số
                var partUsageStats = CalculatePartUsageStats(workOrderParts, request.StartDate, request.EndDate);

                // Phân loại parts
                var hotParts = partUsageStats.Where(p => IsHotPart(p)).ToList();
                var notHotParts = partUsageStats.Where(p => IsNotHotPart(p)).ToList();

                // Lấy parts chưa sử dụng
                var unusedParts = await GetUnusedPartsAsync(centerId, partUsageStats.Select(p => p.PartId).ToList());

                // Tính summary
                var summary = CalculateSummary(partUsageStats, hotParts.Count, notHotParts.Count, unusedParts.Count);

                // Tính trends
                var trends = await CalculateTrendsAsync(centerId, request.StartDate, request.EndDate);

                // So sánh với kỳ trước nếu được yêu cầu
                PartsUsageComparison? comparison = null;
                if (request.CompareWithPrevious)
                {
                    comparison = await CalculateComparisonAsync(centerId, request.StartDate, request.EndDate);
                }

                return new PartsUsageReportResponse
                {
                    Summary = summary,
                    HotParts = hotParts.Take(request.PageSize).ToList(),
                    NotHotParts = notHotParts.Take(request.PageSize).ToList(),
                    UnusedParts = unusedParts.Take(request.PageSize).ToList(),
                    Trends = trends,
                    Comparison = comparison
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo báo cáo sử dụng phụ tùng cho center {CenterId}", centerId);
                throw;
            }
        }

        private List<PartUsageDetail> CalculatePartUsageStats(List<WorkOrderPart> workOrderParts, DateTime startDate, DateTime endDate)
        {
            var partGroups = workOrderParts.GroupBy(wop => wop.PartId);
            var weeksInPeriod = (endDate - startDate).TotalDays / 7;

            return partGroups.Select(group =>
            {
                var part = group.First().Part;
                var totalQuantity = group.Sum(wop => wop.QuantityUsed);
                var totalRevenue = group.Sum(wop => wop.QuantityUsed * part.Price);
                var frequency = weeksInPeriod > 0 ? group.Count() / weeksInPeriod : 0;

                // Lấy tồn kho hiện tại
                var currentStock = GetCurrentStock(part.PartId);

                return new PartUsageDetail
                {
                    PartId = part.PartId,
                    PartNumber = part.PartNumber,
                    PartName = part.PartName,
                    Brand = part.Brand ?? string.Empty,
                    UsageCount = totalQuantity,
                    Revenue = totalRevenue,
                    Frequency = Math.Round(frequency, 1),
                    UsageRate = currentStock > 0 ? Math.Round((double)totalQuantity / currentStock * 100, 1) : 0
                };
            }).ToList();
        }

        private int GetCurrentStock(int partId)
        {
            // TODO: Implement logic to get current stock from inventory
            // For now, return a default value
            return 10;
        }

        private bool IsHotPart(PartUsageDetail part)
        {
            return part.UsageCount >= 5 &&
                   part.Revenue >= 10000000 &&
                   part.Frequency >= 3 &&
                   part.UsageRate >= 50;
        }

        private bool IsNotHotPart(PartUsageDetail part)
        {
            return part.UsageCount <= 2 &&
                   part.Revenue <= 2000000 &&
                   part.Frequency <= 1 &&
                   part.UsageRate <= 20;
        }

        private Task<List<PartUsageDetail>> GetUnusedPartsAsync(int centerId, List<int> usedPartIds)
        {
            // TODO: Implement logic to get parts that are in inventory but never used
            // For now, return empty list
            return Task.FromResult(new List<PartUsageDetail>());
        }

        private PartsUsageSummary CalculateSummary(List<PartUsageDetail> partStats, int hotCount, int notHotCount, int unusedCount)
        {
            return new PartsUsageSummary
            {
                TotalPartsUsed = partStats.Count,
                TotalRevenue = partStats.Sum(p => p.Revenue),
                TotalUsageCount = partStats.Sum(p => p.UsageCount),
                HotPartsCount = hotCount,
                NotHotPartsCount = notHotCount,
                UnusedPartsCount = unusedCount
            };
        }

        private Task<PartsUsageTrends> CalculateTrendsAsync(int centerId, DateTime startDate, DateTime endDate)
        {
            // TODO: Implement trend calculation logic
            return Task.FromResult(new PartsUsageTrends
            {
                RevenueGrowth = "+15%",
                UsageGrowth = "+8%",
                NewPartsAdded = 3
            });
        }

        private async Task<PartsUsageComparison> CalculateComparisonAsync(int centerId, DateTime startDate, DateTime endDate)
        {
            var periodLength = endDate - startDate;
            var previousStartDate = startDate - periodLength;
            var previousEndDate = startDate;

            var previousWorkOrderParts = await _workOrderPartRepository.GetByCenterAndDateRangeAsync(
                centerId, previousStartDate, previousEndDate);

            var currentWorkOrderParts = await _workOrderPartRepository.GetByCenterAndDateRangeAsync(
                centerId, startDate, endDate);

            var previousRevenue = previousWorkOrderParts.Sum(wop => wop.QuantityUsed * wop.Part.Price);
            var currentRevenue = currentWorkOrderParts.Sum(wop => wop.QuantityUsed * wop.Part.Price);
            var previousUsage = previousWorkOrderParts.Sum(wop => wop.QuantityUsed);
            var currentUsage = currentWorkOrderParts.Sum(wop => wop.QuantityUsed);

            return new PartsUsageComparison
            {
                PreviousPeriod = $"{previousStartDate:yyyy-MM-dd} to {previousEndDate:yyyy-MM-dd}",
                RevenueChange = currentRevenue - previousRevenue,
                UsageChange = currentUsage - previousUsage
            };
        }
    }
}
