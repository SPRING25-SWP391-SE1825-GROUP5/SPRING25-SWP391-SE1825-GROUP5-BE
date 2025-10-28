using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace EVServiceCenter.Application.Service
{
    public class CenterRevenueService : ICenterRevenueService
    {
        private readonly ICenterRepository _centerRepository;
        private readonly IBookingRepository _bookingRepository;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly ILogger<CenterRevenueService> _logger;

        public CenterRevenueService(
            ICenterRepository centerRepository,
            IBookingRepository bookingRepository,
            IInvoiceRepository invoiceRepository,
            IPaymentRepository paymentRepository,
            ILogger<CenterRevenueService> logger)
        {
            _centerRepository = centerRepository;
            _bookingRepository = bookingRepository;
            _invoiceRepository = invoiceRepository;
            _paymentRepository = paymentRepository;
            _logger = logger;
        }

        public async Task<CenterRevenueResponse> GetAllCentersRevenueAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            int page = 1,
            int pageSize = 30)
        {
            try
            {
                // Calculate date range
                var dateRange = CalculateDateRange(startDate, endDate);

                // Get all centers
                var centers = await _centerRepository.GetAllCentersAsync();
                if (!centers.Any())
                {
                    return new CenterRevenueResponse
                    {
                        Success = true,
                        Message = "Không có trung tâm nào",
                        Data = new List<CenterRevenueData>(),
                        Summary = new SummaryInfo
                        {
                            TotalCenters = 0,
                            TotalRevenue = 0,
                            TotalBookings = 0,
                            AverageRevenuePerCenter = 0
                        }
                    };
                }

                // Get revenue data for each center
                var revenueData = new List<CenterRevenueData>();
                foreach (var center in centers)
                {
                    var centerData = await CalculateCenterRevenue(center.CenterId, dateRange.StartDate, dateRange.EndDate);
                    revenueData.Add(centerData);
                }

                // Apply pagination
                var totalRecords = revenueData.Count;
                var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
                var paginatedData = revenueData
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Calculate summary
                var summary = CalculateSummary(revenueData, dateRange);

                return new CenterRevenueResponse
                {
                    Success = true,
                    Message = "Lấy doanh thu trung tâm thành công",
                    Data = paginatedData,
                    Pagination = new PaginationInfo
                    {
                        Page = page,
                        PageSize = pageSize,
                        TotalPages = totalPages,
                        TotalRecords = totalRecords
                    },
                    Summary = summary
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy doanh thu tất cả center");
                return new CenterRevenueResponse
                {
                    Success = false,
                    Message = $"Lỗi hệ thống: {ex.Message}"
                };
            }
        }

        public async Task<CenterRevenueResponse> GetCenterRevenueAsync(
            int centerId,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            try
            {
                // Validate center exists
                var center = await _centerRepository.GetCenterByIdAsync(centerId);
                if (center == null)
                {
                    return new CenterRevenueResponse
                    {
                        Success = false,
                        Message = $"Không tìm thấy trung tâm với ID: {centerId}"
                    };
                }

                // Calculate date range
                var dateRange = CalculateDateRange(startDate, endDate);

                // Get revenue data for center
                var centerData = await CalculateCenterRevenue(centerId, dateRange.StartDate, dateRange.EndDate);

                return new CenterRevenueResponse
                {
                    Success = true,
                    Message = "Lấy doanh thu trung tâm thành công",
                    Data = new List<CenterRevenueData> { centerData },
                    Summary = new SummaryInfo
                    {
                        TotalCenters = 1,
                        TotalRevenue = centerData.TotalRevenue,
                        TotalBookings = centerData.TotalBookings,
                        AverageRevenuePerCenter = centerData.TotalRevenue,
                        DateRange = new DateRangeInfo
                        {
                            StartDate = dateRange.StartDate.ToString("yyyy-MM-dd"),
                            EndDate = dateRange.EndDate.ToString("yyyy-MM-dd")
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy doanh thu center {CenterId}", centerId);
                return new CenterRevenueResponse
                {
                    Success = false,
                    Message = $"Lỗi hệ thống: {ex.Message}"
                };
            }
        }

        private (DateTime StartDate, DateTime EndDate) CalculateDateRange(DateTime? startDate, DateTime? endDate)
        {
            var start = startDate ?? DateTime.Today.AddMonths(-12); // Default 12 months ago
            var end = endDate ?? DateTime.Today;

            return (start, end);
        }

        private async Task<CenterRevenueData> CalculateCenterRevenue(int centerId, DateTime startDate, DateTime endDate)
        {
            // Get center info
            var center = await _centerRepository.GetCenterByIdAsync(centerId);

            // Get all bookings for center in date range
            var allBookings = await _bookingRepository.GetAllBookingsAsync();
            var centerBookings = allBookings
                .Where(b => b.CenterId == centerId && 
                           b.CreatedAt >= startDate && 
                           b.CreatedAt <= endDate)
                .ToList();

            // Calculate basic metrics
            var totalBookings = centerBookings.Count;
            var completedBookings = centerBookings.Count(b => b.Status == "PAID" || b.Status == "COMPLETED");
            var cancelledBookings = centerBookings.Count(b => b.Status == "CANCELLED");

            // Calculate revenue from payments
            var totalRevenue = 0m;
            var revenueByService = new Dictionary<int, (string ServiceName, decimal Revenue, int Count)>();
            var revenueByMonth = new Dictionary<string, (decimal Revenue, int Count)>();

            foreach (var booking in centerBookings.Where(b => b.Status == "PAID" || b.Status == "COMPLETED"))
            {
                // Get payment amount (simplified - using service base price)
                var servicePrice = booking.Service?.BasePrice ?? 0m;
                totalRevenue += servicePrice;

                // Revenue by service
                if (booking.Service != null)
                {
                    var serviceId = booking.Service.ServiceId;
                    var serviceName = booking.Service.ServiceName;
                    
                    if (revenueByService.ContainsKey(serviceId))
                    {
                        var existing = revenueByService[serviceId];
                        revenueByService[serviceId] = (serviceName, existing.Revenue + servicePrice, existing.Count + 1);
                    }
                    else
                    {
                        revenueByService[serviceId] = (serviceName, servicePrice, 1);
                    }
                }

                // Revenue by month
                var monthKey = booking.CreatedAt.ToString("yyyy-MM");
                var monthName = $"Tháng {booking.CreatedAt.Month}/{booking.CreatedAt.Year}";
                
                if (revenueByMonth.ContainsKey(monthKey))
                {
                    var existing = revenueByMonth[monthKey];
                    revenueByMonth[monthKey] = (existing.Revenue + servicePrice, existing.Count + 1);
                }
                else
                {
                    revenueByMonth[monthKey] = (servicePrice, 1);
                }
            }

            // Convert to response format
            var revenueByServiceList = revenueByService.Select(kvp => new RevenueByService
            {
                ServiceId = kvp.Key,
                ServiceName = kvp.Value.ServiceName,
                Revenue = kvp.Value.Revenue,
                BookingCount = kvp.Value.Count,
                AveragePrice = kvp.Value.Count > 0 ? kvp.Value.Revenue / kvp.Value.Count : 0
            }).ToList();

            var revenueByMonthList = revenueByMonth.Select(kvp => new RevenueByMonth
            {
                Month = kvp.Key,
                MonthName = $"Tháng {DateTime.Parse(kvp.Key + "-01").Month}/{DateTime.Parse(kvp.Key + "-01").Year}",
                Revenue = kvp.Value.Revenue,
                BookingCount = kvp.Value.Count
            }).OrderBy(r => r.Month).ToList();

            return new CenterRevenueData
            {
                CenterId = centerId,
                CenterName = center?.CenterName ?? "Unknown",
                Address = center?.Address ?? "",
                PhoneNumber = center?.PhoneNumber ?? "",
                TotalRevenue = totalRevenue,
                TotalBookings = totalBookings,
                CompletedBookings = completedBookings,
                CancelledBookings = cancelledBookings,
                AverageBookingValue = completedBookings > 0 ? totalRevenue / completedBookings : 0,
                RevenueByService = revenueByServiceList,
                RevenueByMonth = revenueByMonthList,
                LastUpdated = DateTime.UtcNow
            };
        }

        private SummaryInfo CalculateSummary(List<CenterRevenueData> data, (DateTime StartDate, DateTime EndDate) dateRange)
        {
            var totalRevenue = data.Sum(d => d.TotalRevenue);
            var totalBookings = data.Sum(d => d.TotalBookings);
            var averageRevenuePerCenter = data.Count > 0 ? totalRevenue / data.Count : 0;

            var topCenter = data.OrderByDescending(d => d.TotalRevenue).FirstOrDefault();

            return new SummaryInfo
            {
                TotalCenters = data.Count,
                TotalRevenue = totalRevenue,
                TotalBookings = totalBookings,
                AverageRevenuePerCenter = averageRevenuePerCenter,
                DateRange = new DateRangeInfo
                {
                    StartDate = dateRange.StartDate.ToString("yyyy-MM-dd"),
                    EndDate = dateRange.EndDate.ToString("yyyy-MM-dd")
                },
                TopPerformingCenter = topCenter != null ? new TopCenterInfo
                {
                    CenterId = topCenter.CenterId,
                    CenterName = topCenter.CenterName,
                    Revenue = topCenter.TotalRevenue,
                    BookingCount = topCenter.TotalBookings
                } : null
            };
        }
    }
}
