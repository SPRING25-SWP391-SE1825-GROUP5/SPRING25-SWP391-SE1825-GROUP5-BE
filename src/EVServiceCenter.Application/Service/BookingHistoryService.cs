using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Responses;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;

namespace EVServiceCenter.Application.Service
{
    public class BookingHistoryService : IBookingHistoryService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IWorkOrderRepository _workOrderRepository;

        public BookingHistoryService(
            IBookingRepository bookingRepository,
            ICustomerRepository customerRepository,
            IWorkOrderRepository workOrderRepository)
        {
            _bookingRepository = bookingRepository;
            _customerRepository = customerRepository;
            _workOrderRepository = workOrderRepository;
        }

        public async Task<BookingHistoryListResponse> GetBookingHistoryAsync(int customerId, int page = 1, int pageSize = 10, 
            string? status = null, DateTime? fromDate = null, DateTime? toDate = null, 
            string sortBy = "createdAt", string sortOrder = "desc")
        {
            // Validate customer exists
            var customer = await _customerRepository.GetCustomerByIdAsync(customerId);
            if (customer == null)
            {
                throw new KeyNotFoundException($"Customer with ID {customerId} not found.");
            }

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
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
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
            // Validate customer exists
            var customer = await _customerRepository.GetCustomerByIdAsync(customerId);
            if (customer == null)
            {
                throw new KeyNotFoundException($"Customer with ID {customerId} not found.");
            }

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

            // Get all bookings for the period
            var allBookings = await _bookingRepository.GetBookingsByCustomerIdAsync(
                customerId, 1, int.MaxValue, null, fromDate, null, "createdAt", "desc");

            // Calculate statistics
            var totalBookings = allBookings.Count;
            var statusBreakdown = CalculateStatusBreakdown(allBookings);
            var totalSpent = allBookings.Where(b => b.Status == "COMPLETED").Sum(b => b.TotalCost ?? 0);
            var averageCost = totalBookings > 0 ? totalSpent / totalBookings : 0;

            var favoriteService = CalculateFavoriteService(allBookings);
            var favoriteCenter = CalculateFavoriteCenter(allBookings);
            var recentActivity = CalculateRecentActivity(allBookings);

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
                TotalCost = booking.TotalCost ?? 0,
                CreatedAt = booking.CreatedAt
            };
        }

        private async Task<BookingHistoryResponse> MapToBookingHistoryResponse(Booking booking)
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
                    Brand = booking.Vehicle?.VehicleModel?.Brand,
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
                    SlotId = booking.SlotId,
                    StartTime = booking.Slot?.SlotTime.ToString(@"hh\:mm") ?? "",
                    EndTime = booking.Slot?.SlotTime.AddMinutes(60).ToString(@"hh\:mm") ?? "" // Assume 1 hour duration
                },
                CostInfo = new CostInfo
                {
                    ServiceCost = booking.Service?.BasePrice ?? 0,
                    PartsCost = 0, // Will be calculated from WorkOrderParts
                    TotalCost = booking.TotalCost ?? 0,
                    Discount = 0, // Will be calculated from promotions
                    FinalCost = booking.TotalCost ?? 0
                },
                CreatedAt = booking.CreatedAt,
                UpdatedAt = booking.UpdatedAt
            };

            // Technician info removed from Booking - will be handled in WorkOrder
            response.TechnicianInfo = null;

            // Get work order details
            var workOrder = await _workOrderRepository.GetByBookingIdAsync(booking.BookingId);
            if (workOrder != null)
            {
                response.WorkOrderInfo = new WorkOrderInfo
                {
                    WorkOrderId = workOrder.WorkOrderId,
                    WorkOrderNumber = $"WO-#{workOrder.WorkOrderId}",
                    ActualDuration = null,
                    WorkPerformed = null,
                    CustomerComplaints = null,
                    InitialMileage = null,
                    FinalMileage = null,
                    StartTime = null,
                    EndTime = null
                };

                // Calculate parts cost
                var partsCost = workOrder.WorkOrderParts?.Sum(wp => wp.UnitCost * wp.QuantityUsed) ?? 0;
                response.CostInfo.PartsCost = partsCost;
                response.CostInfo.FinalCost = response.CostInfo.ServiceCost + partsCost;

                // Add parts used
                if (workOrder.WorkOrderParts != null)
                {
                    response.PartsUsed = workOrder.WorkOrderParts.Select(wp => new PartUsedInfo
                    {
                        PartId = wp.PartId,
                        PartName = wp.Part?.PartName ?? "",
                        PartNumber = wp.Part?.PartNumber,
                        Quantity = wp.QuantityUsed,
                        UnitPrice = wp.UnitCost,
                        TotalPrice = wp.UnitCost * wp.QuantityUsed
                    }).ToList();
                }

                // Get payment info
                var payment = workOrder.Invoices?.FirstOrDefault()?.Payments?.FirstOrDefault();
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
            }

            // Generate timeline
            response.Timeline = GenerateStatusTimeline(booking);

            return response;
        }

        private List<StatusTimelineInfo> GenerateStatusTimeline(Booking booking)
        {
            var timeline = new List<StatusTimelineInfo>();

            // Add booking creation
            timeline.Add(new StatusTimelineInfo
            {
                Status = "PENDING",
                Timestamp = booking.CreatedAt,
                Note = "Đặt lịch thành công"
            });

            // Add status changes based on booking status
            if (booking.Status == "CONFIRMED" || booking.Status == "IN_PROGRESS" || booking.Status == "COMPLETED")
            {
                timeline.Add(new StatusTimelineInfo
                {
                    Status = "CONFIRMED",
                    Timestamp = booking.UpdatedAt,
                    Note = "Xác nhận lịch hẹn"
                });
            }

            if (booking.Status == "IN_PROGRESS" || booking.Status == "COMPLETED")
            {
                timeline.Add(new StatusTimelineInfo
                {
                    Status = "IN_PROGRESS",
                    Timestamp = booking.UpdatedAt,
                    Note = "Bắt đầu thực hiện dịch vụ"
                });
            }

            if (booking.Status == "COMPLETED")
            {
                timeline.Add(new StatusTimelineInfo
                {
                    Status = "COMPLETED",
                    Timestamp = booking.UpdatedAt,
                    Note = "Hoàn thành dịch vụ"
                });
            }

            if (booking.Status == "CANCELLED")
            {
                timeline.Add(new StatusTimelineInfo
                {
                    Status = "CANCELLED",
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
                Completed = bookings.Count(b => b.Status == "COMPLETED"),
                Cancelled = bookings.Count(b => b.Status == "CANCELLED"),
                Pending = bookings.Count(b => b.Status == "PENDING"),
                InProgress = bookings.Count(b => b.Status == "IN_PROGRESS"),
                Confirmed = bookings.Count(b => b.Status == "CONFIRMED")
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
            var completedBookings = bookings.Where(b => b.Status == "COMPLETED").OrderByDescending(b => b.CreatedAt);
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
