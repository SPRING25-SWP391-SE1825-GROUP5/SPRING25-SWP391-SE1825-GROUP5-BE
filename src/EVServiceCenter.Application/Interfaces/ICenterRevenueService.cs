using System;
using System.Threading.Tasks;
using EVServiceCenter.Application.Models;

namespace EVServiceCenter.Application.Interfaces
{
    public interface ICenterRevenueService
    {
        /// <summary>
        /// Lấy doanh thu của tất cả center
        /// </summary>
        Task<CenterRevenueResponse> GetAllCentersRevenueAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            int page = 1,
            int pageSize = 30);

        /// <summary>
        /// Lấy doanh thu của 1 center cụ thể
        /// </summary>
        Task<CenterRevenueResponse> GetCenterRevenueAsync(
            int centerId,
            DateTime? startDate = null,
            DateTime? endDate = null);
    }
}
