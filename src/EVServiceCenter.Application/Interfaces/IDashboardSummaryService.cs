using System;
using System.Threading.Tasks;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;

namespace EVServiceCenter.Application.Interfaces
{
    /// <summary>
    /// Service interface cho Dashboard Summary - KPI tổng quan toàn hệ thống
    /// </summary>
    public interface IDashboardSummaryService
    {
        /// <summary>
        /// Lấy KPI tổng quan của toàn hệ thống
        /// Bao gồm: Tổng doanh thu, Tổng nhân viên, Tổng booking hoàn thành, Doanh thu dịch vụ, Doanh thu phụ tùng
        /// </summary>
        /// <param name="request">Request chứa date range (nullable)</param>
        /// <returns>DashboardSummaryResponse chứa các KPI</returns>
        Task<DashboardSummaryResponse> GetDashboardSummaryAsync(DashboardSummaryRequest? request = null);
    }
}

