using System.Threading.Tasks;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;

namespace EVServiceCenter.Application.Interfaces
{
    /// <summary>
    /// Service interface cho Revenue by Store - So sánh doanh thu giữa các cửa hàng
    /// </summary>
    public interface IRevenueByStoreService
    {
        /// <summary>
        /// Lấy doanh thu của tất cả cửa hàng để so sánh theo date range
        /// </summary>
        /// <param name="request">Request chứa date range (nullable)</param>
        /// <returns>RevenueByStoreResponse chứa danh sách cửa hàng với doanh thu</returns>
        Task<RevenueByStoreResponse> GetRevenueByStoreAsync(RevenueByStoreRequest? request = null);
    }
}

