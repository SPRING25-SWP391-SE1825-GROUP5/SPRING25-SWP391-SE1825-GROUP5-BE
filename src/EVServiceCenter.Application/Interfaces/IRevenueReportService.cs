using System;
using System.Threading.Tasks;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;

namespace EVServiceCenter.Application.Interfaces
{
    public interface IRevenueReportService
    {
        Task<RevenueReportResponse> GetRevenueReportAsync(int centerId, RevenueReportRequest request);
        
        /// <summary>
        /// Lấy tổng doanh thu theo khoảng thời gian với các mode: day/week/month/quarter/year
        /// </summary>
        /// <param name="centerId">ID trung tâm</param>
        /// <param name="fromDate">Ngày bắt đầu (nullable, mặc định 30 ngày trước)</param>
        /// <param name="toDate">Ngày kết thúc (nullable, mặc định hôm nay)</param>
        /// <param name="granularity">Chế độ phân loại: day, week, month, quarter, year</param>
        /// <returns>Response chứa totalRevenue, granularity, và items (period, revenue)</returns>
        Task<RevenueByPeriodResponse> GetRevenueByPeriodAsync(int centerId, DateTime? fromDate, DateTime? toDate, string granularity);
        
        /// <summary>
        /// Lấy danh sách doanh thu theo service cho một center, bao gồm cả service không có doanh thu (revenue = 0)
        /// </summary>
        /// <param name="centerId">ID trung tâm</param>
        /// <param name="fromDate">Ngày bắt đầu (nullable, mặc định 30 ngày trước)</param>
        /// <param name="toDate">Ngày kết thúc (nullable, mặc định hôm nay)</param>
        /// <returns>Response chứa danh sách service với revenue (bao gồm cả service revenue = 0)</returns>
        Task<RevenueByServiceResponse> GetRevenueByServiceAsync(int centerId, DateTime? fromDate, DateTime? toDate);
    }
}
