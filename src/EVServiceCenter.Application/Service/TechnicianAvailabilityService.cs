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
    public class TechnicianAvailabilityService : ITechnicianAvailabilityService
    {
        private readonly ITechnicianRepository _technicianRepository;
        private readonly ITimeSlotRepository _timeSlotRepository;
        private readonly ITechnicianTimeSlotRepository _technicianTimeSlotRepository;
        private readonly ICenterRepository _centerRepository;
        private readonly ILogger<TechnicianAvailabilityService> _logger;

        public TechnicianAvailabilityService(
            ITechnicianRepository technicianRepository,
            ITimeSlotRepository timeSlotRepository,
            ITechnicianTimeSlotRepository technicianTimeSlotRepository,
            ICenterRepository centerRepository,
            ILogger<TechnicianAvailabilityService> logger)
        {
            _technicianRepository = technicianRepository;
            _timeSlotRepository = timeSlotRepository;
            _technicianTimeSlotRepository = technicianTimeSlotRepository;
            _centerRepository = centerRepository;
            _logger = logger;
        }

        public async Task<TechnicianAvailabilityResponse> GetCenterTechniciansAvailabilityAsync(
            int centerId,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int page = 1,
            int pageSize = 30)
        {
            try
            {
                // Validate center exists
                var center = await _centerRepository.GetCenterByIdAsync(centerId);
                if (center == null)
                {
                    return new TechnicianAvailabilityResponse
                    {
                        Success = false,
                        Message = $"Không tìm thấy trung tâm với ID: {centerId}"
                    };
                }

                // Get all technicians in center
                var technicians = await _technicianRepository.GetTechniciansByCenterIdAsync(centerId);
                if (!technicians.Any())
                {
                    return new TechnicianAvailabilityResponse
                    {
                        Success = true,
                        Message = "Không có technician nào trong trung tâm này",
                        Data = new List<TechnicianAvailabilityData>(),
                               Summary = new AvailabilitySummary
                        {
                            CenterId = centerId,
                            CenterName = center.CenterName,
                            TotalTechnicians = 0
                        }
                    };
                }

                // Calculate date range
                var dateRange = CalculateDateRange(startDate, endDate);
                var dates = GenerateDateList(dateRange.StartDate, dateRange.EndDate);

                // Get availability data
                var availabilityData = new List<TechnicianAvailabilityData>();
                foreach (var date in dates)
                {
                    var dayData = await CalculateDayAvailability(centerId, technicians, date);
                    availabilityData.Add(dayData);
                }

                // Apply pagination
                var totalRecords = availabilityData.Count;
                var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
                var paginatedData = availabilityData
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Calculate statistics
                var statistics = CalculateStatistics(availabilityData);

                return new TechnicianAvailabilityResponse
                {
                    Success = true,
                    Message = "Lấy thông tin availability thành công",
                    Data = paginatedData,
                    Pagination = new PaginationInfo
                    {
                        Page = page,
                        PageSize = pageSize,
                        TotalPages = totalPages,
                        TotalRecords = totalRecords
                    },
                               Summary = new AvailabilitySummary
                    {
                        CenterId = centerId,
                        CenterName = center.CenterName,
                        TotalTechnicians = technicians.Count,
                        DateRange = new DateRangeInfo
                        {
                            StartDate = dateRange.StartDate.ToString("yyyy-MM-dd"),
                            EndDate = dateRange.EndDate.ToString("yyyy-MM-dd")
                        },
                        Statistics = statistics
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy availability của center {CenterId}", centerId);
                return new TechnicianAvailabilityResponse
                {
                    Success = false,
                    Message = $"Lỗi hệ thống: {ex.Message}"
                };
            }
        }

        public async Task<TechnicianAvailabilityResponse> GetTechnicianAvailabilityAsync(
            int centerId,
            int technicianId,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int page = 1,
            int pageSize = 30)
        {
            try
            {
                // Validate center exists
                var center = await _centerRepository.GetCenterByIdAsync(centerId);
                if (center == null)
                {
                    return new TechnicianAvailabilityResponse
                    {
                        Success = false,
                        Message = $"Không tìm thấy trung tâm với ID: {centerId}"
                    };
                }

                // Validate technician exists and belongs to center
                var technician = await _technicianRepository.GetTechnicianByIdAsync(technicianId);
                if (technician == null || technician.CenterId != centerId)
                {
                    return new TechnicianAvailabilityResponse
                    {
                        Success = false,
                        Message = $"Không tìm thấy technician với ID: {technicianId} trong trung tâm {centerId}"
                    };
                }

                // Calculate date range
                var dateRange = CalculateDateRange(startDate, endDate);
                var dates = GenerateDateList(dateRange.StartDate, dateRange.EndDate);

                // Get availability data for single technician
                var availabilityData = new List<TechnicianAvailabilityData>();
                foreach (var date in dates)
                {
                    var dayData = await CalculateSingleTechnicianAvailability(centerId, technician, date);
                    availabilityData.Add(dayData);
                }

                // Apply pagination
                var totalRecords = availabilityData.Count;
                var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
                var paginatedData = availabilityData
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Calculate statistics
                var statistics = CalculateStatistics(availabilityData);

                return new TechnicianAvailabilityResponse
                {
                    Success = true,
                    Message = "Lấy thông tin availability thành công",
                    Data = paginatedData,
                    Pagination = new PaginationInfo
                    {
                        Page = page,
                        PageSize = pageSize,
                        TotalPages = totalPages,
                        TotalRecords = totalRecords
                    },
                               Summary = new AvailabilitySummary
                    {
                        CenterId = centerId,
                        CenterName = center.CenterName,
                        TotalTechnicians = 1,
                        DateRange = new DateRangeInfo
                        {
                            StartDate = dateRange.StartDate.ToString("yyyy-MM-dd"),
                            EndDate = dateRange.EndDate.ToString("yyyy-MM-dd")
                        },
                        Statistics = statistics
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy availability của technician {TechnicianId} trong center {CenterId}", technicianId, centerId);
                return new TechnicianAvailabilityResponse
                {
                    Success = false,
                    Message = $"Lỗi hệ thống: {ex.Message}"
                };
            }
        }

        private (DateTime StartDate, DateTime EndDate) CalculateDateRange(DateTime? startDate, DateTime? endDate)
        {
            var start = startDate ?? DateTime.Today;
            var end = endDate ?? start.AddDays(30); // Default 30 days if no end date

            return (start, end);
        }

        private List<DateTime> GenerateDateList(DateTime startDate, DateTime endDate)
        {
            var dates = new List<DateTime>();
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                dates.Add(date);
            }
            return dates;
        }

        private async Task<TechnicianAvailabilityData> CalculateDayAvailability(
            int centerId, 
            List<Domain.Entities.Technician> technicians, 
            DateTime date)
        {
            var totalSlots = 0;
            var bookedSlots = 0;
            var technicianInfos = new List<TechnicianInfo>();

            foreach (var technician in technicians)
            {
                var technicianSlots = await _technicianTimeSlotRepository.GetTechnicianTimeSlotsByTechnicianAndDateAsync(technician.TechnicianId, date);
                var technicianBookedSlots = technicianSlots.Count(ts => !ts.IsAvailable);
                
                totalSlots += technicianSlots.Count();
                bookedSlots += technicianBookedSlots;

                technicianInfos.Add(new TechnicianInfo
                {
                    TechnicianId = technician.TechnicianId,
                    Name = technician.User?.FullName ?? "Unknown",
                    IsAvailable = technicianBookedSlots < technicianSlots.Count(),
                    BookedSlots = technicianBookedSlots,
                    TotalSlots = technicianSlots.Count()
                });
            }

            return new TechnicianAvailabilityData
            {
                Date = date.ToString("yyyy-MM-dd"),
                IsFullyBooked = bookedSlots == totalSlots && totalSlots > 0,
                TotalSlots = totalSlots,
                BookedSlots = bookedSlots,
                AvailableSlots = totalSlots - bookedSlots,
                Technicians = technicianInfos
            };
        }

        private async Task<TechnicianAvailabilityData> CalculateSingleTechnicianAvailability(
            int centerId,
            Domain.Entities.Technician technician,
            DateTime date)
        {
                var technicianSlots = await _technicianTimeSlotRepository.GetTechnicianTimeSlotsByTechnicianAndDateAsync(technician.TechnicianId, date);
            var bookedSlots = technicianSlots.Count(ts => !ts.IsAvailable);
            var totalSlots = technicianSlots.Count();

            // Get time slot details
            var timeSlots = new List<TimeSlotInfo>();
            foreach (var slot in technicianSlots)
            {
                timeSlots.Add(new TimeSlotInfo
                {
                    Time = slot.Slot?.SlotTime.ToString(@"hh\:mm") ?? "Unknown",
                    IsAvailable = slot.IsAvailable,
                    BookingId = slot.BookingId
                });
            }

            return new TechnicianAvailabilityData
            {
                Date = date.ToString("yyyy-MM-dd"),
                TechnicianId = technician.TechnicianId,
                TechnicianName = technician.User?.FullName ?? "Unknown",
                IsFullyBooked = bookedSlots == totalSlots && totalSlots > 0,
                TotalSlots = totalSlots,
                BookedSlots = bookedSlots,
                AvailableSlots = totalSlots - bookedSlots,
                TimeSlots = timeSlots
            };
        }

        private StatisticsInfo CalculateStatistics(List<TechnicianAvailabilityData> data)
        {
            var fullyBookedDays = data.Count(d => d.IsFullyBooked);
            var partiallyBookedDays = data.Count(d => !d.IsFullyBooked && d.BookedSlots > 0);
            var availableDays = data.Count(d => d.BookedSlots == 0);

            return new StatisticsInfo
            {
                FullyBookedDays = fullyBookedDays,
                PartiallyBookedDays = partiallyBookedDays,
                AvailableDays = availableDays
            };
        }
    }
}
