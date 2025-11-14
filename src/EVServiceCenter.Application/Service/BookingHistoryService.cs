using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models;
using EVServiceCenter.Application.Models.Responses;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using EVServiceCenter.Application.Constants;

namespace EVServiceCenter.Application.Service
{
    public class BookingHistoryService : IBookingHistoryService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly ICustomerRepository _customerRepository;
        public BookingHistoryService(
            IBookingRepository bookingRepository,
            ICustomerRepository customerRepository)
        {
            _bookingRepository = bookingRepository;
            _customerRepository = customerRepository;
        }

        public async Task<BookingHistoryListResponse> GetBookingHistoryAsync(int customerId, int page = 1, int pageSize = 10,
            string? status = null, DateTime? fromDate = null, DateTime? toDate = null,
            string sortBy = "createdAt", string sortOrder = "desc")
        {
            // Optimize: Skip customer validation - let repository handle it via query
            // If customer doesn't exist, query will return empty list anyway

            // Validate pagination parameters
            page = Math.Max(1, page);
            pageSize = Math.Min(Math.Max(1, pageSize), 50);

            // Get bookings with pagination
            var bookings = await _bookingRepository.GetBookingsByCustomerIdAsync(
                customerId, page, pageSize, status, fromDate, toDate, sortBy, sortOrder);

            // Get total count for pagination
            var totalItems = await _bookingRepository.CountBookingsByCustomerIdAsync(
                customerId, status, fromDate, toDate);

            // Map to summary DTOs
            var bookingSummaries = bookings.Select(MapToBookingHistorySummary).ToList();

            // Calculate pagination info
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            var pagination = new PaginationInfo
            {
                Page = page,
                PageSize = pageSize,
                TotalRecords = totalItems,
                TotalPages = totalPages
            };

            var filters = new FilterInfo
            {
                Status = status,
                FromDate = fromDate,
                ToDate = toDate,
                SortBy = sortBy,
                SortOrder = sortOrder
            };

            return new BookingHistoryListResponse
            {
                Bookings = bookingSummaries,
                Pagination = pagination,
                Filters = filters
            };
        }

        public async Task<BookingHistoryResponse> GetBookingHistoryByIdAsync(int customerId, int bookingId)
        {
            // Validate customer exists
            var customer = await _customerRepository.GetCustomerByIdAsync(customerId);
            if (customer == null)
            {
                throw new KeyNotFoundException($"Customer with ID {customerId} not found.");
            }

            // Get booking with full details
            var booking = await _bookingRepository.GetBookingWithDetailsByIdAsync(bookingId);
            if (booking == null)
            {
                throw new KeyNotFoundException($"Booking with ID {bookingId} not found.");
            }

            // Verify booking belongs to customer
            if (booking.CustomerId != customerId)
            {
                throw new UnauthorizedAccessException("You can only view your own booking history.");
            }

            return await MapToBookingHistoryResponse(booking);
        }

        public async Task<BookingHistoryStatsResponse> GetBookingHistoryStatsAsync(int customerId, string period = "all")
        {
            // Optimize: Skip customer validation - let repository handle it via query

            // Calculate date range based on period
            DateTime? fromDate = null;
            switch (period.ToLower())
            {
                case "7days":
                    fromDate = DateTime.Now.AddDays(-7);
                    break;
                case "30days":
                    fromDate = DateTime.Now.AddDays(-30);
                    break;
                case "90days":
                    fromDate = DateTime.Now.AddDays(-90);
                    break;
                case "1year":
                    fromDate = DateTime.Now.AddYears(-1);
                    break;
                case "all":
                default:
                    fromDate = null;
                    break;
            }

            // Optimize: Calculate statistics directly from database instead of loading all bookings
            // Get total count
            var totalBookings = await _bookingRepository.CountBookingsByCustomerIdAsync(
                customerId, null, fromDate, null);

            // Get bookings for stats calculation (limit to reasonable number for aggregation)
            // Only load what's needed for stats, not all bookings
            var bookingsForStats = await _bookingRepository.GetBookingsByCustomerIdAsync(
                customerId, 1, 1000, null, fromDate, null, "createdAt", "desc");

            // Calculate statistics
            var statusBreakdown = CalculateStatusBreakdown(bookingsForStats);
            var totalSpent = bookingsForStats.Where(b => b.Status == BookingStatusConstants.Completed).Sum(b => b.Service?.BasePrice ?? 0);
            var averageCost = totalBookings > 0 ? totalSpent / totalBookings : 0;

            var favoriteService = CalculateFavoriteService(bookingsForStats);
            var favoriteCenter = CalculateFavoriteCenter(bookingsForStats);
            var recentActivity = CalculateRecentActivity(bookingsForStats);

            return new BookingHistoryStatsResponse
            {
                TotalBookings = totalBookings,
                StatusBreakdown = statusBreakdown,
                TotalSpent = totalSpent,
                AverageCost = averageCost,
                FavoriteService = favoriteService,
                FavoriteCenter = favoriteCenter,
                RecentActivity = recentActivity,
                Period = period
            };
        }

