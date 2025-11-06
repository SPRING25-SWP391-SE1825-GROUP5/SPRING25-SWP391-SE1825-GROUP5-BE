using System;

namespace EVServiceCenter.Application.Models.Requests
{
    /// <summary>
    /// Request thống kê số lượt booking và doanh thu theo dịch vụ
    /// </summary>
    public class ServiceBookingStatsRequest
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}


