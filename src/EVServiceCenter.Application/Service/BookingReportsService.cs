using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Responses;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using EVServiceCenter.Application.Constants;

namespace EVServiceCenter.Application.Service
{
    public class BookingReportsService : IBookingReportsService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IWorkOrderPartRepository _workOrderPartRepository;
        private readonly ILogger<BookingReportsService> _logger;

        public BookingReportsService(
            IBookingRepository bookingRepository,
            IWorkOrderPartRepository workOrderPartRepository,
            ILogger<BookingReportsService> logger)
        {
            _bookingRepository = bookingRepository;
            _workOrderPartRepository = workOrderPartRepository;
            _logger = logger;
        }

        public async Task<BookingTodayResponse> GetTodayBookingsAsync(int centerId)
        {
            try
            {
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);

                // Lấy bookings hôm nay
                var todayBookings = await _bookingRepository.GetBookingsByCenterIdAsync(
                    centerId,
                    page: 1,
                    pageSize: int.MaxValue);

                // Filter theo ngày làm việc của kỹ thuật viên (TechnicianTimeSlot.WorkDate)
                // Fallback: nếu chưa có slot, dùng CreatedAt
                var filteredBookings = todayBookings.Where(b =>
                    (b.TechnicianTimeSlot != null && b.TechnicianTimeSlot.WorkDate.Date == today)
                    || (b.TechnicianTimeSlot == null && b.CreatedAt.Date == today)
                ).ToList();

                var bookings = filteredBookings.Select(MapToBookingTodayItem).ToList();

                var summary = new BookingTodaySummary
                {
                    TotalBookings = bookings.Count,
                    CompletedBookings = bookings.Count(b => b.Status == BookingStatusConstants.Completed),
                    PendingBookings = bookings.Count(b => b.Status == BookingStatusConstants.Pending),
                    CancelledBookings = bookings.Count(b => b.Status == BookingStatusConstants.Cancelled),
                    TotalRevenue = bookings.Where(b => b.Status == BookingStatusConstants.Paid).Sum(b => 0) // TODO: Calculate actual revenue
                };

                return new BookingTodayResponse
                {
                    Bookings = bookings,
                    Summary = summary
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy booking hôm nay cho center {CenterId}", centerId);
                throw;
            }
        }

        public async Task<BookingListResponse> GetBookingsAsync(int centerId, int pageNumber = 1, int pageSize = 10, string? status = null)
        {
            try
            {
                var bookings = await _bookingRepository.GetBookingsByCenterIdAsync(
                    centerId,
                    page: pageNumber,
                    pageSize: pageSize,
                    status: status);

                var totalCount = await _bookingRepository.CountBookingsByCenterIdAsync(centerId, status);
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                var bookingItems = bookings.Select(MapToBookingListItem).ToList();

                return new BookingListResponse
                {
                    Bookings = bookingItems,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = totalPages
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách booking cho center {CenterId}", centerId);
                throw;
            }
        }

        private BookingTodayItem MapToBookingTodayItem(Booking booking)
        {
            return new BookingTodayItem
            {
                BookingId = booking.BookingId,
                CustomerName = booking.Customer?.User?.FullName ?? "Unknown",
                CustomerPhone = booking.Customer?.User?.PhoneNumber ?? "",
                VehicleInfo = $"{booking.Vehicle?.VehicleModel?.ModelName} - {booking.Vehicle?.LicensePlate}",
                ServiceName = booking.Service?.ServiceName ?? "Unknown",
                Status = booking.Status ?? "UNKNOWN",
                SlotTime = booking.TechnicianTimeSlot?.Slot?.SlotTime.ToString() ?? "",
                TechnicianName = booking.TechnicianTimeSlot?.Technician?.User?.FullName ?? "Chưa phân công",
                CreatedAt = booking.CreatedAt,
                UpdatedAt = booking.UpdatedAt
            };
        }

        private BookingListItem MapToBookingListItem(Booking booking)
        {
            return new BookingListItem
            {
                BookingId = booking.BookingId,
                CustomerName = booking.Customer?.User?.FullName ?? "Unknown",
                VehicleInfo = $"{booking.Vehicle?.VehicleModel?.ModelName} - {booking.Vehicle?.LicensePlate}",
                ServiceName = booking.Service?.ServiceName ?? "Unknown",
                Status = booking.Status ?? "UNKNOWN",
                BookingDate = booking.CreatedAt.Date,
                SlotTime = booking.TechnicianTimeSlot?.Slot?.SlotTime.ToString() ?? "",
                TechnicianName = booking.TechnicianTimeSlot?.Technician?.User?.FullName ?? "Chưa phân công",
                ServicePrice = booking.Service?.BasePrice ?? 0,
                PartsPrice = 0, // TODO: Calculate from WorkOrderParts
                TotalPrice = booking.Service?.BasePrice ?? 0,
                CreatedAt = booking.CreatedAt
            };
        }
    }
}