        private BookingHistorySummary MapToBookingHistorySummary(Booking booking)
        {
            return new BookingHistorySummary
            {
                BookingId = booking.BookingId,
                BookingCode = "",
                BookingDate = DateOnly.FromDateTime(booking.CreatedAt),
                Status = booking.Status ?? "",
                CenterName = booking.Center?.CenterName ?? "",
                VehicleInfo = new VehicleSummary
                {
                    LicensePlate = booking.Vehicle?.LicensePlate ?? "",
                    ModelName = booking.Vehicle?.VehicleModel?.ModelName
                },
                ServiceName = booking.Service?.ServiceName ?? "",
                TechnicianName = "N/A", // Technician removed from Booking
                TimeSlotInfo = new TimeSlotSummary
                {
                    SlotId = booking.TechnicianTimeSlot?.SlotId ?? 0,
                    StartTime = booking.TechnicianTimeSlot?.Slot?.SlotTime.ToString("HH:mm") ?? "",
                    EndTime = booking.TechnicianTimeSlot?.Slot?.SlotTime.AddMinutes(30).ToString("HH:mm") ?? ""
                },
                // TotalCost field removed from summary model in response list; omit
                CreatedAt = booking.CreatedAt
            };
        }

        private Task<BookingHistoryResponse> MapToBookingHistoryResponse(Booking booking)
        {
            var response = new BookingHistoryResponse
            {
                BookingId = booking.BookingId,
                BookingCode = "",
                BookingDate = DateOnly.FromDateTime(booking.CreatedAt),
                Status = booking.Status ?? "",
                PartsUsed = new List<PartUsedInfo>(),
                Timeline = new List<StatusTimelineInfo>(),
                CenterInfo = new CenterInfo
                {
                    CenterId = booking.CenterId,
                    CenterName = booking.Center?.CenterName ?? "",
                    CenterAddress = booking.Center?.Address ?? "",
                    PhoneNumber = booking.Center?.PhoneNumber
                },
                VehicleInfo = new VehicleInfo
                {
                    VehicleId = booking.VehicleId,
                    LicensePlate = booking.Vehicle?.LicensePlate ?? "",
                    Vin = booking.Vehicle?.Vin ?? "",
                    ModelName = booking.Vehicle?.VehicleModel?.ModelName,
                    Year = null
                },
                ServiceInfo = new BookingServiceInfo
                {
                    ServiceId = booking.ServiceId,
                    ServiceName = booking.Service?.ServiceName ?? "",
                    Description = booking.Service?.Description ?? "",
                    BasePrice = booking.Service?.BasePrice ?? 0,
                    EstimatedDuration = null // This would need to be added to Service entity
                },
                TimeSlotInfo = new TimeSlotInfo
                {
                    Time = booking.TechnicianTimeSlot?.Slot?.SlotTime.ToString(@"hh\:mm") ?? "",
                    IsAvailable = booking.TechnicianTimeSlot?.IsAvailable ?? false,
                    BookingId = booking.TechnicianTimeSlot?.BookingId
                },
                CostInfo = new CostInfo
                {
                    ServiceCost = booking.Service?.BasePrice ?? 0,
                    PartsCost = 0,
                    TotalCost = booking.Service?.BasePrice ?? 0,
                    Discount = 0,
                    FinalCost = booking.Service?.BasePrice ?? 0
                },
                CreatedAt = booking.CreatedAt,
                UpdatedAt = booking.UpdatedAt
            };

            // Technician info removed from Booking - will be handled in WorkOrder
            response.TechnicianInfo = null;

            // WorkOrder functionality merged into Booking - no separate work order needed
            response.WorkOrderInfo = new WorkOrderInfo
            {
                WorkOrderId = booking.BookingId, // Use BookingId as WorkOrderId
                WorkOrderNumber = $"WO-#{booking.BookingId}",
                ActualDuration = null,
                WorkPerformed = null,
                CustomerComplaints = null,
                InitialMileage = booking.CurrentMileage,
                FinalMileage = null,
                StartTime = null,
                EndTime = null
            };

            // Calculate parts cost from WorkOrderParts (now linked to Booking)
            var partsCost = 0m; // Will be calculated from WorkOrderParts if needed
            response.CostInfo.PartsCost = partsCost;
            response.CostInfo.FinalCost = response.CostInfo.ServiceCost + partsCost;

            // Parts used will be handled separately through WorkOrderParts repository
            response.PartsUsed = new List<PartUsedInfo>();

            // Get payment info from Invoice
            var payment = booking.Invoices?.FirstOrDefault()?.Payments?.FirstOrDefault();
            if (payment != null)
            {
                response.PaymentInfo = new PaymentInfo
                {
                    PaymentId = payment.PaymentId,
                    PaymentStatus = payment.Status ?? "",
                    PaymentMethod = payment.PaymentMethod ?? "",
                    PaidAt = payment.PaidAt,
                    Amount = payment.Amount
                };
            }

            // Generate timeline
            response.Timeline = GenerateStatusTimeline(booking);

            return Task.FromResult(response);
        }

