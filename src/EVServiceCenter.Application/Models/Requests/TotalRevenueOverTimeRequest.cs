using System;

namespace EVServiceCenter.Application.Models.Requests
{
    /// <summary>
    /// Request cho API tổng doanh thu theo khoảng thời gian và granularity
    /// </summary>
    public class TotalRevenueOverTimeRequest
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        /// <summary>
        /// DAY | MONTH | QUARTER | YEAR (mặc định: DAY)
        /// </summary>
        public string? Granularity { get; set; }
    }
}


