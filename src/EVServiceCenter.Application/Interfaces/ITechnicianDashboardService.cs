using System;
using System.Threading.Tasks;
using EVServiceCenter.Application.Models.Responses;

namespace EVServiceCenter.Application.Interfaces
{
    public interface ITechnicianDashboardService
    {
        Task<TechnicianDashboardResponse> GetDashboardAsync(int technicianId);
        Task<TechnicianBookingListResponse> GetTodayBookingsAsync(int technicianId);
        Task<TechnicianBookingListResponse> GetPendingBookingsAsync(int technicianId, int pageNumber = 1, int pageSize = 10);
        Task<TechnicianBookingListResponse> GetInProgressBookingsAsync(int technicianId, int pageNumber = 1, int pageSize = 10);
        Task<TechnicianBookingListResponse> GetCompletedBookingsAsync(int technicianId, int pageNumber = 1, int pageSize = 10);
        Task<TechnicianStats> GetStatsAsync(int technicianId);
        Task<PerformanceSummary> GetPerformanceAsync(int technicianId);
    }
}

