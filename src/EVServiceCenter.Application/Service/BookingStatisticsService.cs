using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models;
using EVServiceCenter.Domain.Interfaces;

namespace EVServiceCenter.Application.Service
{
    public class BookingStatisticsService : IBookingStatisticsService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly ILogger<BookingStatisticsService> _logger;

        public BookingStatisticsService(
            IBookingRepository bookingRepository,
            ILogger<BookingStatisticsService> logger)
        {
            _bookingRepository = bookingRepository;
            _logger = logger;
        }

        public async Task<BookingStatisticsResponse> GetBookingStatisticsAsync(BookingStatisticsRequest request)
        {
            try
            {
                _logger.LogInformation("Getting booking statistics with filters: StartDate={StartDate}, EndDate={EndDate}, CenterId={CenterId}", 
                    request.StartDate, request.EndDate, request.CenterId);

                var bookings = await _bookingRepository.GetAllBookingsAsync();
                
                // Apply filters
                var filteredBookings = bookings.AsQueryable();
                
                if (request.StartDate.HasValue)
                {
                    filteredBookings = filteredBookings.Where(b => b.CreatedAt >= request.StartDate.Value);
                }
                
                if (request.EndDate.HasValue)
                {
                    filteredBookings = filteredBookings.Where(b => b.CreatedAt <= request.EndDate.Value);
                }
                
                if (request.CenterId.HasValue)
                {
                    filteredBookings = filteredBookings.Where(b => b.CenterId == request.CenterId.Value);
                }
                
                if (!string.IsNullOrEmpty(request.ServiceType))
                {
                    filteredBookings = filteredBookings.Where(b => b.Service != null && b.Service.ServiceName == request.ServiceType);
                }
                
                if (!string.IsNullOrEmpty(request.Status))
                {
                    filteredBookings = filteredBookings.Where(b => b.Status == request.Status);
                }

                var bookingList = filteredBookings.ToList();

                var statistics = new BookingStatisticsData
                {
                    TotalBookings = bookingList.Count(),
                    PendingBookings = bookingList.Count(b => b.Status == "PENDING"),
                    ConfirmedBookings = bookingList.Count(b => b.Status == "CONFIRMED"),
                    InProgressBookings = bookingList.Count(b => b.Status == "IN_PROGRESS"),
                    CompletedBookings = bookingList.Count(b => b.Status == "COMPLETED"),
                    CancelledBookings = bookingList.Count(b => b.Status == "CANCELLED"),
                    TotalRevenue = bookingList.Where(b => b.Service != null).Sum(b => b.Service.BasePrice),
                    PendingRevenue = bookingList.Where(b => b.Status == "PENDING" && b.Service != null).Sum(b => b.Service.BasePrice),
                    CompletedRevenue = bookingList.Where(b => b.Status == "COMPLETED" && b.Service != null).Sum(b => b.Service.BasePrice),
                    CancelledRevenue = bookingList.Where(b => b.Status == "CANCELLED" && b.Service != null).Sum(b => b.Service.BasePrice)
                };

                // Status counts
                statistics.StatusCounts = bookingList
                    .GroupBy(b => b.Status)
                    .Select(g => new BookingStatusCount
                    {
                        Status = g.Key ?? "UNKNOWN",
                        Count = g.Count(),
                        Revenue = g.Where(b => b.Service != null).Sum(b => b.Service.BasePrice)
                    })
                    .ToList();

                // Monthly statistics
                if (request.IncludeMonthlyStats)
                {
                    statistics.MonthlyStats = bookingList
                        .GroupBy(b => new { b.CreatedAt.Year, b.CreatedAt.Month })
                        .Select(g => new MonthlyStatistics
                        {
                            Year = g.Key.Year,
                            Month = g.Key.Month,
                            MonthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                            BookingCount = g.Count(),
                            Revenue = g.Where(b => b.Service != null).Sum(b => b.Service.BasePrice)
                        })
                        .OrderBy(s => s.Year)
                        .ThenBy(s => s.Month)
                        .ToList();
                }

                // Daily statistics (last 30 days)
                if (request.IncludeDailyStats)
                {
                    var thirtyDaysAgo = DateTime.Now.AddDays(-30);
                    statistics.DailyStats = bookingList
                        .Where(b => b.CreatedAt >= thirtyDaysAgo)
                        .GroupBy(b => b.CreatedAt.Date)
                        .Select(g => new DailyStatistics
                        {
                            Date = g.Key,
                            BookingCount = g.Count(),
                            Revenue = g.Where(b => b.Service != null).Sum(b => b.Service.BasePrice)
                        })
                        .OrderBy(s => s.Date)
                        .ToList();
                }

                // Service type statistics
                if (request.IncludeServiceTypeStats)
                {
                    statistics.ServiceTypeStats = bookingList
                        .Where(b => b.Service != null)
                        .GroupBy(b => b.Service.ServiceName)
                        .Select(g => new ServiceTypeStatistics
                        {
                            ServiceType = g.Key,
                            BookingCount = g.Count(),
                            Revenue = g.Sum(b => b.Service.BasePrice),
                            AveragePrice = g.Average(b => b.Service.BasePrice)
                        })
                        .ToList();
                }

                return new BookingStatisticsResponse
                {
                    Success = true,
                    Message = "Thống kê booking được lấy thành công",
                    Data = statistics
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting booking statistics");
                return new BookingStatisticsResponse
                {
                    Success = false,
                    Message = $"Lỗi khi lấy thống kê booking: {ex.Message}"
                };
            }
        }

        public async Task<BookingStatisticsResponse> GetCenterBookingStatisticsAsync(CenterBookingStatisticsRequest request)
        {
            try
            {
                _logger.LogInformation("Getting center booking statistics for CenterId={CenterId}", request.CenterId);

                var generalRequest = new BookingStatisticsRequest
                {
                    CenterId = request.CenterId,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    ServiceType = request.ServiceType,
                    Status = request.Status,
                    IncludeMonthlyStats = request.IncludeMonthlyStats,
                    IncludeDailyStats = request.IncludeDailyStats,
                    IncludeServiceTypeStats = request.IncludeServiceTypeStats
                };

                return await GetBookingStatisticsAsync(generalRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting center booking statistics for CenterId={CenterId}", request.CenterId);
                return new BookingStatisticsResponse
                {
                    Success = false,
                    Message = $"Lỗi khi lấy thống kê booking cho center: {ex.Message}"
                };
            }
        }
    }
}
