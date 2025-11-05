using System;

namespace EVServiceCenter.Application.Models.Responses
{
    /// <summary>
    /// Response cho Dashboard Summary API - KPI tổng quan toàn hệ thống
    /// </summary>
    public class DashboardSummaryResponse
    {
        public bool Success { get; set; } = true;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public DashboardSummaryData Summary { get; set; } = new DashboardSummaryData();
    }

    /// <summary>
    /// Dữ liệu KPI tổng quan
    /// </summary>
    public class DashboardSummaryData
    {
        /// <summary>
        /// Tổng doanh thu toàn hệ thống (từ tất cả payments COMPLETED)
        /// </summary>
        public decimal TotalRevenue { get; set; }

        /// <summary>
        /// Tổng số nhân viên của toàn hệ thống (STAFF + TECHNICIAN)
        /// </summary>
        public int TotalEmployees { get; set; }

        /// <summary>
        /// Tổng số lịch hẹn hoàn thành của toàn hệ thống (status COMPLETED hoặc PAID)
        /// </summary>
        public int TotalCompletedBookings { get; set; }

        /// <summary>
        /// Doanh thu từ dịch vụ của toàn hệ thống
        /// </summary>
        public decimal ServiceRevenue { get; set; }

        /// <summary>
        /// Doanh thu từ phụ tùng của toàn hệ thống
        /// </summary>
        public decimal PartsRevenue { get; set; }
    }
}

