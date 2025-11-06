using System.Threading.Tasks;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;

namespace EVServiceCenter.Application.Interfaces
{
    /// <summary>
    /// Service interface cho Timeslot Popularity - Đánh giá số lượng booking của từng timeslot
    /// </summary>
    public interface ITimeslotPopularityService
    {
        /// <summary>
        /// Lấy thống kê số lượng booking của từng timeslot (toàn hệ thống)
        /// </summary>
        /// <param name="request">Request chứa date range (nullable)</param>
        /// <returns>TimeslotPopularityResponse chứa danh sách timeslot với số lượng booking</returns>
        Task<TimeslotPopularityResponse> GetTimeslotPopularityAsync(TimeslotPopularityRequest? request = null);
    }
}

