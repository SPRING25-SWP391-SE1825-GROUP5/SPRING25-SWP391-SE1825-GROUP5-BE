using System.Threading.Tasks;
using EVServiceCenter.Application.Models.Responses;

namespace EVServiceCenter.Application.Interfaces
{
    public interface IBookingReportsService
    {
        Task<BookingTodayResponse> GetTodayBookingsAsync(int centerId);
        Task<BookingListResponse> GetBookingsAsync(int centerId, int pageNumber = 1, int pageSize = 10, string? status = null);
    }
}
