using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
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

        public TechnicianTimeSlotController(ITechnicianTimeSlotService technicianTimeSlotService, ITechnicianRepository technicianRepository)
        {
            _technicianTimeSlotService = technicianTimeSlotService;
            _technicianRepository = technicianRepository;
        }

        /// <summary>
        /// Lấy tất cả technician time slots
        /// </summary>
        /// <returns>Danh sách technician time slots</returns>
        [HttpGet]
        [Authorize(Policy = "StaffOrAdmin")]
        public async Task<IActionResult> GetAllTechnicianTimeSlots()
        {
            try
            {
                // Get all technician time slots by getting all technicians and their time slots
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

        /// <summary>
        /// Lấy technician time slot theo ID
        /// </summary>
        /// <param name="id">ID của technician time slot</param>
        /// <returns>Technician time slot</returns>
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

        /// <summary>
        /// Lấy technician time slots theo technician ID
        /// </summary>
        /// <param name="technicianId">ID của technician</param>
        /// <returns>Danh sách technician time slots</returns>
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

        /// <summary>
        /// Lấy technician time slots theo center ID
        /// </summary>
        /// <param name="centerId">ID của center</param>
        /// <returns>Danh sách technician time slots</returns>
        [HttpGet("center/{centerId}")]
        [Authorize(Policy = "StaffOrAdmin")]
        public async Task<IActionResult> GetTechnicianTimeSlotsByCenterId(int centerId)
        {
            try
            {
                // Get technicians in the center first
                var technicians = await _technicianRepository.GetTechniciansByCenterIdAsync(centerId);
                var allTimeSlots = new List<TechnicianTimeSlotResponse>();
                
                foreach (var technician in technicians)
                {
                    var timeSlots = await _technicianTimeSlotService.GetTechnicianTimeSlotsByTechnicianIdAsync(technician.TechnicianId);
                    allTimeSlots.AddRange(timeSlots);
                }
                
                return Ok(new
                {
                    success = true,
                    message = "Lấy lịch technician theo trung tâm thành công",
                    data = allTimeSlots,
                    total = allTimeSlots.Count
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

        /// <summary>
        /// Lấy technician time slots theo technician và center
        /// </summary>
        /// <param name="technicianId">ID của technician</param>
        /// <param name="centerId">ID của center</param>
        /// <returns>Danh sách technician time slots</returns>
        [HttpGet("technician/{technicianId}/center/{centerId}")]
        [Authorize(Policy = "StaffOrAdmin")]
        public async Task<IActionResult> GetTechnicianTimeSlotsByTechnicianAndCenter(int technicianId, int centerId)
        {
            try
            {
                // Verify technician belongs to center first
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

        /// <summary>
        /// Lấy technician time slots theo ngày trong tuần
        /// </summary>
        /// <param name="dayOfWeek">Ngày trong tuần (1-6)</param>
        /// <returns>Danh sách technician time slots</returns>
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

                // Get all technicians and filter by day of week
                var technicians = await _technicianRepository.GetAllTechniciansAsync();
                var allTimeSlots = new List<TechnicianTimeSlotResponse>();
                
                foreach (var technician in technicians)
                {
                    var timeSlots = await _technicianTimeSlotService.GetTechnicianTimeSlotsByTechnicianIdAsync(technician.TechnicianId);
                    // Filter by day of week (assuming WorkDate is DateTime)
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

        /// <summary>
        /// Tạo technician time slot mới
        /// </summary>
        /// <param name="request">Thông tin tạo technician time slot</param>
        /// <returns>Kết quả tạo technician time slot</returns>
        [HttpPost]
        [Authorize(Policy = "StaffOrAdmin")]
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

        /// <summary>
        /// Tạo lịch tuần cho technician
        /// </summary>
        /// <param name="request">Thông tin tạo lịch tuần</param>
        /// <returns>Kết quả tạo lịch tuần</returns>
        [HttpPost("weekly")]
        [Authorize(Policy = "StaffOrAdmin")]
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

        /// <summary>
        /// Tạo lịch cho tất cả technician trong 1 ngày
        /// </summary>
        /// <param name="request">Thông tin tạo lịch</param>
        /// <returns>Kết quả tạo lịch</returns>
        [HttpPost("all-technicians")]
        [Authorize(Policy = "StaffOrAdmin")]
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

        /// <summary>
        /// Tạo lịch tuần cho tất cả technician
        /// </summary>
        /// <param name="request">Thông tin tạo lịch tuần</param>
        /// <returns>Kết quả tạo lịch tuần</returns>
        [HttpPost("all-technicians-weekly")]
        [Authorize(Policy = "StaffOrAdmin")]
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

        /// <summary>
        /// Cập nhật technician time slot
        /// </summary>
        /// <param name="id">ID của technician time slot</param>
        /// <param name="request">Thông tin cập nhật</param>
        /// <returns>Kết quả cập nhật</returns>
        [HttpPut("{id}")]
        [Authorize(Policy = "StaffOrAdmin")]
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

        /// <summary>
        /// Xóa technician time slot
        /// </summary>
        /// <param name="id">ID của technician time slot</param>
        /// <returns>Kết quả xóa</returns>
        [HttpDelete("{id}")]
        [Authorize(Policy = "StaffOrAdmin")]
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
        [Authorize(Policy = "StaffOrAdmin")]
        public async Task<IActionResult> GetTechnicianSchedule(int technicianId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            try
            {
                var data = await _technicianTimeSlotService.GetTechnicianScheduleAsync(technicianId, startDate, endDate);
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

        
    }
}
