using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;

namespace EVServiceCenter.Api.Controllers
{
	[ApiController]
    [Route("api/[controller]")]
	[Authorize]
    public class ReportsController : BaseController
    {
        private readonly IPartsUsageReportService _partsUsageReportService;
        private readonly IRevenueReportService _revenueReportService;
        private readonly IBookingReportsService _bookingReportsService;
        private readonly ITechnicianReportsService _technicianReportsService;
        private readonly IInventoryReportsService _inventoryReportsService;
        private readonly IDashboardSummaryService _dashboardSummaryService;
		
		public ReportsController(
            IPartsUsageReportService partsUsageReportService, 
            IRevenueReportService revenueReportService,
            IBookingReportsService bookingReportsService,
            ITechnicianReportsService technicianReportsService,
            IInventoryReportsService inventoryReportsService,
            IDashboardSummaryService dashboardSummaryService,
            ILogger<ReportsController> logger)
            : base(logger)
        {
            _partsUsageReportService = partsUsageReportService;
            _revenueReportService = revenueReportService;
            _bookingReportsService = bookingReportsService;
            _technicianReportsService = technicianReportsService;
            _inventoryReportsService = inventoryReportsService;
            _dashboardSummaryService = dashboardSummaryService;
		}

		/// <summary>
        /// Lấy báo cáo sử dụng phụ tùng của chi nhánh
		/// </summary>
        /// <param name="centerId">ID chi nhánh</param>
        /// <param name="request">Thông tin báo cáo</param>
        /// <returns>Báo cáo sử dụng phụ tùng</returns>
        [HttpGet("parts-usage/{centerId}")]
        [Authorize(Roles = "MANAGER,ADMIN")]
        public async Task<IActionResult> GetPartsUsageReport(int centerId, [FromQuery] PartsUsageReportRequest request)
		{
			try
			{
                // Validate center access for MANAGER
                if (User.IsInRole("MANAGER"))
                {
                    // TODO: Implement GetUserCenterId method or use existing method from BaseController
                    // For now, we'll allow the request to proceed
                    // var userCenterId = GetUserCenterId();
                    // if (userCenterId != centerId)
                    // {
                    //     return Forbid("Bạn chỉ có thể xem báo cáo của chi nhánh mình");
                    // }
                }

                // Validate request
                if (request.StartDate >= request.EndDate)
                {
                    return BadRequest(new { success = false, message = "Ngày bắt đầu phải nhỏ hơn ngày kết thúc" });
                }

                if (request.StartDate > DateTime.Now)
                {
                    return BadRequest(new { success = false, message = "Ngày bắt đầu không được lớn hơn ngày hiện tại" });
                }

                // Validate pagination
                if (request.PageNumber < 1) request.PageNumber = 1;
                if (request.PageSize < 1 || request.PageSize > 100) request.PageSize = 20;

                var result = await _partsUsageReportService.GetPartsUsageReportAsync(centerId, request);

                return Ok(new
                {
                    success = true,
                    message = "Lấy báo cáo sử dụng phụ tùng thành công",
                    data = result
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
			}
			catch (Exception ex)
			{
                return StatusCode(500, new
				{
                    success = false,
                    message = "Lỗi hệ thống: " + ex.Message
				});
			}
		}

        /// <summary>
        /// Lấy báo cáo doanh thu của chi nhánh
        /// </summary>
        /// <param name="centerId">ID chi nhánh</param>
        /// <param name="request">Thông tin báo cáo</param>
        /// <returns>Báo cáo doanh thu</returns>
        [HttpGet("revenue/{centerId}")]
        [Authorize(Roles = "MANAGER,ADMIN")]
        public async Task<IActionResult> GetRevenueReport(int centerId, [FromQuery] RevenueReportRequest request)
        {
            try
            {
                // Validate center access for MANAGER
                if (User.IsInRole("MANAGER"))
                {
                    // TODO: Implement GetUserCenterId method or use existing method from BaseController
                    // For now, we'll allow the request to proceed
                }

                // Validate request
                if (request.StartDate >= request.EndDate)
                {
                    return BadRequest(new { success = false, message = "Ngày bắt đầu phải nhỏ hơn ngày kết thúc" });
                }

                if (request.StartDate > DateTime.Now)
                {
                    return BadRequest(new { success = false, message = "Ngày bắt đầu không được lớn hơn ngày hiện tại" });
                }

                // Validate period
                var validPeriods = new[] { "daily", "weekly", "monthly", "quarterly" };
                if (!validPeriods.Contains(request.Period.ToLower()))
                {
                    return BadRequest(new { success = false, message = "Period phải là: daily, weekly, monthly, hoặc quarterly" });
                }

                // Validate groupBy
                var validGroupBy = new[] { "service", "technician", "none" };
                if (!validGroupBy.Contains(request.GroupBy.ToLower()))
                {
                    return BadRequest(new { success = false, message = "GroupBy phải là: service, technician, hoặc none" });
                }

                var result = await _revenueReportService.GetRevenueReportAsync(centerId, request);

                return Ok(new
                {
                    success = true,
                    message = "Lấy báo cáo doanh thu thành công",
                    data = result
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi hệ thống: " + ex.Message
                });
            }
        }

        /// <summary>
        /// Lấy danh sách booking hôm nay của chi nhánh
        /// </summary>
        /// <param name="centerId">ID chi nhánh</param>
        /// <returns>Danh sách booking hôm nay</returns>
        [HttpGet("bookings/today/{centerId}")]
        [Authorize(Roles = "MANAGER,ADMIN")]
        public async Task<IActionResult> GetTodayBookings(int centerId)
        {
            try
            {
                var result = await _bookingReportsService.GetTodayBookingsAsync(centerId);
                
                return Ok(new
                {
                    success = true,
                    message = "Lấy danh sách booking hôm nay thành công",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi hệ thống: " + ex.Message
                });
            }
        }

        /// <summary>
        /// Lấy danh sách booking của chi nhánh
        /// </summary>
        /// <param name="centerId">ID chi nhánh</param>
        /// <param name="pageNumber">Số trang (mặc định: 1)</param>
        /// <param name="pageSize">Kích thước trang (mặc định: 10)</param>
        /// <param name="status">Trạng thái booking (PENDING, CONFIRMED, IN_PROGRESS, COMPLETED, PAID, CANCELLED)</param>
        /// <returns>Danh sách booking</returns>
        [HttpGet("bookings/{centerId}")]
        [Authorize(Roles = "MANAGER,ADMIN")]
        public async Task<IActionResult> GetBookings(int centerId, 
            [FromQuery] int pageNumber = 1, 
            [FromQuery] int pageSize = 10, 
            [FromQuery] string? status = null)
        {
            try
            {
                // Validate pagination parameters
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var result = await _bookingReportsService.GetBookingsAsync(centerId, pageNumber, pageSize, status);
                
                return Ok(new
                {
                    success = true,
                    message = "Lấy danh sách booking thành công",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi hệ thống: " + ex.Message
                });
            }
        }

        /// <summary>
        /// Lấy hiệu suất kỹ thuật viên của chi nhánh
        /// </summary>
        /// <param name="centerId">ID chi nhánh</param>
        /// <param name="period">Khoảng thời gian (week, month, quarter, year)</param>
        /// <returns>Hiệu suất kỹ thuật viên</returns>
        [HttpGet("technicians/performance/{centerId}")]
        [Authorize(Roles = "MANAGER,ADMIN")]
        public async Task<IActionResult> GetTechnicianPerformance(int centerId, 
            [FromQuery] string period = "month")
        {
            try
            {
                // Validate period
                var validPeriods = new[] { "week", "month", "quarter", "year" };
                if (!validPeriods.Contains(period.ToLower()))
                {
                    return BadRequest(new { success = false, message = "Period phải là: week, month, quarter, hoặc year" });
                }

                var result = await _technicianReportsService.GetTechnicianPerformanceAsync(centerId, period);
                
                return Ok(new
                {
                    success = true,
                    message = "Lấy hiệu suất kỹ thuật viên thành công",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi hệ thống: " + ex.Message
                });
            }
        }

        /// <summary>
        /// Lấy lịch làm việc kỹ thuật viên của chi nhánh
        /// </summary>
        /// <param name="centerId">ID chi nhánh</param>
        /// <param name="date">Ngày cần xem lịch (yyyy-MM-dd)</param>
        /// <returns>Lịch làm việc kỹ thuật viên</returns>
        [HttpGet("technicians/schedule/{centerId}")]
        [Authorize(Roles = "MANAGER,ADMIN")]
        public async Task<IActionResult> GetTechnicianSchedule(int centerId, 
            [FromQuery] DateTime date)
        {
            try
            {
                var result = await _technicianReportsService.GetTechnicianScheduleAsync(centerId, date);
                
                return Ok(new
                {
                    success = true,
                    message = "Lấy lịch làm việc kỹ thuật viên thành công",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi hệ thống: " + ex.Message
                });
            }
        }

        /// <summary>
        /// Lấy báo cáo sử dụng kho của chi nhánh
        /// </summary>
        /// <param name="centerId">ID chi nhánh</param>
        /// <param name="period">Khoảng thời gian (week, month, quarter, year)</param>
        /// <returns>Báo cáo sử dụng kho</returns>
        [HttpGet("inventory/usage/{centerId}")]
        [Authorize(Roles = "MANAGER,ADMIN")]
        public async Task<IActionResult> GetInventoryUsage(int centerId, 
            [FromQuery] string period = "month")
        {
            try
            {
                // Validate period
                var validPeriods = new[] { "week", "month", "quarter", "year" };
                if (!validPeriods.Contains(period.ToLower()))
                {
                    return BadRequest(new { success = false, message = "Period phải là: week, month, quarter, hoặc year" });
                }

                var result = await _inventoryReportsService.GetInventoryUsageAsync(centerId, period);
                
                return Ok(new
                {
                    success = true,
                    message = "Lấy báo cáo sử dụng kho thành công",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi hệ thống: " + ex.Message
                });
            }
        }

        /// <summary>
        /// Lấy KPI tổng quan của toàn hệ thống (Dashboard Summary)
        /// </summary>
        /// <param name="fromDate">Ngày bắt đầu (nullable, mặc định: 30 ngày trước)</param>
        /// <param name="toDate">Ngày kết thúc (nullable, mặc định: hôm nay)</param>
        /// <returns>Dashboard Summary với các KPI: Tổng doanh thu, Tổng nhân viên, Tổng booking hoàn thành, Doanh thu dịch vụ, Doanh thu phụ tùng</returns>
        [HttpGet("dashboard-summary")]
        [Authorize(Roles = "ADMIN,MANAGER")]
        public async Task<IActionResult> GetDashboardSummary(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                // Validate date range nếu cả hai đều được cung cấp
                if (fromDate.HasValue && toDate.HasValue && fromDate.Value > toDate.Value)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc"
                    });
                }

                var request = new DashboardSummaryRequest
                {
                    FromDate = fromDate,
                    ToDate = toDate
                };

                var result = await _dashboardSummaryService.GetDashboardSummaryAsync(request);
                
                return Ok(new
                {
                    success = true,
                    message = "Lấy KPI tổng quan thành công",
                    data = result
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi hệ thống: " + ex.Message
                });
            }
        }
	}
}