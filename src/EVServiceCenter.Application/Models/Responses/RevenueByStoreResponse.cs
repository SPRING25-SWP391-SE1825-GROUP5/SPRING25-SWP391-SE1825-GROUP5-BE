using System;
using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    /// <summary>
    /// Response cho Revenue by Store API - So sánh doanh thu giữa các cửa hàng
    /// </summary>
    public class RevenueByStoreResponse
    {
        public bool Success { get; set; } = true;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<StoreRevenueData> Stores { get; set; } = new List<StoreRevenueData>();
        public decimal TotalRevenue { get; set; }
    }

    /// <summary>
    /// Dữ liệu doanh thu của một cửa hàng
    /// </summary>
    public class StoreRevenueData
    {
        /// <summary>
        /// ID cửa hàng
        /// </summary>
        public int StoreId { get; set; }

        /// <summary>
        /// Tên cửa hàng
        /// </summary>
        public string StoreName { get; set; } = string.Empty;

        /// <summary>
        /// Tổng doanh thu của cửa hàng trong khoảng thời gian
        /// </summary>
        public decimal Revenue { get; set; }

        /// <summary>
        /// Số lượng booking hoàn thành (COMPLETED hoặc PAID)
        /// </summary>
        public int CompletedBookings { get; set; }
    }
}

