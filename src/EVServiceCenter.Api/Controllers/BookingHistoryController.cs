using Microsoft.AspNetCore.Mvc;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Responses;
using System.ComponentModel.DataAnnotations;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace EVServiceCenter.Api.Controllers
{
    [ApiController]
    [Route("api")] // unify under one controller
    [ApiExplorerSettings(GroupName = "Booking")] // show under Booking group in Swagger
    public class HistoryController : ControllerBase
    {
        private readonly IBookingHistoryService _bookingHistoryService;
        private readonly IOrderHistoryService _orderHistoryService;

        public HistoryController(IBookingHistoryService bookingHistoryService, IOrderHistoryService orderHistoryService)
        {
            _bookingHistoryService = bookingHistoryService;
            _orderHistoryService = orderHistoryService;
        }

        /// <summary>
        /// Lấy lịch sử đặt lịch của khách hàng với phân trang và lọc
        /// </summary>
        /// <param name="customerId">ID của khách hàng</param>
        /// <param name="page">Số trang (default: 1)</param>
        /// <param name="pageSize">Số lượng item per page (default: 10, max: 50)</param>
        /// <param name="status">Lọc theo trạng thái ("PENDING", "CONFIRMED", "IN_PROGRESS", "COMPLETED", "CANCELLED")</param>
        /// <param name="fromDate">Lọc từ ngày (format: YYYY-MM-DD)</param>
        /// <param name="toDate">Lọc đến ngày (format: YYYY-MM-DD)</param>
        /// <param name="sortBy">Sắp xếp theo ("bookingDate", "createdAt", "totalCost") (default: "bookingDate")</param>
        /// <param name="sortOrder">Thứ tự sắp xếp ("asc", "desc") (default: "desc")</param>
        /// <returns>Danh sách booking history với thông tin phân trang</returns>
        [HttpGet("Booking/Customer/{customerId}/booking-history")]
        public async Task<ActionResult<BookingHistoryListResponse>> GetBookingHistory(
            [FromRoute] int customerId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string sortBy = "bookingDate",
            [FromQuery] string sortOrder = "desc")
        {
            try
            {
                // Validate parameters
                if (customerId <= 0)
                {
                    return BadRequest("Customer ID must be greater than 0.");
                }

                if (page < 1)
                {
                    return BadRequest("Page must be greater than 0.");
                }

                if (pageSize < 1 || pageSize > 50)
                {
                    return BadRequest("Page size must be between 1 and 50.");
                }

                if (!string.IsNullOrEmpty(status) && !IsValidStatus(status))
                {
                    return BadRequest("Invalid status. Valid values: PENDING, CONFIRMED, IN_PROGRESS, COMPLETED, CANCELLED");
                }

                if (!string.IsNullOrEmpty(sortBy) && !IsValidSortBy(sortBy))
                {
                    return BadRequest("Invalid sortBy. Valid values: bookingDate, createdAt, totalCost");
                }

                if (!string.IsNullOrEmpty(sortOrder) && !IsValidSortOrder(sortOrder))
                {
                    return BadRequest("Invalid sortOrder. Valid values: asc, desc");
                }

                var result = await _bookingHistoryService.GetBookingHistoryAsync(
                    customerId, page, pageSize, status, fromDate, toDate, sortBy, sortOrder);

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Lấy chi tiết một booking cụ thể của khách hàng
        /// </summary>
        /// <param name="customerId">ID của khách hàng</param>
        /// <param name="bookingId">ID của booking</param>
        /// <returns>Chi tiết booking với đầy đủ thông tin</returns>
        [HttpGet("Booking/Customer/{customerId}/booking-history/{bookingId}")]
        public async Task<ActionResult<BookingHistoryResponse>> GetBookingHistoryById(
            [FromRoute] int customerId,
            [FromRoute] int bookingId)
        {
            try
            {
                // Validate parameters
                if (customerId <= 0)
                {
                    return BadRequest("Customer ID must be greater than 0.");
                }

                if (bookingId <= 0)
                {
                    return BadRequest("Booking ID must be greater than 0.");
                }

                var result = await _bookingHistoryService.GetBookingHistoryByIdAsync(customerId, bookingId);

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Lấy thống kê tổng quan về booking history của khách hàng
        /// </summary>
        /// <param name="customerId">ID của khách hàng</param>
        /// <param name="period">Khoảng thời gian ("7days", "30days", "90days", "1year", "all") (default: "all")</param>
        /// <returns>Thống kê tổng quan về booking history</returns>
        [HttpGet("Booking/Customer/{customerId}/booking-history/stats")]
        public async Task<ActionResult<BookingHistoryStatsResponse>> GetBookingHistoryStats(
            [FromRoute] int customerId,
            [FromQuery] string period = "all")
        {
            try
            {
                // Validate parameters
                if (customerId <= 0)
                {
                    return BadRequest("Customer ID must be greater than 0.");
                }

                if (!string.IsNullOrEmpty(period) && !IsValidPeriod(period))
                {
                    return BadRequest("Invalid period. Valid values: 7days, 30days, 90days, 1year, all");
                }

                var result = await _bookingHistoryService.GetBookingHistoryStatsAsync(customerId, period);

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        private bool IsValidStatus(string status)
        {
            var validStatuses = new[] { "PENDING", "CONFIRMED", "IN_PROGRESS", "COMPLETED", "CANCELLED" };
            return validStatuses.Contains(status.ToUpper());
        }

        private bool IsValidSortBy(string sortBy)
        {
            var validSortBy = new[] { "bookingDate", "createdAt", "totalCost" };
            return validSortBy.Contains(sortBy.ToLower());
        }

        private bool IsValidSortOrder(string sortOrder)
        {
            var validSortOrder = new[] { "asc", "desc" };
            return validSortOrder.Contains(sortOrder.ToLower());
        }

        private bool IsValidPeriod(string period)
        {
            var validPeriods = new[] { "7days", "30days", "90days", "1year", "all" };
            return validPeriods.Contains(period.ToLower());
        }

        // ===== Order History endpoints unified here =====
        [HttpGet("OrderHistory/Customer/{customerId}/order-history")]
        [ProducesResponseType(typeof(OrderHistoryListResponse), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetOrderHistoryForCustomer(
            int customerId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string sortBy = "orderDate",
            [FromQuery] string sortOrder = "desc")
        {
            if (page < 1 || pageSize < 1 || pageSize > 50)
            {
                return BadRequest("Page and pageSize must be positive, and pageSize cannot exceed 50.");
            }

            var validStatuses = new[] { "PENDING", "CONFIRMED", "SHIPPED", "DELIVERED", "CANCELLED", "RETURNED" };
            if (!string.IsNullOrEmpty(status) && !validStatuses.Contains(status.ToUpper()))
            {
                return BadRequest($"Invalid status. Allowed values are: {string.Join(", ", validStatuses)}");
            }

            var validSortBy = new[] { "orderDate", "createdAt", "totalAmount" };
            if (!validSortBy.Contains(sortBy.ToLower()))
            {
                return BadRequest($"Invalid sortBy parameter. Allowed values are: {string.Join(", ", validSortBy)}");
            }

            var validSortOrder = new[] { "asc", "desc" };
            if (!validSortOrder.Contains(sortOrder.ToLower()))
            {
                return BadRequest($"Invalid sortOrder parameter. Allowed values are: {string.Join(", ", validSortOrder)}");
            }

            try
            {
                var response = await _orderHistoryService.GetOrderHistoryAsync(
                    customerId, page, pageSize, status?.ToUpper(), fromDate, toDate, sortBy, sortOrder);
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("OrderHistory/Customer/{customerId}/order-history/{orderId}")]
        [ProducesResponseType(typeof(OrderHistoryResponse), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetOrderDetailsForCustomer(int customerId, int orderId)
        {
            try
            {
                var response = await _orderHistoryService.GetOrderHistoryByIdAsync(customerId, orderId);
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("OrderHistory/Customer/{customerId}/order-history/stats")]
        [ProducesResponseType(typeof(OrderHistoryStatsResponse), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetOrderHistoryStatsForCustomer(
            int customerId,
            [FromQuery] string period = "all")
        {
            var validPeriods = new[] { "7days", "30days", "90days", "1year", "all" };
            if (!validPeriods.Contains(period.ToLower()))
            {
                return BadRequest($"Invalid period. Allowed values are: {string.Join(", ", validPeriods)}");
            }

            try
            {
                var response = await _orderHistoryService.GetOrderHistoryStatsAsync(customerId, period.ToLower());
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