        private List<StatusTimelineInfo> GenerateStatusTimeline(Booking booking)
        {
            var timeline = new List<StatusTimelineInfo>();

            // Add booking creation
            timeline.Add(new StatusTimelineInfo
            {
                Status = BookingStatusConstants.Pending,
                Timestamp = booking.CreatedAt,
                Note = "Đặt lịch thành công"
            });

            // Add status changes based on booking status
            if (booking.Status == BookingStatusConstants.Confirmed || booking.Status == BookingStatusConstants.InProgress || booking.Status == BookingStatusConstants.Completed)
            {
                timeline.Add(new StatusTimelineInfo
                {
                    Status = BookingStatusConstants.Confirmed,
                    Timestamp = booking.UpdatedAt,
                    Note = "Xác nhận lịch hẹn"
                });
            }

            if (booking.Status == BookingStatusConstants.InProgress || booking.Status == BookingStatusConstants.Completed)
            {
                timeline.Add(new StatusTimelineInfo
                {
                    Status = BookingStatusConstants.InProgress,
                    Timestamp = booking.UpdatedAt,
                    Note = "Bắt đầu thực hiện dịch vụ"
                });
            }

            if (booking.Status == BookingStatusConstants.Completed)
            {
                timeline.Add(new StatusTimelineInfo
                {
                    Status = BookingStatusConstants.Completed,
                    Timestamp = booking.UpdatedAt,
                    Note = "Hoàn thành dịch vụ"
                });
            }

            if (booking.Status == BookingStatusConstants.Cancelled)
            {
                timeline.Add(new StatusTimelineInfo
                {
                    Status = BookingStatusConstants.Cancelled,
                    Timestamp = booking.UpdatedAt,
                    Note = "Hủy lịch hẹn"
                });
            }

            return timeline.OrderBy(t => t.Timestamp).ToList();
        }

        private StatusBreakdown CalculateStatusBreakdown(List<Booking> bookings)
        {
            return new StatusBreakdown
            {
                Completed = bookings.Count(b => b.Status == BookingStatusConstants.Completed),
                Cancelled = bookings.Count(b => b.Status == BookingStatusConstants.Cancelled),
                Pending = bookings.Count(b => b.Status == BookingStatusConstants.Pending),
                InProgress = bookings.Count(b => b.Status == BookingStatusConstants.InProgress),
                Confirmed = bookings.Count(b => b.Status == BookingStatusConstants.Confirmed)
            };
        }

        private FavoriteService CalculateFavoriteService(List<Booking> bookings)
        {
            var serviceGroups = bookings
                .Where(b => b.Service != null)
                .GroupBy(b => new { b.ServiceId, b.Service.ServiceName })
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            if (serviceGroups != null)
            {
                return new FavoriteService
                {
                    ServiceId = serviceGroups.Key.ServiceId,
                    ServiceName = serviceGroups.Key.ServiceName,
                    Count = serviceGroups.Count()
                };
            }

            return new FavoriteService { ServiceId = 0, ServiceName = "Không có", Count = 0 };
        }

        private FavoriteCenter CalculateFavoriteCenter(List<Booking> bookings)
        {
            var centerGroups = bookings
                .Where(b => b.Center != null)
                .GroupBy(b => new { b.CenterId, b.Center.CenterName })
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            if (centerGroups != null)
            {
                return new FavoriteCenter
                {
                    CenterId = centerGroups.Key.CenterId,
                    CenterName = centerGroups.Key.CenterName,
                    Count = centerGroups.Count()
                };
            }

            return new FavoriteCenter { CenterId = 0, CenterName = "Không có", Count = 0 };
        }

        private RecentActivity CalculateRecentActivity(List<Booking> bookings)
        {
            var completedBookings = bookings.Where(b => b.Status == BookingStatusConstants.Completed).OrderByDescending(b => b.CreatedAt);
            var lastBooking = completedBookings.FirstOrDefault();

            return new RecentActivity
            {
                LastBookingDate = lastBooking?.CreatedAt,
                LastService = lastBooking?.Service?.ServiceName,
                DaysSinceLastVisit = lastBooking != null ? (DateTime.Now - lastBooking.CreatedAt).Days : null
            };
        }
    }
}
