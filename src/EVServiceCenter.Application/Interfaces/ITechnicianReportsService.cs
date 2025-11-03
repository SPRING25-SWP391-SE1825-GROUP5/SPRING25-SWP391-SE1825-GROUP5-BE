using System;
using System.Threading.Tasks;
using EVServiceCenter.Application.Models.Responses;

namespace EVServiceCenter.Application.Interfaces
{
    public interface ITechnicianReportsService
    {
        Task<TechnicianPerformanceResponse> GetTechnicianPerformanceAsync(int centerId, string period = "month");
        Task<TechnicianScheduleResponse> GetTechnicianScheduleAsync(int centerId, DateTime date);
        
        /// <summary>
        /// Lấy thống kê số lượng booking của center và mỗi technician thực hiện trong khoảng thời gian
        /// Chỉ tính booking có trạng thái PAID hoặc COMPLETED
        /// </summary>
        /// <param name="centerId">ID trung tâm</param>
        /// <param name="fromDate">Ngày bắt đầu (nullable, mặc định 30 ngày trước)</param>
        /// <param name="toDate">Ngày kết thúc (nullable, mặc định hôm nay)</param>
        /// <returns>Response chứa totalBooking và danh sách technician với số booking đã thực hiện</returns>
        Task<TechnicianBookingStatsResponse> GetTechnicianBookingStatsAsync(int centerId, DateTime? fromDate, DateTime? toDate);
    }
}
