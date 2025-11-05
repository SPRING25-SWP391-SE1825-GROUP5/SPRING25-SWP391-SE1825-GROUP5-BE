using System;

namespace EVServiceCenter.Application.Models.Requests
{
    /// <summary>
    /// Request model cho Revenue by Store API - So sánh doanh thu giữa các cửa hàng
    /// </summary>
    public class RevenueByStoreRequest
    {
        /// <summary>
        /// Ngày bắt đầu (nullable, mặc định: 30 ngày trước)
        /// </summary>
        public DateTime? FromDate { get; set; }

        /// <summary>
        /// Ngày kết thúc (nullable, mặc định: hôm nay)
        /// </summary>
        public DateTime? ToDate { get; set; }
    }
}

