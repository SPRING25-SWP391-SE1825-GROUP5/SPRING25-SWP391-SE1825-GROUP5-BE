using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using EVServiceCenter.Application.Models;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace EVServiceCenter.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TechnicianTimeSlotController : ControllerBase
    {
        private readonly ITechnicianTimeSlotService _technicianTimeSlotService;
        private readonly ITechnicianRepository _technicianRepository;
        private readonly ITechnicianAvailabilityService _technicianAvailabilityService;

        public TechnicianTimeSlotController(
            ITechnicianTimeSlotService technicianTimeSlotService,
            ITechnicianRepository technicianRepository,
            ITechnicianAvailabilityService technicianAvailabilityService)
        {
            _technicianTimeSlotService = technicianTimeSlotService;
            _technicianRepository = technicianRepository;
            _technicianAvailabilityService = technicianAvailabilityService;
        }

        [HttpGet]
        [Authorize(Policy = "StaffOrAdmin")]
        public async Task<IActionResult> GetAllTechnicianTimeSlots()
        {
            try
            {
                var technicians = await _technicianRepository.GetAllTechniciansAsync();
                var allTimeSlots = new List<TechnicianTimeSlotResponse>();

                foreach (var technician in technicians)
                {
                    var timeSlots = await _technicianTimeSlotService.GetTechnicianTimeSlotsByTechnicianIdAsync(technician.TechnicianId);
                    allTimeSlots.AddRange(timeSlots);
                }

                return Ok(new
                {
                    success = true,
                    message = "Lấy danh sách lịch technician thành công",
                    data = allTimeSlots,
                    total = allTimeSlots.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi khi lấy danh sách lịch technician",
                    error = ex.Message
                });
            }
        }

        [HttpPost("technician/{technicianId}/full-week-all-slots")]
        [Authorize(Policy = "StaffOrAdminOrManager")]
        public async Task<IActionResult> CreateFullWeekAllSlots(int technicianId, [FromBody] CreateTechnicianFullWeekAllSlotsRequest req)
        {
            if (req == null) return BadRequest(new { success = false, message = "Body rỗng" });
            req.TechnicianId = technicianId;
            var result = await _technicianTimeSlotService.CreateTechnicianFullWeekAllSlotsAsync(req);
            if (!result.Success) return BadRequest(new { success = false, message = result.Message, errors = result.Errors });
            return Ok(new {
                success = true,
                message = result.Message,
                totalDays = result.TotalDays,
                totalSlotsCreated = result.TotalSlotsCreated,
                totalSlotsSkipped = result.TotalSlotsSkipped,
                weekendDaysSkipped = result.WeekendDaysSkipped,
                weekendDatesSkipped = result.WeekendDatesSkipped,
                duplicateSlotsInfo = result.DuplicateSlotsInfo
            });
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "StaffOrAdmin")]
        public async Task<IActionResult> GetTechnicianTimeSlotById(int id)
        {
            try
            {
                var timeSlot = await _technicianTimeSlotService.GetTechnicianTimeSlotByIdAsync(id);
                if (timeSlot == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Không tìm thấy lịch technician với ID đã cho"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Lấy lịch technician thành công",
                    data = timeSlot
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi khi lấy lịch technician",
                    error = ex.Message
                });
            }
        }

        [HttpGet("technician/{technicianId}")]
        [Authorize(Policy = "StaffOrAdmin")]
        public async Task<IActionResult> GetTechnicianTimeSlotsByTechnicianId(int technicianId)
        {
            try
            {
                var timeSlots = await _technicianTimeSlotService.GetTechnicianTimeSlotsByTechnicianIdAsync(technicianId);
                return Ok(new
                {
                    success = true,
                    message = "Lấy lịch technician thành công",
                    data = timeSlots,
                    total = timeSlots.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi khi lấy lịch technician",
                    error = ex.Message
                });
            }
        }

        [HttpGet("center/{centerId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTechnicianTimeSlotsByCenterId(
            int centerId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 100)
        {
            try
            {
                var technicians = await _technicianRepository.GetTechniciansByCenterIdAsync(centerId);
                var allTimeSlots = new List<TechnicianTimeSlotResponse>();

                foreach (var technician in technicians)
                {
                    var timeSlots = await _technicianTimeSlotService.GetTechnicianTimeSlotsByTechnicianIdAsync(technician.TechnicianId);
                    if (startDate.HasValue || endDate.HasValue)
                    {
                        var s = startDate?.Date ?? DateTime.MinValue.Date;
                        var e = endDate?.Date ?? DateTime.MaxValue.Date;
                        timeSlots = timeSlots
                            .Where(ts => ts.WorkDate.Date >= s && ts.WorkDate.Date <= e)
                            .ToList();
                    }
                    allTimeSlots.AddRange(timeSlots);
                }
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 1; else if (pageSize > 100) pageSize = 100;
                var total = allTimeSlots.Count;
                var items = allTimeSlots
                    .OrderBy(ts => ts.WorkDate)
                    .ThenBy(ts => ts.SlotTime)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Ok(new
                {
                    success = true,
                    message = "Lấy lịch technician theo trung tâm thành công",
                    data = items,
                    total
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi khi lấy lịch technician theo trung tâm",
                    error = ex.Message
                });
            }
        }

        [HttpGet("technician/{technicianId}/center/{centerId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTechnicianTimeSlotsByTechnicianAndCenter(int technicianId, int centerId)
        {
            try
            {
                var technician = await _technicianRepository.GetTechnicianByIdAsync(technicianId);
                if (technician == null || technician.CenterId != centerId)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Không tìm thấy technician trong center này"
                    });
                }

                var timeSlots = await _technicianTimeSlotService.GetTechnicianTimeSlotsByTechnicianIdAsync(technicianId);
                return Ok(new
                {
                    success = true,
                    message = "Lấy lịch technician theo technician và trung tâm thành công",
                    data = timeSlots,
                    total = timeSlots.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi khi lấy lịch technician theo technician và trung tâm",
                    error = ex.Message
                });
            }
        }

        [HttpGet("day/{dayOfWeek}")]
        [Authorize(Policy = "StaffOrAdmin")]
        public async Task<IActionResult> GetTechnicianTimeSlotsByDayOfWeek(byte dayOfWeek)
        {
            try
            {
                if (dayOfWeek < 1 || dayOfWeek > 6)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Ngày trong tuần phải từ 1 (Thứ 2) đến 6 (Thứ 7)"
                    });
                }

                var technicians = await _technicianRepository.GetAllTechniciansAsync();
                var allTimeSlots = new List<TechnicianTimeSlotResponse>();

                foreach (var technician in technicians)
                {
                    var timeSlots = await _technicianTimeSlotService.GetTechnicianTimeSlotsByTechnicianIdAsync(technician.TechnicianId);
                    var filteredSlots = timeSlots.Where(ts => ((int)ts.WorkDate.DayOfWeek + 6) % 7 + 1 == dayOfWeek).ToList();
                    allTimeSlots.AddRange(filteredSlots);
                }

                return Ok(new
                {
                    success = true,
                    message = "Lấy lịch technician theo ngày thành công",
                    data = allTimeSlots,
                    total = allTimeSlots.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi khi lấy lịch technician theo ngày",
                    error = ex.Message
                });
            }
        }

        [HttpPost]
        [Authorize(Policy = "StaffOrAdminOrManager")]
        public async Task<IActionResult> CreateTechnicianTimeSlot([FromBody] CreateTechnicianTimeSlotRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(new
                    {
                        success = false,
                        message = "Dữ liệu đầu vào không hợp lệ",
                        errors = errors
                    });
                }

                var result = await _technicianTimeSlotService.CreateTechnicianTimeSlotAsync(request);

                if (result.Success)
                {
                    return Ok(new
                    {
                        success = true,
                        message = result.Message,
                        data = result.CreatedTimeSlot
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = result.Message,
                        errors = result.Errors
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi khi tạo lịch technician",
                    error = ex.Message
                });
            }
        }

        [HttpPost("weekly")]
        [Authorize(Policy = "StaffOrAdminOrManager")]
        public async Task<IActionResult> CreateWeeklyTechnicianTimeSlot([FromBody] CreateWeeklyTechnicianTimeSlotRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(new
                    {
                        success = false,
                        message = "Dữ liệu đầu vào không hợp lệ",
                        errors = errors
                    });
                }

                var result = await _technicianTimeSlotService.CreateWeeklyTechnicianTimeSlotAsync(request);

                if (result.Success)
                {
                    return Ok(new
                    {
                        success = true,
                        message = result.Message,
                        totalCreated = result.TotalCreated,
                        data = result.CreatedTimeSlots,
                        errors = result.Errors
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = result.Message,
                        errors = result.Errors
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi khi tạo lịch tuần cho technician",
                    error = ex.Message
                });
            }
        }

        [HttpPost("all-technicians")]
        [Authorize(Policy = "StaffOrAdminOrManager")]
        public async Task<IActionResult> CreateAllTechniciansTimeSlot([FromBody] CreateAllTechniciansTimeSlotRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(new
                    {
                        success = false,
                        message = "Dữ liệu đầu vào không hợp lệ",
                        errors = errors
                    });
                }

                var result = await _technicianTimeSlotService.CreateAllTechniciansTimeSlotAsync(request);

                if (result.Success)
                {
                    return Ok(new
                    {
                        success = true,
                        message = result.Message,
                        totalTechnicians = result.TotalTechnicians,
                        totalTimeSlotsCreated = result.TotalTimeSlotsCreated,
                        data = result.TechnicianTimeSlots,
                        errors = result.Errors
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = result.Message,
                        errors = result.Errors
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi khi tạo lịch cho tất cả technician",
                    error = ex.Message
                });
            }
        }

        [HttpPost("all-technicians-weekly")]
        [Authorize(Policy = "StaffOrAdminOrManager")]
        public async Task<IActionResult> CreateAllTechniciansWeeklyTimeSlot([FromBody] CreateAllTechniciansWeeklyTimeSlotRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(new
                    {
                        success = false,
                        message = "Dữ liệu đầu vào không hợp lệ",
                        errors = errors
                    });
                }

                var result = await _technicianTimeSlotService.CreateAllTechniciansWeeklyTimeSlotAsync(request);

                if (result.Success)
                {
                    return Ok(new
                    {
                        success = true,
                        message = result.Message,
                        totalTechnicians = result.TotalTechnicians,
                        totalTimeSlotsCreated = result.TotalTimeSlotsCreated,
                        data = result.TechnicianTimeSlots,
                        errors = result.Errors
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = result.Message,
                        errors = result.Errors
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi khi tạo lịch tuần cho tất cả technician",
                    error = ex.Message
                });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "StaffOrAdminOrManager")]
        public async Task<IActionResult> UpdateTechnicianTimeSlot(int id, [FromBody] UpdateTechnicianTimeSlotRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(new
                    {
                        success = false,
                        message = "Dữ liệu đầu vào không hợp lệ",
                        errors = errors
                    });
                }

                var result = await _technicianTimeSlotService.UpdateTechnicianTimeSlotAsync(id, request);

                return Ok(new
                {
                    success = true,
                    message = "Cập nhật lịch technician thành công",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi khi cập nhật lịch technician",
                    error = ex.Message
                });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "StaffOrAdminOrManager")]
        public async Task<IActionResult> DeleteTechnicianTimeSlot(int id)
        {
            try
            {
                var result = await _technicianTimeSlotService.DeleteTechnicianTimeSlotAsync(id);

                if (result)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Xóa lịch technician thành công"
                    });
                }
                else
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Không tìm thấy lịch technician với ID đã cho"
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi khi xóa lịch technician",
                    error = ex.Message
                });
            }
        }

        [HttpGet("technician/{technicianId}/schedule")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTechnicianSchedule(int technicianId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            try
            {
                var data = await _technicianTimeSlotService.GetTechnicianDailyScheduleAsync(technicianId, startDate, endDate);
                return Ok(new
                {
                    success = true,
                    message = "Lấy lịch làm việc technician thành công",
                    data,
                    total = data.Count
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("center/{centerId}/schedule")]
        [Authorize(Policy = "StaffOrAdmin")]
        public async Task<IActionResult> GetCenterTechnicianSchedule(int centerId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            try
            {
                var data = await _technicianTimeSlotService.GetCenterTechnicianScheduleAsync(centerId, startDate, endDate);
                return Ok(new { success = true, data, total = data.Count });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("centers/{centerId}/availability")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCenterTechniciansAvailability(
            [FromRoute] int centerId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 30)
        {
            try
            {
                if (centerId <= 0)
                {
                    return BadRequest(new TechnicianAvailabilityResponse
                    {
                        Success = false,
                        Message = "CenterId phải lớn hơn 0"
                    });
                }

                if (page <= 0)
                {
                    return BadRequest(new TechnicianAvailabilityResponse
                    {
                        Success = false,
                        Message = "Page phải lớn hơn 0"
                    });
                }

                if (pageSize <= 0 || pageSize > 100)
                {
                    return BadRequest(new TechnicianAvailabilityResponse
                    {
                        Success = false,
                        Message = "PageSize phải từ 1 đến 100"
                    });
                }

                var result = await _technicianAvailabilityService.GetCenterTechniciansAvailabilityAsync(
                    centerId, startDate, endDate, page, pageSize);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new TechnicianAvailabilityResponse
                {
                    Success = false,
                    Message = $"Lỗi hệ thống: {ex.Message}"
                });
            }
        }

        [HttpGet("centers/{centerId}/technicians/{technicianId}/availability")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTechnicianAvailability(
            [FromRoute] int centerId,
            [FromRoute] int technicianId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 30)
        {
            try
            {
                if (centerId <= 0)
                {
                    return BadRequest(new TechnicianAvailabilityResponse
                    {
                        Success = false,
                        Message = "CenterId phải lớn hơn 0"
                    });
                }

                if (technicianId <= 0)
                {
                    return BadRequest(new TechnicianAvailabilityResponse
                    {
                        Success = false,
                        Message = "TechnicianId phải lớn hơn 0"
                    });
                }

                if (page <= 0)
                {
                    return BadRequest(new TechnicianAvailabilityResponse
                    {
                        Success = false,
                        Message = "Page phải lớn hơn 0"
                    });
                }

                if (pageSize <= 0 || pageSize > 100)
                {
                    return BadRequest(new TechnicianAvailabilityResponse
                    {
                        Success = false,
                        Message = "PageSize phải từ 1 đến 100"
                    });
                }

                var result = await _technicianAvailabilityService.GetTechnicianAvailabilityAsync(
                    centerId, technicianId, startDate, endDate, page, pageSize);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new TechnicianAvailabilityResponse
                {
                    Success = false,
                    Message = $"Lỗi hệ thống: {ex.Message}"
                });
            }
        }
    }
}
